using DG.Tweening;
using UnityEngine;

public class GameStartMinigame : MinigameBase
{
    private enum ControlMode
    {
        KeyboardSpace,
        RightLensCursor
    }

    [SerializeField] private Transform circle;
    [SerializeField] private ControlMode controlMode = ControlMode.KeyboardSpace;
    [SerializeField] private bool useSharedStartSequence = true;
    [SerializeField] private GameStartSequenceCoordinator startSequenceCoordinator;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float nearThreshold = 0.1f;

    [Header("Right Lens Cursor")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera rightGameCamera;
    [SerializeField] private Transform lensTransform;
    [SerializeField] private float cursorEdgePadding = 0.6f;

    private bool _control = true;
    private bool _waitingForSharedSequence;
    private bool _completed;
    private Vector3 _circleStartLocalPosition;
    private bool _hasCircleStart;

    private MeshCollider _lensCollider;
    private MeshFilter _lensMeshFilter;
    private Bounds _lensLocalBounds;
    private int _uAxis = 0;
    private int _vAxis = 1;
    private int _normalAxis = 2;
    private bool _invertU;
    private bool _invertV;

    private void Awake()
    {
        CacheStartPosition();
        ResolveRightLensRefs();
        EnsureLensCollider();
    }

    private void OnEnable()
    {
        CacheStartPosition();
        if (controlMode == ControlMode.RightLensCursor)
        {
            ResolveRightLensRefs();
            EnsureLensCollider();
        }
    }

    public override void StartGame()
    {
        base.StartGame();
        _control = true;
        _waitingForSharedSequence = false;
        _completed = false;

        ResolveCoordinator();
        if (useSharedStartSequence && startSequenceCoordinator != null)
            startSequenceCoordinator.Register(this);

        if (circle != null)
        {
            circle.DOKill();
            if (_hasCircleStart)
                circle.localPosition = _circleStartLocalPosition;
        }
    }

    public override void StopGame()
    {
        if (circle != null)
            circle.DOKill();

        _control = false;
        _waitingForSharedSequence = false;
        base.StopGame();
    }

    public void CompleteFromStartSequence()
    {
        if (_completed)
            return;

        _completed = true;
        _waitingForSharedSequence = false;
        _control = false;
        RaiseWin();
    }

    protected override void Update()
    {
        if (circle == null)
        {
            base.Update();
            return;
        }

        if (_control)
        {
            if (controlMode == ControlMode.RightLensCursor)
            {
                UpdateCircleFromRightLensCursor();
            }
            else
            {
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");
                Vector3 input = new Vector3(h, v, 0f);

                if (input.sqrMagnitude > 1f) input.Normalize();

                circle.localPosition += input * moveSpeed * Time.deltaTime;
            }
        }

        Vector2 pos2 = new Vector2(circle.localPosition.x, circle.localPosition.y);
        if (pos2.sqrMagnitude <= nearThreshold * nearThreshold)
        {
            bool confirmed = controlMode == ControlMode.RightLensCursor || Input.GetKeyDown(KeyCode.Space);
            if (confirmed)
            {
                _control = false;
                _waitingForSharedSequence = useSharedStartSequence;
                circle.DOLocalMove(Vector2.zero, 0.5f).OnComplete(HandleCircleActivated);
            }
        }
    }

    private void HandleCircleActivated()
    {
        if (_completed)
            return;

        ResolveCoordinator();
        if (_waitingForSharedSequence && startSequenceCoordinator != null)
        {
            startSequenceCoordinator.NotifyActivated(this);
            return;
        }

        CompleteFromStartSequence();
    }

    private void CacheStartPosition()
    {
        if (_hasCircleStart || circle == null)
            return;

        _circleStartLocalPosition = circle.localPosition;
        _hasCircleStart = true;
    }

    private void UpdateCircleFromRightLensCursor()
    {
        if (!TryGetMouseUv(out Vector2 uv))
            return;

        Vector3 worldPosition = GetWorldPositionFromUv(uv);
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        circle.localPosition = new Vector3(localPosition.x, localPosition.y, circle.localPosition.z);
    }

    private void ResolveRightLensRefs()
    {
        if (mainCamera == null)
            mainCamera = Camera.main != null ? Camera.main : GameObject.Find("Main Camera")?.GetComponent<Camera>();

        if (rightGameCamera == null)
            rightGameCamera = GameObject.Find("RightGameCamera")?.GetComponent<Camera>();

        if (lensTransform == null)
            lensTransform = GameObject.Find("Lens2")?.transform;
    }

    private void ResolveCoordinator()
    {
        if (!useSharedStartSequence || startSequenceCoordinator != null)
            return;

        startSequenceCoordinator = GameStartSequenceCoordinator.FindOrCreate();
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

    private bool TryGetMouseUv(out Vector2 uv)
    {
        uv = default;
        if (mainCamera == null || lensTransform == null || _lensMeshFilter == null)
            return false;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (_lensCollider != null && _lensCollider.Raycast(ray, out RaycastHit hit, 1000f))
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
        Vector3 center = rightGameCamera != null ? rightGameCamera.transform.position : transform.position;
        float halfHeight = rightGameCamera != null ? rightGameCamera.orthographicSize : 3.5f;
        float halfWidth = rightGameCamera != null ? halfHeight * rightGameCamera.aspect : 3.5f;

        float minX = center.x - halfWidth + cursorEdgePadding;
        float maxX = center.x + halfWidth - cursorEdgePadding;
        float minY = center.y - halfHeight + cursorEdgePadding;
        float maxY = center.y + halfHeight - cursorEdgePadding;

        return new Vector3(
            Mathf.Lerp(minX, maxX, Mathf.Clamp01(uv.x)),
            Mathf.Lerp(minY, maxY, Mathf.Clamp01(uv.y)),
            circle.position.z);
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

        if (_invertU) u = 1f - u;
        if (_invertV) v = 1f - v;

        return new Vector2(u, v);
    }

    private void ConfigureLensMapping(Mesh mesh)
    {
        Vector3 size = _lensLocalBounds.size;
        _normalAxis = GetSmallestAxis(size);

        int[] planarAxes = new int[2];
        int index = 0;
        for (int axis = 0; axis < 3; axis++)
        {
            if (axis == _normalAxis) continue;
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
        if (value.x <= value.y && value.x <= value.z) return 0;
        if (value.y <= value.z) return 1;
        return 2;
    }

    private static float GetAxisValue(Vector3 value, int axis)
    {
        switch (axis)
        {
            case 0: return value.x;
            case 1: return value.y;
            default: return value.z;
        }
    }

    private static Vector3 GetAxisVector(int axis)
    {
        switch (axis)
        {
            case 0: return Vector3.right;
            case 1: return Vector3.up;
            default: return Vector3.forward;
        }
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
}
