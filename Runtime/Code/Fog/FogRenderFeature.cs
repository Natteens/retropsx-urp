using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering. RenderGraphModule;

namespace RetroPSXURP. Code.Fog
{
    public class FogRenderFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader fogShader;
        FogPass fogPass;

        public override void Create()
        {
            if (fogShader == null)
            {
                Debug.LogError("Fog Shader is not assigned in the Renderer Feature!");
                return;
            }

            fogPass = new FogPass(fogShader);
            fogPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (fogPass != null && renderingData.cameraData. cameraType == CameraType.Game)
            {
                renderer.EnqueuePass(fogPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            fogPass?. Dispose();
        }
    }

    public class FogPass : ScriptableRenderPass
    {
        private Material fogMaterial;

        static readonly int FogDensity = Shader.PropertyToID("_FogDensity");
        static readonly int FogDistance = Shader.PropertyToID("_FogDistance");
        static readonly int FogColor = Shader. PropertyToID("_FogColor");
        static readonly int AmbientColor = Shader.PropertyToID("_AmbientColor");
        static readonly int FogNear = Shader.PropertyToID("_FogNear");
        static readonly int FogFar = Shader.PropertyToID("_FogFar");
        static readonly int FogAltScale = Shader.PropertyToID("_FogAltScale");
        static readonly int FogThinning = Shader.PropertyToID("_FogThinning");
        static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");
        static readonly int NoiseStrength = Shader.PropertyToID("_NoiseStrength");

        public FogPass(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogError("Fog Shader is null!");
                return;
            }
            this.fogMaterial = CoreUtils.CreateEngineMaterial(shader);
        }

        private class PassData
        {
            internal TextureHandle source;
            internal Material material;
            internal Fog fogSettings;
        }

        private static void ExecutePass(PassData data, RasterGraphContext context)
        {
            if (data. material == null || data.fogSettings == null) return;

            data.material.SetFloat(FogDensity, data.fogSettings.fogDensity.value);
            data. material.SetFloat(FogDistance, data.fogSettings.fogDistance.value);
            data.material. SetColor(FogColor, data.fogSettings.fogColor. value);
            data.material.SetColor(AmbientColor, data.fogSettings.ambientColor. value);
            data.material.SetFloat(FogNear, data.fogSettings.fogNear. value);
            data.material.SetFloat(FogFar, data.fogSettings.fogFar. value);
            data.material.SetFloat(FogAltScale, data.fogSettings.fogAltScale.value);
            data.material. SetFloat(FogThinning, data.fogSettings.fogThinning.value);
            data. material.SetFloat(NoiseScale, data.fogSettings.noiseScale.value);
            data.material.SetFloat(NoiseStrength, data.fogSettings.noiseStrength.value);

            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (fogMaterial == null)
            {
                Debug.LogWarning("Fog Material is null, skipping pass");
                return;
            }

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            if (!cameraData.postProcessEnabled) return;

            var stack = VolumeManager.instance. stack;
            var fogSettings = stack.GetComponent<Fog>();
            if (fogSettings == null || !fogSettings.IsActive()) return;

            TextureHandle cameraTex = resourceData.activeColorTexture;
            if (!cameraTex.IsValid()) return;

            var desc = renderGraph. GetTextureDesc(cameraTex);
            desc.name = "_FogDestination";
            desc. clearBuffer = false;
            TextureHandle destination = renderGraph. CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Fog Effect Pass", out var passData))
            {
                passData.source = cameraTex;
                passData. material = fogMaterial;
                passData.fogSettings = fogSettings;

                builder.UseTexture(passData.source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }

            resourceData.cameraColor = destination;
        }

        public void Dispose()
        {
            CoreUtils. Destroy(fogMaterial);
        }
    }
}