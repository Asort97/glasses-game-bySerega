using UnityEngine;

public sealed class CommandsMinigame : MinigameBase
{
    [SerializeField] private CommandsConveyor conveyor;
    [Tooltip("Ordered from point0 to the highest point number.")]
    [SerializeField] private GameObject[] points;
    [Min(0f)] [SerializeField] private float wrongCommandDelay = 0.5f;

    private int _remaining;
    private float _elapsed;
    private bool _commandsRunning;
    private bool _failurePending;
    private float _failureTimeLeft;

    private void Reset() => surviveTime = 8f;

    public override void StartGame()
    {
        StopGame();
        if (!conveyor || !conveyor.IsConfigured || points == null || points.Length == 0)
        {
            Debug.LogError("Assign the conveyor and ordered point indicators on CommandsMinigame.", this);
            return;
        }
        foreach (GameObject point in points)
            if (!point)
            {
                Debug.LogError("CommandsMinigame has an unassigned point indicator.", this);
                return;
            }

        foreach (GameObject point in points) point.SetActive(true);
        _remaining = points.Length;
        _elapsed = 0f;
        conveyor.ResetRound(points.Length);
        base.StartGame();
        _commandsRunning = true;
    }

    public override void StopGame()
    {
        _commandsRunning = false;
        _failurePending = false;
        _failureTimeLeft = 0f;
        base.StopGame();
    }

    private void OnDisable()
    {
        StopGame();
        if (conveyor) conveyor.ClearNotes();
    }

    protected override void Update()
    {
        if (!_commandsRunning || Time.deltaTime <= 0f) return;
        Advance(Time.deltaTime);
        if (!_commandsRunning || _failurePending) return;

        int pressedCount = 0;
        CommandsConveyor.Command pressed = CommandsConveyor.Command.Up;
        for (int i = 0; i < 5; i++)
        {
            if (!WasPressed((CommandsConveyor.Command)i)) continue;
            pressed = (CommandsConveyor.Command)i;
            pressedCount++;
        }
        if (pressedCount == 1) SubmitCommand(pressed);
    }

    private void Advance(float deltaTime)
    {
        if (!_commandsRunning || deltaTime <= 0f) return;
        if (_failurePending)
        {
            _failureTimeLeft -= deltaTime;
            if (_failureTimeLeft <= 0f) Finish(false);
            return;
        }
        _elapsed += deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, surviveTime));
        if (Progress <= 0f)
        {
            Finish(false);
            return;
        }
    }

    private void SubmitCommand(CommandsConveyor.Command command)
    {
        if (!_commandsRunning || _failurePending || !conveyor.CanAcceptInput) return;
        if (!conveyor.TryHit(command))
        {
            conveyor.ShowWrongCommand(command);
            _failurePending = true;
            _failureTimeLeft = wrongCommandDelay;
            return;
        }
        points[--_remaining].SetActive(false);
        if (_remaining == 0) Finish(true);
    }

    private static bool WasPressed(CommandsConveyor.Command command)
    {
        switch (command)
        {
            case CommandsConveyor.Command.Up: return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            case CommandsConveyor.Command.Right: return Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
            case CommandsConveyor.Command.Down: return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
            case CommandsConveyor.Command.Left: return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
            case CommandsConveyor.Command.Space: return Input.GetKeyDown(KeyCode.Space);
            default: return false;
        }
    }

    private void Finish(bool won)
    {
        if (!_commandsRunning) return;
        StopGame();
        if (won) RaiseWin();
        else RaiseLose();
    }
}
