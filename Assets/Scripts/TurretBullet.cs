using UnityEngine;

public sealed class TurretBullet : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D hitbox;
    public Collider2D Hitbox => hitbox;
    public bool IsConfigured => body && hitbox;
    public event System.Action PlayerHit;
    private Collider2D _player;
    private Vector2 _direction;
    private float _speed;
    private bool _flying;

    public void Launch(Vector3 position, Vector2 direction, float speed, Collider2D player)
    {
        _player = player;
        _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        _speed = speed;
        transform.position = position;
        body.position = position;
        gameObject.SetActive(true);
        body.simulated = true;
        _flying = true;
        ApplyVelocity();
    }

    public void Stop()
    {
        _flying = false;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.simulated = false;
    }

    private void OnDisable() { if (body) Stop(); }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_flying) return;
        if (collision.collider == _player)
        {
            PlayerHit?.Invoke();
            return;
        }
        Vector2 normal = Vector2.zero;
        for (int i = 0; i < collision.contactCount; i++) normal += collision.GetContact(i).normal;
        if (normal.sqrMagnitude > 0f && Vector2.Dot(_direction, normal) < 0f)
            _direction = Vector2.Reflect(_direction, normal.normalized).normalized;
        ApplyVelocity();
    }

    private void ApplyVelocity()
    {
        body.rotation = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        body.linearVelocity = _direction * _speed;
    }
}
