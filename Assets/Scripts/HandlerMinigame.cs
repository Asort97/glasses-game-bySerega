using UnityEngine;

public class HandlerMinigame : MinigameBase
{
    [Header("Refs")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera rightGameCamera;
    [SerializeField] private Transform lensTransform;
    [SerializeField] private Transform handleTransform;
    [SerializeField] private SpriteRenderer batteryFrame;
    [SerializeField] private SpriteRenderer batteryFill;

    [Header("Handle")]
    [SerializeField] private float requiredRotationDegrees = 720f;
    [SerializeField] private float minPointerRadius = 0.2f;
    [SerializeField] private float maxPointerRadius = 2.5f;
    [SerializeField] private float handleZRotationOffset = 0f;
    [SerializeField] private bool requireGrabToRotate = true;
    [SerializeField] private bool ignoreMaxRadiusWhileDragging = true;
    [SerializeField] private bool autoResolveRefs = true;
    [SerializeField] private bool startWithoutActiveManager = true;

    [Header("Battery Fill")]
    [SerializeField] private float batteryFillTopPadding = 0.08f;
    [SerializeField] private bool hideFillWhenEmpty = true;

    private MeshCollider _lensCollider;
    private MeshFilter _lensMeshFilter;
    private Bounds _lensLocalBounds;
    private int _uAxis = 0;
    private int _vAxis = 1;
    private int _normalAxis = 2;
    private bool _invertU;
    private bool _invertV;

    private Transform _fillTransform;
    private Vector3 _fillBaseLocalScale;
    private Vector3 _fillBaseLocalPosition;
    private float _fillBaseSpriteHeight;
    private float _fillBottomLocalY;
    private float _fillFullLocalHeight;
    private bool _fillBaseCached;

    private SpriteRenderer _handleSprite;
    private Collider2D _handleCollider;

    private float _elapsed;
    private float _rotationAccum;
    private float _dragStartRotationAccum;
    private float _dragNetRotation;
    private float _dragMaxAbsNetRotation;
    private float _lastAngle;
    private float _grabAngleOffset;
    private bool _hasLastAngle;
    private bool _handlerRunning;
    private bool _dragging;

    private void Awake()
    {
        if (autoResolveRefs)
            ResolveRefs();
        EnsureLensCollider();
        CacheFillBase();
    }

    private void OnEnable()
    {
        if (autoResolveRefs)
            ResolveRefs();
        EnsureLensCollider();
        CacheFillBase();
    }

    private void Start()
    {
        if (startWithoutActiveManager && !HasActiveManager())
            StartGame();
    }

    public override void StartGame()
    {
        base.StartGame();
        _elapsed = 0f;
        _rotationAccum = 0f;
        _dragStartRotationAccum = 0f;
        _dragNetRotation = 0f;
        _dragMaxAbsNetRotation = 0f;
        _hasLastAngle = false;
        _dragging = false;
        _handlerRunning = true;

        if (autoResolveRefs)
            ResolveRefs();
        EnsureLensCollider();
        CacheFillBase();

        SetBatteryFill(0f);
    }

    public override void StopGame()
    {
        _handlerRunning = false;
        _hasLastAngle = false;
        _dragging = false;
        _dragNetRotation = 0f;
        _dragMaxAbsNetRotation = 0f;
        base.StopGame();
    }

    protected override void Update()
    {
        if (!_handlerRunning)
            return;

        _elapsed += Time.deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / surviveTime);
        if (_elapsed >= surviveTime)
        {
            _handlerRunning = false;
            RaiseLose();
            return;
        }

        UpdateHandleAndCharge();
        if (_rotationAccum >= requiredRotationDegrees)
        {
            _handlerRunning = false;
            SetBatteryFill(1f);
            RaiseWin();
        }
    }

    private void UpdateHandleAndCharge()
    {
        if (handleTransform == null)
        {
            _hasLastAngle = false;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (!requireGrabToRotate || IsPointerOverHandle())
            {
                if (TryGetPointerAngle(out float pointerAngle, out _))
                {
                    float currentAngle = handleTransform.eulerAngles.z;
                    _grabAngleOffset = Mathf.DeltaAngle(pointerAngle, currentAngle);
                    _lastAngle = currentAngle;
                    _dragStartRotationAccum = _rotationAccum;
                    _dragNetRotation = 0f;
                    _dragMaxAbsNetRotation = 0f;
                    _hasLastAngle = true;
                    _dragging = true;
                }
            }
        }

        if (!Input.GetMouseButton(0))
        {
            _dragging = false;
            _hasLastAngle = false;
            _dragNetRotation = 0f;
            _dragMaxAbsNetRotation = 0f;
            return;
        }

        if (requireGrabToRotate && !_dragging)
        {
            _hasLastAngle = false;
            return;
        }

        if (!TryGetPointerAngle(out float pointerAngleCurrent, out float radius))
        {
            _hasLastAngle = false;
            return;
        }

        if (radius < minPointerRadius)
        {
            return;
        }

        if (!ignoreMaxRadiusWhileDragging && radius > maxPointerRadius)
        {
            _hasLastAngle = false;
            return;
        }

        float angle = pointerAngleCurrent + _grabAngleOffset;
        if (_hasLastAngle)
        {
            float delta = Mathf.DeltaAngle(_lastAngle, angle);
            _dragNetRotation += delta;
            _dragMaxAbsNetRotation = Mathf.Max(_dragMaxAbsNetRotation, Mathf.Abs(_dragNetRotation));
            _rotationAccum = _dragStartRotationAccum + _dragMaxAbsNetRotation;
        }

        _lastAngle = angle;
        _hasLastAngle = true;

        handleTransform.rotation = Quaternion.Euler(0f, 0f, angle);

        float fill = Mathf.Clamp01(_rotationAccum / Mathf.Max(1f, requiredRotationDegrees));
        SetBatteryFill(fill);
    }

    private bool TryGetPointerAngle(out float angle, out float radius)
    {
        angle = 0f;
        radius = 0f;

        if (!TryGetPointerWorld(out Vector2 pointerWorld))
            return false;

        Vector2 toPointer = pointerWorld - (Vector2)handleTransform.position;
        radius = toPointer.magnitude;
        if (radius < 0.0001f)
            return false;

        angle = Mathf.Atan2(toPointer.y, toPointer.x) * Mathf.Rad2Deg + handleZRotationOffset;
        return true;
    }

    private void ResolveRefs()
    {
        if (mainCamera == null)
            mainCamera = Camera.main != null ? Camera.main : GameObject.Find("Main Camera")?.GetComponent<Camera>();

        if (rightGameCamera == null)
            rightGameCamera = GameObject.Find("RightGameCamera")?.GetComponent<Camera>();

        if (lensTransform == null)
            lensTransform = GameObject.Find("Lens2")?.transform;

        if (handleTransform == null)
            handleTransform = transform.Find("Handle");

        if (handleTransform != null)
        {
            _handleSprite = handleTransform.GetComponent<SpriteRenderer>();
            _handleCollider = handleTransform.GetComponent<Collider2D>();
        }

        if (batteryFill == null)
            batteryFill = transform.Find("Battery/Fill")?.GetComponent<SpriteRenderer>();
        if (batteryFill == null)
            batteryFill = transform.Find("Battery/fill")?.GetComponent<SpriteRenderer>();
        if (batteryFill == null)
            batteryFill = FindChildRecursive(transform, "fill")?.GetComponent<SpriteRenderer>();

        if (batteryFrame == null)
            batteryFrame = transform.Find("Battery")?.GetComponent<SpriteRenderer>();
        if (batteryFrame == null && batteryFill != null)
            batteryFrame = batteryFill.transform.parent?.GetComponent<SpriteRenderer>();
    }

    private void EnsureLensCollider()
    {
        if (lensTransform == null) return;

        _lensMeshFilter = lensTransform.GetComponent<MeshFilter>();
        if (_lensMeshFilter == null || _lensMeshFilter.sharedMesh == null) return;

        _lensLocalBounds = _lensMeshFilter.sharedMesh.bounds;
        ConfigureLensMapping(_lensMeshFilter.sharedMesh);

        _lensCollider = lensTransform.GetComponent<MeshCollider>();
        if (_lensCollider == null)
            _lensCollider = lensTransform.gameObject.AddComponent<MeshCollider>();

        _lensCollider.sharedMesh = _lensMeshFilter.sharedMesh;
        _lensCollider.convex = false;
    }

    private void CacheFillBase()
    {
        if (batteryFill == null)
            return;

        Transform currentFillTransform = batteryFill.transform;
        if (_fillBaseCached && _fillTransform == currentFillTransform)
            return;

        _fillTransform = currentFillTransform;
        _fillBaseLocalScale = _fillTransform.localScale;
        _fillBaseLocalPosition = _fillTransform.localPosition;

        float spriteHeight = 1f;
        if (batteryFill.sprite != null)
            spriteHeight = Mathf.Max(0.0001f, batteryFill.sprite.bounds.size.y);
        _fillBaseSpriteHeight = spriteHeight;

        float baseHeight = _fillBaseSpriteHeight * Mathf.Abs(_fillBaseLocalScale.y);
        _fillBottomLocalY = _fillBaseLocalPosition.y - baseHeight * 0.5f;
        _fillFullLocalHeight = baseHeight;

        if (batteryFrame != null)
        {
            Bounds frameBounds = batteryFrame.localBounds;
            float topLocalY = frameBounds.max.y - Mathf.Max(0f, batteryFillTopPadding);
            _fillFullLocalHeight = Mathf.Max(baseHeight, topLocalY - _fillBottomLocalY);
        }

        _fillBaseCached = true;
    }

    private void SetBatteryFill(float normalized)
    {
        if (batteryFill == null || _fillTransform == null)
            return;

        float t = Mathf.Clamp01(normalized);
        if (hideFillWhenEmpty)
            batteryFill.enabled = t > 0.001f;

        float height = _fillFullLocalHeight * t;
        Vector3 scale = _fillBaseLocalScale;
        scale.y = Mathf.Sign(_fillBaseLocalScale.y == 0f ? 1f : _fillBaseLocalScale.y) * height / _fillBaseSpriteHeight;
        _fillTransform.localScale = scale;

        _fillTransform.localPosition = new Vector3(
            _fillBaseLocalPosition.x,
            _fillBottomLocalY + height * 0.5f,
            _fillBaseLocalPosition.z);
    }

    private bool TryGetPointerWorld(out Vector2 worldPoint)
    {
        worldPoint = default;
        if (handleTransform == null)
            return false;

        if (mainCamera != null && rightGameCamera != null && _lensCollider != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector2 uv;
            if (_lensCollider.Raycast(ray, out RaycastHit hit, 1000f))
            {
                uv = hit.textureCoord;
            }
            else if (TryProjectMouseToLensPlane(ray, out Vector3 localPoint))
            {
                uv = GetUvFromLocalPoint(localPoint);
            }
            else
            {
                return false;
            }

            Bounds2D bounds = GetCameraBounds();
            worldPoint = new Vector2(
                Mathf.LerpUnclamped(bounds.min.x, bounds.max.x, uv.x),
                Mathf.LerpUnclamped(bounds.min.y, bounds.max.y, uv.y));
            return true;
        }

        if (rightGameCamera != null)
        {
            Vector3 screen = Input.mousePosition;
            float z = rightGameCamera.WorldToScreenPoint(handleTransform.position).z;
            Vector3 world = rightGameCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z));
            worldPoint = new Vector2(world.x, world.y);
            return true;
        }

        return false;
    }

    private bool IsPointerOverHandle()
    {
        if (!TryGetPointerWorld(out Vector2 pointerWorld))
            return false;

        if (_handleCollider != null)
            return _handleCollider.OverlapPoint(pointerWorld);

        if (_handleSprite != null)
        {
            Vector3 point = new Vector3(pointerWorld.x, pointerWorld.y, _handleSprite.bounds.center.z);
            return _handleSprite.bounds.Contains(point);
        }

        float fallbackRadius = Mathf.Max(0.1f, minPointerRadius);
        return Vector2.Distance(pointerWorld, handleTransform.position) <= fallbackRadius;
    }

    private bool HasActiveManager()
    {
        LensMinigameManager manager = GetComponentInParent<LensMinigameManager>(true);
        return manager != null && manager.enabled && manager.gameObject.activeInHierarchy;
    }

    private Bounds2D GetCameraBounds()
    {
        if (rightGameCamera == null)
            return new Bounds2D(new Vector2(16.5f, -3.5f), new Vector2(23.5f, 3.5f));

        float halfHeight = rightGameCamera.orthographicSize;
        float halfWidth = halfHeight * rightGameCamera.aspect;
        Vector3 center = rightGameCamera.transform.position;
        return new Bounds2D(
            new Vector2(center.x - halfWidth, center.y - halfHeight),
            new Vector2(center.x + halfWidth, center.y + halfHeight));
    }

    private bool TryProjectMouseToLensPlane(Ray ray, out Vector3 localPoint)
    {
        localPoint = default;
        if (_lensMeshFilter == null || lensTransform == null) return false;

        Vector3 planeNormal = lensTransform.TransformDirection(GetAxisVector(_normalAxis)).normalized;
        Vector3 planePoint = lensTransform.TransformPoint(_lensLocalBounds.center);
        Plane lensPlane = new Plane(planeNormal, planePoint);
        if (!lensPlane.Raycast(ray, out float enter)) return false;

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

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null) return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
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

    private struct Bounds2D
    {
        public Vector2 min;
        public Vector2 max;

        public Bounds2D(Vector2 min, Vector2 max)
        {
            this.min = min;
            this.max = max;
        }
    }
}
