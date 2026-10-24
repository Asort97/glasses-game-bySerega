using UnityEngine;

public sealed class CommandsMinigame : MinigameBase
{
    private enum Command { Up, Right, Down, Left, Space }

    [Header("Commands")]
    [SerializeField] private Transform arrow;
    [SerializeField] private GameObject space;
    [Min(1)] [SerializeField] private int requiredCommands = 10;
    [Tooltip("Z rotation that makes the original arrow sprite point up.")]
    [SerializeField] private float arrowUpAngle = 90f;

    private Command _currentCommand;
    private int _completedCommands;
    private int _shownFrame;
    private float _elapsed;
    private bool _commandsRunning;

    private void Reset() => surviveTime = 8f;

    public override void StartGame()
    {
        base.StartGame();
        _completedCommands = 0;
        _elapsed = 0f;
        _commandsRunning = true;
        if (arrow == null || space == null)
        {
            Debug.LogError("Assign Arrow and Space on CommandsMinigame.", this);
            Finish(false);
            return;
        }
        ShowCommand((Command)Random.Range(0, 5));
    }

    public override void StopGame()
    {
        _commandsRunning = false;
        base.StopGame();
    }

    private void OnDisable() => StopGame();

    protected override void Update()
    {
        Advance(Time.deltaTime);
        if (!_commandsRunning || Time.deltaTime <= 0f || Time.frameCount == _shownFrame)
            return;

        int pressedCount = 0;
        Command pressed = Command.Up;
        for (int i = 0; i < 5; i++)
        {
            if (!WasPressed((Command)i)) continue;
            pressed = (Command)i;
            pressedCount++;
        }
        if (pressedCount > 1)
            Finish(false);
        else if (pressedCount == 1)
            SubmitCommand(pressed);
    }

    private void Advance(float deltaTime)
    {
        if (!_commandsRunning || deltaTime <= 0f) return;
        _elapsed += deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, surviveTime));
        if (Progress <= 0f) Finish(false);
    }

    private void SubmitCommand(Command command)
    {
        if (!_commandsRunning) return;
        if (command != _currentCommand)
        {
            Finish(false);
            return;
        }
        if (++_completedCommands >= Mathf.Max(1, requiredCommands))
        {
            Finish(true);
            return;
        }

        int next = Random.Range(0, 4);
        if (next >= (int)_currentCommand) next++;
        ShowCommand((Command)next);
    }

    private void ShowCommand(Command command)
    {
        _currentCommand = command;
        _shownFrame = Time.frameCount;
        bool isSpace = command == Command.Space;
        if (!isSpace)
            arrow.localRotation = Quaternion.Euler(0f, 0f, arrowUpAngle - 90f * (int)command);
        arrow.gameObject.SetActive(!isSpace);
        space.SetActive(isSpace);
    }

    private static bool WasPressed(Command command)
    {
        switch (command)
        {
            case Command.Up: return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            case Command.Right: return Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
            case Command.Down: return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
            case Command.Left: return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
            case Command.Space: return Input.GetKeyDown(KeyCode.Space);
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
