using System;
using UnityEngine;

namespace RetroPSX
{
    /// <summary>
    /// Ordered list of LDR sRGB colors used by Palette Mapping. Alpha is ignored: palettes never change transparency.
    /// Duplicates are allowed; the first occurrence wins ties.
    /// </summary>
    [CreateAssetMenu(menuName = "RetroPSX/Profiles/Palette", fileName = "RetroPaletteProfile")]
    public sealed class RetroPaletteProfile : ScriptableObject
    {
        public const int MaxColors = 64;

        [SerializeField] private Color[] colors = { Color.black, Color.white };

        [NonSerialized] private Color[] cachedSource;
        [NonSerialized] private Vector4[] cachedLinear;
        [NonSerialized] private Vector4[] cachedOklab;

        public int Count => colors?.Length ?? 0;
        public bool IsValid => Count >= 1 && Count <= MaxColors;
        public Color this[int index] => Sanitize(colors[index]);

        public Color[] GetColors()
        {
            var result = new Color[Count];
            for (int index = 0; index < result.Length; index++)
                result[index] = this[index];
            return result;
        }

        public void SetColors(Color[] value)
        {
            colors = value != null ? (Color[])value.Clone() : Array.Empty<Color>();
            for (int index = 0; index < colors.Length; index++)
                colors[index] = Sanitize(colors[index]);
        }

        /// <summary>Linear RGB and OKLab arrays sized <see cref="MaxColors"/>, rebuilt only when the colors change.</summary>
        internal bool TryGetShaderData(out int count, out Vector4[] linear, out Vector4[] oklab)
        {
            count = IsValid ? Count : 0;
            cachedLinear ??= new Vector4[MaxColors];
            cachedOklab ??= new Vector4[MaxColors];
            if (count > 0 && !MatchesCache())
            {
                cachedSource = new Color[count];
                for (int index = 0; index < count; index++)
                {
                    Color color = this[index];
                    cachedSource[index] = colors[index];
                    Color linearColor = color.linear;
                    cachedLinear[index] = new Vector4(linearColor.r, linearColor.g, linearColor.b, 1f);
                    Vector3 lab = RetroPSXColorMath.LinearToOklab(new Vector3(linearColor.r, linearColor.g, linearColor.b));
                    cachedOklab[index] = new Vector4(lab.x, lab.y, lab.z, 0f);
                }
            }
            linear = cachedLinear;
            oklab = cachedOklab;
            return count > 0;
        }

        private bool MatchesCache()
        {
            if (cachedSource == null || cachedSource.Length != Count)
                return false;
            for (int index = 0; index < cachedSource.Length; index++)
                if (cachedSource[index] != colors[index])
                    return false;
            return true;
        }

        public static Color Sanitize(Color color)
        {
            static float Channel(float value) => float.IsNaN(value) ? 0f : Mathf.Clamp01(value);
            return new Color(Channel(color.r), Channel(color.g), Channel(color.b), 1f);
        }

        private void OnValidate() => cachedSource = null;
    }
}
