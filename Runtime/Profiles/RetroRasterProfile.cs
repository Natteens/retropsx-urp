using UnityEngine;

namespace RetroPSX
{
    [CreateAssetMenu(menuName = "RetroPSX/Profiles/Raster", fileName = "RetroRasterProfile")]
    public sealed class RetroRasterProfile : ScriptableObject
    {
        [SerializeField] private RetroRasterMode mode = RetroRasterMode.InternalHeight;
        [SerializeField] private RetroResolutionPreset preset = RetroResolutionPreset.R320x240;
        [SerializeField] private Vector2Int customResolution = new(320, 240);
        [SerializeField, Min(64)] private int internalHeight = 240;
        [SerializeField, Range(0.1f, 1f)] private float scaleFactor = 0.5f;
        [SerializeField] private RetroPresentationMode presentation = RetroPresentationMode.Stretch;
        [SerializeField] private Color letterboxColor = Color.black;

        public RetroRasterMode Mode => mode;
        public RetroResolutionPreset Preset => preset;
        public Vector2Int CustomResolution => customResolution;
        public int InternalHeight => internalHeight;
        public float ScaleFactor => scaleFactor;
        public RetroPresentationMode Presentation => presentation;
        public Color LetterboxColor => letterboxColor;

        public RetroRasterContext BuildContext(int sourceWidth, int sourceHeight)
        {
            Vector2Int source = new(Mathf.Max(1, sourceWidth), Mathf.Max(1, sourceHeight));
            Vector2Int internalSize = RetroPSXMath.InternalSize(source.x, source.y, mode, preset, customResolution, internalHeight, scaleFactor);
            RectInt viewport = RetroPSXMath.PresentationViewport(source, internalSize, presentation);
            return new RetroRasterContext(source, internalSize, viewport, presentation);
        }

        private void OnValidate()
        {
            customResolution.x = Mathf.Clamp(customResolution.x, 64, 4096);
            customResolution.y = Mathf.Clamp(customResolution.y, 64, 4096);
            internalHeight = Mathf.Clamp(internalHeight, 64, 2160);
            scaleFactor = Mathf.Clamp(scaleFactor, 0.1f, 1f);
        }
    }
}
