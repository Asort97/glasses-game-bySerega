using UnityEngine;

/// <summary>
/// Абстрактный базовый класс для всех мини-игр.
/// Управляет таймером выживания, прогрессом и событиями победы/поражения.
/// </summary>
public abstract class MinigameBase : MonoBehaviour
{
    [SerializeField] protected float surviveTime = 10f;

    /// <summary>0 = пусто, 1 = полный таймер</summary>
    public float Progress { get; protected set; } = 1f;

    public event System.Action OnWin;
    public event System.Action OnLose;

    private float _timer;
    private bool  _running;

    /// <summary>Запускает мини-игру. LensMinigameManager вызывает после SetActive(true).</summary>
    public virtual void StartGame()
    {
        _timer   = 0f;
        _running = true;
        Progress = 1f;
    }

    /// <summary>Останавливает мини-игру. LensMinigameManager вызывает перед SetActive(false).</summary>
    public virtual void StopGame()
    {
        _running = false;
    }

    protected virtual void Update()
    {
        if (!_running) return;

        _timer  += Time.deltaTime;
        Progress = 1f - Mathf.Clamp01(_timer / surviveTime);

        if (_timer >= surviveTime)
        {
            _running = false;
            RaiseWin();
        }
    }

    protected void RaiseWin()  => OnWin?.Invoke();
    protected void RaiseLose() => OnLose?.Invoke();
}
