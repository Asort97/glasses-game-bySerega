using UnityEngine;

public sealed class PursuerBody2D : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D hitbox;
    [Range(0f, 0.5f)] [SerializeField] private float accelerationVariation = 0.25f;
    [Range(0f, 0.5f)] [SerializeField] private float speedVariation = 0.15f;
    [Min(0f)] [SerializeField] private float initialSpeed = 0.5f;
    private Rigidbody2D _target;
    private Collider2D _playerCollider;
    private float _acceleration;
    private float _maxSpeed;
    private bool _chasing;
    private Transform _arena;
    private Vector2 _arenaHalfSize;
    private BoxCollider2D[] _walls;
    private bool _enteredArena;
    public event System.Action PlayerHit;

    public void Begin(Vector3 position, Rigidbody2D target, Collider2D playerCollider,
        float acceleration, float maxSpeed, Transform arena, Vector2 arenaSize, BoxCollider2D[] walls)
    {
        _target = target;
        _playerCollider = playerCollider;
        _arena = arena;
        _arenaHalfSize = arenaSize * 0.5f;
        _walls = walls;
        _enteredArena = false;
        _acceleration = acceleration * Random.Range(1f - accelerationVariation, 1f + accelerationVariation);
        _maxSpeed = maxSpeed * Random.Range(1f - speedVariation, 1f + speedVariation);
        transform.position = position;
        body.position = position;
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        body.rotation = 0f;
        body.linearVelocity = Random.insideUnitCircle * initialSpeed;
        body.angularVelocity = 0f;
        gameObject.SetActive(true);
        body.simulated = true;
        SetWallCollisions(false);
        _chasing = true;
    }

    public void Stop()
    {
        _chasing = false;
        if (!body) return;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.simulated = false;
        SetWallCollisions(true);
    }

    private void OnDisable() => Stop();

    private void FixedUpdate()
    {
        if (!_chasing || !_target) return;
        if (!_enteredArena)
        {
            Bounds bounds = hitbox.bounds;
            Vector3 min = _arena.InverseTransformPoint(bounds.min);
            Vector3 max = _arena.InverseTransformPoint(bounds.max);
            if (min.x >= -_arenaHalfSize.x && max.x <= _arenaHalfSize.x
                && min.y >= -_arenaHalfSize.y && max.y <= _arenaHalfSize.y)
            {
                _enteredArena = true;
                SetWallCollisions(true);
            }
        }
        // Acceleration bends the existing velocity instead of snapping toward the target.
        Vector2 acceleration = (_target.position - body.position).normalized * _acceleration;
        body.linearVelocity = Vector2.ClampMagnitude(body.linearVelocity
            + acceleration * Time.fixedDeltaTime, _maxSpeed);
    }

    private void SetWallCollisions(bool enabled)
    {
        if (!hitbox || _walls == null) return;
        foreach (var wall in _walls)
            if (wall) Physics2D.IgnoreCollision(hitbox, wall, !enabled);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_chasing && collision.collider == _playerCollider) PlayerHit?.Invoke();
    }
}
