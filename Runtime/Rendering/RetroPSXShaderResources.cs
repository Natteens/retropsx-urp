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

        internal Shader Resolve => resolve;
        internal Shader Presentation => presentation;
        internal Shader Volumetric => volumetric;
        internal Shader VolumetricComposite => volumetricComposite;
        internal Shader CRT => crt;
    }
}
