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
    [Min(0f)] [SerializeField] private float restartDelay = 3f;

    private LensHealthSystem _recoveringLens;
    private bool _gameOver;

    public bool IsGameOver => _gameOver;

    private void Awake()
    {
        if (gameOverMenu != null)
            gameOverMenu.SetActive(false);

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
        if (_gameOver)
            return;

        _gameOver = true;

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

        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        if (crtPowerOffController != null)
            yield return crtPowerOffController.PlayPowerOff();

        if (healthSystems != null)
        {
            foreach (LensHealthSystem health in healthSystems)
                if (health != null)
                    health.CompleteGameOverShutdown();
        }

        yield return new WaitForSecondsRealtime(restartDelay);

        _gameOver = false;
        _recoveringLens = null;
        if (crtPowerOffController != null)
            crtPowerOffController.ResetEffect();

        bossLevelDirector.ResetForNewRun();
        bossApproachCamera.ResetImmediately();
        gameStartSequenceCoordinator.ResetSequence();

        foreach (LensHealthSystem health in healthSystems)
            health.ResetHealth();

        foreach (LensMinigameManager manager in minigameManagers)
            manager.RestartFromGameStart();
    }
}
