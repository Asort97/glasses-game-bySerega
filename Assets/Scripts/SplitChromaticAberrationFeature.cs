using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
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
        public LayerMask excludedLayers = 0;
    }

    public Settings settings = new Settings();

    private SplitChromaticAberrationPass _pass;
    private Material _material;

    private static readonly int PropLeftColor  = Shader.PropertyToID("_LeftColor");
    private static readonly int PropRightColor = Shader.PropertyToID("_RightColor");
    private static readonly int PropStrength   = Shader.PropertyToID("_Strength");
    private static readonly int PropMaskTexture = Shader.PropertyToID("_SplitCAMaskTexture");
    private static readonly int PropExcludeAll = Shader.PropertyToID("_ExcludeAll");

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
        _material.SetFloat(PropExcludeAll, settings.excludedLayers.value == -1 ? 1f : 0f);
        _pass.ExcludedLayerMask = settings.excludedLayers;
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
        private readonly List<ShaderTagId> _shaderTagIds = new List<ShaderTagId>
        {
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("LightweightForward"),
            new ShaderTagId("Universal2D")
        };

        public LayerMask ExcludedLayerMask { get; set; } = 0;

        private class EffectPassData
        {
            public TextureHandle src;
            public TextureHandle mask;
            public Material      mat;
        }

        private class MaskPassData
        {
            public RendererListHandle rendererList;
        }

        private class CopyBackPassData
        {
            public TextureHandle src;
        }

        public SplitChromaticAberrationPass(Material material)
        {
            _material = material;
        }

        private void InitMaskRendererList(RenderGraph renderGraph, ContextContainer frameData, ref MaskPassData passData)
        {
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();

            var drawingSettings = RenderingUtils.CreateDrawingSettings(
                _shaderTagIds,
                renderingData,
                cameraData,
                lightData,
                SortingCriteria.CommonTransparent);
            drawingSettings.overrideMaterial = _material;
            drawingSettings.overrideMaterialPassIndex = 1;

            var filteringSettings = new FilteringSettings(RenderQueueRange.all, ExcludedLayerMask);
            var rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
            passData.rendererList = renderGraph.CreateRendererList(rendererListParams);
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

            var maskDesc = desc;
            maskDesc.name = "_SplitCAMask";
            maskDesc.clearBuffer = true;
            maskDesc.clearColor = Color.black;
            TextureHandle mask = renderGraph.CreateTexture(maskDesc);

            // Pass 0: render excluded layers into a mask texture.
            using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>(
                "SplitCA_LayerMask", out var pd))
            {
                InitMaskRendererList(renderGraph, frameData, ref pd);

                builder.UseRendererList(pd.rendererList);
                builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                if (resourceData.activeDepthTexture.IsValid())
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(mask, PropMaskTexture);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((MaskPassData d, RasterGraphContext ctx) =>
                {
                    ctx.cmd.ClearRenderTarget(RTClearFlags.Color, Color.black, 1f, 0);
                    ctx.cmd.DrawRendererList(d.rendererList);
                });
            }

            // Проход 1: src → dst через шейдер хроматической аберрации
            using (var builder = renderGraph.AddRasterRenderPass<EffectPassData>(
                "SplitCA_Effect", out var pd))
            {
                pd.src = src;
                pd.mask = mask;
                pd.mat = _material;

                builder.UseTexture(pd.src, AccessFlags.Read);
                builder.UseTexture(pd.mask, AccessFlags.Read);
                builder.SetRenderAttachment(dst, 0);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((EffectPassData d, RasterGraphContext ctx) =>
                {
                    ctx.cmd.SetGlobalTexture(PropMaskTexture, d.mask);
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

