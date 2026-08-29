using UnityEngine;

namespace RetroPSX
{
    [CreateAssetMenu(menuName = "RetroPSX/Texture Import Profile", fileName = "RetroTextureProfile")]
    public sealed class RetroTextureProfile : ScriptableObject
    {
        [SerializeField] private FilterMode filterMode = FilterMode.Point;
        [SerializeField] private bool mipmaps;
        [SerializeField, Range(32, 2048)] private int maxSize = 256;
        [SerializeField] private TextureWrapMode wrapMode = TextureWrapMode.Repeat;
        [SerializeField] private bool compressed;
        [SerializeField] private bool alphaIsTransparency = true;

        public FilterMode FilterMode => filterMode;
        public bool Mipmaps => mipmaps;
        public int MaxSize => maxSize;
        public TextureWrapMode WrapMode => wrapMode;
        public bool Compressed => compressed;
        public bool AlphaIsTransparency => alphaIsTransparency;
    }
}
