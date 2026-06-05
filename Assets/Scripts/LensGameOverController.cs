using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LensGameOverController : MonoBehaviour
{
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private Button restartButton;
    [SerializeField] private LensMinigameManager[] minigameManagers;
    [SerializeField] private LensHealthSystem[] healthSystems;

    private LensHealthSystem _recoveringLens;
    private bool _gameOver;

    public bool IsGameOver => _gameOver;

    private void Awake()
    {
        if (gameOverMenu != null)
            gameOverMenu.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartScene);
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

        if (gameOverMenu != null)
            gameOverMenu.SetActive(true);
    }

    private void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
