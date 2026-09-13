using UnityEngine;

public sealed class UnpressableButtonMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform button;
    [SerializeField] private Camera screenCamera;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Transform lensTransform;
    [SerializeField] private MeshFilter lensMeshFilter;
    [SerializeField] private MeshCollider lensCollider;

    [Header("Movement")]
    [Min(0.01f)] [SerializeField] private float maxSpeed = 8f;
    [Min(0.01f)] [SerializeField] private float acceleration = 50f;
    [Min(0f)] [SerializeField] private float centerPull = 12f;
    [Min(0.01f)] [SerializeField] private float avoidRadius = 4f;
    [Min(0f)] [SerializeField] private float avoidStrength = 20f;
    [Min(0f)] [SerializeField] private float orbitStrength = 8f;
    [Min(0f)] [SerializeField] private float edgePadding = 1.7f;

    private Bounds _lensLocalBounds;
    private Vector2 _velocity;
    private float _buttonZ;
    private float _orbitDirection;
    private int _uAxis;
    private int _vAxis = 1;
    private int _normalAxis = 2;
    private bool _invertU;
    private bool _invertV;
    private bool _moving;

    private void Awake()
    {
        CacheLensMapping();
        StopMoving();
    }

    public void StartMoving()
    {
        if (button == null || gameCamera == null)
            return;

        _buttonZ = button.position.z;
        _velocity = Vector2.zero;
        _orbitDirection = Random.value < 0.5f ? -1f : 1f;
        button.position = ClampToCamera(new Vector3(
            gameCamera.transform.position.x,
            gameCamera.transform.position.y,
            _buttonZ));
        _moving = true;
    }

    public void StopMoving()
    {
        _moving = false;
        _velocity = Vector2.zero;
    }

    private void LateUpdate()
    {
        if (!_moving || !TryGetCursorWorldPosition(out Vector2 cursorPosition))
            return;

        Vector2 position = button.position;
        Vector2 center = gameCamera.transform.position;
        Vector2 fromCursor = position - cursorPosition;
        float cursorDistance = fromCursor.magnitude;

        if (cursorDistance < 0.001f)
            fromCursor = Vector2.right * _orbitDirection;
        else
            fromCursor /= cursorDistance;

        float proximity = 1f / (1f + Mathf.Pow(cursorDistance / avoidRadius, 2f));
        Vector2 tangent = new Vector2(-fromCursor.y, fromCursor.x) * _orbitDirection;
        Vector2 desiredVelocity =
            (center - position) * centerPull +
            fromCursor * (avoidStrength * proximity) +
            tangent * (orbitStrength * proximity);

        desiredVelocity = Vector2.ClampMagnitude(desiredVelocity, maxSpeed);
        _velocity = Vector2.MoveTowards(_velocity, desiredVelocity, acceleration * Time.deltaTime);

        Vector3 nextPosition = button.position + (Vector3)(_velocity * Time.deltaTime);
        nextPosition.z = _buttonZ;
        button.position = ClampToCamera(nextPosition);
    }

    private bool TryGetCursorWorldPosition(out Vector2 cursorWorldPosition)
    {
        cursorWorldPosition = default;
        if (screenCamera == null || gameCamera == null || lensTransform == null ||
            lensMeshFilter == null || lensCollider == null)
            return false;

        Ray ray = screenCamera.ScreenPointToRay(GameInput.MousePosition);
        Vector2 uv;

        if (lensCollider.Raycast(ray, out RaycastHit hit, 1000f))
        {
            uv = hit.textureCoord;
        }
        else
        {
            Vector3 planeNormal = lensTransform.TransformDirection(GetAxisVector(_normalAxis)).normalized;
            Vector3 planePoint = lensTransform.TransformPoint(_lensLocalBounds.center);
            Plane lensPlane = new Plane(planeNormal, planePoint);
            if (!lensPlane.Raycast(ray, out float distance))
                return false;

            Vector3 localPoint = lensTransform.InverseTransformPoint(ray.GetPoint(distance));
            uv = GetUvFromLocalPoint(localPoint);
        }

        Vector3 worldPoint = gameCamera.ViewportToWorldPoint(
            new Vector3(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y), gameCamera.nearClipPlane));
        cursorWorldPosition = worldPoint;
        return true;
    }

    private Vector3 ClampToCamera(Vector3 position)
    {
        GetMovementBounds(out float minX, out float maxX, out float minY, out float maxY);

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        return position;
    }

    private void GetMovementBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        float halfHeight = gameCamera.orthographicSize;
        float halfWidth = halfHeight * gameCamera.aspect;
        float horizontalPadding = Mathf.Min(edgePadding, Mathf.Max(0f, halfWidth - 0.01f));
        float verticalPadding = Mathf.Min(edgePadding, Mathf.Max(0f, halfHeight - 0.01f));
        Vector3 center = gameCamera.transform.position;

        minX = center.x - halfWidth + horizontalPadding;
        maxX = center.x + halfWidth - horizontalPadding;
        minY = center.y - halfHeight + verticalPadding;
        maxY = center.y + halfHeight - verticalPadding;
    }

    private void CacheLensMapping()
    {
        if (lensMeshFilter == null || lensMeshFilter.sharedMesh == null)
            return;

        Mesh mesh = lensMeshFilter.sharedMesh;
        _lensLocalBounds = mesh.bounds;
        Vector3 size = _lensLocalBounds.size;
        _normalAxis = GetSmallestAxis(size);

        int[] planarAxes = new int[2];
        int index = 0;
        for (int axis = 0; axis < 3; axis++)
        {
            if (axis != _normalAxis)
                planarAxes[index++] = axis;
        }

        _uAxis = planarAxes[0];
        _vAxis = planarAxes[1];

        Vector3[] vertices = mesh.vertices;
        Vector2[] uv = mesh.uv;
        if (vertices == null || uv == null || vertices.Length != uv.Length || vertices.Length < 2)
            return;

        if (Mathf.Abs(GetCovariance(vertices, uv, planarAxes[1], true)) >
            Mathf.Abs(GetCovariance(vertices, uv, planarAxes[0], true)))
        {
            _uAxis = planarAxes[1];
            _vAxis = planarAxes[0];
        }

        _invertU = GetCovariance(vertices, uv, _uAxis, true) < 0f;
        _invertV = GetCovariance(vertices, uv, _vAxis, false) < 0f;
    }

    private Vector2 GetUvFromLocalPoint(Vector3 localPoint)
    {
        float u = Mathf.InverseLerp(
            GetAxisValue(_lensLocalBounds.min, _uAxis),
            GetAxisValue(_lensLocalBounds.max, _uAxis),
            GetAxisValue(localPoint, _uAxis));
        float v = Mathf.InverseLerp(
            GetAxisValue(_lensLocalBounds.min, _vAxis),
            GetAxisValue(_lensLocalBounds.max, _vAxis),
            GetAxisValue(localPoint, _vAxis));

        return new Vector2(_invertU ? 1f - u : u, _invertV ? 1f - v : v);
    }

    private static int GetSmallestAxis(Vector3 value)
    {
        if (value.x <= value.y && value.x <= value.z)
            return 0;

        return value.y <= value.z ? 1 : 2;
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
            covariance +=
                (GetAxisValue(vertices[i], axis) - vertexMean) *
                ((useU ? uv[i].x : uv[i].y) - uvMean);
        }

        return covariance;
    }
}
