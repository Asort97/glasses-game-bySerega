using UnityEngine;

/// <summary>
/// Мини-игра крестики-нолики (правая линза).
/// Победа — пройти все сценарии до истечения времени.
/// Поражение — неверный клик или время вышло.
/// </summary>
public class TicTacMinigame : MinigameBase
{
    private TicTacToeBoard _board;
    private float _elapsed;
    private bool _timerRunning;

    private void Awake()
    {
        _board = GetComponent<TicTacToeBoard>();
        if (_board == null)
            Debug.LogError("[TicTacMinigame] TicTacToeBoard not found on same GameObject!");
    }

    public override void StartGame()
    {
        _elapsed = 0f;
        _timerRunning = true;
        Progress = 1f;

        _board?.ResetGame();
        if (_board != null)
        {
            _board.OnCorrectClick += HandleWin;
            _board.OnWrongClick  += HandleLose;
        }
    }

    public override void StopGame()
    {
        _timerRunning = false;
        if (_board != null)
        {
            _board.OnCorrectClick -= HandleWin;
            _board.OnWrongClick  -= HandleLose;
        }
    }

    protected override void Update()
    {
        if (!_timerRunning) return;
        _elapsed += Time.deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / surviveTime);
        if (_elapsed >= surviveTime)
        {
            _timerRunning = false;
            RaiseLose(); // время вышло — проигрыш
        }
    }

    private void HandleWin()  { _timerRunning = false; RaiseWin(); }
    private void HandleLose() { _timerRunning = false; RaiseLose(); }
}
