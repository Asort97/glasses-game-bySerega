using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class ScoreDigitsView : MonoBehaviour
{
    [SerializeField] private RectTransform digitsRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image firstDigit;
    [SerializeField] private Image secondDigit;
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];
    [SerializeField, Range(0f, 1f)] private float pointAddedAlpha = 0.5f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.5f;
    [SerializeField, Min(0f)] private float showDuration = 0.5f;
    [SerializeField] private Ease fadeEase = Ease.Linear;

    private readonly List<Image> _digits = new List<Image>();
    private Tween _fadeTween;
    private float _defaultAlpha;

    private void Awake()
    {
        _defaultAlpha = canvasGroup.alpha;
    }

    private void OnDisable()
    {
        _fadeTween?.Kill();
        _fadeTween = null;
        canvasGroup.alpha = _defaultAlpha;
    }

    public void SetScore(int score)
    {
        EnsureInitialized();

        string value = Mathf.Max(0, score).ToString("D2");
        EnsureDigitCount(value.Length);

        for (int i = 0; i < _digits.Count; i++)
        {
            bool visible = i < value.Length;
            _digits[i].gameObject.SetActive(visible);

            if (visible)
                _digits[i].sprite = digitSprites[value[i] - '0'];
        }
    }

    public void PlayPointAddedAnimation()
    {
        _fadeTween?.Kill();
        canvasGroup.alpha = pointAddedAlpha;
        _fadeTween = canvasGroup
            .DOFade(_defaultAlpha, fadeDuration)
            .SetDelay(showDuration)
            .SetEase(fadeEase)
            .SetUpdate(UpdateType.Late, true);
    }

    private void EnsureInitialized()
    {
        if (_digits.Count > 0)
            return;

        _digits.Add(firstDigit);
        _digits.Add(secondDigit);

        firstDigit.name = "number1";
        secondDigit.name = "number2";
        firstDigit.raycastTarget = false;
        secondDigit.raycastTarget = false;
    }

    private void EnsureDigitCount(int requiredCount)
    {
        while (_digits.Count < requiredCount)
        {
            Image digit = Instantiate(firstDigit, digitsRoot);
            digit.name = $"number{_digits.Count + 1}";
            digit.raycastTarget = false;
            _digits.Add(digit);
        }
    }
}
