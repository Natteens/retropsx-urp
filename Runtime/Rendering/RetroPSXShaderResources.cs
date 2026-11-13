using UnityEngine;

namespace RetroPSX.Rendering
{
    internal sealed class RetroPSXShaderResources : ScriptableObject
    {
        internal const string ResourcePath = "RetroPSX/RetroPSXShaderResources";

        [SerializeField] private Shader resolve;
        [SerializeField] private Shader presentation;
        [SerializeField] private Shader volumetric;
        [SerializeField] private Shader volumetricComposite;
        [SerializeField] private Shader crt;
        [SerializeField] private Texture2D defaultBlueNoise;

        private static RetroPSXShaderResources instance;

        internal Shader Resolve => resolve;
        internal Shader Presentation => presentation;
        internal Shader Volumetric => volumetric;
        internal Shader VolumetricComposite => volumetricComposite;
        internal Shader CRT => crt;
        internal Texture2D DefaultBlueNoise => defaultBlueNoise;

        internal static RetroPSXShaderResources Instance => instance != null ? instance : instance = Resources.Load<RetroPSXShaderResources>(ResourcePath);
    }
}
