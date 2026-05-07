using UnityEngine;

public class RightLensMouseFollower : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera rightGameCamera;
    [SerializeField] private Transform lensTransform;
    [SerializeField] private Transform target;
    [SerializeField] private float edgePadding = 0.6f;

    private MeshCollider _lensCollider;
    private MeshFilter _lensMeshFilter;
    private Vector3 _desiredPosition;
    private float _targetZ;
    private bool _hasDesiredPosition;
    private Bounds _lensLocalBounds;
    private int _uAxis = 0;
    private int _vAxis = 1;
    private int _normalAxis = 2;
    private bool _invertU;
    private bool _invertV;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main != null ? Camera.main : GameObject.Find("Main Camera")?.GetComponent<Camera>();

        if (rightGameCamera == null)
            rightGameCamera = GameObject.Find("RightGameCamera")?.GetComponent<Camera>();

        if (lensTransform == null)
            lensTransform = GameObject.Find("Lens2")?.transform;

        if (target == null)
            target = GameObject.Find("Cube (1)")?.transform;

        // Фолбэк — двигать сам объект со скриптом
        if (target == null)
            target = transform;

        if (target != null)
        {
            _targetZ = target.position.z;
            _desiredPosition = target.position;
            _hasDesiredPosition = true;
        }

        EnsureLensCollider();
    }

    private void Update()
    {
        if (!IsReady()) return;

        if (TryGetMouseUv(out Vector2 uv))
        {
            _desiredPosition = GetWorldPositionFromUv(uv);
            _hasDesiredPosition = true;
        }
        if (_hasDesiredPosition)
            target.position = _desiredPosition;
    }

    private bool TryGetMouseUv(out Vector2 uv)
    {
        uv = default;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (_lensCollider.Raycast(ray, out RaycastHit hit, 1000f))
        {
            uv = hit.textureCoord;
            return true;
        }

        if (!TryProjectMouseToLensPlane(ray, out Vector3 localPoint))
            return false;

        uv = GetUvFromLocalPoint(localPoint);
        return true;
    }

    private Vector3 GetWorldPositionFromUv(Vector2 uv)
    {
        Vector3 center = rightGameCamera.transform.position;
        float halfHeight = rightGameCamera.orthographicSize;
        float halfWidth = halfHeight * rightGameCamera.aspect;

        float minX = center.x - halfWidth + edgePadding;
        float maxX = center.x + halfWidth - edgePadding;
        float minY = center.y - halfHeight + edgePadding;
        float maxY = center.y + halfHeight - edgePadding;

        float worldX = Mathf.Lerp(minX, maxX, uv.x);
        float worldY = Mathf.Lerp(minY, maxY, uv.y);

        return new Vector3(worldX, worldY, _targetZ);
    }

    private void EnsureLensCollider()
    {
        if (lensTransform == null)
            return;

        _lensMeshFilter = lensTransform.GetComponent<MeshFilter>();
        if (_lensMeshFilter == null || _lensMeshFilter.sharedMesh == null)
            return;

        _lensLocalBounds = _lensMeshFilter.sharedMesh.bounds;
        ConfigureLensMapping(_lensMeshFilter.sharedMesh);

        _lensCollider = lensTransform.GetComponent<MeshCollider>();
        if (_lensCollider == null)
            _lensCollider = lensTransform.gameObject.AddComponent<MeshCollider>();

        _lensCollider.sharedMesh = _lensMeshFilter.sharedMesh;
        _lensCollider.convex = false;
    }

    private bool TryProjectMouseToLensPlane(Ray ray, out Vector3 localPoint)
    {
        localPoint = default;

        if (_lensMeshFilter == null)
            return false;

        Vector3 planeNormal = lensTransform.TransformDirection(GetAxisVector(_normalAxis)).normalized;
        Vector3 planePoint = lensTransform.TransformPoint(_lensLocalBounds.center);
        Plane lensPlane = new Plane(planeNormal, planePoint);
        if (!lensPlane.Raycast(ray, out float enter))
            return false;

        localPoint = lensTransform.InverseTransformPoint(ray.GetPoint(enter));
        return true;
    }

    private Vector2 GetUvFromLocalPoint(Vector3 localPoint)
    {
        float minU = GetAxisValue(_lensLocalBounds.min, _uAxis);
        float maxU = GetAxisValue(_lensLocalBounds.max, _uAxis);
        float minV = GetAxisValue(_lensLocalBounds.min, _vAxis);
        float maxV = GetAxisValue(_lensLocalBounds.max, _vAxis);

        float u = Mathf.InverseLerp(minU, maxU, GetAxisValue(localPoint, _uAxis));
        float v = Mathf.InverseLerp(minV, maxV, GetAxisValue(localPoint, _vAxis));

        if (_invertU)
            u = 1f - u;

        if (_invertV)
            v = 1f - v;

        return new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
    }

    private void ConfigureLensMapping(Mesh mesh)
    {
        Vector3 size = _lensLocalBounds.size;
        _normalAxis = GetSmallestAxis(size);

        int[] planarAxes = new int[2];
        int index = 0;
        for (int axis = 0; axis < 3; axis++)
        {
            if (axis == _normalAxis)
                continue;

            planarAxes[index++] = axis;
        }

        _uAxis = planarAxes[0];
        _vAxis = planarAxes[1];
        _invertU = false;
        _invertV = false;

        Vector3[] vertices = mesh.vertices;
        Vector2[] uv = mesh.uv;
        if (vertices == null || uv == null || vertices.Length != uv.Length || vertices.Length < 2)
            return;

        float firstAxisToU = Mathf.Abs(GetCovariance(vertices, uv, planarAxes[0], true));
        float secondAxisToU = Mathf.Abs(GetCovariance(vertices, uv, planarAxes[1], true));
        if (secondAxisToU > firstAxisToU)
        {
            _uAxis = planarAxes[1];
            _vAxis = planarAxes[0];
        }

        _invertU = GetCovariance(vertices, uv, _uAxis, true) < 0f;
        _invertV = GetCovariance(vertices, uv, _vAxis, false) < 0f;
    }

    private static int GetSmallestAxis(Vector3 value)
    {
        if (value.x <= value.y && value.x <= value.z)
            return 0;

        if (value.y <= value.z)
            return 1;

        return 2;
    }

    private static float GetAxisValue(Vector3 value, int axis)
    {
        return axis switch
        {
            0 => value.x,
            1 => value.y,
            _ => value.z,
        };
    }

    private static Vector3 GetAxisVector(int axis)
    {
        return axis switch
        {
            0 => Vector3.right,
            1 => Vector3.up,
            _ => Vector3.forward,
        };
    }

    private static float GetCovariance(Vector3[] vertices, Vector2[] uv, int axis, bool useU)
    {
        float vertexMean = 0f;
        float uvMean = 0f;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertexMean += GetAxisValue(vertices[i], axis);
            uvMean += useU ? uv[i].x : uv[i].y;
        }

        vertexMean /= vertices.Length;
        uvMean /= vertices.Length;

        float covariance = 0f;
        for (int i = 0; i < vertices.Length; i++)
        {
            float vertexValue = GetAxisValue(vertices[i], axis) - vertexMean;
            float uvValue = (useU ? uv[i].x : uv[i].y) - uvMean;
            covariance += vertexValue * uvValue;
        }

        return covariance;
    }

    private bool IsReady()
    {
        return mainCamera != null
            && rightGameCamera != null
            && lensTransform != null
            && target != null
            && _lensCollider != null;
    }
}