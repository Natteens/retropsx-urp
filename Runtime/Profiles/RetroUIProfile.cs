using UnityEngine;

namespace RetroPSX
{
    [CreateAssetMenu(menuName = "RetroPSX/Profiles/UI", fileName = "RetroUIProfile")]
    public sealed class RetroUIProfile : ScriptableObject
    {
        [SerializeField, Tooltip("The default policy for world-space UI Toolkit panels. Native panels are drawn after RetroPSX presentation on the Native UI layer.")]
        private RetroUIRenderMode worldSpaceDefault = RetroUIRenderMode.Native;
        [SerializeField, Tooltip("GameObject layer reserved by this project for Native RetroPSX UI. Leave Unconfigured until the project has chosen a free layer, then exclude it from the Universal Renderer opaque and transparent masks.")]
        private int nativeWorldSpaceLayer = -1;

        public RetroUIRenderMode WorldSpaceDefault => worldSpaceDefault;
        public int NativeWorldSpaceLayer => nativeWorldSpaceLayer;
        public bool HasNativeWorldSpaceLayer => nativeWorldSpaceLayer is >= 0 and <= 31;
    }
}
