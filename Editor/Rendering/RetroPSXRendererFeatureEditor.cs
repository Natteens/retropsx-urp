using RetroPSX.Rendering;
using UnityEditor;
using UnityEngine;

namespace RetroPSX.Editor
{
    [CustomEditor(typeof(RetroPSXRendererFeature))]
    public sealed class RetroPSXRendererFeatureEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty profile = serializedObject.FindProperty("profile");
            EditorGUILayout.PropertyField(profile);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("renderGameCameras"), new GUIContent("Render Game Cameras"));
            serializedObject.ApplyModifiedProperties();

            if (profile.objectReferenceValue == null && GUILayout.Button("Create Complete Pipeline Profile"))
            {
                RetroPSXPipelineProfile created = RetroPSXProfileFactory.PromptCreateCompleteProfile();
                if (created != null)
                {
                    serializedObject.Update();
                    profile.objectReferenceValue = created;
                    serializedObject.ApplyModifiedProperties();
                    ((RetroPSXRendererFeature)target).Create();
                }
            }

            EditorGUILayout.HelpBox(
                "Scene View uses the URP asset's default renderer. If a game camera selects another renderer, add a second RetroPSX feature to the default renderer with Render Game Cameras disabled.",
                MessageType.Info);

            string[] shaders =
            {
                "Hidden/RetroPSX/Resolve", "Hidden/RetroPSX/Presentation", "Hidden/RetroPSX/VolumetricRaymarch",
                "Hidden/RetroPSX/VolumetricComposite", "Hidden/RetroPSX/CRT"
            };
            for (int index = 0; index < shaders.Length; index++)
            {
                if (Shader.Find(shaders[index]) == null)
                {
                    EditorGUILayout.HelpBox($"Missing internal shader: {shaders[index]}", MessageType.Error);
                    break;
                }
            }
        }
    }
}
