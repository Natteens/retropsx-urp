using UnityEditor;
using UnityEngine;

namespace RetroPSX.Editor
{
    [CustomEditor(typeof(RetroPSXUI))]
    public sealed class RetroPSXUIEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("uiProfile"));
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.HelpBox(
                "Place this component on the same GameObject as the world-space UI Renderer. Native mode temporarily uses the profile's configured layer and restores the original layer in Retro mode or when disabled. Exclude that layer from the Universal Renderer prepass, opaque, and transparent masks. Retro mode participates in the canonical image.",
                MessageType.Info);
            EditorGUILayout.HelpBox(
                "While Native mode is active, physics, raycasts, cameras, and other layer-based systems observe the configured Native layer. Keep gameplay colliders and interaction logic on separate GameObjects when their layer must not change.",
                MessageType.Warning);

            RetroPSXUI marker = (RetroPSXUI)target;
            if (marker.GetComponent<Renderer>() == null)
                EditorGUILayout.HelpBox("No Renderer is present on this GameObject. Move the marker to the GameObject that owns PanelRenderer/UIRenderer.", MessageType.Warning);
            if (marker.UIProfile == null || !marker.UIProfile.HasNativeWorldSpaceLayer)
                EditorGUILayout.HelpBox("Assign a UI profile with a configured Native World Space Layer before using Native mode.", MessageType.Warning);
        }
    }
}
