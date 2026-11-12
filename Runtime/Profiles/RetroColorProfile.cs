using UnityEngine;

namespace RetroPSX
{
    [CreateAssetMenu(menuName = "RetroPSX/Profiles/Color", fileName = "RetroColorProfile")]
    public sealed class RetroColorProfile : ScriptableObject
    {
        public const int MinPosterizeLevels = 2;
        public const int MaxPosterizeLevels = 64;

        [SerializeField] private RetroColorMode mode = RetroColorMode.RGB555;
        [SerializeField] private Vector3Int customBits = new(5, 5, 5);
        [SerializeField] private RetroDitherMode materialDither = RetroDitherMode.PSX;
        [SerializeField, Range(0f, 2f)] private float materialDitherStrength = 1f;
        [SerializeField] private bool quantizeFinalImage;
        [SerializeField] private RetroDitherMode finalImageDither = RetroDitherMode.Off;
        [SerializeField, Range(0f, 2f)] private float finalImageDitherStrength = 0.5f;
        [SerializeField] private Texture2D customPattern;
        [SerializeField] private Texture2D blueNoise;
        [SerializeField] private RetroPosterizeMode posterizeMode = RetroPosterizeMode.Off;
        [SerializeField, Range(MinPosterizeLevels, MaxPosterizeLevels)] private int posterizeLevels = 6;
        [SerializeField, Range(0f, 1f)] private float posterizeStrength = 1f;
        [SerializeField] private RetroDitherMode posterizeDither = RetroDitherMode.Off;
        [SerializeField, Range(0f, 1f)] private float posterizeDitherStrength = 1f;
        [SerializeField] private bool paletteEnabled;
        [SerializeField] private RetroPaletteProfile palette;
        [SerializeField, Range(0f, 1f)] private float paletteStrength = 1f;
        [SerializeField] private RetroDitherMode paletteDither = RetroDitherMode.Off;
        [SerializeField, Range(0f, 1f)] private float paletteDitherStrength = 1f;

        public RetroColorMode Mode => mode;
        public Vector3Int Bits => RetroPSXMath.ColorBits(mode, customBits);
        public RetroDitherMode MaterialDither => materialDither;
        public float MaterialDitherStrength => materialDitherStrength;
        public bool QuantizeFinalImage => quantizeFinalImage;
        public RetroDitherMode FinalImageDither => finalImageDither;
        public float FinalImageDitherStrength => finalImageDitherStrength;
        public Texture2D CustomPattern => customPattern;
        public Texture2D BlueNoise => blueNoise;
        public RetroPosterizeMode PosterizeMode => posterizeMode;
        public int PosterizeLevels => Mathf.Clamp(posterizeLevels, MinPosterizeLevels, MaxPosterizeLevels);
        public float PosterizeStrength => Mathf.Clamp01(posterizeStrength);
        public RetroDitherMode PosterizeDither => posterizeDither;
        public float PosterizeDitherStrength => Mathf.Clamp01(posterizeDitherStrength);
        public bool PaletteEnabled => paletteEnabled;
        public RetroPaletteProfile Palette => palette;
        public float PaletteStrength => Mathf.Clamp01(paletteStrength);
        public RetroDitherMode PaletteDither => paletteDither;
        public float PaletteDitherStrength => Mathf.Clamp01(paletteDitherStrength);

        /// <summary>True when Final Image Precision actually quantizes (Quantize Final Image with a color mode).</summary>
        public bool FinalImagePrecisionActive => quantizeFinalImage && mode != RetroColorMode.Off;
        public bool PosterizeActive => posterizeMode != RetroPosterizeMode.Off && PosterizeStrength > 0f;
        /// <summary>Palette Mapping runs only with a valid palette; an empty or invalid palette is skipped.</summary>
        public bool PaletteActive => paletteEnabled && PaletteStrength > 0f && palette != null && palette.IsValid;
        /// <summary>Posterize dithering is suppressed while Palette Mapping runs: the last artistic stage owns dithering.</summary>
        public bool PosterizeDitherActive => PosterizeActive && posterizeDither != RetroDitherMode.Off && PosterizeDitherStrength > 0f && !PaletteActive;

        private void OnValidate()
        {
            customBits.x = Mathf.Clamp(customBits.x, 1, 8);
            customBits.y = Mathf.Clamp(customBits.y, 1, 8);
            customBits.z = Mathf.Clamp(customBits.z, 1, 8);
            materialDitherStrength = Mathf.Clamp(materialDitherStrength, 0f, 2f);
            finalImageDitherStrength = Mathf.Clamp(finalImageDitherStrength, 0f, 2f);
            posterizeLevels = Mathf.Clamp(posterizeLevels, MinPosterizeLevels, MaxPosterizeLevels);
            posterizeStrength = Mathf.Clamp01(posterizeStrength);
            posterizeDitherStrength = Mathf.Clamp01(posterizeDitherStrength);
            paletteStrength = Mathf.Clamp01(paletteStrength);
            paletteDitherStrength = Mathf.Clamp01(paletteDitherStrength);
        }
    }
}
