using DG.Tweening;
using UnityEngine;

public sealed class LensHeartsView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] hearts;
    [SerializeField] private Sprite healthySprite;
    [SerializeField] private Sprite damagedSprite;
    [SerializeField] private Sprite brokenSprite;

    public void ShowBeforeLoss(int currentHp, bool loseFromLeft)
    {
        ShowRemaining(currentHp, loseFromLeft, 0);
    }

    public void ShowBreakingHeart(int currentHp, bool loseFromLeft)
    {
        ShowRemaining(currentHp, loseFromLeft, 0);

        int heartIndex = GetNextLostHeartIndex(currentHp, loseFromLeft);
        if (IsValidHeart(heartIndex))
            hearts[heartIndex].sprite = brokenSprite;
    }

    public void ShowAfterLoss(int remainingHp, bool loseFromLeft, int state)
    {
        ShowRemaining(remainingHp, loseFromLeft, state);
    }

    public void ShowSingleHeart(int currentHp, bool loseFromLeft)
    {
        ShowRemaining(currentHp, loseFromLeft, 0);

        int keepIndex = loseFromLeft ? hearts.Length - 1 : 0;
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null && i != keepIndex)
                hearts[i].gameObject.SetActive(false);
        }
    }

    public void Shake()
    {
        if (hearts == null)
            return;

        foreach (SpriteRenderer heart in hearts)
        {
            if (heart == null || !heart.gameObject.activeSelf)
                continue;

            heart.transform.DOKill(true);
            heart.transform.DOShakePosition(0.3f, 0.08f, 25, 90f, false, true);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void ShowRemaining(int currentHp, bool loseFromLeft, int state)
    {
        if (hearts == null || hearts.Length == 0)
            return;

        gameObject.SetActive(true);

        int clampedHp = Mathf.Clamp(currentHp, 0, hearts.Length);
        int lostCount = hearts.Length - clampedHp;
        Sprite stateSprite = GetStateSprite(state);

        for (int i = 0; i < hearts.Length; i++)
        {
            SpriteRenderer heart = hearts[i];
            if (heart == null)
                continue;

            bool isLost = loseFromLeft
                ? i < lostCount
                : i >= hearts.Length - lostCount;

            heart.gameObject.SetActive(!isLost);
            if (!isLost)
                heart.sprite = stateSprite;
        }
    }

    private int GetNextLostHeartIndex(int currentHp, bool loseFromLeft)
    {
        int lostCount = hearts.Length - Mathf.Clamp(currentHp, 0, hearts.Length);
        return loseFromLeft ? lostCount : hearts.Length - 1 - lostCount;
    }

    private Sprite GetStateSprite(int state)
    {
        if (state == 1)
            return damagedSprite;
        if (state == 2)
            return brokenSprite;
        return healthySprite;
    }

    private bool IsValidHeart(int index)
    {
        return hearts != null &&
               index >= 0 &&
               index < hearts.Length &&
               hearts[index] != null;
    }
}
