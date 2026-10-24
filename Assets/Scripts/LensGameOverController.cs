using System.Collections;
using UnityEngine;

public class LensGameOverController : MonoBehaviour
{
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private LensMinigameManager[] minigameManagers;
    [SerializeField] private LensHealthSystem[] healthSystems;
    [SerializeField] private BossLevelDirector bossLevelDirector;
    [SerializeField] private BossApproachCamera bossApproachCamera;
    [SerializeField] private GameStartSequenceCoordinator gameStartSequenceCoordinator;
    [SerializeField] private LensCrtPowerOffController crtPowerOffController;
    [SerializeField] private DebuffSpawner debuffSpawner;
    [SerializeField] private GameScoreSystem gameScoreSystem;
    [Min(0f)] [SerializeField] private float restartDelay = 3f;

    private LensHealthSystem _recoveringLens;
    private bool _gameOver;

    public bool IsGameOver => _gameOver;
    public event System.Action GameOverStarted;
    public event System.Action RunReset;

    private void Awake()
    {
        if (gameOverMenu != null)
            gameOverMenu.SetActive(false);

    }

    private void OnEnable()
    {
        if (bossLevelDirector != null)
            bossLevelDirector.BossFailed += TriggerGameOver;
    }

    private void OnDisable()
    {
        if (bossLevelDirector != null)
            bossLevelDirector.BossFailed -= TriggerGameOver;
    }

    public void NotifyLensBroken(LensHealthSystem lens)
    {
        if (_gameOver)
            return;

        if (_recoveringLens != null && _recoveringLens != lens)
        {
            TriggerGameOver();
            return;
        }

        _recoveringLens = lens;
    }

    public void NotifyLensRestored(LensHealthSystem lens)
    {
        if (_recoveringLens == lens)
            _recoveringLens = null;
    }

    public void TriggerGameOver()
    {
        if (TryBeginGameOver())
            StartCoroutine(RestartAfterDelay());
    }

    public bool TryBeginGameOver()
    {
        if (_gameOver)
            return false;

        _gameOver = true;
        GameOverStarted?.Invoke();

        if (debuffSpawner != null)
            debuffSpawner.StopAndReset();

        if (healthSystems != null)
        {
            foreach (LensHealthSystem health in healthSystems)
                if (health != null)
                    health.StopForGameOver();
        }

        if (minigameManagers != null)
        {
            foreach (LensMinigameManager manager in minigameManagers)
            {
                if (manager == null)
                    continue;

                manager.SetPaused(true);
                manager.enabled = false;
            }
        }

        return true;
    }

    public IEnumerator ShutdownLenses()
    {
        if (crtPowerOffController != null)
            yield return crtPowerOffController.PlayPowerOff();

        if (healthSystems != null)
        {
            foreach (LensHealthSystem health in healthSystems)
                if (health != null)
                    health.CompleteGameOverShutdown();
        }

    }

    public void ResetRunState(bool replayIntro = false)
    {
        _gameOver = false;
        _recoveringLens = null;
        bossLevelDirector.ResetForNewRun();
        if (replayIntro)
            bossApproachCamera.ResetImmediately();
        else
            bossApproachCamera.ResetSmoothly();
        gameStartSequenceCoordinator.ResetSequence();
        if (debuffSpawner != null)
            debuffSpawner.StartSpawning();
        gameScoreSystem.ResetScore();

        foreach (LensHealthSystem health in healthSystems)
        {
            if (replayIntro)
                health.RestoreFullHealthAfterTutorial();
            else
                health.ResetHealth();
        }

        RunReset?.Invoke();
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return ShutdownLenses();
        yield return new WaitForSecondsRealtime(restartDelay);
        ResetRunState();

        bool skipInitialSequence = GameAdminSettings.SkipInitialIntroAndTutorial;
        foreach (LensMinigameManager manager in minigameManagers)
        {
            if (skipInitialSequence)
                manager.RestartFromRegularMinigames();
            else
                manager.RestartFromGameStart();
        }

        if (crtPowerOffController != null)
            yield return crtPowerOffController.PlayPowerOn();
    }
}
