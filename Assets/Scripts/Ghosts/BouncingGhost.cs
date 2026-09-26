using UnityEngine;

/// <summary>
/// Движение по диагонали (45°), отскок от любых коллайдеров кроме других призраков.
/// Требует Rigidbody2D (Dynamic, gravityScale=0) и Collider2D на объекте.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Ps1GridMovement2D))]
public class BouncingGhost : MonoBehaviour
{
    private SpriteRenderer _sr;
    private Rigidbody2D    _rb;
    private Ps1GridMovement2D _movement;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        _movement = GetComponent<Ps1GridMovement2D>();

        _rb.gravityScale = 0f;
        _rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.None;

        // Случайная начальная диагональ
        float sx = Random.value > 0.5f ? 1f : -1f;
        float sy = Random.value > 0.5f ? 1f : -1f;
        SetDirection(new Vector2(sx, sy));
    }

    private void Start()
    {
        ApplyIgnoreCollisions();
    }

    private void OnEnable()
    {
        if (_movement != null)
        {
            _movement.SetPaused(false);
            _movement.ResyncToBody();
        }

        ApplyIgnoreCollisions();
    }

    private void OnDisable()
    {
        if (_movement != null)
            _movement.SetPaused(true);
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

    private void OnCollisionEnter2D(Collision2D col)
    {
        // Усредняем нормаль всех контактов
        Vector2 normal = Vector2.zero;
        for (int i = 0; i < col.contactCount; i++)
            normal += col.GetContact(i).normal;
        normal.Normalize();

        // Отражаем и снепим к ближайшей 45° диагонали
        Vector2 reflectedDirection = Vector2.Reflect(_movement.Direction, normal);
        SetDirection(new Vector2(
            Mathf.Sign(reflectedDirection.x),
            Mathf.Sign(reflectedDirection.y)));
        _movement.ResyncToBody();
    }

    private void SetDirection(Vector2 direction)
    {
        direction.Normalize();
        _movement.SetDirection(direction);
        _sr.flipX = direction.x < 0f;
    }
}

