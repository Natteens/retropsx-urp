using UnityEngine;
using UnityEngine. Rendering;
using UnityEngine. Rendering.Universal;
using UnityEngine. Rendering.RenderGraphModule;

namespace RetroPSXURP. Code. Pixelation
{
    public class PixelationRenderFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader pixelationShader;
        PixelationPass pixelationPass;

        public override void Create()
        {
            if (pixelationShader == null)
            {
                Debug.LogError("Pixelation Shader is not assigned in the Renderer Feature!");
                return;
            }

            pixelationPass = new PixelationPass(pixelationShader);
            pixelationPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (pixelationPass != null && renderingData.cameraData.cameraType == CameraType.Game)
            {
                renderer.EnqueuePass(pixelationPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            pixelationPass?. Dispose();
        }
    }

    public class PixelationPass : ScriptableRenderPass
    {
        private Material pixelationMaterial;

        static readonly int WidthPixelation = Shader. PropertyToID("_WidthPixelation");
        static readonly int HeightPixelation = Shader.PropertyToID("_HeightPixelation");
        static readonly int ColorPrecision = Shader. PropertyToID("_ColorPrecision");

        public PixelationPass(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogError("Pixelation Shader is null!");
                return;
            }
            this.pixelationMaterial = CoreUtils.CreateEngineMaterial(shader);
        }

        private class PassData
        {
            internal TextureHandle source;
            internal Material material;
            internal Pixelation pixelationSettings;
        }

        private static void ExecutePass(PassData data, RasterGraphContext context)
        {
            if (data.material == null || data.pixelationSettings == null) return;

            data.material.SetFloat(WidthPixelation, data. pixelationSettings. widthPixelation. value);
            data.material.SetFloat(HeightPixelation, data.pixelationSettings.heightPixelation.value);
            data. material.SetFloat(ColorPrecision, data.pixelationSettings.colorPrecision.value);

            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (pixelationMaterial == null)
            {
                Debug. LogWarning("Pixelation Material is null, skipping pass");
                return;
            }

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            if (! cameraData.postProcessEnabled) return;

            var stack = VolumeManager.instance.stack;
            var pixelationSettings = stack.GetComponent<Pixelation>();
            if (pixelationSettings == null || ! pixelationSettings. IsActive()) return;

            TextureHandle cameraTex = resourceData.activeColorTexture;
            if (! cameraTex.IsValid()) return;

            var desc = renderGraph.GetTextureDesc(cameraTex);
            desc.name = "_PixelationDestination";
            desc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Pixelation Effect Pass", out var passData))
            {
                passData.source = cameraTex;
                passData.material = pixelationMaterial;
                passData. pixelationSettings = pixelationSettings;

                builder.UseTexture(passData.source, AccessFlags.Read);
                builder. SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }

            resourceData.cameraColor = destination;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(pixelationMaterial);
        }
    }
}