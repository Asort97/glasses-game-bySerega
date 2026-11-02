using UnityEngine;

public sealed class SquareFollowerMinigame : MinigameBase
{
    [SerializeField] private Transform square;
    [SerializeField] private SpriteRenderer[] squareRenderers;
    [SerializeField] private Transform cursorVisual;
    [SerializeField] private LensPointerProjection pointer;
    [SerializeField] private CanvasCursor canvasCursor;
    [SerializeField] private DvdBounceMovement movement;
    [SerializeField] private Rect insideBounds = new Rect(-0.9f, -0.9f, 1.8f, 1.8f);
    [Min(0f)] [SerializeField] private float entryGraceDuration = 1f;

    private bool _playing;
    private bool _tracking;
    private bool _ownsCursor;
    private float _entryElapsed;
    private float _elapsed;

    public override void StartGame()
    {
        StopGame();
        if (!square || !cursorVisual || !pointer || !canvasCursor || !movement || !movement.IsConfigured)
        {
            Debug.LogError("Assign SquareFollower references in the inspector.", this);
            return;
        }
        base.StartGame();
        movement.ResetMotion();
        _elapsed = _entryElapsed = 0f;
        _tracking = false;
        SetSquareColor(new Color(0.4f, 0.4f, 0.4f, 1f));
        _playing = true;
        _ownsCursor = true;
        canvasCursor.SetShowOutsideRightLensWhenHidden(true);
        canvasCursor.SetCursorSpriteVisible(false);
        UpdatePointer(out _);
    }

    public override void StopGame()
    {
        _playing = false;
        if (_ownsCursor && canvasCursor)
        {
            canvasCursor.SetShowOutsideRightLensWhenHidden(false);
            canvasCursor.SetCursorSpriteVisible(true);
        }
        _ownsCursor = false;
        base.StopGame();
    }

    private void OnDisable() => StopGame();

    protected override void Update()
    {
        if (!_playing || Time.deltaTime <= 0f) return;
        Advance(Time.deltaTime);
    }

    private void Advance(float deltaTime)
    {
        if (!_playing) return;
        if (_tracking) movement.Advance(deltaTime);
        bool valid = UpdatePointer(out Vector3 position);
        EvaluatePointer(deltaTime, valid, position);
    }

    private bool UpdatePointer(out Vector3 position)
    {
        bool valid = pointer.TryGetPosition(square.position.z, true, out position);
        cursorVisual.gameObject.SetActive(valid);
        if (valid) cursorVisual.position = position;
        return valid;
    }

    private void EvaluatePointer(float deltaTime, bool valid, Vector3 position)
    {
        if (!_tracking)
        {
            _entryElapsed += deltaTime;
            if (_entryElapsed < entryGraceDuration) return;
            _tracking = true;
            SetSquareColor(Color.white);
            deltaTime = Mathf.Max(0f, _entryElapsed - entryGraceDuration);
        }
        bool inside = valid && insideBounds.Contains((Vector2)square.InverseTransformPoint(position));
        if (!inside) { Finish(false); return; }
        _elapsed += deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, surviveTime));
        if (Progress <= 0f) Finish(true);
    }

    private void SetSquareColor(Color color)
    {
        if (squareRenderers == null) return;
        foreach (SpriteRenderer renderer in squareRenderers)
            if (renderer) renderer.color = color;
    }

    private void Finish(bool won)
    {
        if (!_playing) return;
        StopGame();
        if (won) RaiseWin(); else RaiseLose();
    }
}
