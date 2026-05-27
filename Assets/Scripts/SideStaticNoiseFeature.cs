using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class SideStaticNoiseFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        [Range(0f, 1f)] public float leftAmount = 0f;
        [Range(0f, 1f)] public float rightAmount = 0f;
        [Range(0.001f, 0.5f)] public float edgeSoftness = 0.36f;
        [Range(0f, 1f)] public float edgeRoughness = 0.28f;
        [Range(0f, 1f)] public float intensity = 1f;
        [Range(1f, 12f)] public float noiseScale = 5f;
        [Range(0f, 60f)] public float noiseSpeed = 20f;
        [Range(0f, 1f)] public float blackDensity = 0.38f;
        [Range(0f, 1f)] public float whiteBoost = 0.85f;
    }

    public Settings settings = new Settings();

    private static readonly int PropLeftAmount = Shader.PropertyToID("_LeftAmount");
    private static readonly int PropRightAmount = Shader.PropertyToID("_RightAmount");
    private static readonly int PropEdgeSoftness = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int PropEdgeRoughness = Shader.PropertyToID("_EdgeRoughness");
    private static readonly int PropIntensity = Shader.PropertyToID("_Intensity");
    private static readonly int PropNoiseScale = Shader.PropertyToID("_NoiseScale");
    private static readonly int PropNoiseSpeed = Shader.PropertyToID("_NoiseSpeed");
    private static readonly int PropBlackDensity = Shader.PropertyToID("_BlackDensity");
    private static readonly int PropWhiteBoost = Shader.PropertyToID("_WhiteBoost");
    private static readonly int PropNoiseResolution = Shader.PropertyToID("_NoiseResolution");

    private SideStaticNoisePass _pass;
    private Material _material;

    public override void Create()
    {
        Shader shader = Shader.Find("Custom/SideStaticNoise");
        if (shader == null)
        {
            Debug.LogError("[SideStaticNoise] Shader not found.");
            return;
        }

        _material = CoreUtils.CreateEngineMaterial(shader);
        _pass = new SideStaticNoisePass(_material)
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null || _pass == null) return;
        if (renderingData.cameraData.cameraType != CameraType.Game) return;
        if (settings.leftAmount <= 0f && settings.rightAmount <= 0f) return;

        _material.SetFloat(PropLeftAmount, settings.leftAmount);
        _material.SetFloat(PropRightAmount, settings.rightAmount);
        _material.SetFloat(PropEdgeSoftness, settings.edgeSoftness);
        _material.SetFloat(PropEdgeRoughness, settings.edgeRoughness);
        _material.SetFloat(PropIntensity, settings.intensity);
        _material.SetFloat(PropNoiseScale, settings.noiseScale);
        _material.SetFloat(PropNoiseSpeed, settings.noiseSpeed);
        _material.SetFloat(PropBlackDensity, settings.blackDensity);
        _material.SetFloat(PropWhiteBoost, settings.whiteBoost);
        _material.SetVector(PropNoiseResolution, new Vector4(
            renderingData.cameraData.camera.pixelWidth,
            renderingData.cameraData.camera.pixelHeight,
            0f,
            0f));

        _pass.renderPassEvent = settings.renderPassEvent;
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _material != null)
            CoreUtils.Destroy(_material);
    }

    private sealed class SideStaticNoisePass : ScriptableRenderPass
    {
        private readonly Material _material;

        private sealed class EffectPassData
        {
            public TextureHandle src;
            public Material material;
        }

        private sealed class CopyBackPassData
        {
            public TextureHandle src;
        }

        public SideStaticNoisePass(Material material)
        {
            _material = material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.camera.targetTexture != null) return;

            TextureHandle source = resourceData.cameraColor;
            if (!source.IsValid()) return;

            TextureDesc desc = renderGraph.GetTextureDesc(source);
            desc.name = "_SideStaticNoiseTmp";
            desc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(desc);

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<EffectPassData>(
                "SideStaticNoise_Effect", out EffectPassData passData))
            {
                passData.src = source;
                passData.material = _material;

                builder.UseTexture(passData.src, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((EffectPassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1f, 1f, 0f, 0f), data.material, 0);
                });
            }

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<CopyBackPassData>(
                "SideStaticNoise_CopyBack", out CopyBackPassData passData))
            {
                passData.src = destination;

                builder.UseTexture(passData.src, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((CopyBackPassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1f, 1f, 0f, 0f), 0, false);
                });
            }
        }
    }
}
