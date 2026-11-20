using System.Collections;
using UnityEngine;

/// <summary>
/// Мини-игра крестики-нолики (правая линза).
/// Победа — кликнуть выигрышную клетку до истечения времени.
/// Поражение — неверный клик или время вышло.
/// </summary>
public class TicTacMinigame : MinigameBase
{
    [Tooltip("Pause after the click before the win or lose result.")]
    [Min(0f)] [SerializeField] private float resultDelay = 0.5f;

    private TicTacToeBoard _board;
    private float _elapsed;
    private bool _timerRunning;
    private Coroutine _resultRoutine;

    private void Awake()
    {
        _board = GetComponent<TicTacToeBoard>();
        if (_board == null)
            Debug.LogError("[TicTacMinigame] TicTacToeBoard not found on same GameObject!");
    }

    public override void StartGame()
    {
        StopGame();
        _elapsed = 0f;
        _timerRunning = true;
        Progress = 1f;

        if (_board == null) return;
        _board.ResetGame();
        _board.OnCorrectClick += HandleWin;
        _board.OnWrongClick  += HandleLose;
    }

    public override void StopGame()
    {
        _timerRunning = false;
        if (_resultRoutine != null) StopCoroutine(_resultRoutine);
        _resultRoutine = null;
        if (_board != null)
        {
            _board.Lock();
            _board.OnCorrectClick -= HandleWin;
            _board.OnWrongClick  -= HandleLose;
        }
    }

    private void OnDisable() => StopGame();

    protected override void Update()
    {
        if (!_timerRunning) return;
        _elapsed += Time.deltaTime;
        Progress = 1f - Mathf.Clamp01(_elapsed / surviveTime);
        if (_elapsed >= surviveTime)
        {
            StopGame();
            RaiseLose(); // время вышло — проигрыш
        }
    }

    private void HandleWin()  => BeginResult(true);
    private void HandleLose() => BeginResult(false);

    private void BeginResult(bool won)
    {
        if (!_timerRunning) return;
        _timerRunning = false;
        _resultRoutine = StartCoroutine(ResultAfterDelay(won));
    }

    private IEnumerator ResultAfterDelay(bool won)
    {
        if (resultDelay > 0f) yield return new WaitForSeconds(resultDelay);
        _resultRoutine = null;
        StopGame();
        if (won) RaiseWin(); else RaiseLose();
    }
}
