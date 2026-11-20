using System.Collections;
using UnityEngine;

public sealed class LensSkipController : MonoBehaviour
{
    [SerializeField] private LensMinigameManager manager;
    [SerializeField] private LensGameOverController gameOver;
    [SerializeField] private LensSkipEffect effect;
    [SerializeField] private LensSkipView view;
    [UnityEngine.Serialization.FormerlySerializedAs("useShift")]
    [SerializeField] private bool isLeftLens;
    [Header("Reward")]
    [Min(1)] [SerializeField] private int requiredCharges = 3;
    [Tooltip("Award a charge while the remaining timer fraction is above this value. 0.2 means more than one fifth remains.")]
    [Range(0f, 1f)] [SerializeField] private float remainingThreshold = 0.2f;

    private int _charges;
    private Coroutine _skipRoutine;
    public int Charges => _charges;
    public void RefillCharges() => _charges = requiredCharges;
    public bool CanUse => _charges >= requiredCharges && _skipRoutine == null
        && manager && manager.CanSkip && effect && effect.IsConfigured;

    private void OnEnable()
    {
        manager.RegularWinResolved += OnWin;
        manager.TransitionInterrupted += CancelSkip;
        manager.RunRestarted += ResetRewards;
        if (gameOver)
        {
            gameOver.GameOverStarted += ResetRewards;
            gameOver.RunReset += ResetRewards;
        }
    }

    private void OnDisable()
    {
        manager.RegularWinResolved -= OnWin;
        manager.TransitionInterrupted -= CancelSkip;
        manager.RunRestarted -= ResetRewards;
        if (gameOver)
        {
            gameOver.GameOverStarted -= ResetRewards;
            gameOver.RunReset -= ResetRewards;
        }
        CancelSkip();
    }

    private void Update()
    {
        if (isLeftLens ? Input.GetKeyDown(KeyCode.Tab) : Input.GetMouseButtonDown(1))
            TryUse();
        MinigameBase game = manager.ActiveMinigame;
        view.Refresh(_charges, manager.ShowSkipRewards,
            manager.CanSkip && game && game.Goal == MinigameBase.Objective.Completion, remainingThreshold);
    }

    private void OnWin(MinigameBase game)
    {
        if (game.Goal == MinigameBase.Objective.Completion && game.Progress > remainingThreshold)
            _charges = Mathf.Min(requiredCharges, _charges + 1);
    }

    public void TryUse()
    {
        if (!CanUse || !effect.TryCapture(view.IsOnLeftHalf)) return;
        if (!manager.TryBeginSkip())
        {
            effect.ResetEffect();
            return;
        }
        _charges = 0;
        view.Hide();
        manager.ClearSkippedGame();
        _skipRoutine = StartCoroutine(SkipRoutine());
    }

    private IEnumerator SkipRoutine()
    {
        yield return new WaitForSecondsRealtime(effect.Duration);
        effect.ResetEffect();
        _skipRoutine = null;
        manager.CompleteSkip();
    }

    private void CancelSkip()
    {
        if (_skipRoutine != null) StopCoroutine(_skipRoutine);
        _skipRoutine = null;
        effect.ResetEffect();
        view.Hide();
    }

    private void ResetRewards()
    {
        _charges = 0;
        CancelSkip();
    }
}
