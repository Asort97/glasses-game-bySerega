using UnityEngine;

public sealed class PlatformMinigame : MinigameBase
{
    [SerializeField] private PlatformCourseGenerator course;
    [SerializeField] private PlatformFinishWall finishWall;
    [SerializeField] private HorizontalMinigameCameraFollow cameraFollow;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Player player;
    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private Animator playerAnimator;

    private Vector3 _playerStartPosition;
    private Quaternion _playerStartRotation;
    private float _animatorSpeed;
    private float _elapsed;
    private bool _platformsRunning;
    private bool _initialized;

    private void Awake()
    {
        _playerStartPosition = player.transform.localPosition;
        _playerStartRotation = player.transform.localRotation;
        _animatorSpeed = playerAnimator.speed;
        finishWall.Reached += HandleFinish;
        _initialized = true;
    }

    public override void StartGame()
    {
        StopGame();
        base.StartGame();
        course.Generate();
        player.transform.localPosition = _playerStartPosition;
        player.transform.localRotation = _playerStartRotation;
        playerBody.position = player.transform.position;
        playerBody.rotation = player.transform.eulerAngles.z;
        playerBody.linearVelocity = Vector2.zero;
        playerBody.angularVelocity = 0f;
        playerBody.simulated = true;
        player.enabled = true;
        playerAnimator.speed = _animatorSpeed;
        playerAnimator.Rebind();
        playerAnimator.Update(0f);
        Physics2D.SyncTransforms();
        _elapsed = 0f;
        _platformsRunning = true;
        cameraFollow.BeginFollowing();
    }

    public override void StopGame()
    {
        _platformsRunning = false;
        base.StopGame();
        // The manager stops inactive games before their first Awake/StartGame.
        if (!_initialized) return;
        if (player != null) player.enabled = false;
        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
            playerBody.simulated = false;
        }
        if (playerAnimator != null) playerAnimator.speed = 0f;
        if (cameraFollow != null) cameraFollow.StopFollowing();
    }

    protected override void Update()
    {
        if (!_platformsRunning) return;
        _elapsed += Time.deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, surviveTime));
        float cameraBottom = gameCamera.transform.position.y - gameCamera.orthographicSize;
        if (Progress <= 0f || playerCollider.bounds.max.y < cameraBottom)
            Finish(false);
    }

    private void HandleFinish() => Finish(true);

    private void Finish(bool won)
    {
        if (!_platformsRunning) return;
        StopGame();
        if (won) RaiseWin();
        else RaiseLose();
    }

    private void OnDisable() => StopGame();

    private void OnDestroy()
    {
        if (finishWall != null) finishWall.Reached -= HandleFinish;
    }
}
