using System.Collections;
using UnityEngine;

public class LensCrtPowerOffController : MonoBehaviour
{
    [SerializeField] private Renderer[] lensRenderers = new Renderer[0];
    [Min(0.01f)] [SerializeField] private float powerOffDuration = 0.5f;
    [Min(0.01f)] [SerializeField] private float powerOnDuration = 0.5f;

    private static readonly int EnabledId = Shader.PropertyToID("_CRTPowerOffEnabled");
    private static readonly int ProgressId = Shader.PropertyToID("_CRTPowerOffProgress");

    private Material[] _materials;

    private void Awake()
    {
        CacheMaterials();
        ResetEffect();
    }

    public IEnumerator PlayPowerOff()
    {
        yield return PlayPowerOff(1f);
    }

    public IEnumerator PlayPowerOff(float speedMultiplier)
    {
        CacheMaterials();
        SetEffect(true, 0f);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, powerOffDuration)
            / Mathf.Max(0.01f, speedMultiplier);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetEffect(true, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        SetEffect(true, 1f);
    }

    public IEnumerator PlayPowerOn()
    {
        CacheMaterials();
        SetEffect(true, 1f);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, powerOnDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetEffect(true, 1f - Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        ResetEffect();
    }

    public void ResetEffect()
    {
        CacheMaterials();
        SetEffect(false, 0f);
    }

    private void CacheMaterials()
    {
        if (_materials != null && _materials.Length == lensRenderers.Length)
            return;

        _materials = new Material[lensRenderers.Length];
        for (int i = 0; i < lensRenderers.Length; i++)
            if (lensRenderers[i] != null)
                _materials[i] = lensRenderers[i].material;
    }

    private void SetEffect(bool enabled, float progress)
    {
        if (_materials == null)
            return;

        foreach (Material material in _materials)
        {
            if (material == null)
                continue;

            if (material.HasProperty(EnabledId))
                material.SetFloat(EnabledId, enabled ? 1f : 0f);

            if (material.HasProperty(ProgressId))
                material.SetFloat(ProgressId, progress);
        }
    }
}
