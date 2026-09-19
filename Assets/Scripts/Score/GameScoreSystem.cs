using UnityEngine;

public sealed class GameScoreSystem : MonoBehaviour
{
    private const int DefaultRegularMinigameScoreDelta = 1;

    [SerializeField] private LensMinigameManager[] minigameManagers;
    [SerializeField] private ScoreDigitsView scoreView;

    private int _regularMinigameScoreDelta = DefaultRegularMinigameScoreDelta;

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
                manager.RegularMinigameWon += ApplyRegularMinigameScore;
    }

    private void OnDisable()
    {
        if (minigameManagers == null)
            return;

        foreach (LensMinigameManager manager in minigameManagers)
            if (manager != null)
                manager.RegularMinigameWon -= ApplyRegularMinigameScore;
    }

    public void SetRegularMinigameScoreDelta(int scoreDelta)
    {
        _regularMinigameScoreDelta = scoreDelta;
    }

    private void ApplyRegularMinigameScore()
    {
        Score = Mathf.Max(0, Score + _regularMinigameScoreDelta);
        scoreView.SetScore(Score);
        scoreView.PlayPointAddedAnimation();
    }

    public void ResetScore()
    {
        Score = 0;
        _regularMinigameScoreDelta = DefaultRegularMinigameScoreDelta;
        scoreView.SetScore(Score);
    }
}
