using UnityEngine;

public sealed class ArenaKeyboardPlayer2D : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Transform arena;
    [SerializeField] private Vector2 halfSize = new Vector2(0.17f, 0.17f);
    [Min(0f)] [SerializeField] private float speed = 6.5f;
    private Vector2 _arenaSize;
    private bool _moving;
    public Rigidbody2D Body => body;

    public void Begin(Vector2 arenaSize)
    {
        _arenaSize = arenaSize;
        transform.localPosition = Vector3.zero;
        body.position = transform.position;
        body.simulated = true;
        _moving = true;
    }

    public void Stop()
    {
        _moving = false;
        if (!body) return;
        body.linearVelocity = Vector2.zero;
        body.simulated = false;
    }

    private void OnDisable() => Stop();

    private void FixedUpdate()
    {
        if (!_moving) return;
        Vector2 input = new Vector2(
            (Held(KeyCode.D, KeyCode.RightArrow) ? 1f : 0f) - (Held(KeyCode.A, KeyCode.LeftArrow) ? 1f : 0f),
            (Held(KeyCode.W, KeyCode.UpArrow) ? 1f : 0f) - (Held(KeyCode.S, KeyCode.DownArrow) ? 1f : 0f));
        Vector3 position = arena.InverseTransformPoint(body.position);
        position += (Vector3)(input.normalized * (speed * Time.fixedDeltaTime));
        Vector2 limit = Vector2.Max(Vector2.zero, _arenaSize * 0.5f - halfSize);
        position.x = Mathf.Clamp(position.x, -limit.x, limit.x);
        position.y = Mathf.Clamp(position.y, -limit.y, limit.y);
        body.MovePosition(arena.TransformPoint(position));
    }

    private static bool Held(KeyCode a, KeyCode b) => Input.GetKey(a) || Input.GetKey(b);
}
