using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RetroPSX.Editor
{
    public sealed class RetroPSXMaterialInspector : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);
            MaterialProperty modeProperty = FindProperty("_TransparencyMode", properties, false);
            if (modeProperty == null)
                return;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            RetroTransparencyMode mode = (RetroTransparencyMode)EditorGUILayout.EnumPopup("Transparency Mode", (RetroTransparencyMode)Mathf.RoundToInt(modeProperty.floatValue));
            if (!EditorGUI.EndChangeCheck())
                return;

            materialEditor.RegisterPropertyChangeUndo("RetroPSX Transparency Mode");
            modeProperty.floatValue = (float)mode;
            foreach (Object target in materialEditor.targets)
                ApplyMode((Material)target, mode);
        }

        public override void ValidateMaterial(Material material)
        {
            ApplyMode(material, (RetroTransparencyMode)Mathf.RoundToInt(material.GetFloat("_TransparencyMode")));
        }

        private static void ApplyMode(Material material, RetroTransparencyMode mode)
        {
            bool transparent = mode >= RetroTransparencyMode.ModernAlpha;
            bool cutout = mode == RetroTransparencyMode.AlphaTest;
            material.SetFloat("_AlphaClip", cutout ? 1f : 0f);
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);
            material.SetOverrideTag("RenderType", transparent ? "Transparent" : (cutout ? "TransparentCutout" : "Opaque"));
            material.renderQueue = transparent ? (int)RenderQueue.Transparent : (cutout ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry);

            BlendMode source = BlendMode.One;
            BlendMode destination = BlendMode.Zero;
            BlendOp operation = BlendOp.Add;
            switch (mode)
            {
                case RetroTransparencyMode.ModernAlpha:
                    source = BlendMode.SrcAlpha;
                    destination = BlendMode.OneMinusSrcAlpha;
                    break;
                case RetroTransparencyMode.Average:
                    source = BlendMode.SrcAlpha;
                    destination = BlendMode.OneMinusSrcAlpha;
                    break;
                case RetroTransparencyMode.Additive:
                    source = BlendMode.One;
                    destination = BlendMode.One;
                    break;
                case RetroTransparencyMode.Subtractive:
                    source = BlendMode.One;
                    destination = BlendMode.One;
                    operation = BlendOp.ReverseSubtract;
                    break;
                case RetroTransparencyMode.AddQuarter:
                    source = BlendMode.SrcAlpha;
                    destination = BlendMode.One;
                    break;
            }
            material.SetFloat("_SrcBlend", (float)source);
            material.SetFloat("_DstBlend", (float)destination);
            material.SetFloat("_BlendOp", (float)operation);
        }
    }
}
