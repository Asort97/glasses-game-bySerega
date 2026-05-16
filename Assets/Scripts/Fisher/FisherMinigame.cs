using UnityEngine;

public class FisherMinigame : MinigameBase
{
    [SerializeField] private bool startAutomaticallyWhenEnabled = true;
    [SerializeField] private bool restartAutomaticallyWithoutManager = true;
    [SerializeField] private Fisher fisher;
    [SerializeField] private Transform fishTransform;
    [SerializeField] private SpriteRenderer fishRenderer;
    [SerializeField] private float fisherMinX = -2.15f;
    [SerializeField] private float fisherMaxX = 2.15f;
    [SerializeField] private float fishLeftX = -3.05f;
    [SerializeField] private float fishRightX = 3.05f;
    [SerializeField] private float fishY = -1.75f;
    [SerializeField] private float fishSpeed = 1.25f;
    [SerializeField] private Vector2 catchSize = new Vector2(0.42f, 0.32f);

    private bool _isRunning;
    private bool _finished;
    private bool _fishCaught;
    private bool _startedAutomatically;
    private bool _hookBottomSubscribed;
    private bool _hookReturnedSubscribed;
    private float _elapsed;
    private float _fisherStartY;
    private float _fisherScaleX = 1f;
    private float _fishDirection = -1f;
    private Vector3 _fishStartScale = Vector3.one;
    private Quaternion _fishStartRotation = Quaternion.identity;

    private void OnEnable()
    {
        TryStartAutomatically();
    }

    private void Start()
    {
        TryStartAutomatically();
    }

    private void OnDisable()
    {
        if (!_startedAutomatically)
            return;

        _startedAutomatically = false;
        StopGame();
    }

    public override void StartGame()
    {
        base.StartGame();
        ResolveRefs();

        _isRunning = true;
        _finished = false;
        _fishCaught = false;
        _elapsed = 0f;
        Progress = 1f;

        if (fisher != null)
        {
            Vector3 fisherPosition = fisher.transform.localPosition;
            fisherPosition.x = Random.Range(fisherMinX, fisherMaxX);
            fisherPosition.y = _fisherStartY;
            fisher.transform.localPosition = fisherPosition;
            FaceFisherToCenter(fisherPosition.x);
            fisher.ResetHook();
            SubscribeFisherEvents();
        }

        if (fishTransform != null)
        {
            fishTransform.gameObject.SetActive(true);
            fishTransform.SetParent(transform, false);
            fishTransform.localScale = _fishStartScale;
            fishTransform.localRotation = _fishStartRotation;
            fishTransform.localPosition = new Vector3(fishRightX, fishY, fishTransform.localPosition.z);
        }

        _fishDirection = -1f;
        UpdateFishFlip();
    }

    public override void StopGame()
    {
        _isRunning = false;
        UnsubscribeFisherEvents();

        base.StopGame();
    }

    protected override void Update()
    {
        if (!_isRunning || _finished)
            return;

        _elapsed += Time.deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / surviveTime);

        if (!_fishCaught)
        {
            MoveFish(Time.deltaTime);
            TryCatchFish();

            if (_elapsed >= surviveTime)
                Finish(false);
        }
    }

    private void ResolveRefs()
    {
        if (fisher == null)
            fisher = GetComponentInChildren<Fisher>(true);

        if (fishTransform == null)
            fishTransform = transform.Find("fish");

        if (fishRenderer == null && fishTransform != null)
            fishRenderer = fishTransform.GetComponent<SpriteRenderer>();

        if (fishTransform != null && fishTransform.parent == transform)
        {
            _fishStartScale = fishTransform.localScale;
            _fishStartRotation = fishTransform.localRotation;
        }

        if (fisher != null)
        {
            _fisherStartY = fisher.transform.localPosition.y;
            _fisherScaleX = Mathf.Abs(fisher.transform.localScale.x);
        }
    }

    private void TryStartAutomatically()
    {
        if (!Application.isPlaying || !startAutomaticallyWhenEnabled || _isRunning || _finished || HasActiveManager())
            return;

        _startedAutomatically = true;
        StartGame();
    }

    private bool HasActiveManager()
    {
        LensMinigameManager manager = GetComponentInParent<LensMinigameManager>(true);
        return manager != null && manager.isActiveAndEnabled;
    }

    private void FaceFisherToCenter(float x)
    {
        if (fisher == null)
            return;

        Vector3 scale = fisher.transform.localScale;
        scale.x = x < 0f ? _fisherScaleX : -_fisherScaleX;
        fisher.transform.localScale = scale;
    }

    private void SubscribeFisherEvents()
    {
        if (fisher == null)
            return;

        if (!_hookBottomSubscribed)
        {
            fisher.HookReachedBottom += HandleHookReachedBottom;
            _hookBottomSubscribed = true;
        }

        if (!_hookReturnedSubscribed)
        {
            fisher.HookReturnedToStart += HandleHookReturnedToStart;
            _hookReturnedSubscribed = true;
        }
    }

    private void UnsubscribeFisherEvents()
    {
        if (fisher == null)
            return;

        if (_hookBottomSubscribed)
        {
            fisher.HookReachedBottom -= HandleHookReachedBottom;
            _hookBottomSubscribed = false;
        }

        if (_hookReturnedSubscribed)
        {
            fisher.HookReturnedToStart -= HandleHookReturnedToStart;
            _hookReturnedSubscribed = false;
        }
    }

    private void MoveFish(float dt)
    {
        if (fishTransform == null)
            return;

        Vector3 position = fishTransform.localPosition;
        position.x += _fishDirection * fishSpeed * dt;

        if (position.x <= fishLeftX)
        {
            position.x = fishLeftX;
            _fishDirection = 1f;
        }
        else if (position.x >= fishRightX)
        {
            position.x = fishRightX;
            _fishDirection = -1f;
        }

        position.y = fishY;
        fishTransform.localPosition = position;
        UpdateFishFlip();
    }

    private void UpdateFishFlip()
    {
        if (fishRenderer != null)
            fishRenderer.flipX = _fishDirection < 0f;
    }

    private void TryCatchFish()
    {
        if (fisher == null || !fisher.IsHookDropping || fisher.HookTransform == null || fishTransform == null)
            return;

        Vector3 delta = fisher.HookTransform.position - fishTransform.position;
        if (Mathf.Abs(delta.x) > catchSize.x || Mathf.Abs(delta.y) > catchSize.y)
            return;

        _fishCaught = true;
        fishTransform.SetParent(fisher.HookTransform, true);
        fisher.BeginReturn();
    }

    private void HandleHookReachedBottom()
    {
        if (_fishCaught)
            return;

        TryCatchFish();
    }

    private void HandleHookReturnedToStart()
    {
        if (_fishCaught)
            Finish(true);
    }

    private void Finish(bool win)
    {
        if (_finished)
            return;

        _finished = true;
        _isRunning = false;

        UnsubscribeFisherEvents();

        if (_startedAutomatically && restartAutomaticallyWithoutManager && !HasActiveManager())
        {
            StartGame();
            return;
        }

        if (win)
            RaiseWin();
        else
            RaiseLose();
    }
}
