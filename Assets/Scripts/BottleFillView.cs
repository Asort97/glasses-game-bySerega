using UnityEngine;

public sealed class BottleFillView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer liquidRenderer;
    [SerializeField] private ParticleSystem bubbles;
    [Min(0f)] [SerializeField] private float emptyEmissionRate = 0f;
    [Min(0f)] [SerializeField] private float fullEmissionRate = 10f;

    private static readonly int FillId = Shader.PropertyToID("_Fill");
    private MaterialPropertyBlock _properties;

    public void SetFill(float fill)
    {
        fill = Mathf.Clamp01(fill);
        if (liquidRenderer)
        {
            if (_properties == null) _properties = new MaterialPropertyBlock();
            liquidRenderer.GetPropertyBlock(_properties);
            _properties.SetFloat(FillId, fill);
            liquidRenderer.SetPropertyBlock(_properties);
        }
        if (bubbles)
        {
            var emission = bubbles.emission;
            emission.rateOverTime = Mathf.Lerp(emptyEmissionRate, fullEmissionRate, fill);
        }
    }

    public void RestartParticles()
    {
        if (!bubbles) return;
        bubbles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        bubbles.Play(true);
    }

    public void PauseParticles()
    {
        if (bubbles) bubbles.Pause(true);
    }

    public void ClearParticles()
    {
        if (bubbles) bubbles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
