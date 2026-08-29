using UnityEngine;

namespace RetroPSX
{
    [CreateAssetMenu(menuName = "RetroPSX/Profiles/Lighting", fileName = "RetroLightingProfile")]
    public sealed class RetroLightingProfile : ScriptableObject
    {
        [SerializeField] private RetroLightingMode defaultMode = RetroLightingMode.VertexLit;
        [SerializeField] private Color ambientColor = new(0.22f, 0.22f, 0.24f, 1f);
        [SerializeField, Range(0f, 4f)] private float intensity = 1f;
        [SerializeField, Range(0, 8)] private int additionalLightLimit = 3;
        [SerializeField, Range(0f, 2f)] private float vertexLightExaggeration = 1f;

        public RetroLightingMode DefaultMode => defaultMode;
        public Color AmbientColor => ambientColor;
        public float Intensity => intensity;
        public int AdditionalLightLimit => additionalLightLimit;
        public float VertexLightExaggeration => vertexLightExaggeration;

        private void OnValidate()
        {
            intensity = Mathf.Clamp(intensity, 0f, 4f);
            additionalLightLimit = Mathf.Clamp(additionalLightLimit, 0, 8);
            vertexLightExaggeration = Mathf.Clamp(vertexLightExaggeration, 0f, 2f);
        }
    }
}
