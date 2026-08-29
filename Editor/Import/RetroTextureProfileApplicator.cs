using UnityEditor;
using UnityEngine;

namespace RetroPSX.Editor
{
    internal static class RetroTextureProfileApplicator
    {
        [MenuItem("Assets/RetroPSX/Apply Texture Profile", priority = 2200)]
        private static void Apply()
        {
            RetroTextureProfile profile = null;
            for (int index = 0; index < Selection.objects.Length; index++)
            {
                if (Selection.objects[index] is RetroTextureProfile selected)
                {
                    profile = selected;
                    break;
                }
            }
            if (profile == null)
            {
                EditorUtility.DisplayDialog("RetroPSX Texture Import", "Select one RetroTextureProfile together with the textures to update.", "OK");
                return;
            }

            int changed = 0;
            for (int index = 0; index < Selection.objects.Length; index++)
            {
                if (Selection.objects[index] is not Texture2D texture)
                    continue;
                string path = AssetDatabase.GetAssetPath(texture);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;
                importer.filterMode = profile.FilterMode;
                importer.mipmapEnabled = profile.Mipmaps;
                importer.maxTextureSize = profile.MaxSize;
                importer.wrapMode = profile.WrapMode;
                importer.alphaIsTransparency = profile.AlphaIsTransparency;
                importer.textureCompression = profile.Compressed ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                changed++;
            }
            Debug.Log($"RetroPSX applied '{profile.name}' to {changed} texture(s).");
        }

        [MenuItem("Assets/RetroPSX/Apply Texture Profile", true)]
        private static bool ValidateApply()
        {
            bool hasProfile = false;
            bool hasTexture = false;
            for (int index = 0; index < Selection.objects.Length; index++)
            {
                hasProfile |= Selection.objects[index] is RetroTextureProfile;
                hasTexture |= Selection.objects[index] is Texture2D;
            }
            return hasProfile && hasTexture;
        }
    }
}
