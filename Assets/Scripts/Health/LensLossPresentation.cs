using System.Collections;
using UnityEngine;

public sealed class LensLossPresentation : MonoBehaviour
{
    private const int HeartStages = 5;
    private const int IntervalCount = HeartStages * 2 - 1;

    [SerializeField] private LensHeartsView heartsView;
    [SerializeField] private Vector2 intervalWeightRange = new Vector2(0.75f, 1.25f);

    public IEnumerator Play(GameObject minigame, LensHealthSystem health, float duration)
    {
        if (heartsView == null || health == null)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, duration));
            yield break;
        }

        int currentHp = health.CurrentHp;
        int remainingHp = Mathf.Max(0, currentHp - 1);
        float[] intervals = CreateIntervals(Mathf.Max(0f, duration));
        int intervalIndex = 0;

        for (int stage = 0; stage < HeartStages; stage++)
        {
            SetMinigameVisible(minigame, false);
            ShowHeartStage(stage, currentHp, remainingHp, health.LoseFromLeft);
            yield return Wait(intervals[intervalIndex++]);

            if (stage == HeartStages - 1)
                break;

            heartsView.Hide();
            SetMinigameVisible(minigame, true);
            yield return Wait(intervals[intervalIndex++]);
        }

        SetMinigameVisible(minigame, false);
    }

    public void Hide()
    {
        if (heartsView != null)
            heartsView.Hide();
    }

    private void ShowHeartStage(int stage, int currentHp, int remainingHp, bool loseFromLeft)
    {
        if (stage <= 1)
        {
            heartsView.ShowBeforeLoss(currentHp, loseFromLeft);
            return;
        }

        if (stage == 2)
        {
            heartsView.ShowBreakingHeart(currentHp, loseFromLeft);
            return;
        }

        heartsView.ShowAfterLoss(remainingHp, loseFromLeft, stage == 3 ? 0 : 2);
    }

    private float[] CreateIntervals(float duration)
    {
        float[] intervals = new float[IntervalCount];
        float minWeight = Mathf.Max(0.01f, Mathf.Min(intervalWeightRange.x, intervalWeightRange.y));
        float maxWeight = Mathf.Max(minWeight, Mathf.Max(intervalWeightRange.x, intervalWeightRange.y));
        float totalWeight = 0f;

        for (int i = 0; i < intervals.Length; i++)
        {
            intervals[i] = Random.Range(minWeight, maxWeight);
            totalWeight += intervals[i];
        }

        float scale = duration / Mathf.Max(0.0001f, totalWeight);
        for (int i = 0; i < intervals.Length; i++)
            intervals[i] *= scale;

        return intervals;
    }

    private static IEnumerator Wait(float duration)
    {
        if (duration > 0f)
            yield return new WaitForSeconds(duration);
    }

    private static void SetMinigameVisible(GameObject minigame, bool visible)
    {
        if (minigame != null)
            minigame.SetActive(visible);
    }
}
