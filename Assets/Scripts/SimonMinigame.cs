using UnityEngine;

public sealed class SimonMinigame : MinigameBase
{
    private enum Direction { Up, Right, Down, Left }
    private enum Phase { Stopped, InitialPause, Showing, Gap, Input }

    [Header("Visuals")]
    [SerializeField] private Transform crossHighlight;
    [Header("Sequence Timing")]
    [Min(0f)] [SerializeField] private float initialPause = 0.35f;
    [Min(0.01f)] [SerializeField] private float directionDuration = 0.5f;
    [Min(0.01f)] [SerializeField] private float gapDuration = 0.25f;
    [Min(0.01f)] [SerializeField] private float inputHighlightDuration = 0.15f;

    private readonly Direction[] _sequence = new Direction[3];
    private readonly Direction[] _choices = new Direction[4];
    private Phase _phase;
    private int _shownIndex;
    private int _inputIndex;
    private float _phaseRemaining;
    private float _inputElapsed;
    private float _highlightRemaining;

    public override void StartGame()
    {
        StopGame();
        HideHighlight();
        base.StartGame();
        if (crossHighlight == null)
        {
            Debug.LogError("Assign Cross Highlight on SimonMinigame.", this);
            Finish(false);
            return;
        }

        for (int i = 0; i < _choices.Length; i++)
            _choices[i] = (Direction)i;
        for (int i = 0; i < _sequence.Length; i++)
        {
            int pick = Random.Range(i, _choices.Length);
            Direction choice = _choices[pick];
            _choices[pick] = _choices[i];
            _choices[i] = choice;
            _sequence[i] = choice;
        }

        _shownIndex = _inputIndex = 0;
        _inputElapsed = _highlightRemaining = 0f;
        _phase = Phase.InitialPause;
        _phaseRemaining = Mathf.Max(0f, initialPause);
    }

    public override void StopGame()
    {
        _phase = Phase.Stopped;
        base.StopGame();
    }

    private void OnDisable() => StopGame();

    protected override void Update()
    {
        bool wasAcceptingInput = _phase == Phase.Input;
        Advance(Time.deltaTime);
        if (!wasAcceptingInput || _phase != Phase.Input || Time.deltaTime <= 0f)
            return;

        int count = 0;
        Direction pressed = Direction.Up;
        for (int i = 0; i < 4; i++)
        {
            if (!WasPressed((Direction)i)) continue;
            pressed = (Direction)i;
            count++;
        }
        if (count > 1)
            Finish(false);
        else if (count == 1)
            SubmitDirection(pressed);
    }

    private void Advance(float deltaTime)
    {
        if (_phase == Phase.Stopped || deltaTime <= 0f) return;
        if (_phase == Phase.Input)
        {
            _inputElapsed += deltaTime;
            Progress = 1f - Mathf.Clamp01(_inputElapsed / Mathf.Max(0.01f, surviveTime));
            _highlightRemaining -= deltaTime;
            if (_highlightRemaining <= 0f) HideHighlight();
            if (Progress <= 0f) Finish(false);
            return;
        }

        _phaseRemaining -= deltaTime;
        if (_phaseRemaining > 0f) return;
        if (_phase == Phase.Showing)
        {
            HideHighlight();
            _shownIndex++;
            _phase = Phase.Gap;
            _phaseRemaining = Mathf.Max(0.01f, gapDuration);
        }
        else if (_shownIndex < _sequence.Length)
        {
            ShowDirection(_sequence[_shownIndex]);
            _phase = Phase.Showing;
            _phaseRemaining = Mathf.Max(0.01f, directionDuration);
        }
        else
        {
            _phase = Phase.Input;
        }
    }

    private void SubmitDirection(Direction direction)
    {
        if (_phase != Phase.Input) return;
        ShowDirection(direction);
        _highlightRemaining = Mathf.Max(0.01f, inputHighlightDuration);
        if (direction != _sequence[_inputIndex])
            Finish(false);
        else if (++_inputIndex == _sequence.Length)
            Finish(true);
    }

    private static bool WasPressed(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up: return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            case Direction.Right: return Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
            case Direction.Down: return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
            case Direction.Left: return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
            default: return false;
        }
    }

    private void ShowDirection(Direction direction)
    {
        crossHighlight.localRotation = Quaternion.Euler(0f, 0f, -90f * (int)direction);
        crossHighlight.gameObject.SetActive(true);
    }

    private void HideHighlight()
    {
        if (crossHighlight != null) crossHighlight.gameObject.SetActive(false);
    }

    private void Finish(bool won)
    {
        StopGame();
        if (won) RaiseWin();
        else RaiseLose();
    }
}
