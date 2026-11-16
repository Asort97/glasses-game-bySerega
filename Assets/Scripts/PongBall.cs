using UnityEngine;

public sealed class PongBall : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D leftPaddle;
    [SerializeField] private Collider2D rightPaddle;
    [Range(10f, 70f)] [SerializeField] private float maxBounceAngle = 55f;

    private Vector2 _direction;
    private float _speed;
    private bool _flying;

    public Vector2 Position => body.position;
    public bool IsConfigured => body && leftPaddle && rightPaddle;

    public void ResetBall(Vector2 position)
    {
        Stop();
        body.position = position;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    public void Launch(float speed)
    {
        _speed = Mathf.Max(0.1f, speed);
        _direction = new Vector2(-1f, Random.Range(-0.35f, 0.35f)).normalized;
        _flying = true;
        body.simulated = true;
        body.linearVelocity = _direction * _speed;
    }

    public void Stop()
    {
        _flying = false;
        if (!body) return;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.simulated = false;
    }

    private void OnDisable() => Stop();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_flying || collision.contactCount == 0) return;
        Vector2 normal = collision.GetContact(0).normal;
        if (Vector2.Dot(_direction, normal) >= 0f) return;

        Collider2D paddle = collision.collider;
        if (paddle == leftPaddle || paddle == rightPaddle)
        {
            float offset = Mathf.Clamp((body.position.y - paddle.bounds.center.y)
                / Mathf.Max(0.01f, paddle.bounds.extents.y), -1f, 1f);
            float angle = offset * maxBounceAngle * Mathf.Deg2Rad;
            _direction = new Vector2((paddle == leftPaddle ? 1f : -1f) * Mathf.Cos(angle), Mathf.Sin(angle));
        }
        else _direction = Vector2.Reflect(_direction, normal).normalized;

        body.linearVelocity = _direction * _speed;
    }
}
