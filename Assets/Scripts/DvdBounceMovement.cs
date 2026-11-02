using UnityEngine;

public sealed class DvdBounceMovement : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 arenaSize = new Vector2(9f, 6f);
    [SerializeField] private Vector2 targetHalfSize = new Vector2(1.1f, 1.1f);
    [Min(0f)] [SerializeField] private float speed = 2f;
    private Vector3 _origin;
    private Vector2 _phase;
    private Vector2 _direction;
    private bool _initialized;

    public bool IsConfigured => target != null;

    public void ResetMotion()
    {
        if (!_initialized) { _origin = target.localPosition; _initialized = true; }
        target.localPosition = _origin;
        _phase = Vector2.zero;
        _direction = new Vector2(Random.value < 0.5f ? -1f : 1f,
            Random.value < 0.5f ? -1f : 1f).normalized;
    }

    public void Advance(float deltaTime)
    {
        _phase += _direction * (speed * deltaTime);
        Vector2 limit = Vector2.Max(Vector2.zero, arenaSize * 0.5f - targetHalfSize);
        target.localPosition = _origin + new Vector3(Reflect(_phase.x, limit.x), Reflect(_phase.y, limit.y), 0f);
    }

    private static float Reflect(float position, float limit) => limit <= 0f
        ? 0f : Mathf.PingPong(position + limit, limit * 2f) - limit;
}
