using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public sealed class LensFocusBlurFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public sealed class Settings
    {
        public Shader shader;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

        [Header("Lens Focus")]
        public bool leftLensEnabled;
        public bool rightLensEnabled;
        public LayerMask leftLensLayer;
        public LayerMask rightLensLayer;

        [Header("Blur")]
        [Range(0f, 1f)] public float intensity = 1f;
        [Range(0.25f, 12f)] public float blurStrength = 4f;
        [Range(1, 4)] public int iterations = 2;
        [Range(1, 4)] public int downsample = 2;
        [Range(0f, 24f)] public float edgeSoftness = 6f;
        [Range(8f, 240f)] public float radialGradientRadius = 96f;
        [Range(0.25f, 4f)] public float radialGradientPower = 1.25f;
    }

    public Settings settings = new Settings();

    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int BlurStrengthId = Shader.PropertyToID("_BlurStrength");
    private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int RadialFalloffId = Shader.PropertyToID("_RadialFalloff");
    private static readonly int RadialGradientPowerId = Shader.PropertyToID("_RadialGradientPower");

    private Material _material;
    private LensFocusBlurPass _pass;

    public override void Create()
    {
        Shader shader = settings.shader != null
            ? settings.shader
            : Shader.Find("Hidden/LensFocusBlur");

        if (shader == null)
        {
            Debug.LogError("[LensFocusBlur] Shader not found.");
            return;
        }

        _material = CoreUtils.CreateEngineMaterial(shader);
        _pass = new LensFocusBlurPass(_material)
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null || _pass == null)
            return;

        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        int selectedLayers = 0;
        if (settings.leftLensEnabled)
            selectedLayers |= settings.leftLensLayer.value;
        if (settings.rightLensEnabled)
            selectedLayers |= settings.rightLensLayer.value;

        if (selectedLayers == 0 || settings.intensity <= 0f)
            return;

        _material.SetFloat(IntensityId, settings.intensity);
        _material.SetFloat(BlurStrengthId, settings.blurStrength);
        _material.SetFloat(EdgeSoftnessId, settings.edgeSoftness);
        _material.SetFloat(
            RadialFalloffId,
            settings.radialGradientRadius / Mathf.Clamp(settings.downsample, 1, 4));
        _material.SetFloat(RadialGradientPowerId, settings.radialGradientPower);

        _pass.Setup(
            selectedLayers,
            Mathf.Clamp(settings.iterations, 1, 4),
            Mathf.Clamp(settings.downsample, 1, 4));
        _pass.renderPassEvent = settings.renderPassEvent;
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _material != null)
            CoreUtils.Destroy(_material);
    }

    private sealed class LensFocusBlurPass : ScriptableRenderPass
    {
        private static readonly int BlurTextureId = Shader.PropertyToID("_LensFocusBlurTexture");
        private static readonly int MaskTextureId = Shader.PropertyToID("_LensFocusMaskTexture");
        private static readonly int RadialMaskTextureId = Shader.PropertyToID("_LensFocusRadialMaskTexture");

        private readonly Material _material;
        private readonly List<ShaderTagId> _shaderTags = new List<ShaderTagId>
        {
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("LightweightForward"),
            new ShaderTagId("Universal2D")
        };

        private int _lensLayerMask;
        private int _iterations = 1;
        private int _downsample = 1;

        private sealed class MaskPassData
        {
            public RendererListHandle rendererList;
        }

        private sealed class BlurPassData
        {
            public TextureHandle source;
            public Material material;
            public int materialPass;
        }

        private sealed class CompositePassData
        {
            public TextureHandle source;
            public TextureHandle blurred;
            public TextureHandle mask;
            public TextureHandle radialMask;
            public Material material;
        }

        private sealed class CopyPassData
        {
            public TextureHandle source;
        }

        public LensFocusBlurPass(Material material)
        {
            _material = material;
        }

        public void Setup(int lensLayerMask, int iterations, int downsample)
        {
            _lensLayerMask = lensLayerMask;
            _iterations = iterations;
            _downsample = downsample;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.camera.targetTexture != null)
                return;

            TextureHandle cameraColor = resourceData.cameraColor;
            if (!cameraColor.IsValid())
                return;

            TextureDesc fullResolutionDesc = renderGraph.GetTextureDesc(cameraColor);
            TextureHandle mask = CreateMaskTexture(renderGraph, fullResolutionDesc);
            RenderLensMask(renderGraph, frameData, resourceData, mask);
            TextureHandle radialMask = CreateRadialGradientMask(renderGraph, mask, fullResolutionDesc);

            TextureDesc blurDesc = fullResolutionDesc;
            blurDesc.name = "_LensFocusBlur";
            blurDesc.width = Mathf.Max(1, blurDesc.width / _downsample);
            blurDesc.height = Mathf.Max(1, blurDesc.height / _downsample);
            blurDesc.msaaSamples = MSAASamples.None;
            blurDesc.clearBuffer = false;

            TextureHandle blurred = cameraColor;
            for (int iteration = 0; iteration < _iterations; iteration++)
            {
                blurDesc.name = $"_LensFocusBlurH_{iteration}";
                TextureHandle horizontal = renderGraph.CreateTexture(blurDesc);
                AddBlurPass(renderGraph, blurred, horizontal, 0, $"LensFocusBlur_H_{iteration}");

                blurDesc.name = $"_LensFocusBlurV_{iteration}";
                TextureHandle vertical = renderGraph.CreateTexture(blurDesc);
                AddBlurPass(renderGraph, horizontal, vertical, 1, $"LensFocusBlur_V_{iteration}");
                blurred = vertical;
            }

            fullResolutionDesc.name = "_LensFocusComposite";
            fullResolutionDesc.msaaSamples = MSAASamples.None;
            fullResolutionDesc.clearBuffer = false;
            TextureHandle composite = renderGraph.CreateTexture(fullResolutionDesc);
            AddCompositePass(renderGraph, cameraColor, blurred, mask, radialMask, composite);
            AddCopyPass(renderGraph, composite, cameraColor);
        }

        private TextureHandle CreateMaskTexture(RenderGraph renderGraph, TextureDesc sourceDesc)
        {
            sourceDesc.name = "_LensFocusMask";
            sourceDesc.msaaSamples = MSAASamples.None;
            sourceDesc.clearBuffer = true;
            sourceDesc.clearColor = Color.black;
            return renderGraph.CreateTexture(sourceDesc);
        }

        private void RenderLensMask(
            RenderGraph renderGraph,
            ContextContainer frameData,
            UniversalResourceData resourceData,
            TextureHandle mask)
        {
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<MaskPassData>(
                "LensFocusBlur_Mask", out MaskPassData passData))
            {
                InitializeMaskRendererList(renderGraph, frameData, ref passData);
                builder.UseRendererList(passData.rendererList);
                builder.SetRenderAttachment(mask, 0, AccessFlags.Write);

                if (resourceData.activeDepthTexture.IsValid())
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);

                builder.AllowPassCulling(false);
                builder.SetRenderFunc((MaskPassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.black, 1f, 0);
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }
        }

        private void InitializeMaskRendererList(
            RenderGraph renderGraph,
            ContextContainer frameData,
            ref MaskPassData passData)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                _shaderTags,
                renderingData,
                cameraData,
                lightData,
                SortingCriteria.CommonTransparent);
            drawingSettings.overrideMaterial = _material;
            drawingSettings.overrideMaterialPassIndex = 3;

            FilteringSettings filteringSettings = new FilteringSettings(
                RenderQueueRange.all,
                _lensLayerMask);
            RendererListParams rendererListParams = new RendererListParams(
                renderingData.cullResults,
                drawingSettings,
                filteringSettings);
            passData.rendererList = renderGraph.CreateRendererList(rendererListParams);
        }

        private void AddBlurPass(
            RenderGraph renderGraph,
            TextureHandle source,
            TextureHandle destination,
            int materialPass,
            string passName)
        {
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<BlurPassData>(
                passName, out BlurPassData passData))
            {
                passData.source = source;
                passData.material = _material;
                passData.materialPass = materialPass;

                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((BlurPassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(
                        context.cmd,
                        data.source,
                        new Vector4(1f, 1f, 0f, 0f),
                        data.material,
                        data.materialPass);
                });
            }
        }

        private TextureHandle CreateRadialGradientMask(
            RenderGraph renderGraph,
            TextureHandle mask,
            TextureDesc fullResolutionDesc)
        {
            TextureDesc radialDesc = fullResolutionDesc;
            radialDesc.width = Mathf.Max(1, radialDesc.width / _downsample);
            radialDesc.height = Mathf.Max(1, radialDesc.height / _downsample);
            radialDesc.msaaSamples = MSAASamples.None;
            radialDesc.clearBuffer = false;

            TextureHandle current = mask;
            for (int iteration = 0; iteration < 2; iteration++)
            {
                radialDesc.name = $"_LensFocusRadialH_{iteration}";
                TextureHandle horizontal = renderGraph.CreateTexture(radialDesc);
                AddBlurPass(renderGraph, current, horizontal, 4, $"LensFocusRadial_H_{iteration}");

                radialDesc.name = $"_LensFocusRadialV_{iteration}";
                TextureHandle vertical = renderGraph.CreateTexture(radialDesc);
                AddBlurPass(renderGraph, horizontal, vertical, 5, $"LensFocusRadial_V_{iteration}");
                current = vertical;
            }

            return current;
        }

        private void AddCompositePass(
            RenderGraph renderGraph,
            TextureHandle source,
            TextureHandle blurred,
            TextureHandle mask,
            TextureHandle radialMask,
            TextureHandle destination)
        {
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<CompositePassData>(
                "LensFocusBlur_Composite", out CompositePassData passData))
            {
                passData.source = source;
                passData.blurred = blurred;
                passData.mask = mask;
                passData.radialMask = radialMask;
                passData.material = _material;

                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(blurred, AccessFlags.Read);
                builder.UseTexture(mask, AccessFlags.Read);
                builder.UseTexture(radialMask, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((CompositePassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(BlurTextureId, data.blurred);
                    context.cmd.SetGlobalTexture(MaskTextureId, data.mask);
                    context.cmd.SetGlobalTexture(RadialMaskTextureId, data.radialMask);
                    Blitter.BlitTexture(
                        context.cmd,
                        data.source,
                        new Vector4(1f, 1f, 0f, 0f),
                        data.material,
                        2);
                });
            }
        }

        private static void AddCopyPass(
            RenderGraph renderGraph,
            TextureHandle source,
            TextureHandle destination)
        {
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<CopyPassData>(
                "LensFocusBlur_CopyBack", out CopyPassData passData))
            {
                passData.source = source;
                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(
                        context.cmd,
                        data.source,
                        new Vector4(1f, 1f, 0f, 0f),
                        0,
                        false);
                });
            }
        }
    }
}
