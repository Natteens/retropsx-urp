using System.IO;
using UnityEditor;
using UnityEngine;

namespace RetroPSX.Editor
{
    public static class RetroPSXProfileFactory
    {
        [MenuItem("Assets/Create/RetroPSX/Complete Pipeline Profile", priority = 201)]
        public static void CreateCompleteProfile()
        {
            RetroPSXPipelineProfile root = PromptCreateCompleteProfile();
            if (root != null)
                Selection.activeObject = root;
        }

        internal static RetroPSXPipelineProfile PromptCreateCompleteProfile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create RetroPSX Pipeline", "RetroPSXPipelineProfile", "asset", "Choose a location for the complete profile set.");
            if (string.IsNullOrEmpty(path))
                return null;
            RetroPSXPipelineProfile root = CreateProfileSet(Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets", Path.GetFileNameWithoutExtension(path));
            AssetDatabase.SaveAssets();
            return root;
        }

        internal static RetroPSXPipelineProfile CreateProfileSet(string folder, string prefix)
        {
            RetroPSXPipelineProfile root = GetOrCreate<RetroPSXPipelineProfile>($"{folder}/{prefix} Pipeline.asset");
            RetroRasterProfile raster = GetOrCreate<RetroRasterProfile>($"{folder}/{prefix} Raster.asset");
            RetroGeometryProfile geometry = GetOrCreate<RetroGeometryProfile>($"{folder}/{prefix} Geometry.asset");
            RetroColorProfile color = GetOrCreate<RetroColorProfile>($"{folder}/{prefix} Color.asset");
            RetroLightingProfile lighting = GetOrCreate<RetroLightingProfile>($"{folder}/{prefix} Lighting.asset");
            RetroFogProfile fog = GetOrCreate<RetroFogProfile>($"{folder}/{prefix} Fog.asset");
            RetroVolumetricProfile volumetrics = GetOrCreate<RetroVolumetricProfile>($"{folder}/{prefix} Volumetrics.asset");
            RetroDisplayProfile display = GetOrCreate<RetroDisplayProfile>($"{folder}/{prefix} Display.asset");
            RetroDebugProfile debug = GetOrCreate<RetroDebugProfile>($"{folder}/{prefix} Debug.asset");

            SerializedObject rootObject = new(root);
            rootObject.FindProperty("raster").objectReferenceValue = raster;
            rootObject.FindProperty("geometry").objectReferenceValue = geometry;
            rootObject.FindProperty("color").objectReferenceValue = color;
            rootObject.FindProperty("lighting").objectReferenceValue = lighting;
            rootObject.FindProperty("fog").objectReferenceValue = fog;
            rootObject.FindProperty("volumetrics").objectReferenceValue = volumetrics;
            rootObject.FindProperty("display").objectReferenceValue = display;
            rootObject.FindProperty("debug").objectReferenceValue = debug;
            rootObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(root);
            return root;
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

    }
}
