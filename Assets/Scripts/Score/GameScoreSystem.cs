using UnityEngine;

public sealed class GameScoreSystem : MonoBehaviour
{
    [SerializeField] private LensMinigameManager[] minigameManagers;
    [SerializeField] private ScoreDigitsView scoreView;

    public int Score { get; private set; }

    private void Awake()
    {
        ResetScore();
    }

    private void OnEnable()
    {
        if (minigameManagers == null)
            return;

        foreach (LensMinigameManager manager in minigameManagers)
            if (manager != null)
                manager.RegularMinigameWon += AddPoint;
    }

    private void OnDisable()
    {
        if (minigameManagers == null)
            return;

        foreach (LensMinigameManager manager in minigameManagers)
            if (manager != null)
                manager.RegularMinigameWon -= AddPoint;
    }

    public void AddPoint()
    {
        Score++;
        scoreView.SetScore(Score);
        scoreView.PlayPointAddedAnimation();
    }

    public void ResetScore()
    {
        Score = 0;
        scoreView.SetScore(Score);
    }
}
