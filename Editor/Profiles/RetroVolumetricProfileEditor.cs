using UnityEditor;
using UnityEngine;

namespace RetroPSX.Editor
{
    [CustomEditor(typeof(RetroVolumetricProfile)), CanEditMultipleObjects]
    public sealed class RetroVolumetricProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            bool custom = serializedObject.FindProperty("quality").enumValueIndex == (int)RetroVolumetricQuality.Custom;
            bool stylizedDirectional = serializedObject.FindProperty("stylizedDirectional").boolValue;
            bool centered = serializedObject.FindProperty("pixelated").boolValue || stylizedDirectional;
            bool jitter = serializedObject.FindProperty("jitter").boolValue;
            bool lightSteps = serializedObject.FindProperty("lightSteps").intValue > 1;
            bool shafts = serializedObject.FindProperty("sunShafts").floatValue > 0f;
            SerializedProperty property = serializedObject.GetIterator();
            for (bool enter = true; property.NextVisible(enter); enter = false)
            {
                if (property.name == "m_Script")
                    continue;
                string inactive = property.name switch
                {
                    "resolutionDivisor" or "raySteps" when !custom => "Set by Quality. Select Custom to use this value.",
                    "jitter" or "jitterStrength" when centered => "Pixelated and Stylized Directional use centered samples, so jitter is off.",
                    "jitterStrength" when !jitter => "Jitter is disabled.",
                    "directionalContrast" when !stylizedDirectional => "Requires Stylized Directional.",
                    "directionalTint" when !stylizedDirectional && !shafts => "Requires Stylized Directional or Sun Shafts.",
                    "directionalDither" when !shafts && !(stylizedDirectional && lightSteps) => "Requires Sun Shafts, or Stylized Directional with Light Steps above 1.",
                    _ => null
                };
                using (new EditorGUI.DisabledScope(inactive != null))
                {
                    var label = new GUIContent(property.displayName, inactive != null ? $"{inactive} {property.tooltip}" : property.tooltip);
                    EditorGUILayout.PropertyField(property, label, true);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
