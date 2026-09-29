using UnityEngine.Rendering.Universal;

public sealed class StartupPostProcessingFeature : ScriptableRendererFeature
{
    public override void Create() { }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // URP has resolved scene volumes and default profiles before this callback.
        DesktopStartupEffects.ApplyVolumes(renderingData.cameraData.camera);
    }
}
