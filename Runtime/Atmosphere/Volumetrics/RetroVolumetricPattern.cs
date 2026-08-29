using UnityEngine;

namespace RetroPSX
{
    [CreateAssetMenu(menuName = "RetroPSX/Volumetric Pattern", fileName = "RetroVolumetricPattern")]
    public sealed class RetroVolumetricPattern : ScriptableObject
    {
        [SerializeField] private RetroVolumetricPatternType type = RetroVolumetricPatternType.None;
        [SerializeField] private Texture2D texture;
        [SerializeField] private Vector2 scale = Vector2.one;
        [SerializeField] private Vector2 offset;
        [SerializeField] private Vector2 scrollVelocity;
        [SerializeField, Range(-180f, 180f)] private float rotation;
        [SerializeField, Range(0.01f, 8f)] private float contrast = 1f;
        [SerializeField, Range(0f, 1f)] private float threshold = 0.5f;
        [SerializeField, Range(0.001f, 1f)] private float softness = 0.1f;
        [SerializeField] private bool inverted;
        [SerializeField] private RetroPatternMapping mapping = RetroPatternMapping.Projector;

        public RetroVolumetricPatternType Type => type;
        public Texture2D Texture => texture;
        public Vector2 Scale => scale;
        public Vector2 Offset => offset;
        public Vector2 ScrollVelocity => scrollVelocity;
        public float Rotation => rotation;
        public float Contrast => contrast;
        public float Threshold => threshold;
        public float Softness => softness;
        public bool Inverted => inverted;
        public RetroPatternMapping Mapping => mapping;
    }
}
