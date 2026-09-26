using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class Ps1GridMovement2D : MonoBehaviour
{
    [Header("Motion")]
    [Min(0f)] [SerializeField] private float speed = 2f;
    [SerializeField] private Vector2 initialDirection = Vector2.right;

    [Header("PS1 Grid")]
    [Min(0.01f)] [SerializeField] private float gridCellSize = 0.1f;
    [Range(0f, 20f)] [SerializeField] private float directionJitterDegrees = 6f;
    [SerializeField] private Vector2 directionJitterInterval = new Vector2(0.12f, 0.28f);

    private Rigidbody2D _body;
    private Vector2 _direction;
    private Vector2 _unsnappedPosition;
    private float _directionJitter;
    private float _nextJitterTime;
    private bool _hasRuntimeDirection;
    private bool _paused;

    public Vector2 Direction => _direction;
    public float Speed => speed;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        ResyncToBody();
        PickDirectionJitter();
    }

    private void OnDisable()
    {
        StopBody();
    }

    private void FixedUpdate()
    {
        if (_paused)
        {
            StopBody();
            return;
        }

        StopBody();

        _nextJitterTime -= Time.fixedDeltaTime;
        if (_nextJitterTime <= 0f)
            PickDirectionJitter();

        Vector2 movementDirection = Rotate(_direction, _directionJitter);
        _unsnappedPosition += movementDirection * speed * Time.fixedDeltaTime;

        float cellSize = Mathf.Max(0.01f, gridCellSize);
        Vector2 snappedPosition = new Vector2(
            Mathf.Round(_unsnappedPosition.x / cellSize) * cellSize,
            Mathf.Round(_unsnappedPosition.y / cellSize) * cellSize);

        _body.MovePosition(snappedPosition);
    }

    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return;

        _direction = direction.normalized;
        _hasRuntimeDirection = true;
        PickDirectionJitter();
    }

    public void SetSpeed(float value)
    {
        speed = Mathf.Max(0f, value);
    }

    public void SetVelocity(Vector2 velocity)
    {
        speed = velocity.magnitude;
        if (speed <= Mathf.Epsilon)
            return;

        _direction = velocity / speed;
        _hasRuntimeDirection = true;
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        if (paused)
            StopBody();
        else
            ResyncToBody();
    }

    public void ResyncToBody()
    {
        EnsureInitialized();
        _unsnappedPosition = _body.position;
    }

    private void EnsureInitialized()
    {
        if (_body == null)
            _body = GetComponent<Rigidbody2D>();

        _body.interpolation = RigidbodyInterpolation2D.None;

        if (_hasRuntimeDirection)
            return;

        _direction = initialDirection.sqrMagnitude > Mathf.Epsilon
            ? initialDirection.normalized
            : Vector2.right;
    }

    private void StopBody()
    {
        if (_body != null)
            _body.linearVelocity = Vector2.zero;
    }

    private void PickDirectionJitter()
    {
        _directionJitter = Random.Range(-directionJitterDegrees, directionJitterDegrees);
        float minInterval = Mathf.Max(0.02f, Mathf.Min(directionJitterInterval.x, directionJitterInterval.y));
        float maxInterval = Mathf.Max(minInterval, Mathf.Max(directionJitterInterval.x, directionJitterInterval.y));
        _nextJitterTime = Random.Range(minInterval, maxInterval);
    }

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos);
    }
}
