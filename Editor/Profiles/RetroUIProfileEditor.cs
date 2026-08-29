using UnityEditor;
using UnityEngine;

namespace RetroPSX.Editor
{
    [CustomEditor(typeof(RetroUIProfile))]
    public sealed class RetroUIProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("worldSpaceDefault"));
            SerializedProperty layer = serializedObject.FindProperty("nativeWorldSpaceLayer");
            EditorGUI.BeginChangeCheck();
            int selectedLayer = EditorGUILayout.LayerField("Native World Space Layer", Mathf.Max(0, layer.intValue));
            if (EditorGUI.EndChangeCheck())
                layer.intValue = selectedLayer;
            if (layer.intValue >= 0 && GUILayout.Button("Clear Native Layer Assignment"))
                layer.intValue = -1;
            if (layer.intValue < 0)
                EditorGUILayout.HelpBox("Native world-space UI is not configured. Choose a free project layer to enable its late composition pass.", MessageType.Warning);
            EditorGUILayout.HelpBox(
                "Choose a project layer reserved for Native world-space UI, then exclude it from the Universal Renderer prepass, opaque, and transparent layer masks. The package does not claim a layer automatically. Screen-space UI stays on Unity's normal native overlay path.",
                MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
