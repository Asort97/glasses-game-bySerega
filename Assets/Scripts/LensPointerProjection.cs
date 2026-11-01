using UnityEngine;

// Maps the displayed lens back into its game camera, including drags outside its outline.
public sealed class LensPointerProjection : MonoBehaviour
{
    [SerializeField] private Camera screenCamera;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private MeshCollider lensCollider;

    private Vector3 _origin, _edgeA, _edgeB;
    private Vector2 _uvOrigin, _uvA, _uvB;
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
            Vector3 a = lens.TransformPoint(_origin);
            Plane plane = new Plane(a, lens.TransformPoint(_origin + _edgeA),
                lens.TransformPoint(_origin + _edgeB));
            if (!plane.Raycast(ray, out float distance)) return false;
            Vector3 p = lens.InverseTransformPoint(ray.GetPoint(distance)) - _origin;
            float aa = Vector3.Dot(_edgeA, _edgeA), ab = Vector3.Dot(_edgeA, _edgeB);
            float bb = Vector3.Dot(_edgeB, _edgeB);
            float pa = Vector3.Dot(p, _edgeA), pb = Vector3.Dot(p, _edgeB);
            float determinant = aa * bb - ab * ab;
            uv = _uvOrigin + _uvA * ((pa * bb - pb * ab) / determinant)
                + _uvB * ((pb * aa - pa * ab) / determinant);
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
        int[] triangles = mesh.triangles;
        if (uv.Length != vertices.Length) return false;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            _origin = vertices[a];
            _edgeA = vertices[b] - _origin;
            _edgeB = vertices[c] - _origin;
            if (Vector3.Cross(_edgeA, _edgeB).sqrMagnitude < 0.00000001f) continue;
            _uvOrigin = uv[a];
            _uvA = uv[b] - uv[a];
            _uvB = uv[c] - uv[a];
            _mapped = true;
            return true;
        }
        return false;
    }
}
