using UnityEngine;

public sealed class DvdBounceMovement : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 arenaSize = new Vector2(9f, 6f);
    [SerializeField] private Vector2 targetHalfSize = new Vector2(1.1f, 1.1f);
    [Min(0f)] [SerializeField] private float speed = 2f;
    [Tooltip("Time to accelerate from rest to full speed after motion starts.")]
    [Min(0f)] [SerializeField] private float accelerationDuration = 1f;
    [Tooltip("Lets the target travel further down before bouncing, in local units.")]
    [SerializeField] private float bottomLimitOffset = 0.2f;
    private Vector3 _origin;
    private Vector2 _phase;
    private Vector2 _direction;
    private float _moveElapsed;
    private bool _initialized;

    public bool IsConfigured => target != null;

    public void ResetMotion()
    {
        if (!_initialized) { _origin = target.localPosition; _initialized = true; }
        target.localPosition = _origin;
        _moveElapsed = 0f;
        _direction = new Vector2(Random.value < 0.5f ? -1f : 1f,
            Random.value < 0.5f ? -1f : 1f).normalized;
        // Start the asymmetric vertical range at the origin rather than at its middle.
        _phase = new Vector2(0f, -VerticalCenter());
    }

    public void Advance(float deltaTime)
    {
        // Distance covered while speed ramps linearly from zero, integrated exactly.
        float before = Travel(_moveElapsed);
        _moveElapsed += deltaTime;
        _phase += _direction * (Travel(_moveElapsed) - before);
        Vector2 limit = Vector2.Max(Vector2.zero, arenaSize * 0.5f - targetHalfSize);
        float halfY = limit.y + bottomLimitOffset * 0.5f;
        target.localPosition = _origin + new Vector3(Reflect(_phase.x, limit.x),
            VerticalCenter() + Reflect(_phase.y, halfY), 0f);
    }

    private float VerticalCenter() => -bottomLimitOffset * 0.5f;

    private float Travel(float time)
    {
        if (accelerationDuration <= 0f) return speed * time;
        if (time < accelerationDuration) return speed * time * time / (2f * accelerationDuration);
        return speed * (time - accelerationDuration * 0.5f);
    }

    private static float Reflect(float position, float limit) => limit <= 0f
        ? 0f : Mathf.PingPong(position + limit, limit * 2f) - limit;
}
