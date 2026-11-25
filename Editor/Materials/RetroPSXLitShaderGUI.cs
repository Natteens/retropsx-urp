using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;

namespace RetroPSX.Editor
{
    /// <summary>
    /// Inspector for RetroPSX/Lit: URP's own Lit inspector plus a "RetroPSX" foldout.
    /// URP's LitShader class is internal, so this mirrors it with the public LitGUI helpers.
    /// </summary>
    // shortcut: URP's Detail Inputs foldout is internal and is not shown, upgrade if detail maps are needed.
    public sealed class RetroPSXLitShaderGUI : BaseShaderGUI
    {
        private static readonly GUIContent RetroHeader = new("RetroPSX", "PS1 vertex jitter and color grading. Every option is optional.");
        private static readonly string[] WorkflowModeNames = System.Enum.GetNames(typeof(LitGUI.WorkflowMode));
        private const uint RetroExpandable = 1u << 20;

        private LitGUI.LitProperties litProperties;
        private MaterialProperty[] retroProperties;

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties = new LitGUI.LitProperties(properties);
            retroProperties = new[]
            {
                FindProperty("_PSXJitter", properties, false),
                FindProperty("_PSXResolution", properties, false),
                FindProperty("_PSXExposure", properties, false),
                FindProperty("_PSXSaturation", properties, false),
                FindProperty("_PSXTint", properties, false),
                FindProperty("_PSXColorSteps", properties, false),
            };
        }

        public override void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList) =>
            materialScopesList.RegisterHeaderScope(RetroHeader, RetroExpandable, _ => DrawRetro());

        public override void ValidateMaterial(Material material) =>
            SetMaterialKeywords(material, LitGUI.SetMaterialKeywords);

        public override void DrawSurfaceOptions(Material material)
        {
            EditorGUIUtility.labelWidth = 0f;
            if (litProperties.workflowMode != null)
                DoPopup(LitGUI.Styles.workflowModeText, litProperties.workflowMode, WorkflowModeNames);
            base.DrawSurfaceOptions(material);
        }

        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            LitGUI.Inputs(litProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        public override void DrawAdvancedOptions(Material material)
        {
            if (litProperties.reflections != null && litProperties.highlights != null)
            {
                materialEditor.ShaderProperty(litProperties.highlights, LitGUI.Styles.highlightsText);
                materialEditor.ShaderProperty(litProperties.reflections, LitGUI.Styles.reflectionsText);
            }
            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            SetupMaterialBlendMode(material);
        }

        private void DrawRetro()
        {
            foreach (MaterialProperty property in retroProperties)
                if (property != null)
                    materialEditor.ShaderProperty(property, property.displayName);
        }
    }
}
