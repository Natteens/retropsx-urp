using UnityEditor;
using UnityEngine;

namespace RetroPSX.Editor
{
    [CustomEditor(typeof(RetroVolumetricLight))]
    public sealed class RetroVolumetricLightEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var shaft = serializedObject.FindProperty("stylizedShaft");
            bool isSpot = ((RetroVolumetricLight)target).Light?.type == LightType.Spot;
            if (!isSpot)
                DrawPropertiesExcluding(serializedObject, "m_Script", "stylizedShaft", "radialFalloff", "sourceFade", "rangeFade", "bandDither");
            else if (!shaft.boolValue)
                DrawPropertiesExcluding(serializedObject, "m_Script", "radialFalloff", "sourceFade", "rangeFade", "bandDither");
            else
                DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            if (targets.Length != 1)
                return;

            RetroVolumetricLight volume = (RetroVolumetricLight)target;
            if (volume.RequiresRealtimeShadows)
            {
                EditorGUILayout.HelpBox(
                    "Volumetric Shadows is set to Use Light Shadows, but the associated Unity Light has realtime shadows disabled. Direct volumetric scattering is suppressed until URP can allocate that Light's shadow data.",
                    MessageType.Warning);
            }
        }
    }
}
