using System.Collections;
using DG.Tweening;
using UnityEngine;

public sealed class RealityCollapseSequence : MonoBehaviour
{
    public enum CollapseStage { Idle, FirstDemon, Blackout, Restart }

    [Header("Systems")]
    [SerializeField] private RealityScalerController scaler;
    [SerializeField] private LensGameOverController gameOver;
    [SerializeField] private BossLevelDirector bossDirector;
    [SerializeField] private GlassesIntroBootstrap intro;
    [SerializeField] private GameQuitController quitController;
    [SerializeField] private QuarteredScreenCapture screenCapture;
    [SerializeField] private RealityAudioFreeze audioFreeze;
    [Header("Scene Objects")]
    [SerializeField] private GameObject firstDemon;
    [SerializeField] private GameObject glasses;
    [SerializeField] private GameObject[] hideDuringSequence;
    [Header("Timing")]
    [Min(0f)] [SerializeField] private float firstDemonDuration = 1f;
    [Min(0f)] [SerializeField] private float blackHoldDuration = 3f;
    [Min(0.01f)] [SerializeField] private float blackFadeDuration = 1f;

    public CollapseStage Stage { get; private set; }
    public bool IsPlaying => Stage != CollapseStage.Idle;
    private bool[] _objectStates;
    private float _timeScale;
    private bool _cursorVisible;
    private bool _restorePending;
    private Tween _fade;

    private void Awake()
    {
        firstDemon.SetActive(false);
    }

    private void OnEnable() => scaler.LimitReached += Trigger;

    [ContextMenu("Test Reality Collapse (Play Mode)")]
    public void Trigger()
    {
        if (!Application.isPlaying || IsPlaying || quitController.IsQuitting || gameOver.IsGameOver)
            return;

        // Capture before game-over handlers hide games, reset debuffs, or change lens materials.
        Stage = CollapseStage.FirstDemon;
        firstDemon.SetActive(true);
        try
        {
            audioFreeze.BeginFreeze();
            screenCapture.CaptureAndShow();
        }
        catch (System.Exception exception)
        {
            audioFreeze.EndFreeze();
            screenCapture.Clear();
            firstDemon.SetActive(false);
            Stage = CollapseStage.Idle;
            Debug.LogException(exception, this);
            return;
        }
        if (!gameOver.TryBeginGameOver())
        {
            audioFreeze.EndFreeze();
            screenCapture.Clear();
            firstDemon.SetActive(false);
            Stage = CollapseStage.Idle;
            return;
        }
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        _timeScale = Time.timeScale;
        _cursorVisible = Cursor.visible;
        _restorePending = true;
        quitController.SetInputBlocked(true);
        _objectStates = new bool[hideDuringSequence.Length];
        for (int i = 0; i < hideDuringSequence.Length; i++)
        {
            _objectStates[i] = hideDuringSequence[i].activeSelf;
            hideDuringSequence[i].SetActive(false);
        }
        Cursor.visible = false;
        Time.timeScale = 0f;
        bossDirector.StopForGameOver();
        yield return new WaitForSecondsRealtime(firstDemonDuration);

        screenCapture.SetBlackAlpha(1f);
        audioFreeze.EndFreeze();
        screenCapture.HideSnapshot();
        glasses.SetActive(false);
        firstDemon.SetActive(false);
        Stage = CollapseStage.Blackout;
        yield return new WaitForSecondsRealtime(blackHoldDuration);

        RestoreEnvironment();
        gameOver.ResetRunState(true);
        intro.PrepareIntroReplay();
        Stage = CollapseStage.Restart;
        _fade = DOTween.To(() => screenCapture.BlackAlpha, screenCapture.SetBlackAlpha,
                0f, Mathf.Max(0.01f, blackFadeDuration))
            .SetEase(Ease.Linear).SetUpdate(true);
        yield return intro.ReplayIntro();
        if (_fade != null && _fade.IsActive())
            yield return _fade.WaitForCompletion();
        _fade = null;
        screenCapture.Clear();
        quitController.SetInputBlocked(false);
        Stage = CollapseStage.Idle;
    }

    private void RestoreEnvironment()
    {
        if (!_restorePending)
            return;
        for (int i = 0; i < hideDuringSequence.Length; i++)
            hideDuringSequence[i].SetActive(_objectStates[i]);
        Cursor.visible = _cursorVisible;
        Time.timeScale = _timeScale;
        _restorePending = false;
    }

    private void OnDisable()
    {
        scaler.LimitReached -= Trigger;
        StopAllCoroutines();
        _fade?.Kill();
        if (audioFreeze != null) audioFreeze.EndFreeze();
        screenCapture.Clear();
        firstDemon.SetActive(false);
        RestoreEnvironment();
        quitController.SetInputBlocked(false);
        Stage = CollapseStage.Idle;
    }
}
