using UnityEngine;

namespace RetroPSX
{
    /// <summary>Immutable per-camera description of the canonical retro pixel grid.</summary>
    public readonly struct RetroRasterContext
    {
        public RetroRasterContext(Vector2Int sourceSize, Vector2Int internalSize, RectInt viewport, RetroPresentationMode presentationMode)
        {
            SourceSize = sourceSize;
            InternalSize = internalSize;
            Viewport = viewport;
            PresentationMode = presentationMode;
            Scale = new Vector2(sourceSize.x / (float)internalSize.x, sourceSize.y / (float)internalSize.y);
            TexelSize = new Vector2(1f / internalSize.x, 1f / internalSize.y);
            PixelPhase = new Vector2(viewport.x & 1, viewport.y & 1) * 0.5f;
        }

        public Vector2Int SourceSize { get; }
        public Vector2Int InternalSize { get; }
        public RectInt Viewport { get; }
        public Vector2 Scale { get; }
        public Vector2 TexelSize { get; }
        public Vector2 PixelPhase { get; }
        public RetroPresentationMode PresentationMode { get; }
        public bool IsNative => SourceSize == InternalSize;
    }
}
