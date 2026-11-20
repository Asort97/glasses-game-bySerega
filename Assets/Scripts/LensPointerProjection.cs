using UnityEngine;

// Maps the displayed lens back into its game camera, including drags outside its outline.
public sealed class LensPointerProjection : MonoBehaviour
{
    [SerializeField] private Camera screenCamera;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private MeshCollider lensCollider;

    // Outside the outline the cursor is projected onto the lens plane and mapped through an
    // affine UV fit over every vertex, so it keeps moving smoothly past the lens edge.
    private Vector3 _planePoint, _planeNormal;
    private int _axisA, _axisB;
    private Vector3 _fitU, _fitV;
    private bool _mapped;

    private void Awake() => CacheMapping();

    public bool TryGetPosition(float worldZ, bool requireInside, out Vector3 position)
    {
        position = default;
        if (!screenCamera || !gameCamera || !lensCollider) return false;
        Ray ray = screenCamera.ScreenPointToRay(GameInput.MousePosition);
        Vector2 uv;
        if (lensCollider.Raycast(ray, out RaycastHit hit, 1000f)) uv = hit.textureCoord;
        else
        {
            if (requireInside || (!_mapped && !CacheMapping())) return false;
            Transform lens = lensCollider.transform;
            Plane plane = new Plane(lens.TransformDirection(_planeNormal).normalized,
                lens.TransformPoint(_planePoint));
            if (!plane.Raycast(ray, out float distance)) return false;
            Vector3 local = lens.InverseTransformPoint(ray.GetPoint(distance));
            Vector3 p = new Vector3(local[_axisA], local[_axisB], 1f);
            uv = new Vector2(Vector3.Dot(_fitU, p), Vector3.Dot(_fitV, p));
        }

        Ray gameRay = gameCamera.ViewportPointToRay(new Vector3(uv.x, uv.y, 0f));
        if (!new Plane(Vector3.forward, new Vector3(0f, 0f, worldZ)).Raycast(gameRay, out float depth))
            return false;
        position = gameRay.GetPoint(depth);
        return true;
    }

    private bool CacheMapping()
    {
        if (!lensCollider || !lensCollider.sharedMesh) return false;
        Mesh mesh = lensCollider.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Vector2[] uv = mesh.uv;
        if (vertices.Length < 3 || uv.Length != vertices.Length) return false;

        Bounds bounds = mesh.bounds;
        Vector3 size = bounds.size;
        int normal = size.x <= size.y && size.x <= size.z ? 0 : size.y <= size.z ? 1 : 2;
        _axisA = normal == 0 ? 1 : 0;
        _axisB = normal == 2 ? 1 : 2;
        _planeNormal = Vector3.zero;
        _planeNormal[normal] = 1f;
        _planePoint = bounds.center;

        // Least-squares fit of u and v as a + b*x + c*y over the planar vertex coordinates.
        double sxx = 0, sxy = 0, sx = 0, syy = 0, sy = 0, n = vertices.Length;
        double sux = 0, suy = 0, su = 0, svx = 0, svy = 0, sv = 0;
        for (int i = 0; i < vertices.Length; i++)
        {
            double x = vertices[i][_axisA], y = vertices[i][_axisB];
            sxx += x * x; sxy += x * y; sx += x; syy += y * y; sy += y;
            sux += uv[i].x * x; suy += uv[i].x * y; su += uv[i].x;
            svx += uv[i].y * x; svy += uv[i].y * y; sv += uv[i].y;
        }
        if (!Solve(sxx, sxy, sx, sxy, syy, sy, sx, sy, n, sux, suy, su, out _fitU)) return false;
        if (!Solve(sxx, sxy, sx, sxy, syy, sy, sx, sy, n, svx, svy, sv, out _fitV)) return false;
        _mapped = true;
        return true;
    }

    // Cramer's rule for a symmetric 3x3 system.
    private static bool Solve(double a, double b, double c, double d, double e, double f,
        double g, double h, double k, double r0, double r1, double r2, out Vector3 result)
    {
        double det = a * (e * k - f * h) - b * (d * k - f * g) + c * (d * h - e * g);
        result = default;
        if (System.Math.Abs(det) < 1e-12) return false;
        double x = (r0 * (e * k - f * h) - b * (r1 * k - f * r2) + c * (r1 * h - e * r2)) / det;
        double y = (a * (r1 * k - f * r2) - r0 * (d * k - f * g) + c * (d * r2 - r1 * g)) / det;
        double z = (a * (e * r2 - r1 * h) - b * (d * r2 - r1 * g) + r0 * (d * h - e * g)) / det;
        result = new Vector3((float)x, (float)y, (float)z);
        return true;
    }
}
