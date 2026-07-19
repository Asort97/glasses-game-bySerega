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

    [Header("Movement")]
    [SerializeField] private float movingPlatformChance = 0.5f;
    [SerializeField] private float movementDistance = 1.25f;
    [SerializeField] private float movementSpeed = 1.5f;

    public int Index { get; private set; }
    public int RequiredNumber { get; private set; }

    private float _startLocalX;
    private float _movementPhase;
    private float _movementAmplitude;
    private bool _movesHorizontally;

    public void Initialize(
        int index,
        int selectedNumber,
        int previousNumber,
        int twoPlatformsAgoNumber,
        Vector2 horizontalBounds)
    {
        Index = index;
        RequiredNumber = GetRandomNumber(previousNumber, twoPlatformsAgoNumber);
        _startLocalX = Mathf.Clamp(transform.localPosition.x, horizontalBounds.x, horizontalBounds.y);
        _movementAmplitude = Mathf.Min(
            movementDistance,
            _startLocalX - horizontalBounds.x,
            horizontalBounds.y - _startLocalX);
        _movementPhase = Random.value * Mathf.PI * 2f;
        _movesHorizontally = Random.value < movingPlatformChance;

        if (numberRenderer != null)
            numberRenderer.sprite = GetNumberSprite(RequiredNumber);

        SetUsable(RequiredNumber == selectedNumber);
    }

    private void Update()
    {
        if (!_movesHorizontally)
            return;

        Vector3 position = transform.localPosition;
        position.x = _startLocalX
            + Mathf.Sin(Time.time * movementSpeed + _movementPhase) * _movementAmplitude;
        transform.localPosition = position;
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
