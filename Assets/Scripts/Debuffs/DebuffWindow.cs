using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DebuffWindow : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private Image timerFill;
    [SerializeField] private Button closeButton;

    private DebuffDefinition _definition;
    private Action<DebuffWindow, DebuffId> _expired;
    private Action<DebuffWindow, DebuffId> _closed;
    private float _duration;
    private float _elapsed;
    private bool _initialized;
    private bool _finished;
    private RectTransform _screenBounds;
    private float _screenPadding;

    public RectTransform RectTransform => (RectTransform)transform;
    public Vector2 VisualSize
    {
        get
        {
            Vector2 scale = new Vector2(
                Mathf.Abs(RectTransform.localScale.x),
                Mathf.Abs(RectTransform.localScale.y));
            return Vector2.Scale(RectTransform.rect.size, scale);
        }
    }

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
    }

    private void Update()
    {
        if (!_initialized || _finished)
            return;

        _elapsed += Time.unscaledDeltaTime;
        timerFill.fillAmount = Mathf.Clamp01(_elapsed / _duration);

        if (_elapsed >= _duration)
            Expire();
    }

    private void LateUpdate()
    {
        KeepInsideScreen();
    }

    private void OnDestroy()
    {
        closeButton.onClick.RemoveListener(Close);
    }

    public void Initialize(
        DebuffDefinition definition,
        float duration,
        Action<DebuffWindow, DebuffId> expired,
        Action<DebuffWindow, DebuffId> closed)
    {
        _definition = definition;
        _duration = Mathf.Max(0.1f, duration);
        _expired = expired;
        _closed = closed;
        _elapsed = 0f;
        _finished = false;
        _initialized = true;

        title.text = definition.Title;
        timerFill.fillAmount = 0f;
        closeButton.interactable = definition.CanBeCancelled;
    }

    public void SetScreenBounds(RectTransform screenBounds, float padding)
    {
        _screenBounds = screenBounds;
        _screenPadding = Mathf.Max(0f, padding);
        FitInsideScreen();
        KeepInsideScreen();
    }

    private void FitInsideScreen()
    {
        if (_screenBounds == null)
            return;

        Vector2 availableSize = _screenBounds.rect.size
            - Vector2.one * (_screenPadding * 2f);
        Vector2 visualSize = VisualSize;

        if (availableSize.x <= 0f || availableSize.y <= 0f
            || visualSize.x <= 0f || visualSize.y <= 0f)
            return;

        float fit = Mathf.Min(
            1f,
            availableSize.x / visualSize.x,
            availableSize.y / visualSize.y);

        if (fit < 1f)
            RectTransform.localScale *= fit;
    }

    private void KeepInsideScreen()
    {
        if (_screenBounds == null)
            return;

        Rect bounds = _screenBounds.rect;
        Vector2 size = VisualSize;
        Vector2 pivot = RectTransform.pivot;
        Vector2 position = RectTransform.anchoredPosition;

        position.x = ClampAxis(
            position.x,
            bounds.xMin + _screenPadding + size.x * pivot.x,
            bounds.xMax - _screenPadding - size.x * (1f - pivot.x));
        position.y = ClampAxis(
            position.y,
            bounds.yMin + _screenPadding + size.y * pivot.y,
            bounds.yMax - _screenPadding - size.y * (1f - pivot.y));

        RectTransform.anchoredPosition = position;
    }

    private static float ClampAxis(float value, float min, float max)
    {
        return min <= max ? Mathf.Clamp(value, min, max) : (min + max) * 0.5f;
    }

    private void Close()
    {
        if (!_initialized || _finished || !_definition.CanBeCancelled)
            return;

        _finished = true;
        _closed?.Invoke(this, _definition.Id);
        Destroy(gameObject);
    }

    private void Expire()
    {
        _finished = true;
        timerFill.fillAmount = 1f;
        _expired?.Invoke(this, _definition.Id);
        Destroy(gameObject);
    }
}
