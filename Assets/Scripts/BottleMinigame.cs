using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public sealed class BottleMinigame : MinigameBase
{
    [Header("References")]
    [SerializeField] private Transform bottle;
    [SerializeField] private SpriteRenderer bottleRenderer;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private LensPointerProjection pointer;
    [SerializeField] private BottleFillView fillView;
    [SerializeField] private BottleVictoryPresentation victoryPresentation;

    [Header("Dragging")]
    [Min(0.01f)] [SerializeField] private float followSmoothTime = 0.035f;
    [Min(0.01f)] [SerializeField] private float returnDuration = 0.2f;
    [Min(0f)] [SerializeField] private float edgePadding = 0.05f;

    [Header("Shaking")]
    [Tooltip("Overall filling speed. 0.1 fills ten times slower than 1.")]
    [Min(0f)] [SerializeField] private float fillSpeedMultiplier = 1f;
    [Min(0.01f)] [SerializeField] private float shakeDistanceToFill = 24f;
    [Min(0.01f)] [SerializeField] private float minimumStrokeDistance = 0.15f;
    [Min(0.01f)] [SerializeField] private float maximumStrokeCredit = 1.5f;
    [Min(0f)] [SerializeField] private float minimumShakeSpeed = 0.5f;
    [FormerlySerializedAs("fullShakeSpeed")]
    [Tooltip("Reference movement speed in world units per second, not a speed cap.")]
    [Min(0.01f)] [SerializeField] private float referenceShakeSpeed = 8f;

    private Vector3 _center, _grabOffset, _velocity;
    private Vector2 _strokeDirection;
    private float _strokeDistance, _strokeCredit, _fill, _elapsed;
    private bool _running, _dragging, _centerCached;
    private Tween _returnTween;

    private void Awake() => CacheCenter();

    public override void StartGame()
    {
        StopGame();
        CacheCenter();
        if (!bottle || !bottleRenderer || !gameCamera || !pointer || !fillView || !victoryPresentation)
        {
            Debug.LogError("Bottle minigame references are not assigned.", this);
            return;
        }
        base.StartGame();
        victoryPresentation.ResetPresentation();
        bottle.localPosition = _center;
        _fill = _elapsed = 0f;
        _velocity = Vector3.zero;
        fillView.SetFill(0f);
        fillView.RestartParticles();
        _running = true;
    }

    public override void StopGame()
    {
        _running = _dragging = false;
        _returnTween?.Kill();
        _returnTween = null;
        if (victoryPresentation) victoryPresentation.Cancel();
        _strokeDistance = _strokeCredit = 0f;
        if (fillView) fillView.PauseParticles();
        base.StopGame();
    }

    private void OnDisable() => StopGame();

    protected override void Update()
    {
        if (!_running) return;
        _elapsed += Time.deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, surviveTime));

        if (!_dragging && Input.GetMouseButtonDown(0) &&
            !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()) &&
            pointer.TryGetPosition(bottle.position.z, true, out Vector3 click) &&
            ContainsPoint(click))
            BeginDrag(click);

        if (_dragging)
        {
            if (!Input.GetMouseButton(0)) ReleaseBottle();
            else if (pointer.TryGetPosition(bottle.position.z, false, out Vector3 point))
                MoveBottle(point, Time.deltaTime);
        }

        if (_fill >= 1f) BeginVictory();
        else if (_elapsed >= surviveTime) { StopGame(); RaiseLose(); }
    }

    private void BeginVictory()
    {
        _running = _dragging = false;
        _returnTween?.Kill();
        _returnTween = null;
        _velocity = Vector3.zero;
        base.StopGame();
        victoryPresentation.Play(_center, returnDuration, RaiseWin);
    }

    private void BeginDrag(Vector3 point)
    {
        _returnTween?.Kill();
        _returnTween = null;
        _grabOffset = bottle.position - point;
        _velocity = Vector3.zero;
        _strokeDistance = _strokeCredit = 0f;
        _strokeDirection = Vector2.zero;
        _dragging = true;
    }

    private void MoveBottle(Vector3 point, float deltaTime)
    {
        Vector3 previous = bottle.position;
        Vector3 target = ClampToCamera(point + _grabOffset);
        bottle.position = Vector3.SmoothDamp(previous, target, ref _velocity,
            followSmoothTime, Mathf.Infinity, deltaTime);
        AccumulateShake((Vector2)(bottle.position - previous), deltaTime);
    }

    private void AccumulateShake(Vector2 movement, float deltaTime)
    {
        float distance = movement.magnitude;
        if (deltaTime <= 0f || distance < 0.0001f) return;
        Vector2 direction = movement / distance;
        if (_strokeDirection == Vector2.zero) _strokeDirection = direction;
        if (Vector2.Dot(direction, _strokeDirection) < -0.25f)
        {
            // Credit completed back-and-forth strokes, never initial pickup or the return tween.
            if (_strokeDistance >= minimumStrokeDistance)
            {
                _fill = Mathf.Clamp01(_fill + _strokeCredit * Mathf.Max(0f, fillSpeedMultiplier)
                    / Mathf.Max(0.01f, shakeDistanceToFill));
                fillView.SetFill(_fill);
            }
            _strokeDirection = direction;
            _strokeDistance = _strokeCredit = 0f;
        }
        // Weight travelled distance by actual speed, independent of the frame rate.
        float speed = distance / deltaTime;
        // No saturation at the reference speed. The square root softens very fast spikes.
        float speedFactor = Mathf.Sqrt(Mathf.Max(0f, speed - minimumShakeSpeed)
            / Mathf.Max(0.01f, referenceShakeSpeed - minimumShakeSpeed));
        _strokeCredit += Mathf.Min(distance, Mathf.Max(0f, maximumStrokeCredit - _strokeDistance))
            * speedFactor;
        _strokeDistance += distance;
    }

    private void ReleaseBottle()
    {
        _dragging = false;
        _strokeDistance = _strokeCredit = 0f;
        _velocity = Vector3.zero;
        _returnTween = bottle.DOLocalMove(_center, returnDuration).SetEase(Ease.OutCubic);
    }

    private bool ContainsPoint(Vector3 point)
    {
        Bounds bounds = bottleRenderer.bounds;
        point.z = bounds.center.z;
        return bounds.Contains(point);
    }

    private Vector3 ClampToCamera(Vector3 target)
    {
        Bounds bounds = bottleRenderer.bounds;
        Vector3 offset = bounds.center - bottle.position;
        Vector3 center = gameCamera.transform.position;
        float y = Mathf.Max(0f, gameCamera.orthographicSize - bounds.extents.y - edgePadding);
        float x = Mathf.Max(0f, gameCamera.orthographicSize * gameCamera.aspect - bounds.extents.x - edgePadding);
        target.x = Mathf.Clamp(target.x, center.x - x - offset.x, center.x + x - offset.x);
        target.y = Mathf.Clamp(target.y, center.y - y - offset.y, center.y + y - offset.y);
        target.z = bottle.position.z;
        return target;
    }

    private void CacheCenter()
    {
        if (_centerCached || !bottle) return;
        _center = bottle.localPosition;
        _centerCached = true;
    }
}
