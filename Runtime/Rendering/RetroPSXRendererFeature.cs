using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RetroPSX.Rendering
{
    /// <summary>Coordinates the RetroPSX material state and image passes for each eligible camera.</summary>
    public sealed class RetroPSXRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private RetroPSXPipelineProfile profile;
        [SerializeField] private bool renderGameCameras = true;

        private Material resolveMaterial;
        private Material presentationMaterial;
        private Material volumetricMaterial;
        private Material volumetricCompositeMaterial;
        private Material crtMaterial;
        private RetroPSXGlobalSetupPass setupPass;
        private RetroPSXGlobalSetupPass resetPass;
        private RetroPSXPostPass postPass;
        private RetroPSXNativeUIPass nativeUIPass;

        public RetroPSXPipelineProfile Profile => profile;

        public override void Create()
        {
            DestroyMaterials();
            RetroPSXShaderResources shaders = Resources.Load<RetroPSXShaderResources>(RetroPSXShaderResources.ResourcePath);
            resolveMaterial = CreateMaterial(shaders != null ? shaders.Resolve : null);
            presentationMaterial = CreateMaterial(shaders != null ? shaders.Presentation : null);
            volumetricMaterial = CreateMaterial(shaders != null ? shaders.Volumetric : null);
            volumetricCompositeMaterial = CreateMaterial(shaders != null ? shaders.VolumetricComposite : null);
            crtMaterial = CreateMaterial(shaders != null ? shaders.CRT : null);

            setupPass = new RetroPSXGlobalSetupPass("RetroPSX / Material Globals")
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingOpaques
            };
            resetPass = new RetroPSXGlobalSetupPass("RetroPSX / Reset Material Globals")
            {
                renderPassEvent = RenderPassEvent.AfterRendering
            };
            postPass = new RetroPSXPostPass(resolveMaterial, presentationMaterial, volumetricMaterial, volumetricCompositeMaterial, crtMaterial)
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
            };
            nativeUIPass = new RetroPSXNativeUIPass
            {
                renderPassEvent = (RenderPassEvent)((int)RenderPassEvent.BeforeRenderingPostProcessing + 1)
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (setupPass == null || resetPass == null)
                return;

            bool overlay = renderingData.cameraData.renderType == CameraRenderType.Overlay;
            bool profileReady = profile != null && profile.Enabled && profile.IsComplete;
            RetroCameraPolicy policy = profileReady
                ? RetroCameraUtility.ResolvePolicy(
                    renderingData.cameraData.cameraType,
                    renderGameCameras,
                    profile.SceneViewPreview,
                    overlay,
                    renderingData.cameraData.targetTexture != null,
                    renderingData.cameraData.isAlphaOutputEnabled)
                : default;
            setupPass.SetProfile(policy.WorldEffects ? profile : null, policy.FullPipeline);
            renderer.EnqueuePass(setupPass);

            bool needsImagePass = policy.FullPipeline || (policy.WorldEffects && profile.Volumetrics.Enabled);
            if (needsImagePass && postPass != null && postPass.HasRequiredMaterials)
            {
                postPass.SetProfile(profile, policy.FullPipeline, policy.PreserveAlpha);
                postPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
                renderer.EnqueuePass(postPass);
            }

            bool supportsNativeUI = profile != null
                && profile.IsComplete
                && profile.UI != null
                && RetroCameraUtility.SupportsNativeWorldSpaceUI(renderingData.cameraData.cameraType, overlay);
            if (supportsNativeUI && nativeUIPass != null)
            {
                nativeUIPass.SetProfile(profile);
                nativeUIPass.ConfigureInput(ScriptableRenderPassInput.Depth);
                renderer.EnqueuePass(nativeUIPass);
            }

            resetPass.SetProfile(null, false);
            renderer.EnqueuePass(resetPass);
        }

        protected override void Dispose(bool disposing)
        {
            DestroyMaterials();
            base.Dispose(disposing);
        }

        private static Material CreateMaterial(Shader shader)
        {
            return shader != null ? CoreUtils.CreateEngineMaterial(shader) : null;
        }

        private void DestroyMaterials()
        {
            CoreUtils.Destroy(resolveMaterial);
            CoreUtils.Destroy(presentationMaterial);
            CoreUtils.Destroy(volumetricMaterial);
            CoreUtils.Destroy(volumetricCompositeMaterial);
            CoreUtils.Destroy(crtMaterial);
            resolveMaterial = null;
            presentationMaterial = null;
            volumetricMaterial = null;
            volumetricCompositeMaterial = null;
            crtMaterial = null;
        }
    }
}
