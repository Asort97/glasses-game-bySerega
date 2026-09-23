using UnityEngine;

/// <summary>
/// Движение по диагонали (45°), отскок от любых коллайдеров кроме других призраков.
/// Требует Rigidbody2D (Dynamic, gravityScale=0) и Collider2D на объекте.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class BouncingGhost : MonoBehaviour
{
    [SerializeField] private float speed = 2f;

    [Header("PS1 Grid Movement")]
    [Min(0.01f)] [SerializeField] private float gridCellSize = 0.1f;
    [Range(0f, 20f)] [SerializeField] private float directionJitterDegrees = 6f;
    [SerializeField] private Vector2 directionJitterInterval = new Vector2(0.12f, 0.28f);

    private SpriteRenderer _sr;
    private Rigidbody2D    _rb;
    private Vector2        _dir;
    private Vector2        _unsnappedPosition;
    private float          _directionJitter;
    private float          _nextJitterTime;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();

        _rb.gravityScale = 0f;
        _rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.None;

        // Случайная начальная диагональ
        float sx = Random.value > 0.5f ? 1f : -1f;
        float sy = Random.value > 0.5f ? 1f : -1f;
        _dir = new Vector2(sx, sy).normalized;
    }

    private void Start()
    {
        ApplyIgnoreCollisions();
    }

    private void OnEnable()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _unsnappedPosition = _rb.position;
        }

        PickDirectionJitter();

        ApplyIgnoreCollisions();
    }

    private void OnDisable()
    {
        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;
    }

    private void ApplyIgnoreCollisions()
    {
        var myCol = GetComponent<Collider2D>();
        if (myCol == null) return;
        var ghosts = Object.FindObjectsByType<BouncingGhost>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var g in ghosts)
        {
            if (g == this) continue;
            var otherCol = g.GetComponent<Collider2D>();
            if (otherCol != null)
                Physics2D.IgnoreCollision(myCol, otherCol, true);
        }
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = Vector2.zero;
        _sr.flipX = _dir.x < 0f;

        _nextJitterTime -= Time.fixedDeltaTime;
        if (_nextJitterTime <= 0f)
            PickDirectionJitter();

        Vector2 movementDirection = Rotate(_dir, _directionJitter);
        _unsnappedPosition += movementDirection * speed * Time.fixedDeltaTime;

        float cellSize = Mathf.Max(0.01f, gridCellSize);
        Vector2 snappedPosition = new Vector2(
            Mathf.Round(_unsnappedPosition.x / cellSize) * cellSize,
            Mathf.Round(_unsnappedPosition.y / cellSize) * cellSize);

        _rb.MovePosition(snappedPosition);
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        // Усредняем нормаль всех контактов
        Vector2 normal = Vector2.zero;
        for (int i = 0; i < col.contactCount; i++)
            normal += col.GetContact(i).normal;
        normal.Normalize();

        // Отражаем и снепим к ближайшей 45° диагонали
        _dir = Vector2.Reflect(_dir, normal);
        _dir = new Vector2(Mathf.Sign(_dir.x), Mathf.Sign(_dir.y)).normalized;
        _unsnappedPosition = _rb.position;
        PickDirectionJitter();
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

