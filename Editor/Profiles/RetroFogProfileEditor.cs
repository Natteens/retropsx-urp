using UnityEditor;
using UnityEngine;

namespace RetroPSX.Editor
{
    [CustomEditor(typeof(RetroFogProfile))]
    public sealed class RetroFogProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty mode = serializedObject.FindProperty("mode");
            EditorGUILayout.PropertyField(mode, new GUIContent("Fog Mode"));
            if ((RetroFogMode)mode.enumValueIndex != RetroFogMode.Off)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("color"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("nearDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("farDistance"));
                if ((RetroFogMode)mode.enumValueIndex == RetroFogMode.SteppedDistanceColor)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("steps"), new GUIContent("Distance Steps"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("strength"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("applyToWholeFrame"));
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
