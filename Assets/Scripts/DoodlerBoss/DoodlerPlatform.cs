using UnityEngine;

public class DoodlerPlatform : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider2D platformCollider;
    [SerializeField] private SpriteRenderer platformRenderer;
    [SerializeField] private SpriteRenderer numberRenderer;

    [Header("Number Sprites")]
    [SerializeField] private Sprite numberOne;
    [SerializeField] private Sprite numberTwo;
    [SerializeField] private Sprite numberThree;
    [SerializeField] private Sprite numberFour;

    public int Index { get; private set; }
    public int RequiredNumber { get; private set; }

    public void Initialize(int index, int selectedNumber, int previousNumber, int twoPlatformsAgoNumber)
    {
        Index = index;
        RequiredNumber = GetRandomNumber(previousNumber, twoPlatformsAgoNumber);

        if (numberRenderer != null)
            numberRenderer.sprite = GetNumberSprite(RequiredNumber);

        SetUsable(RequiredNumber == selectedNumber);
    }

    private int GetRandomNumber(int previousNumber, int twoPlatformsAgoNumber)
    {
        int number;
        do
        {
            number = Random.Range(1, 5);
        }
        while (number == previousNumber || number == twoPlatformsAgoNumber);

        return number;
    }

    public void SetUsable(bool isUsable)
    {
        if (platformCollider != null)
            platformCollider.enabled = isUsable;

        if (platformRenderer != null)
            platformRenderer.color = isUsable ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
    }

    private Sprite GetNumberSprite(int number)
    {
        switch (number)
        {
            case 1:
                return numberOne;
            case 2:
                return numberTwo;
            case 3:
                return numberThree;
            case 4:
                return numberFour;
            default:
                return null;
        }
    }
}
