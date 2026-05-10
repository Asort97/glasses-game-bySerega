using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

[System.Serializable]
public class SplitChromaticAberrationFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        [ColorUsage(false, false)]
        public Color leftColor  = new Color(1f, 0f, 0f);
        [ColorUsage(false, false)]
        public Color rightColor = new Color(0f, 0f, 1f);
        [Range(0.001f, 0.05f)]
        public float strength = 0.01f;
    }

    public Settings settings = new Settings();

    private SplitChromaticAberrationPass _pass;
    private Material _material;

    private static readonly int PropLeftColor  = Shader.PropertyToID("_LeftColor");
    private static readonly int PropRightColor = Shader.PropertyToID("_RightColor");
    private static readonly int PropStrength   = Shader.PropertyToID("_Strength");

    public override void Create()
    {
        var shader = Shader.Find("Custom/SplitChromaticAberration");
        if (shader == null)
        {
            Debug.LogError("[SplitChromaticAberration] Shader not found!");
            return;
        }
        _material = CoreUtils.CreateEngineMaterial(shader);
        _pass = new SplitChromaticAberrationPass(_material);
        _pass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null || _pass == null) return;
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

        _material.SetColor(PropLeftColor,  settings.leftColor);
        _material.SetColor(PropRightColor, settings.rightColor);
        _material.SetFloat(PropStrength,   settings.strength);
        _pass.renderPassEvent = settings.renderPassEvent;
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _material != null)
            CoreUtils.Destroy(_material);
    }

    private class SplitChromaticAberrationPass : ScriptableRenderPass
    {
        private readonly Material _material;

        private class EffectPassData
        {
            public TextureHandle src;
            public Material      mat;
        }

        private class CopyBackPassData
        {
            public TextureHandle src;
        }

        public SplitChromaticAberrationPass(Material material)
        {
            _material = material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData   = frameData.Get<UniversalCameraData>();

            if (cameraData.camera.targetTexture != null) return;

            TextureHandle src = resourceData.cameraColor;
            if (!src.IsValid()) return;

            // Промежуточный RT с теми же параметрами что и src
            var desc         = renderGraph.GetTextureDesc(src);
            desc.name        = "_SplitCATmp";
            desc.clearBuffer = false;
            TextureHandle dst = renderGraph.CreateTexture(desc);

            // Проход 1: src → dst через шейдер хроматической аберрации
            using (var builder = renderGraph.AddRasterRenderPass<EffectPassData>(
                "SplitCA_Effect", out var pd))
            {
                pd.src = src;
                pd.mat = _material;

                builder.UseTexture(pd.src, AccessFlags.Read);
                builder.SetRenderAttachment(dst, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((EffectPassData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1f, 1f, 0f, 0f), d.mat, 0);
                });
            }

            // Проход 2: dst → src (копирование результата обратно в буфер камеры)
            using (var builder = renderGraph.AddRasterRenderPass<CopyBackPassData>(
                "SplitCA_CopyBack", out var pd))
            {
                pd.src = dst;

                builder.UseTexture(pd.src, AccessFlags.Read);
                builder.SetRenderAttachment(src, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((CopyBackPassData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1f, 1f, 0f, 0f), 0, false);
                });
            }
        }
    }
}

