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

    private SpriteRenderer _sr;
    private Rigidbody2D    _rb;
    private Vector2        _dir;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();

        _rb.gravityScale = 0f;
        _rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

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
        ApplyIgnoreCollisions();
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
        _rb.linearVelocity = _dir * speed;
        _sr.flipX = _dir.x < 0f;
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
    }
}

