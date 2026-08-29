using UnityEngine;

namespace RetroPSX
{
    [CreateAssetMenu(menuName = "RetroPSX/Profiles/Display", fileName = "RetroDisplayProfile")]
    public sealed class RetroDisplayProfile : ScriptableObject
    {
        [SerializeField] private bool enabled;
        [SerializeField, Range(0f, 1f)] private float scanlines = 0.12f;
        [SerializeField, Range(0f, 1f)] private float maskStrength = 0.08f;
        [SerializeField] private RetroCRTMaskMode maskMode = RetroCRTMaskMode.ApertureGrille;
        [SerializeField, Range(0f, 0.2f)] private float curvature;
        [SerializeField, Range(0f, 0.2f)] private float overscan;
        [SerializeField, Range(0f, 1f)] private float vignette = 0.08f;
        [SerializeField, Range(0f, 2f)] private float horizontalBleed = 0.15f;
        [SerializeField, Range(0f, 2f)] private float chromaBleed = 0.06f;
        [SerializeField, Range(0f, 2f)] private float chromaticMisalignment = 0.1f;
        [SerializeField, Range(0f, 1f)] private float signalNoise = 0.015f;
        [SerializeField, Range(0.5f, 2f)] private float brightness = 1f;
        [SerializeField] private bool interlacing;
        [SerializeField, Range(0f, 1f)] private float pixelBloom = 0.05f;

        public bool Enabled => enabled;
        public float Scanlines => scanlines;
        public float MaskStrength => maskStrength;
        public RetroCRTMaskMode MaskMode => maskMode;
        public float Curvature => curvature;
        public float Overscan => overscan;
        public float Vignette => vignette;
        public float HorizontalBleed => horizontalBleed;
        public float ChromaBleed => chromaBleed;
        public float ChromaticMisalignment => chromaticMisalignment;
        public float SignalNoise => signalNoise;
        public float Brightness => brightness;
        public bool Interlacing => interlacing;
        public float PixelBloom => pixelBloom;
    }
}
