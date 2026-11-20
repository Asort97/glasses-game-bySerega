using UnityEngine;

/// <summary>
/// Абстрактный базовый класс для всех мини-игр.
/// Управляет таймером выживания, прогрессом и событиями победы/поражения.
/// </summary>
public abstract class MinigameBase : MonoBehaviour
{
    public enum Objective { Survival, Completion }
    [SerializeField] private Objective objective;
    public Objective Goal => objective;
    [SerializeField] protected float surviveTime = 10f;
    [SerializeField] private Sprite previewTitleSprite;
    [SerializeField] private MiniTutorialType tutorialType;
    [Tooltip("Seconds to finish a Completion game and earn a skip charge. 0 uses the lens default.")]
    [Min(0f)] [SerializeField] private float perfectTime;
    [Tooltip("Hide the perfect-time marker on the timer while keeping the reward rule.")]
    [SerializeField] private bool hidePerfectMarker;

    /// <summary>0 = пусто, 1 = полный таймер</summary>
    public float Progress { get; protected set; } = 1f;
    public Sprite PreviewTitleSprite => previewTitleSprite != null ? previewTitleSprite : GetFallbackPreviewSprite();
    public MiniTutorialType TutorialType => tutorialType;
    public bool HidePerfectMarker => hidePerfectMarker;

    /// <summary>Remaining timer fraction above which a win earns a skip charge.</summary>
    public float GetRemainingThreshold(float fallback) => perfectTime > 0f && surviveTime > 0f
        ? Mathf.Clamp01(1f - perfectTime / surviveTime) : fallback;

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

    /// <summary>Stops gameplay but preserves the result background. Reset visuals in StartGame.</summary>
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

    private Sprite GetFallbackPreviewSprite()
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(true);
        return sr != null ? sr.sprite : null;
    }
}
