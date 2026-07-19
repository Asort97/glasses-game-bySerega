using UnityEngine;
using System.Collections;

public class BossLevelDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LensMinigameManager leftManager;
    [SerializeField] private LensMinigameManager rightManager;
    [SerializeField] private BossLevelBase[] bossLevels;

    [Header("Schedule")]
    [Min(1)]
    [SerializeField] private int minigamesPerBoss = 20;

    private int _passedMinigames;
    private int _lastBossIndex = -1;
    private bool _bossQueued;
    private bool _leftWaiting;
    private bool _rightWaiting;
    private BossLevelBase _activeBoss;
    private Coroutine _startRoutine;
    private Coroutine _resumeRoutine;

    public bool NotifyRegularMinigamePassed(LensMinigameManager manager)
    {
        if (manager != leftManager && manager != rightManager)
            return false;

        if (_activeBoss != null)
            return false;

        if (!_bossQueued)
        {
            _passedMinigames++;
            _bossQueued = _passedMinigames >= minigamesPerBoss;
        }

        return _bossQueued;
    }

    public void WaitForBoss(LensMinigameManager manager)
    {
        if (!_bossQueued)
            return;

        if (manager == leftManager)
            _leftWaiting = true;
        else if (manager == rightManager)
            _rightWaiting = true;
        else
            return;

        manager.EnterBossWait();

        if (_leftWaiting && _rightWaiting)
            StartQueuedBoss();
    }

    private void StartQueuedBoss()
    {
        _activeBoss = PickBoss();
        if (_activeBoss == null)
        {
            Debug.LogError("Boss level pool is empty.", this);
            ResumeRegularMinigames();
            return;
        }

        _activeBoss.OnCompleted += HandleBossCompleted;
        _activeBoss.OnFailed += HandleBossFailed;
        _startRoutine = StartCoroutine(StartBossRoutine(_activeBoss));
    }

    private IEnumerator StartBossRoutine(BossLevelBase boss)
    {
        boss.ShowPreviewImage();

        if (boss.PreviewImageDuration > 0f)
            yield return new WaitForSeconds(boss.PreviewImageDuration);

        boss.PlayPreviewAnimation();

        if (boss.PreviewAnimationDuration > 0f)
            yield return new WaitForSeconds(boss.PreviewAnimationDuration);

        boss.HidePreview();

        if (_activeBoss == boss)
            boss.StartBoss();

        _startRoutine = null;
    }

    private BossLevelBase PickBoss()
    {
        if (bossLevels == null || bossLevels.Length == 0)
            return null;

        int validCount = 0;
        for (int i = 0; i < bossLevels.Length; i++)
        {
            if (bossLevels[i] != null)
                validCount++;
        }

        if (validCount == 0)
            return null;

        int selectedIndex;
        do
        {
            selectedIndex = Random.Range(0, bossLevels.Length);
        }
        while (bossLevels[selectedIndex] == null
            || (validCount > 1 && selectedIndex == _lastBossIndex));

        _lastBossIndex = selectedIndex;
        return bossLevels[selectedIndex];
    }

    private void HandleBossCompleted(BossLevelBase boss)
    {
        FinishBoss(boss);
    }

    private void HandleBossFailed(BossLevelBase boss)
    {
        FinishBoss(boss);
    }

    private void FinishBoss(BossLevelBase boss)
    {
        if (_startRoutine != null)
        {
            StopCoroutine(_startRoutine);
            _startRoutine = null;
        }

        boss.OnCompleted -= HandleBossCompleted;
        boss.OnFailed -= HandleBossFailed;
        boss.StopBoss();
        _activeBoss = null;
        _passedMinigames = 0;

        if (_resumeRoutine != null)
            StopCoroutine(_resumeRoutine);

        _resumeRoutine = StartCoroutine(ResumeAfterBossRoutine());
    }

    private IEnumerator ResumeAfterBossRoutine()
    {
        // The boss roots are disabled first. The regular game transition starts next frame.
        yield return null;
        ResumeRegularMinigames();
        _resumeRoutine = null;
    }

    private void ResumeRegularMinigames()
    {
        _bossQueued = false;
        _leftWaiting = false;
        _rightWaiting = false;

        if (leftManager != null)
            leftManager.ResumeAfterBoss();
        if (rightManager != null)
            rightManager.ResumeAfterBoss();
    }
}
