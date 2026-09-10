using System.Collections;
using UnityEngine;

public sealed class GlassesLossRocking : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float duration = 1f;
    [SerializeField, Min(0.1f)] private float oscillations = 2.25f;
    [SerializeField, Min(0f)] private float damping = 3.5f;
    [SerializeField, Min(0.01f)] private float impactRiseTime = 0.06f;

    [Header("Virtual Pivot")]
    [SerializeField, Min(0f)] private float oppositeLensPivotDistance = 1.9f;

    [Header("Motion")]
    [SerializeField, Min(0f)] private float horizontalOffset = 0.035f;
    [SerializeField, Min(0f)] private float verticalOffset = 0.025f;
    [SerializeField, Min(0f)] private float depthOffset = 0.055f;
    [SerializeField, Min(0f)] private float pitchAngle = 0.7f;
    [SerializeField, Min(0f)] private float yawAngle = 1.2f;
    [SerializeField, Min(0f)] private float rollAngle = 2.2f;

    private Vector3 _restLocalPosition;
    private Quaternion _restLocalRotation;
    private Coroutine _rockRoutine;

    private void Awake()
    {
        CacheRestPose();
    }

    [ContextMenu("TEST1")]
    public void PLAY_TEST_1()
    {
        Play(true);
    }

    [ContextMenu("TEST2")]
    public void PLAY_TEST_2()
    {
        Play(false);
    }

    public void Play(bool fromLeftLens)
    {
        if (_rockRoutine != null)
            StopCoroutine(_rockRoutine);

        RestoreRestPose();
        _rockRoutine = StartCoroutine(Rock(fromLeftLens));
    }

    public void CacheRestPose()
    {
        _restLocalPosition = transform.localPosition;
        _restLocalRotation = transform.localRotation;
    }

    private IEnumerator Rock(bool fromLeftLens)
    {
        float side = fromLeftLens ? -1f : 1f;
        Vector3 pivot = Vector3.right * -side * oppositeLensPivotDistance;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float attack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / impactRiseTime));
            float tail = 1f - Mathf.SmoothStep(0.78f, 1f, normalizedTime);
            float envelope = attack * Mathf.Exp(-damping * normalizedTime) * tail;
            float phase = normalizedTime * oscillations * Mathf.PI * 2f;
            float primaryWave = Mathf.Cos(phase) * envelope;
            float secondaryWave = Mathf.Sin(phase * 0.72f) * envelope;

            Quaternion rotationOffset = Quaternion.Euler(
                pitchAngle * secondaryWave,
                yawAngle * side * primaryWave,
                rollAngle * side * primaryWave);

            Vector3 pivotOffset = pivot - rotationOffset * pivot;
            Vector3 positionOffset = new Vector3(
                horizontalOffset * side * secondaryWave,
                verticalOffset * secondaryWave,
                -depthOffset * primaryWave);

            transform.localRotation = _restLocalRotation * rotationOffset;
            transform.localPosition = _restLocalPosition
                + _restLocalRotation * (pivotOffset + positionOffset);

            yield return null;
        }

        RestoreRestPose();
        _rockRoutine = null;
    }

    private void OnDisable()
    {
        if (_rockRoutine != null)
        {
            StopCoroutine(_rockRoutine);
            _rockRoutine = null;
        }

        RestoreRestPose();
    }

    private void RestoreRestPose()
    {
        transform.localPosition = _restLocalPosition;
        transform.localRotation = _restLocalRotation;
    }
}
