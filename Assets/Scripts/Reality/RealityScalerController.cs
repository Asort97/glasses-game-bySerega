using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class RealityScalerController : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private Image fill;
    [SerializeField] private GameObject label;
    [Header("Run Events")]
    [SerializeField] private GameStartSequenceCoordinator initialSequence;
    [SerializeField] private LensMinigameManager[] minigameManagers;
    [SerializeField] private BossLevelDirector bossDirector;
    [SerializeField] private LensGameOverController gameOver;
    [SerializeField] private GameQuitController quitController;
    [Header("Progress")]
    [Min(0.1f)] [SerializeField] private float fillDuration = 60f;
    [Range(0f, 1f)] [SerializeField] private float minigameReduction = 0.01f;
    [Range(0f, 1f)] [SerializeField] private float bossReduction = 0.06f;

    public event Action LimitReached;
    public float Progress { get; private set; }
    public bool IsRunning { get; private set; }

    private void Awake() => ResetForRun();

    private void OnEnable()
    {
        initialSequence.InitialSequenceCompleted += BeginRun;
        foreach (var manager in minigameManagers)
        {
            manager.RegularSequenceStarted += BeginRun;
            manager.RegularMinigameWon += OnMinigameWon;
        }
        bossDirector.BossWon += OnBossWon;
        gameOver.GameOverStarted += StopAndHide;
        gameOver.RunReset += ResetForRun;
    }

    private void OnDisable()
    {
        initialSequence.InitialSequenceCompleted -= BeginRun;
        foreach (var manager in minigameManagers)
        {
            manager.RegularSequenceStarted -= BeginRun;
            manager.RegularMinigameWon -= OnMinigameWon;
        }
        bossDirector.BossWon -= OnBossWon;
        gameOver.GameOverStarted -= StopAndHide;
        gameOver.RunReset -= ResetForRun;
        StopAndHide();
    }

    private void Update()
    {
        if (!IsRunning || Time.timeScale <= 0f || quitController.IsQuitting)
            return;

        SetProgress(Progress + Time.unscaledDeltaTime / Mathf.Max(0.1f, fillDuration));
        if (Progress < 1f)
            return;

        IsRunning = false;
        LimitReached?.Invoke();
    }

    private void BeginRun()
    {
        if (IsRunning || gameOver.IsGameOver)
            return;
        IsRunning = true;
        SetVisible(true);
    }

    private void OnMinigameWon() => Reduce(minigameReduction);
    private void OnBossWon() => Reduce(bossReduction);

    private void Reduce(float amount)
    {
        if (IsRunning)
            SetProgress(Progress - amount);
    }

    private void ResetForRun()
    {
        StopAndHide();
        SetProgress(0f);
    }

    private void StopAndHide()
    {
        IsRunning = false;
        SetVisible(false);
    }

    private void SetProgress(float value)
    {
        Progress = Mathf.Clamp01(value);
        fill.fillAmount = Progress;
    }

    private void SetVisible(bool visible)
    {
        fill.gameObject.SetActive(visible);
        if (label != null)
            label.SetActive(visible);
    }
}
