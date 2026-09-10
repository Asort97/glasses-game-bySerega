using UnityEngine;

public sealed class UnpressableButtonMinigame : MinigameBase
{
    [SerializeField] private UnpressableButtonMovement buttonMovement;

    private float _elapsed;
    private bool _isRunning;

    public override void StartGame()
    {
        base.StartGame();
        _elapsed = 0f;
        _isRunning = true;

        if (buttonMovement != null)
            buttonMovement.StartMoving();
    }

    public override void StopGame()
    {
        _isRunning = false;

        if (buttonMovement != null)
            buttonMovement.StopMoving();

        base.StopGame();
    }

    protected override void Update()
    {
        if (!_isRunning)
            return;

        _elapsed += Time.deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, surviveTime));

        if (_elapsed < surviveTime)
            return;

        _isRunning = false;
        RaiseLose();
    }
}
