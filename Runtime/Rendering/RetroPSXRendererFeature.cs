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

        public RetroPSXPipelineProfile Profile => profile;

        public override void Create()
        {
            DestroyMaterials();
            resolveMaterial = CreateMaterial("Hidden/RetroPSX/Resolve");
            presentationMaterial = CreateMaterial("Hidden/RetroPSX/Presentation");
            volumetricMaterial = CreateMaterial("Hidden/RetroPSX/VolumetricRaymarch");
            volumetricCompositeMaterial = CreateMaterial("Hidden/RetroPSX/VolumetricComposite");
            crtMaterial = CreateMaterial("Hidden/RetroPSX/CRT");

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
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (setupPass == null || resetPass == null)
                return;

            bool overlay = renderingData.cameraData.renderType == CameraRenderType.Overlay;
            bool profileReady = profile != null && profile.Enabled && profile.IsComplete;
            RetroCameraPolicy policy = profileReady
                ? RetroCameraUtility.ResolvePolicy(
                    renderingData.cameraData.cameraType, renderGameCameras, profile.SceneViewPreview, overlay)
                : default;
            setupPass.SetProfile(policy.WorldEffects ? profile : null, policy.FullPipeline);
            renderer.EnqueuePass(setupPass);

            bool needsImagePass = policy.FullPipeline || (policy.WorldEffects && profile.Volumetrics.Enabled);
            if (needsImagePass && postPass != null && postPass.HasRequiredMaterials)
            {
                postPass.SetProfile(profile, policy.FullPipeline);
                postPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
                renderer.EnqueuePass(postPass);
            }

            resetPass.SetProfile(null, false);
            renderer.EnqueuePass(resetPass);
        }

        protected override void Dispose(bool disposing)
        {
            DestroyMaterials();
            base.Dispose(disposing);
        }

        private static Material CreateMaterial(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
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
