using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PSX
{
    public class CRTRenderFeature : ScriptableRendererFeature
    {
        CRTPass crtPass;

        public override void Create()
        {
            crtPass = new CRTPass(RenderPassEvent.BeforeRenderingPostProcessing);
        }

        //ScripstableRendererFeature is an abstract class, you need this method
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(crtPass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            crtPass.Setup(renderer.cameraColorTargetHandle);
        }
    }


    public class CRTPass : ScriptableRenderPass
    {
        private static readonly string shaderPath = "PostEffect/CRTShader";
        static readonly string k_RenderTag = "Render CRT Effects";
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        static readonly int TempTargetId = Shader.PropertyToID("_TempTargetCRT");

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

        static readonly int ChromaticRed = Shader.PropertyToID("_ChromaticRed");
        static readonly int ChromaticGreen = Shader.PropertyToID("_ChromaticGreen");
        static readonly int ChromaticBlue = Shader.PropertyToID("_ChromaticBlue");
        
        static readonly int GrilleOpacity = Shader.PropertyToID("_GrilleOpacity");
        static readonly int GrilleCounterOpacity = Shader.PropertyToID("_GrilleCounterOpacity");
        static readonly int GrilleResolution = Shader.PropertyToID("_GrilleResolution");
        static readonly int GrilleCounterResolution = Shader.PropertyToID("_GrilleCounterResolution");
        static readonly int GrilleBrightness = Shader.PropertyToID("_GrilleBrightness");
        static readonly int GrilleUvRotation = Shader.PropertyToID("_GrilleUvRotation");
        static readonly int GrilleUvMidPoint = Shader.PropertyToID("_GrilleUvMidPoint");
        static readonly int GrilleShift = Shader.PropertyToID("_GrilleShift");

        Crt m_Crt;
        Material crtMaterial;
        RenderTargetIdentifier currentTarget;

        public CRTPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            var shader = Shader.Find(shaderPath);
            if (shader == null)
            {
                Debug.LogError("Shader not found (crt).");
                return;
            }

            this.crtMaterial = CoreUtils.CreateEngineMaterial(shader);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (this.crtMaterial == null)
            {
                Debug.LogError("Material not created.");
                return;
            }

            if (!renderingData.cameraData.postProcessEnabled) return;

            var stack = VolumeManager.instance.stack;

            this.m_Crt = stack.GetComponent<Crt>();
            if (this.m_Crt == null)
            {
                return;
            }

            if (!this.m_Crt.IsActive())
            {
                return;
            }

            var cmd = CommandBufferPool.Get(k_RenderTag);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Setup(in RenderTargetIdentifier currentTarget)
        {
            this.currentTarget = currentTarget;
        }

        void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            var source = currentTarget;
            int destination = TempTargetId;

            //getting camera width and height 
            var w = cameraData.camera.scaledPixelWidth;
            var h = cameraData.camera.scaledPixelHeight;

            //setting parameters here 
            cameraData.camera.depthTextureMode = cameraData.camera.depthTextureMode | DepthTextureMode.Depth;

            this.crtMaterial.SetFloat(ScanLinesWeight, this.m_Crt.scanlinesWeight.value);
            this.crtMaterial.SetFloat(NoiseWeight, this.m_Crt.noiseWeight.value);
            
            this.crtMaterial.SetFloat(ScreenBendX, this.m_Crt.screenBendX.value);
            this.crtMaterial.SetFloat(ScreenBendY, this.m_Crt.screenBendY.value);
            this.crtMaterial.SetFloat(VignetteAmount, this.m_Crt.vignetteAmount.value);
            this.crtMaterial.SetFloat(VignetteSize, this.m_Crt.vignetteSize.value);
            this.crtMaterial.SetFloat(VignetteRounding, this.m_Crt.vignetteRounding.value);
            this.crtMaterial.SetFloat(VignetteSmoothing, this.m_Crt.vignetteSmoothing.value);

            this.crtMaterial.SetFloat(ScanLinesDensity, this.m_Crt.scanlinesDensity.value);
            this.crtMaterial.SetFloat(ScanLinesSpeed, this.m_Crt.scanlinesSpeed.value);
            this.crtMaterial.SetFloat(NoiseAmount, this.m_Crt.noiseAmount.value);

            this.crtMaterial.SetVector(ChromaticRed, this.m_Crt.chromaticRed.value);
            this.crtMaterial.SetVector(ChromaticGreen, this.m_Crt.chromaticGreen.value);
            this.crtMaterial.SetVector(ChromaticBlue, this.m_Crt.chromaticBlue.value);

            this.crtMaterial.SetFloat(GrilleOpacity, this.m_Crt.grilleOpacity.value);
            this.crtMaterial.SetFloat(GrilleCounterOpacity, this.m_Crt.grilleCounterOpacity.value);
            this.crtMaterial.SetFloat(GrilleResolution, this.m_Crt.grilleResolution.value);
            this.crtMaterial.SetFloat(GrilleCounterResolution, this.m_Crt.grilleCounterResolution.value);
            this.crtMaterial.SetFloat(GrilleBrightness, this.m_Crt.grilleBrightness.value);
            this.crtMaterial.SetFloat(GrilleUvRotation, this.m_Crt.grilleUvRotation.value);
            this.crtMaterial.SetFloat(GrilleUvMidPoint, this.m_Crt.grilleUvMidPoint.value);
            this.crtMaterial.SetVector(GrilleShift, this.m_Crt.grilleShift.value);
            
            int shaderPass = 0;
            cmd.SetGlobalTexture(MainTexId, source);
            cmd.GetTemporaryRT(destination, w, h, 0, FilterMode.Point, RenderTextureFormat.Default);
            cmd.Blit(source, destination);
            cmd.Blit(destination, source, this.crtMaterial, shaderPass);
        }
    }
}