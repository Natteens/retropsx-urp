using UnityEditor;
using UnityEngine;

namespace RetroPSX.Editor
{
    [CustomEditor(typeof(RetroPSXPipelineProfile))]
    public sealed class RetroPSXPipelineProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enabled"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("sceneViewPreview"),
                new GUIContent(
                    "Scene View Preview",
                    "Off: ordinary URP. World Effects: native-resolution material and atmosphere preview. Full Pipeline: complete raster, final color, and display preview."));
            EditorGUILayout.Space(8f);
            DrawSection("Raster", "raster");
            DrawSection("Geometry", "geometry");
            DrawSection("Color", "color");
            DrawSection("Lighting", "lighting");
            DrawSection("Atmosphere", "fog");
            DrawSection("Volumetrics (Modern)", "volumetrics");
            DrawSection("Display (Modern)", "display");
            DrawSection("UI", "ui");
            DrawSection("Debug", "debug");
            serializedObject.ApplyModifiedProperties();

            RetroPSXPipelineProfile profile = (RetroPSXPipelineProfile)target;
            if (!profile.IsComplete)
                EditorGUILayout.HelpBox("Assign every subsystem profile. The renderer skips incomplete roots instead of rendering with ambiguous defaults.", MessageType.Warning);
        }

        private void DrawSection(string label, string property)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(property), GUIContent.none);
        }
    }
}
