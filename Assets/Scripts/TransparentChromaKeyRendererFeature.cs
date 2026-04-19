using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TransparentChromaKeyRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Color keyColor = new Color(1f, 0f, 1f, 1f);

        [Range(0f, 0.1f)]
        public float glowThreshold = 0.01f;
    }

    public Settings settings = new Settings();

    private Material _material;
    private TransparentChromaKeyPass _pass;

    public override void Create()
    {
        Shader shader = Shader.Find("Hidden/TransparentChromaKey");
        if (shader == null)
            return;

        _material = CoreUtils.CreateEngineMaterial(shader);
        _pass = new TransparentChromaKeyPass(_material)
        {
            renderPassEvent = RenderPassEvent.AfterRendering
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null || _pass == null)
            return;

        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        _material.SetColor("_KeyColor", settings.keyColor);
        _material.SetFloat("_GlowThreshold", settings.glowThreshold);
        renderer.EnqueuePass(_pass);
    }

    #pragma warning disable 618
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (_pass == null)
            return;

        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        _pass.Setup(renderer.cameraColorTargetHandle);
    }
    #pragma warning restore 618

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pass?.Dispose();

            if (_material != null)
                CoreUtils.Destroy(_material);
        }
    }

    private class TransparentChromaKeyPass : ScriptableRenderPass
    {
        private readonly Material _material;
        private RTHandle _source;
        private RTHandle _tempTexture;

        public TransparentChromaKeyPass(Material material)
        {
            _material = material;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup(RTHandle source)
        {
            _source = source;
        }

        [System.Obsolete]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateHandleIfNeeded(ref _tempTexture, descriptor, FilterMode.Bilinear, name: "_TransparentChromaKeyTemp");
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || _source == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("TransparentChromaKey");

            Blitter.BlitCameraTexture(cmd, _source, _tempTexture);
            Blitter.BlitCameraTexture(cmd, _tempTexture, _source, _material, 0);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _tempTexture?.Release();
        }
    }
}