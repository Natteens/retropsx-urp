using UnityEngine;

namespace RetroPSX
{
    [CreateAssetMenu(menuName = "RetroPSX/Profiles/Debug", fileName = "RetroDebugProfile")]
    public sealed class RetroDebugProfile : ScriptableObject
    {
        [SerializeField] private RetroDebugMode mode;
        [SerializeField, Range(0, 3), Tooltip("Packed local volumetric-light index for the current camera. Indexing is camera-dependent because lights are frustum-culled.")]
        private int volumetricLightIndex;

        public RetroDebugMode Mode => mode;
        public int VolumetricLightIndex => volumetricLightIndex;
    }
}
