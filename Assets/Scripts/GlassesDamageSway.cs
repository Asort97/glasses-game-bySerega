using UnityEngine;

/// <summary>
/// Self-contained idle sway of the glasses that grows with the hearts lost in both lenses.
/// It only adds an offset on top of whatever pose other scripts set, so disabling or removing
/// this component returns the glasses to their exact normal motion.
/// </summary>
[DefaultExecutionOrder(-500)]
public sealed class GlassesDamageSway : MonoBehaviour
{
    [SerializeField] private LensHealthSystem[] lenses;
    [Tooltip("Shapes how quickly the sway grows: 1 is linear, above 1 keeps the first lost heart subtle.")]
    [Min(0.1f)] [SerializeField] private float intensityExponent = 1.3f;
    [Min(0.01f)] [SerializeField] private float intensitySmoothTime = 0.6f;

    [Header("Amplitude at all hearts lost")]
    [SerializeField] private Vector3 positionAmplitude = new Vector3(0.025f, 0.02f, 0.08f);
    [Tooltip("Pitch, yaw, roll in degrees.")]
    [SerializeField] private Vector3 rotationAmplitude = new Vector3(0.8f, 1.2f, 2.4f);

    [Header("Motion")]
    [Min(0.01f)] [SerializeField] private float swaySpeed = 0.55f;
    [Min(0.01f)] [SerializeField] private float depthSpeed = 0.35f;

    private Vector3 _appliedPosition;
    private Quaternion _appliedRotation = Quaternion.identity;
    private float _intensity, _intensityVelocity, _time;
    private float _seed;

    private void Awake() => _seed = Random.Range(0f, 100f);

    // Runs before other scripts so they always read and write the clean pose.
    private void Update() => RemoveOffset();

    private void LateUpdate()
    {
        float target = Mathf.Pow(LostFraction(), intensityExponent);
        _intensity = Mathf.SmoothDamp(_intensity, target, ref _intensityVelocity,
            intensitySmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        _time += Time.unscaledDeltaTime;
        if (_intensity < 0.0005f) return;

        float t = _time * swaySpeed + _seed;
        // Layered slow waves read as a natural, slightly irregular sway.
        float sx = Wave(t, 1f, 0.37f), sy = Wave(t, 1.31f, 0.53f), sr = Wave(t, 0.83f, 0.61f);
        float depth = Wave(_time * depthSpeed + _seed * 0.5f, 1f, 0.29f);

        Vector3 position = new Vector3(positionAmplitude.x * sx, positionAmplitude.y * sy,
            positionAmplitude.z * depth) * _intensity;
        Quaternion rotation = Quaternion.Euler(rotationAmplitude.x * sy * _intensity,
            rotationAmplitude.y * sx * _intensity, rotationAmplitude.z * sr * _intensity);

        _appliedPosition = transform.localRotation * position;
        _appliedRotation = rotation;
        transform.localPosition += _appliedPosition;
        transform.localRotation *= _appliedRotation;
    }

    private void OnDisable()
    {
        RemoveOffset();
        _intensity = _intensityVelocity = 0f;
    }

    private void RemoveOffset()
    {
        if (_appliedPosition == Vector3.zero && _appliedRotation == Quaternion.identity) return;
        transform.localRotation *= Quaternion.Inverse(_appliedRotation);
        transform.localPosition -= _appliedPosition;
        _appliedPosition = Vector3.zero;
        _appliedRotation = Quaternion.identity;
    }

    private float LostFraction()
    {
        if (lenses == null) return 0f;
        int max = 0, current = 0;
        foreach (LensHealthSystem lens in lenses)
        {
            if (!lens) continue;
            max += Mathf.Max(0, lens.MaxHp);
            current += Mathf.Clamp(lens.CurrentHp, 0, lens.MaxHp);
        }
        return max > 0 ? Mathf.Clamp01(1f - (float)current / max) : 0f;
    }

    private static float Wave(float t, float frequency, float detail) =>
        (Mathf.Sin(t * frequency * Mathf.PI * 2f) + detail * Mathf.Sin(t * frequency * 2.7f * Mathf.PI * 2f + 1.3f))
        / (1f + detail);
}
