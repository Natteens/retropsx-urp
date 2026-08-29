using UnityEngine;

namespace RetroPSX
{
    [CreateAssetMenu(menuName = "RetroPSX/Pipeline Profile", fileName = "RetroPSXPipelineProfile")]
    public sealed class RetroPSXPipelineProfile : ScriptableObject
    {
        [SerializeField] private bool enabled = true;
        [SerializeField, Tooltip("Controls which RetroPSX stages Unity's Scene View previews.")]
        private RetroSceneViewMode sceneViewPreview = RetroSceneViewMode.WorldEffects;
        [SerializeField] private RetroRasterProfile raster;
        [SerializeField] private RetroGeometryProfile geometry;
        [SerializeField] private RetroColorProfile color;
        [SerializeField] private RetroLightingProfile lighting;
        [SerializeField] private RetroFogProfile fog;
        [SerializeField] private RetroVolumetricProfile volumetrics;
        [SerializeField] private RetroDisplayProfile display;
        [SerializeField] private RetroDebugProfile debug;
        [SerializeField] private RetroUIProfile ui;

        public bool Enabled => enabled;
        public RetroSceneViewMode SceneViewPreview => sceneViewPreview;
        public RetroRasterProfile Raster => raster;
        public RetroGeometryProfile Geometry => geometry;
        public RetroColorProfile Color => color;
        public RetroLightingProfile Lighting => lighting;
        public RetroFogProfile Fog => fog;
        public RetroVolumetricProfile Volumetrics => volumetrics;
        public RetroDisplayProfile Display => display;
        public RetroDebugProfile Debug => debug;
        public RetroUIProfile UI => ui;
        // UI composition is optional so profiles created before the UI module continue to render normally.
        public bool IsComplete => raster != null && geometry != null && color != null && lighting != null && fog != null && volumetrics != null && display != null && debug != null;
    }
}
