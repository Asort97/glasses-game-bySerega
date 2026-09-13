using System.Collections;
using UnityEngine;

public sealed class NightModeDebuffController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private DebuffSpawner debuffSpawner;
    [SerializeField] private Renderer[] lensRenderers;

    [Header("Timing")]
    [Min(0.1f)] [SerializeField] private float duration = 15f;
    [Min(0.01f)] [SerializeField] private float minFadeDuration = 0.25f;
    [Min(0.01f)] [SerializeField] private float maxFadeDuration = 0.8f;
    [Min(0f)] [SerializeField] private float minVisiblePause = 0.15f;
    [Min(0f)] [SerializeField] private float maxVisiblePause = 0.7f;
    [Min(0f)] [SerializeField] private float minDarkPause = 0.05f;
    [Min(0f)] [SerializeField] private float maxDarkPause = 0.25f;
    [Min(0.01f)] [SerializeField] private float finalFadeDuration = 0.4f;

    [Header("Darkness")]
    [Range(0f, 1f)] [SerializeField] private float minDarkness = 1f;
    [Range(0f, 1f)] [SerializeField] private float maxDarkness = 1f;
    [Range(0f, 1f)] [SerializeField] private float fullDarknessChance = 0.3f;

    private static readonly int DarknessId = Shader.PropertyToID("_NightModeDarkness");

    private Material[] _materials;
    private Coroutine _routine;
    private float _darkness;

    private void Awake()
    {
        CacheMaterials();
        SetDarkness(0f);
    }

    private void OnEnable()
    {
        debuffSpawner.DebuffApplied += HandleDebuffApplied;
    }

    private void OnDisable()
    {
        debuffSpawner.DebuffApplied -= HandleDebuffApplied;
        StopEffect();
        SetDarkness(0f);
    }

    private void HandleDebuffApplied(DebuffId id)
    {
        if (id == DebuffId.NightMode)
            Play();
    }

    [ContextMenu("Play Night Mode")]
    public void Play()
    {
        StopEffect();
        _routine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        float recoveryDuration = Mathf.Min(finalFadeDuration, duration);
        float flickerDeadline = Time.unscaledTime + duration - recoveryDuration;

        while (Time.unscaledTime < flickerDeadline)
        {
            yield return WaitUntilDeadline(
                Random.Range(minVisiblePause, maxVisiblePause),
                flickerDeadline);

            float targetDarkness = Random.value < fullDarknessChance
                ? 1f
                : Random.Range(minDarkness, maxDarkness);

            yield return FadeTo(
                targetDarkness,
                Random.Range(minFadeDuration, maxFadeDuration),
                flickerDeadline);

            yield return WaitUntilDeadline(
                Random.Range(minDarkPause, maxDarkPause),
                flickerDeadline);

            yield return FadeTo(
                0f,
                Random.Range(minFadeDuration, maxFadeDuration),
                flickerDeadline);
        }

        yield return FadeTo(0f, recoveryDuration, float.PositiveInfinity);
        _routine = null;
    }

    private IEnumerator FadeTo(float target, float fadeDuration, float deadline)
    {
        float start = _darkness;
        float elapsed = 0f;
        fadeDuration = Mathf.Max(0.01f, fadeDuration);

        while (elapsed < fadeDuration && Time.unscaledTime < deadline)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration);
            SetDarkness(Mathf.Lerp(start, target, progress));
            yield return null;
        }

        if (Time.unscaledTime < deadline)
            SetDarkness(target);
    }

    private static IEnumerator WaitUntilDeadline(float waitDuration, float deadline)
    {
        float endTime = Mathf.Min(Time.unscaledTime + waitDuration, deadline);
        while (Time.unscaledTime < endTime)
            yield return null;
    }

    private void StopEffect()
    {
        if (_routine == null)
            return;

        StopCoroutine(_routine);
        _routine = null;
    }

    private void CacheMaterials()
    {
        _materials = new Material[lensRenderers.Length];
        for (int i = 0; i < lensRenderers.Length; i++)
            _materials[i] = lensRenderers[i].material;
    }

    private void SetDarkness(float darkness)
    {
        _darkness = Mathf.Clamp01(darkness);

        foreach (Material material in _materials)
            material.SetFloat(DarknessId, _darkness);
    }
}
