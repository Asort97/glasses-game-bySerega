using System.Collections.Generic;
using UnityEngine;

public class UnouroborosMinigame : MinigameBase
{
    [Header("Refs")]
    [SerializeField] private Transform snakeSegmentTemplate;
    [SerializeField] private Transform borderTransform;

    [Header("Snake")]
    [SerializeField] private Vector3 startLocalPosition = new Vector3(0.899999976f, 1.39600003f, 0f);
    [SerializeField] private float stepInterval = 1f;
    [SerializeField] private float cellSize = 0.6f;
    [SerializeField] private float borderTouchRadius = 0.04f;
    [SerializeField] private bool startWithoutActiveManager = true;

    private readonly List<Transform> _runtimeSegments = new List<Transform>();
    private readonly HashSet<Vector2Int> _occupiedCells = new HashSet<Vector2Int>();

    private Collider2D[] _borderColliders;
    private SpriteRenderer _segmentRenderer;
    private Vector2Int _headCell;
    private Vector2Int _lastMoveDirection;
    private Vector2Int _queuedDirection;
    private bool _hasMoved;
    private float _elapsed;
    private float _stepTimer;
    private bool _unouroborosRunning;

    private void Awake()
    {
        ResolveRefs();
    }

    private void OnEnable()
    {
        ResolveRefs();
    }

    private void Start()
    {
        if (startWithoutActiveManager && !HasActiveManager())
            StartGame();
    }

    public override void StartGame()
    {
        base.StartGame();

        ResolveRefs();
        ClearRuntimeSegments();

        _elapsed = 0f;
        _stepTimer = 0f;
        _unouroborosRunning = true;
        _headCell = Vector2Int.zero;
        _lastMoveDirection = Vector2Int.zero;
        _queuedDirection = Vector2Int.zero;
        _hasMoved = false;
        _occupiedCells.Clear();
        _occupiedCells.Add(_headCell);
        Progress = 1f;

        if (snakeSegmentTemplate != null)
        {
            snakeSegmentTemplate.gameObject.SetActive(true);
            snakeSegmentTemplate.localPosition = startLocalPosition;
        }
    }

    public override void StopGame()
    {
        _unouroborosRunning = false;
        _queuedDirection = Vector2Int.zero;
        _hasMoved = false;
        ClearRuntimeSegments();

        if (snakeSegmentTemplate != null)
        {
            snakeSegmentTemplate.gameObject.SetActive(true);
            snakeSegmentTemplate.localPosition = startLocalPosition;
        }

        base.StopGame();
    }

    protected override void Update()
    {
        if (!_unouroborosRunning)
            return;

        float dt = Time.deltaTime;
        _elapsed += dt;
        Progress = 1f - Mathf.Clamp01(_elapsed / surviveTime);

        if (_elapsed >= surviveTime)
        {
            _unouroborosRunning = false;
            RaiseWin();
            return;
        }

        if (_hasMoved)
            ReadDirectionInput();

        _stepTimer += dt;
        while (_stepTimer >= stepInterval && _unouroborosRunning)
        {
            _stepTimer -= stepInterval;
            StepSnake();
        }
    }

    private void StepSnake()
    {
        if (snakeSegmentTemplate == null)
            return;

        Vector2Int direction = PickDirection();
        Vector2Int nextCell = _headCell + direction;

        if (_occupiedCells.Contains(nextCell))
        {
            _unouroborosRunning = false;
            RaiseLose();
            return;
        }

        Vector3 nextLocalPosition = startLocalPosition + new Vector3(nextCell.x * cellSize, nextCell.y * cellSize, 0f);
        if (TouchesBorder(nextLocalPosition))
        {
            _unouroborosRunning = false;
            RaiseLose();
            return;
        }

        Transform segment = Instantiate(snakeSegmentTemplate, snakeSegmentTemplate.parent);
        segment.name = $"Snake_segment_{_runtimeSegments.Count + 1}";
        segment.localPosition = nextLocalPosition;
        segment.localRotation = snakeSegmentTemplate.localRotation;
        segment.localScale = snakeSegmentTemplate.localScale;
        segment.gameObject.SetActive(true);

        _runtimeSegments.Add(segment);
        _occupiedCells.Add(nextCell);
        _headCell = nextCell;
        _lastMoveDirection = direction;
        _hasMoved = true;

        if (_queuedDirection == direction)
            _queuedDirection = Vector2Int.zero;
    }

    private void ReadDirectionInput()
    {
        Vector2Int direction = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            direction = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            direction = Vector2Int.right;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            direction = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            direction = Vector2Int.left;
        else if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            direction = Vector2Int.up;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            direction = Vector2Int.right;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            direction = Vector2Int.down;
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            direction = Vector2Int.left;

        if (direction == Vector2Int.zero)
            return;

        if (_lastMoveDirection != Vector2Int.zero && direction == -_lastMoveDirection)
            return;

        _queuedDirection = direction;
    }

    private Vector2Int PickDirection()
    {
        if (!_hasMoved)
            return Vector2Int.down;

        if (_queuedDirection != Vector2Int.zero)
            return _queuedDirection;

        return _lastMoveDirection == Vector2Int.zero ? Vector2Int.down : _lastMoveDirection;
    }

    private void ClearRuntimeSegments()
    {
        for (int i = _runtimeSegments.Count - 1; i >= 0; i--)
        {
            if (_runtimeSegments[i] != null)
                Destroy(_runtimeSegments[i].gameObject);
        }

        _runtimeSegments.Clear();
        _occupiedCells.Clear();
    }

    private void ResolveRefs()
    {
        if (snakeSegmentTemplate == null)
            snakeSegmentTemplate = transform.Find("Snake_snakeStart");
        if (snakeSegmentTemplate != null)
            _segmentRenderer = snakeSegmentTemplate.GetComponent<SpriteRenderer>();

        if (borderTransform == null)
            borderTransform = transform.Find("Border");

        if (borderTransform != null)
            _borderColliders = borderTransform.GetComponents<Collider2D>();
    }

    private bool HasActiveManager()
    {
        LensMinigameManager manager = GetComponentInParent<LensMinigameManager>(true);
        return manager != null && manager.enabled && manager.gameObject.activeInHierarchy;
    }

    private bool TouchesBorder(Vector3 segmentLocalPosition)
    {
        if (_borderColliders == null || _borderColliders.Length == 0 || snakeSegmentTemplate == null)
            return false;

        Bounds segmentBounds = GetSegmentWorldBounds(segmentLocalPosition);
        float touchRadius = Mathf.Max(0f, borderTouchRadius);

        for (int i = 0; i < _borderColliders.Length; i++)
        {
            Collider2D borderCollider = _borderColliders[i];
            if (borderCollider == null || !borderCollider.enabled)
                continue;

            Bounds borderBounds = borderCollider.bounds;
            borderBounds.Expand(touchRadius * 2f);
            if (borderBounds.Intersects(segmentBounds))
                return true;
        }

        return false;
    }

    private Bounds GetSegmentWorldBounds(Vector3 segmentLocalPosition)
    {
        Transform parent = snakeSegmentTemplate.parent != null ? snakeSegmentTemplate.parent : transform;
        Vector3 worldCenter = parent.TransformPoint(segmentLocalPosition);

        if (_segmentRenderer != null)
        {
            Bounds bounds = _segmentRenderer.bounds;
            bounds.center = worldCenter;
            return bounds;
        }

        Vector3 worldSize = parent.TransformVector(new Vector3(cellSize, cellSize, 0f));
        return new Bounds(worldCenter, new Vector3(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y), 0.01f));
    }
}
