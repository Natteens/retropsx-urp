using UnityEngine;

namespace RetroPSX
{
    [CreateAssetMenu(menuName = "RetroPSX/Profiles/Color", fileName = "RetroColorProfile")]
    public sealed class RetroColorProfile : ScriptableObject
    {
        [SerializeField] private RetroColorMode mode = RetroColorMode.RGB555;
        [SerializeField] private Vector3Int customBits = new(5, 5, 5);
        [SerializeField] private RetroDitherMode materialDither = RetroDitherMode.PSX;
        [SerializeField, Range(0f, 2f)] private float materialDitherStrength = 1f;
        [SerializeField] private bool quantizeFinalImage;
        [SerializeField] private RetroDitherMode finalImageDither = RetroDitherMode.Off;
        [SerializeField, Range(0f, 2f)] private float finalImageDitherStrength = 0.5f;
        [SerializeField] private Texture2D customPattern;
        [SerializeField] private Texture2D blueNoise;

        public RetroColorMode Mode => mode;
        public Vector3Int Bits => RetroPSXMath.ColorBits(mode, customBits);
        public RetroDitherMode MaterialDither => materialDither;
        public float MaterialDitherStrength => materialDitherStrength;
        public bool QuantizeFinalImage => quantizeFinalImage;
        public RetroDitherMode FinalImageDither => finalImageDither;
        public float FinalImageDitherStrength => finalImageDitherStrength;
        public Texture2D CustomPattern => customPattern;
        public Texture2D BlueNoise => blueNoise;

        private void OnValidate()
        {
            customBits.x = Mathf.Clamp(customBits.x, 1, 8);
            customBits.y = Mathf.Clamp(customBits.y, 1, 8);
            customBits.z = Mathf.Clamp(customBits.z, 1, 8);
            materialDitherStrength = Mathf.Clamp(materialDitherStrength, 0f, 2f);
            finalImageDitherStrength = Mathf.Clamp(finalImageDitherStrength, 0f, 2f);
        }
    }
}
