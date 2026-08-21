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

    public RectTransform RectTransform => (RectTransform)transform;

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
