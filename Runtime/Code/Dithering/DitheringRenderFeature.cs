using UnityEngine;
using UnityEngine. Rendering;
using UnityEngine. Rendering.Universal;
using UnityEngine. Rendering.RenderGraphModule;

namespace RetroPSXURP.Code. Dithering
{
    public class DitheringRenderFeature : ScriptableRendererFeature
    {
        [HideInInspector] public Shader ditheringShader;
        DitheringPass ditheringPass;

        public override void Create()
        {
            if (ditheringShader == null)
            {
                Debug.LogError("Dithering Shader is not assigned in the Renderer Feature!");
                return;
            }

            ditheringPass = new DitheringPass(ditheringShader);
            ditheringPass. renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (ditheringPass != null && renderingData. cameraData.cameraType == CameraType.Game)
            {
                renderer.EnqueuePass(ditheringPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            ditheringPass?.Dispose();
        }
    }

    public class DitheringPass : ScriptableRenderPass
    {
        private Material ditheringMaterial;

        static readonly int PatternIndex = Shader.PropertyToID("_PatternIndex");
        static readonly int DitherThreshold = Shader. PropertyToID("_DitherThreshold");
        static readonly int DitherStrength = Shader. PropertyToID("_DitherStrength");
        static readonly int DitherScale = Shader.PropertyToID("_DitherScale");

        public DitheringPass(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogError("Dithering Shader is null!");
                return;
            }
            this.ditheringMaterial = CoreUtils.CreateEngineMaterial(shader);
        }

        private class PassData
        {
            internal TextureHandle source;
            internal Material material;
            internal Dithering ditheringSettings;
        }

        private static void ExecutePass(PassData data, RasterGraphContext context)
        {
            if (data. material == null || data.ditheringSettings == null) return;

            data.material.SetInt(PatternIndex, data.ditheringSettings.patternIndex. value);
            data.material.SetFloat(DitherThreshold, data.ditheringSettings.ditherThreshold.value);
            data. material.SetFloat(DitherStrength, data.ditheringSettings.ditherStrength.value);
            data.material.SetFloat(DitherScale, data.ditheringSettings.ditherScale. value);

            Blitter.BlitTexture(context. cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (ditheringMaterial == null)
            {
                Debug.LogWarning("Dithering Material is null, skipping pass");
                return;
            }

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            if (!cameraData.postProcessEnabled) return;

            var stack = VolumeManager.instance. stack;
            var ditheringSettings = stack.GetComponent<Dithering>();
            if (ditheringSettings == null || !ditheringSettings.IsActive()) return;

            TextureHandle cameraTex = resourceData.activeColorTexture;
            if (!cameraTex.IsValid()) return;

            var desc = renderGraph. GetTextureDesc(cameraTex);
            desc.name = "_DitheringDestination";
            desc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Dithering Effect Pass", out var passData))
            {
                passData.source = cameraTex;
                passData.material = ditheringMaterial;
                passData.ditheringSettings = ditheringSettings;

                builder. UseTexture(passData.source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }

            resourceData. cameraColor = destination;
        }

        public void Dispose()
        {
            CoreUtils. Destroy(ditheringMaterial);
        }
    }
}