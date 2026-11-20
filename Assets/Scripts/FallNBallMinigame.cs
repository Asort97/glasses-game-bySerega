using UnityEngine;
using UnityEngine.EventSystems;

public sealed class FallNBallMinigame : MinigameBase
{
    [Header("Scene References")]
    [SerializeField] private Rigidbody2D ball;
    [SerializeField] private CircleCollider2D ballCollider;
    [SerializeField] private Transform activationLine;
    [SerializeField] private LensPointerProjection pointer;
    [SerializeField] private CanvasCursor canvasCursor;

    [Header("Bounce")]
    [Tooltip("Random rise above the ball position at the moment of the click, in world units.")]
    [SerializeField] private Vector2 bounceHeight = new Vector2(2.8f, 3.8f);
    [Tooltip("Lose when the entire ball falls below this local Y coordinate.")]
    [SerializeField] private float bottomY = -3.2f;

    [Header("Click")]
    [Tooltip("Click hitbox area relative to the ball collider. 2 doubles the area, the sprite is unchanged.")]
    [Min(0.1f)] [SerializeField] private float clickAreaMultiplier = 2f;
    [Tooltip("Clicks still bounce the ball this far above the activation line, in world units. The cursor still changes at the line.")]
    [Min(0f)] [SerializeField] private float clickGraceAboveLine = 0.3f;

    private Vector3 _spawnPosition;
    private float _elapsed;
    private bool _initialized;
    private bool _playing;
    private bool _ownsCursorColor;

    public override void StartGame()
    {
        StopGame();
        if (!ball || !ballCollider || !activationLine || !pointer)
        {
            Debug.LogError("Assign FallNBall references in the inspector.", this);
            return;
        }
        if (Physics2D.gravity.y * ball.gravityScale >= 0f)
        {
            Debug.LogError("FallNBall requires downward Rigidbody2D gravity.", this);
            return;
        }
        if (!_initialized)
        {
            _spawnPosition = ball.transform.localPosition;
            _initialized = true;
        }
        ball.transform.localPosition = _spawnPosition;
        ball.position = ball.transform.position;
        ball.linearVelocity = Vector2.zero;
        ball.angularVelocity = 0f;
        ball.simulated = true;
        _elapsed = 0f;
        _playing = true;
        _ownsCursorColor = true;
        UpdatePointer(out _);
        base.StartGame();
    }

    public override void StopGame()
    {
        _playing = false;
        if (_ownsCursorColor && canvasCursor)
            canvasCursor.SetInactiveAppearance(false);
        _ownsCursorColor = false;
        if (ball)
        {
            ball.linearVelocity = Vector2.zero;
            ball.angularVelocity = 0f;
            ball.simulated = false;
        }
        base.StopGame();
    }

    private void OnDisable() => StopGame();

    protected override void Update()
    {
        if (!_playing || Time.deltaTime <= 0f) return;
        if (HasFallen()) { Finish(false); return; }

        bool validPointer = UpdatePointer(out Vector3 point);
        if (Input.GetMouseButtonDown(0)
            && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject())
            && validPointer)
            TryBounceAt(point);

        _elapsed += Time.deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, surviveTime));
        if (Progress <= 0f) Finish(true);
    }

    private bool UpdatePointer(out Vector3 point)
    {
        bool valid = pointer.TryGetPosition(ball.transform.position.z, true, out point);
        if (canvasCursor)
            canvasCursor.SetInactiveAppearance(valid && !IsBelowActivationLine(point));
        return valid;
    }

    private bool IsBelowActivationLine(Vector3 point) => activationLine.InverseTransformPoint(point).y < 0f;

    private bool TryBounceAt(Vector3 point)
    {
        if (!_playing || HasFallen()
            || activationLine.InverseTransformPoint(point).y >= LocalGrace()
            || !IsInsideClickArea(point)) return false;

        float minHeight = Mathf.Max(0.01f, bounceHeight.x);
        float height = Random.Range(minHeight, Mathf.Max(minHeight, bounceHeight.y));
        float gravity = -Physics2D.gravity.y * ball.gravityScale;
        // Replace downward velocity rather than adding an impulse dependent on fall speed.
        ball.linearVelocity = new Vector2(0f+Random.Range(-10, 10), Mathf.Sqrt(2f * gravity * height));
        return true;
    }

    private float LocalGrace()
    {
        float scale = Mathf.Abs(activationLine.lossyScale.y);
        return scale > 0.0001f ? clickGraceAboveLine / scale : clickGraceAboveLine;
    }

    private bool IsInsideClickArea(Vector3 point)
    {
        Bounds bounds = ballCollider.bounds;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.y) * Mathf.Sqrt(Mathf.Max(0.1f, clickAreaMultiplier));
        Vector2 offset = (Vector2)(point - bounds.center);
        return offset.sqrMagnitude <= radius * radius;
    }

    private bool HasFallen()
    {
        Vector3 top = new Vector3(ballCollider.bounds.center.x, ballCollider.bounds.max.y, ball.transform.position.z);
        return transform.InverseTransformPoint(top).y < bottomY;
    }

    private void Finish(bool won)
    {
        if (!_playing) return;
        StopGame();
        if (won) RaiseWin(); else RaiseLose();
    }
}
