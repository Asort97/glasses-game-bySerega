using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fruit-Ninja-like minigame for the right lens.
/// Circles launch from the bottom; the player must drag through them on Lens2.
/// </summary>
public class FruitNinjaMinigame : MinigameBase
{
    [Header("Refs")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera rightGameCamera;
    [SerializeField] private Transform lensTransform;

    [Header("Rules")]
    [SerializeField] private float spawnIntervalMin = 0.65f;
    [SerializeField] private float spawnIntervalMax = 1.05f;
    [SerializeField] private float minSliceDistance = 0.035f;
    [SerializeField] private float minSliceSpeed = 2.5f;
    [SerializeField] private float minSliceScreenDistance = 3f;
    [SerializeField] private float minSliceScreenSpeed = 320f;
    [SerializeField, Range(0f, 1f)] private float minSliceChordFraction = 0.35f;
    [SerializeField] private float sliceHitPadding = 0.12f;
    [SerializeField, Range(0f, 0.5f)] private float maxVisualCutOffsetFraction = 0.2f;
    [SerializeField] private float trailVisibleTime = 0.18f;
    [SerializeField] private float sliceSeparationVelocity = 0.45f;
    [SerializeField, Range(0f, 1f)] private float bombSpawnChance = 0.2f;
    [SerializeField, Range(0f, 1f)] private float largeBombChance = 0.3f;
    [SerializeField] private GameObject[] bombPrefabs;

    [Header("Fruit Motion")]
    [SerializeField] private float fruitZ = -4f;
    [SerializeField] private float spawnPadding = 0.65f;
    [SerializeField, Range(0.1f, 1f)] private float spawnHorizontalFraction = 0.42f;
    [SerializeField] private Vector2 radiusRange = new Vector2(0.34f, 0.52f);
    [SerializeField] private Vector2 upwardVelocityRange = new Vector2(8.2f, 10.8f);
    [SerializeField] private Vector2 sideVelocityRange = new Vector2(-1.45f, 1.45f);
    [SerializeField] private float gravity = 7.8f;
    [SerializeField] private int fruitSortingOrder = 30;
    [SerializeField] private int sliceSortingOrder = 31;

    private readonly List<FruitBody> _fruits = new List<FruitBody>();
    private readonly List<SlicePiece> _pieces = new List<SlicePiece>();

    private MeshCollider _lensCollider;
    private MeshFilter _lensMeshFilter;
    private Bounds _lensLocalBounds;
    private int _uAxis = 0;
    private int _vAxis = 1;
    private int _normalAxis = 2;
    private bool _invertU;
    private bool _invertV;

    [SerializeField] private Sprite[] fruitsSprites;
    private Sprite _circleSprite;
    private Texture2D _circleTexture;
    private Material _sliceMaterialTemplate;
    private LineRenderer _trail;
    private Material _trailMaterial;

    private bool _fruitRunning;
    private bool _hasPreviousPoint;
    private bool _hasActiveSliceStroke;
    private Vector2 _sliceStrokeStart;
    private Vector2 _previousPoint;
    private Vector2 _previousScreenPoint;
    private Vector2 _lastSliceDirection = Vector2.right;
    private float _trailAge;
    private float _elapsed;
    private float _spawnTimer;

    private sealed class FruitBody
    {
        public GameObject gameObject;
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector2 velocity;
        public float radius;
        public float angularVelocity;
        public bool enteredView;
        public Color color;
        public bool isBomb;
    }

    private sealed class SlicePiece
    {
        public GameObject gameObject;
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector2 velocity;
        public float angularVelocity;
        public float lifetime;
        public Color color;
    }

    private void Awake()
    {
        ResolveRefs();
        EnsureLensCollider();
        EnsureRuntimeAssets();
        EnsureTrail();
    }

    public override void StartGame()
    {
        StopGame();

        ResolveRefs();
        EnsureLensCollider();
        EnsureRuntimeAssets();
        EnsureTrail();

        _fruitRunning = true;
        _elapsed = 0f;
        _spawnTimer = 0.15f;
        _hasPreviousPoint = false;
        _hasActiveSliceStroke = false;
        Progress = 1f;
    }

    public override void StopGame()
    {
        _fruitRunning = false;
        _hasPreviousPoint = false;
        _hasActiveSliceStroke = false;
        HideTrail();
        ClearRuntimeObjects();
        base.StopGame();
    }

    protected override void Update()
    {
        if (!_fruitRunning) return;

        float dt = Time.deltaTime;
        _elapsed += dt;
        Progress = 1f - Mathf.Clamp01(_elapsed / surviveTime);

        UpdateSpawning(dt);
        UpdatePointer(dt);
        if (!_fruitRunning) return;

        UpdateFruits(dt);
        if (!_fruitRunning) return;

        UpdatePieces(dt);
        UpdateTrail(dt);

        if (_elapsed >= surviveTime)
            Finish(true);
    }

    private void ResolveRefs()
    {
        if (mainCamera == null)
            mainCamera = Camera.main != null ? Camera.main : GameObject.Find("Main Camera")?.GetComponent<Camera>();

        if (rightGameCamera == null)
            rightGameCamera = GameObject.Find("RightGameCamera")?.GetComponent<Camera>();

        if (lensTransform == null)
            lensTransform = GameObject.Find("Lens2")?.transform;
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

    private void EnsureRuntimeAssets()
    {
        if (_circleSprite == null)
            CreateCircleSprite();

        if (_sliceMaterialTemplate == null)
        {
            Shader shader = Shader.Find("Custom/FruitSliceClip");
            if (shader != null)
            {
                _sliceMaterialTemplate = new Material(shader)
                {
                    name = "FruitSliceClip_Runtime",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
        }
    }

    private void EnsureTrail()
    {
        if (_trail != null) return;

        var go = new GameObject("SliceTrail_Runtime");
        go.transform.SetParent(transform, false);
        go.layer = gameObject.layer;

        _trail = go.AddComponent<LineRenderer>();
        _trail.useWorldSpace = true;
        _trail.positionCount = 2;
        _trail.startWidth = 0.18f;
        _trail.endWidth = 0.05f;
        _trail.numCapVertices = 8;
        _trail.sortingOrder = sliceSortingOrder + 100;
        _trail.enabled = false;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            _trailMaterial = new Material(shader)
            {
                name = "FruitNinjaTrail_Runtime",
                hideFlags = HideFlags.HideAndDontSave
            };
            _trail.sharedMaterial = _trailMaterial;
        }
    }

    private void CreateCircleSprite()
    {
        const int size = 64;
        _circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "FruitCircle_Runtime",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.46f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius + 1.5f - distance);
                float shade = Mathf.Lerp(1.08f, 0.82f, distance / radius);
                pixels[y * size + x] = new Color(shade, shade, shade, alpha);
            }
        }

        _circleTexture.SetPixels(pixels);
        _circleTexture.Apply(false, true);
        _circleSprite = Sprite.Create(_circleTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        _circleSprite.name = "FruitCircle_Runtime";
        _circleSprite.hideFlags = HideFlags.HideAndDontSave;
    }

    private void UpdateSpawning(float dt)
    {
        _spawnTimer -= dt;
        if (_spawnTimer > 0f) return;

        SpawnFlyingObject();
        _spawnTimer = Random.Range(spawnIntervalMin, spawnIntervalMax);
    }

    private void SpawnFlyingObject()
    {
        if (_circleSprite == null || rightGameCamera == null) return;

        Bounds2D bounds = GetCameraBounds();
        float radius = 1;
        float centerX = (bounds.min.x + bounds.max.x) * 0.5f;
        float halfSpawnWidth = Mathf.Max(0.05f, (bounds.max.x - bounds.min.x - spawnPadding * 2f) * spawnHorizontalFraction * 0.5f);
        float x = Random.Range(centerX - halfSpawnWidth, centerX + halfSpawnWidth);
        float y = bounds.min.y - radius - 0.35f;

        bool spawnBomb = ShouldSpawnBomb();
        var go = new GameObject("FruitCircle");
        SpriteRenderer sr;

        if (spawnBomb)
        {
            Destroy(go);
            go = Instantiate(GetRandomBombPrefab(), new Vector3(x, y, fruitZ), Quaternion.identity, transform);
            go.name = "FruitNinjaBomb";
            SetLayerRecursive(go, gameObject.layer);
            SetSortingOrderRecursive(go, fruitSortingOrder);
            sr = GetPrimaryRenderer(go);
            radius = GetHitRadius(go, radius);
            DisablePhysicsColliders(go);
        }
        else
        {
            go.transform.SetParent(transform, true);
            go.transform.position = new Vector3(x, y, fruitZ);
            go.transform.localScale = Vector3.one * (radius * 2f);
            go.layer = gameObject.layer;

            sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetRandomFruitSprite();
            sr.sortingOrder = fruitSortingOrder;
        }

        _fruits.Add(new FruitBody
        {
            gameObject = go,
            transform = go.transform,
            renderer = sr,
            velocity = new Vector2(Random.Range(sideVelocityRange.x, sideVelocityRange.y), Random.Range(upwardVelocityRange.x, upwardVelocityRange.y)),
            radius = radius,
            angularVelocity = Random.Range(-220f, 220f),
            color = sr != null ? sr.color : Color.white,
            isBomb = spawnBomb,
        });
    }

    private void UpdatePointer(float dt)
    {
        if (!TryGetPointerWorld(out Vector2 current))
        {
            _hasPreviousPoint = false;
            _hasActiveSliceStroke = false;
            return;
        }

        Vector2 currentScreen = Input.mousePosition;
        if (!_hasPreviousPoint)
        {
            _previousPoint = current;
            _previousScreenPoint = currentScreen;
            _hasPreviousPoint = true;
            return;
        }

        Vector2 segment = current - _previousPoint;
        float distance = segment.magnitude;
        float speed = dt > 0f ? distance / dt : 0f;
        Vector2 screenSegment = currentScreen - _previousScreenPoint;
        float screenDistance = screenSegment.magnitude;
        float screenSpeed = dt > 0f ? screenDistance / dt : 0f;
        bool fastEnough = distance >= minSliceDistance
            && speed >= minSliceSpeed
            && screenDistance >= minSliceScreenDistance
            && screenSpeed >= minSliceScreenSpeed;

        if (fastEnough)
        {
            if (!_hasActiveSliceStroke)
            {
                _sliceStrokeStart = _previousPoint;
                _hasActiveSliceStroke = true;
            }

            _lastSliceDirection = segment.normalized;
            SliceAlongSegment(_previousPoint, current, _lastSliceDirection);
            SliceAlongSegment(_sliceStrokeStart, current, _lastSliceDirection);
            ShowTrail(_sliceStrokeStart, current);
        }
        else
        {
            _hasActiveSliceStroke = false;
        }

        _previousPoint = current;
        _previousScreenPoint = currentScreen;
    }

    private bool TryGetPointerWorld(out Vector2 worldPoint)
    {
        worldPoint = default;
        if (mainCamera == null || rightGameCamera == null || _lensCollider == null) return false;

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
            Mathf.Lerp(bounds.min.x, bounds.max.x, uv.x),
            Mathf.Lerp(bounds.min.y, bounds.max.y, uv.y));
        return true;
    }

    private void SliceAlongSegment(Vector2 from, Vector2 to, Vector2 sliceDir)
    {
        sliceDir = GetValidSliceDirection(sliceDir);
        for (int i = _fruits.Count - 1; i >= 0; i--)
        {
            FruitBody fruit = _fruits[i];
            if (fruit == null || fruit.transform == null) continue;

            if (!TryGetValidSliceHit(fruit, from, to, out Vector2 hitPoint)) continue;

            _fruits.RemoveAt(i);
            SliceFruit(fruit, sliceDir, hitPoint);
            if (!_fruitRunning) return;
        }
    }

    private bool TryGetValidSliceHit(FruitBody fruit, Vector2 from, Vector2 to, out Vector2 hitPoint)
    {
        hitPoint = default;
        if (fruit == null || fruit.transform == null)
            return false;

        Vector2 center = fruit.transform.position;
        Vector2 segment = to - from;
        float segmentLength = segment.magnitude;
        if (segmentLength < 0.0001f)
            return false;

        float radius = Mathf.Max(0.001f, fruit.radius + sliceHitPadding);
        float minChordLength = radius * 2f * minSliceChordFraction;

        Vector2 direction = segment / segmentLength;
        Vector2 fromToCenter = from - center;
        float b = Vector2.Dot(fromToCenter, direction);
        float c = Vector2.Dot(fromToCenter, fromToCenter) - radius * radius;
        float discriminant = b * b - c;
        if (discriminant < 0f)
            return false;

        float root = Mathf.Sqrt(discriminant);
        float enterDistance = -b - root;
        float exitDistance = -b + root;
        float clippedEnter = Mathf.Clamp(enterDistance, 0f, segmentLength);
        float clippedExit = Mathf.Clamp(exitDistance, 0f, segmentLength);
        if (clippedExit <= clippedEnter)
            return false;

        float chordLength = clippedExit - clippedEnter;
        if (chordLength < minChordLength)
            return false;

        hitPoint = from + direction * ((clippedEnter + clippedExit) * 0.5f);
        return true;
    }

    private void SliceFruit(FruitBody fruit, Vector2 sliceDir, Vector2 hitPoint)
    {
        if (fruit.isBomb)
        {
            if (fruit.gameObject != null)
                Destroy(fruit.gameObject);
            Finish(false);
            return;
        }

        Vector2 worldNormal = new Vector2(-sliceDir.y, sliceDir.x).normalized;
        Vector3 localNormal3 = fruit.transform.InverseTransformVector(new Vector3(worldNormal.x, worldNormal.y, 0f));
        Vector2 localNormal = new Vector2(localNormal3.x, localNormal3.y).normalized;
        if (localNormal.sqrMagnitude < 0.001f)
            localNormal = Vector2.up;

        Vector3 localHit3 = fruit.transform.InverseTransformPoint(new Vector3(hitPoint.x, hitPoint.y, fruitZ));
        float rawCutOffset = Vector2.Dot(new Vector2(localHit3.x, localHit3.y), localNormal);
        float maxVisualCutOffset = GetMaxVisualCutOffset(fruit, localNormal);
        float cutOffset = Mathf.Clamp(
            rawCutOffset,
            -maxVisualCutOffset,
            maxVisualCutOffset);

        CreateSlicePiece(fruit, localNormal, cutOffset, 1f, worldNormal);
        CreateSlicePiece(fruit, localNormal, cutOffset, -1f, -worldNormal);

        if (fruit.gameObject != null)
            Destroy(fruit.gameObject);
    }

    private float GetMaxVisualCutOffset(FruitBody fruit, Vector2 localNormal)
    {
        Sprite sprite = fruit.renderer != null ? fruit.renderer.sprite : _circleSprite;
        if (sprite == null)
            return 0.1f;

        Vector2 extents = sprite.bounds.extents;
        float radiusAlongNormal = Mathf.Abs(localNormal.x) * extents.x
            + Mathf.Abs(localNormal.y) * extents.y;
        return radiusAlongNormal * maxVisualCutOffsetFraction;
    }

    private void CreateSlicePiece(FruitBody fruit, Vector2 localNormal, float cutOffset, float side, Vector2 push)
    {
        var go = new GameObject(side > 0f ? "FruitHalf_A" : "FruitHalf_B");
        go.transform.SetParent(transform, true);
        go.transform.position = fruit.transform.position;
        go.transform.rotation = fruit.transform.rotation;
        go.transform.localScale = fruit.transform.localScale;
        go.layer = gameObject.layer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = fruit.renderer != null ? fruit.renderer.sprite : _circleSprite;
        sr.color = fruit.color;
        sr.sortingOrder = sliceSortingOrder;

        if (_sliceMaterialTemplate != null)
        {
            Material material = new Material(_sliceMaterialTemplate)
            {
                name = "FruitSliceClip_Instance",
                hideFlags = HideFlags.HideAndDontSave
            };
            material.SetVector("_CutNormal", new Vector4(localNormal.x, localNormal.y, 0f, 0f));
            material.SetFloat("_CutOffset", cutOffset);
            material.SetFloat("_Side", side);
            sr.sharedMaterial = material;
        }

        _pieces.Add(new SlicePiece
        {
            gameObject = go,
            transform = go.transform,
            renderer = sr,
            velocity = fruit.velocity + push * sliceSeparationVelocity,
            angularVelocity = fruit.angularVelocity + side * Random.Range(80f, 170f),
            lifetime = 3f,
            color = fruit.color
        });
    }

    private void UpdateFruits(float dt)
    {
        Bounds2D bounds = GetCameraBounds();
        for (int i = _fruits.Count - 1; i >= 0; i--)
        {
            FruitBody fruit = _fruits[i];
            if (fruit == null || fruit.transform == null)
            {
                _fruits.RemoveAt(i);
                continue;
            }

            fruit.velocity += Vector2.down * gravity * dt;
            fruit.transform.position += (Vector3)(fruit.velocity * dt);
            fruit.transform.Rotate(0f, 0f, fruit.angularVelocity * dt);

            if (fruit.transform.position.y > bounds.min.y + fruit.radius)
                fruit.enteredView = true;

            if (fruit.enteredView && fruit.velocity.y < 0f && fruit.transform.position.y < bounds.min.y - fruit.radius)
            {
                _fruits.RemoveAt(i);
                Destroy(fruit.gameObject);
                if (!fruit.isBomb)
                {
                    Finish(false);
                    return;
                }
            }
        }
    }

    private void UpdatePieces(float dt)
    {
        for (int i = _pieces.Count - 1; i >= 0; i--)
        {
            SlicePiece piece = _pieces[i];
            if (piece == null || piece.transform == null)
            {
                _pieces.RemoveAt(i);
                continue;
            }

            piece.lifetime -= dt;
            piece.velocity += Vector2.down * gravity * dt;
            piece.transform.position += (Vector3)(piece.velocity * dt);
            piece.transform.Rotate(0f, 0f, piece.angularVelocity * dt);

            Bounds2D bounds = GetCameraBounds();
            if (piece.lifetime <= 0f || piece.transform.position.y < bounds.min.y - 1.5f)
            {
                _pieces.RemoveAt(i);
                Destroy(piece.gameObject);
            }
        }
    }

    private void ShowTrail(Vector2 from, Vector2 to)
    {
        if (_trail == null) return;

        _trailAge = trailVisibleTime;
        _trail.enabled = true;
        _trail.startColor = new Color(1f, 1f, 1f, 1f);
        _trail.endColor = new Color(1f, 1f, 1f, 0.85f);
        _trail.SetPosition(0, new Vector3(from.x, from.y, fruitZ - 0.35f));
        _trail.SetPosition(1, new Vector3(to.x, to.y, fruitZ - 0.35f));
    }

    private void UpdateTrail(float dt)
    {
        if (_trail == null || !_trail.enabled) return;

        _trailAge -= dt;
        float alpha = Mathf.Clamp01(_trailAge / trailVisibleTime);
        _trail.startColor = new Color(1f, 1f, 1f, alpha);
        _trail.endColor = new Color(1f, 1f, 1f, alpha * 0.65f);

        if (_trailAge <= 0f)
            HideTrail();
    }

    private void HideTrail()
    {
        if (_trail != null)
            _trail.enabled = false;
    }

    private void Finish(bool win)
    {
        _fruitRunning = false;
        HideTrail();
        if (win) RaiseWin();
        else RaiseLose();
    }

    private void ClearRuntimeObjects()
    {
        for (int i = 0; i < _fruits.Count; i++)
            if (_fruits[i] != null && _fruits[i].gameObject != null) Destroy(_fruits[i].gameObject);
        _fruits.Clear();

        for (int i = 0; i < _pieces.Count; i++)
            if (_pieces[i] != null && _pieces[i].gameObject != null) Destroy(_pieces[i].gameObject);
        _pieces.Clear();
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

    private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 from, Vector2 to)
    {
        Vector2 segment = to - from;
        float lengthSqr = segment.sqrMagnitude;
        if (lengthSqr < 0.0001f) return from;
        float t = Mathf.Clamp01(Vector2.Dot(point - from, segment) / lengthSqr);
        return from + segment * t;
    }

    private Sprite GetRandomFruitSprite()
    {
        if (fruitsSprites != null && fruitsSprites.Length > 0)
        {
            Sprite sprite = fruitsSprites[Random.Range(0, fruitsSprites.Length)];
            if (sprite != null)
                return sprite;
        }

        return _circleSprite;
    }

    private bool ShouldSpawnBomb()
    {
        return bombSpawnChance > 0f && HasBombPrefabs() && Random.value < bombSpawnChance;
    }

    private bool HasBombPrefabs()
    {
        if (bombPrefabs == null || bombPrefabs.Length == 0)
            return false;

        for (int i = 0; i < bombPrefabs.Length; i++)
            if (bombPrefabs[i] != null)
                return true;

        return false;
    }

    private GameObject GetRandomBombPrefab()
    {
        if (bombPrefabs == null || bombPrefabs.Length == 0)
            return null;

        if (bombPrefabs.Length >= 2)
        {
            GameObject weightedPrefab = Random.value < largeBombChance ? bombPrefabs[0] : bombPrefabs[1];
            if (weightedPrefab != null)
                return weightedPrefab;
        }

        for (int attempts = 0; attempts < bombPrefabs.Length; attempts++)
        {
            GameObject prefab = bombPrefabs[Random.Range(0, bombPrefabs.Length)];
            if (prefab != null)
                return prefab;
        }

        for (int i = 0; i < bombPrefabs.Length; i++)
            if (bombPrefabs[i] != null)
                return bombPrefabs[i];

        return null;
    }

    private static SpriteRenderer GetPrimaryRenderer(GameObject target)
    {
        if (target == null)
            return null;

        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        return renderer != null ? renderer : target.GetComponentInChildren<SpriteRenderer>(true);
    }

    private static float GetHitRadius(GameObject target, float fallbackRadius)
    {
        if (target == null)
            return fallbackRadius;

        Collider2D collider = target.GetComponentInChildren<Collider2D>(true);
        if (collider != null)
            return Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.y, fallbackRadius * 0.35f);

        SpriteRenderer renderer = GetPrimaryRenderer(target);
        if (renderer != null)
            return Mathf.Max(renderer.bounds.extents.x, renderer.bounds.extents.y, fallbackRadius * 0.35f);

        return fallbackRadius;
    }

    private static void DisablePhysicsColliders(GameObject target)
    {
        if (target == null)
            return;

        Collider2D[] colliders = target.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    private static void SetLayerRecursive(GameObject target, int layer)
    {
        if (target == null)
            return;

        target.layer = layer;
        Transform targetTransform = target.transform;
        for (int i = 0; i < targetTransform.childCount; i++)
            SetLayerRecursive(targetTransform.GetChild(i).gameObject, layer);
    }

    private static void SetSortingOrderRecursive(GameObject target, int sortingOrder)
    {
        if (target == null)
            return;

        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingOrder = sortingOrder;
    }

    private static Vector2 GetValidSliceDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.0001f)
            return direction.normalized;

        return Vector2.right;
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

    private void OnDestroy()
    {
        if (_circleSprite != null) Destroy(_circleSprite);
        if (_circleTexture != null) Destroy(_circleTexture);
        if (_sliceMaterialTemplate != null) Destroy(_sliceMaterialTemplate);
        if (_trailMaterial != null) Destroy(_trailMaterial);
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
