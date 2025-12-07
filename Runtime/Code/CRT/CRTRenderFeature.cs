using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine. Rendering.RenderGraphModule;

namespace RetroPSXURP.Code.CRT
{
    public class CRTRenderFeature : ScriptableRendererFeature
    {
        [HideInInspector] public Shader crtShader;
        CRTPass crtPass;

        public override void Create()
        {
            if (crtShader == null)
            {
                Debug. LogError("CRT Shader is not assigned in the Renderer Feature!");
                return;
            }

            crtPass = new CRTPass(crtShader);
            crtPass.renderPassEvent = RenderPassEvent. BeforeRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (crtPass != null && renderingData. cameraData.cameraType == CameraType.Game)
            {
                renderer.EnqueuePass(crtPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            crtPass?.Dispose();
        }
    }

    public class CRTPass : ScriptableRenderPass
    {
        private Material crtMaterial;

        static readonly int ScanLinesWeight = Shader.PropertyToID("_ScanlinesWeight");
        static readonly int NoiseWeight = Shader.PropertyToID("_NoiseWeight");
        static readonly int ScreenBendX = Shader.PropertyToID("_ScreenBendX");
        static readonly int ScreenBendY = Shader.PropertyToID("_ScreenBendY");
        static readonly int VignetteAmount = Shader.PropertyToID("_VignetteAmount");
        static readonly int VignetteSize = Shader.PropertyToID("_VignetteSize");
        static readonly int VignetteRounding = Shader.PropertyToID("_VignetteRounding");
        static readonly int VignetteSmoothing = Shader.PropertyToID("_VignetteSmoothing");
        static readonly int ScanLinesDensity = Shader.PropertyToID("_ScanLinesDensity");
        static readonly int ScanLinesSpeed = Shader.PropertyToID("_ScanLinesSpeed");
        static readonly int NoiseAmount = Shader.PropertyToID("_NoiseAmount");
        static readonly int ChromaticRed = Shader. PropertyToID("_ChromaticRed");
        static readonly int ChromaticGreen = Shader. PropertyToID("_ChromaticGreen");
        static readonly int ChromaticBlue = Shader. PropertyToID("_ChromaticBlue");
        static readonly int GrilleOpacity = Shader. PropertyToID("_GrilleOpacity");
        static readonly int GrilleCounterOpacity = Shader. PropertyToID("_GrilleCounterOpacity");
        static readonly int GrilleResolution = Shader. PropertyToID("_GrilleResolution");
        static readonly int GrilleCounterResolution = Shader. PropertyToID("_GrilleCounterResolution");
        static readonly int GrilleBrightness = Shader.PropertyToID("_GrilleBrightness");
        static readonly int GrilleUvRotation = Shader. PropertyToID("_GrilleUvRotation");
        static readonly int GrilleUvMidPoint = Shader.PropertyToID("_GrilleUvMidPoint");
        static readonly int GrilleShift = Shader.PropertyToID("_GrilleShift");

        public CRTPass(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogError("CRT Shader is null!");
                return;
            }
            this. crtMaterial = CoreUtils.CreateEngineMaterial(shader);
        }

        private class PassData
        {
            internal TextureHandle source;
            internal Material material;
            internal Crt crtSettings;
        }

        private static void ExecutePass(PassData data, RasterGraphContext context)
        {
            if (data.material == null || data.crtSettings == null) return;

            data.material.SetFloat(ScanLinesWeight, data.crtSettings.scanlinesWeight.value);
            data. material.SetFloat(NoiseWeight, data.crtSettings. noiseWeight.value);
            data. material.SetFloat(ScreenBendX, data.crtSettings.screenBendX.value);
            data.material. SetFloat(ScreenBendY, data.crtSettings. screenBendY. value);
            data.material.SetFloat(VignetteAmount, data.crtSettings.vignetteAmount.value);
            data. material.SetFloat(VignetteSize, data.crtSettings.vignetteSize.value);
            data.material. SetFloat(VignetteRounding, data.crtSettings.vignetteRounding.value);
            data.material. SetFloat(VignetteSmoothing, data.crtSettings.vignetteSmoothing.value);
            data. material.SetFloat(ScanLinesDensity, data.crtSettings.scanlinesDensity.value);
            data. material.SetFloat(ScanLinesSpeed, data.crtSettings.scanlinesSpeed.value);
            data. material.SetFloat(NoiseAmount, data.crtSettings. noiseAmount.value);
            data. material.SetVector(ChromaticRed, data.crtSettings.chromaticRed. value);
            data.material.SetVector(ChromaticGreen, data.crtSettings.chromaticGreen.value);
            data. material.SetVector(ChromaticBlue, data.crtSettings.chromaticBlue.value);
            data.material.SetFloat(GrilleOpacity, data.crtSettings.grilleOpacity.value);
            data. material.SetFloat(GrilleCounterOpacity, data. crtSettings.grilleCounterOpacity.value);
            data.material. SetFloat(GrilleResolution, data.crtSettings. grilleResolution.value);
            data.material.SetFloat(GrilleCounterResolution, data.crtSettings.grilleCounterResolution.value);
            data. material.SetFloat(GrilleBrightness, data. crtSettings.grilleBrightness.value);
            data.material.SetFloat(GrilleUvRotation, data.crtSettings.grilleUvRotation.value);
            data.material. SetFloat(GrilleUvMidPoint, data.crtSettings.grilleUvMidPoint.value);
            data.material. SetVector(GrilleShift, data.crtSettings.grilleShift.value);

            Blitter. BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data. material, 0);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (crtMaterial == null)
            {
                Debug.LogWarning("CRT Material is null, skipping pass");
                return;
            }

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData. Get<UniversalResourceData>();

            if (!cameraData.postProcessEnabled) return;

            var stack = VolumeManager.instance.stack;
            var crtSettings = stack. GetComponent<Crt>();
            if (crtSettings == null || !crtSettings.IsActive()) return;

            TextureHandle cameraTex = resourceData.activeColorTexture;
            if (!cameraTex.IsValid()) return;

            var desc = renderGraph. GetTextureDesc(cameraTex);
            desc.name = "_CRTDestination";
            desc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("CRT Effect Pass", out var passData))
            {
                passData.source = cameraTex;
                passData.material = crtMaterial;
                passData. crtSettings = crtSettings;

                builder.UseTexture(passData.source, AccessFlags.Read);
                builder. SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder. SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }

            resourceData.cameraColor = destination;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(crtMaterial);
        }
    }
}