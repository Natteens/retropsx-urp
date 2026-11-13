using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RetroPSX.Editor
{
    // Lucide icons (ISC, see Editor/Icons/LICENSE.txt) drawn at 16 px and tinted for the current skin.
    internal static class RetroColorEditorGUI
    {
        private const string IconFolder = "Packages/com.natteens.retropsxurp/Editor/Icons/";
        private const float IconSize = 16f;
        private static readonly Dictionary<string, Texture2D> Icons = new();

        internal static Color IconTint => EditorGUIUtility.isProSkin ? new Color(0.82f, 0.82f, 0.82f) : new Color(0.24f, 0.24f, 0.24f);
        internal static Color LineColor => EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.12f);

        internal static Texture2D Icon(string name)
        {
            if (!Icons.TryGetValue(name, out Texture2D icon) || icon == null)
                Icons[name] = icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconFolder + name + ".png");
            return icon;
        }

        internal static bool IconButton(Rect rect, string icon, string tooltip)
        {
            bool clicked = GUI.Button(rect, new GUIContent(string.Empty, tooltip), EditorStyles.iconButton);
            DrawIcon(rect, icon);
            return clicked;
        }

        internal static bool IconButton(string icon, string tooltip)
        {
            Rect rect = GUILayoutUtility.GetRect(IconSize + 4f, IconSize + 2f, GUILayout.Width(IconSize + 4f), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            return IconButton(rect, icon, tooltip);
        }

        internal static bool IconTextButton(string icon, string text, string tooltip, params GUILayoutOption[] options)
        {
            var content = new GUIContent("     " + text, tooltip);
            Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.miniButton, options);
            bool clicked = GUI.Button(rect, content, EditorStyles.miniButton);
            DrawIcon(new Rect(rect.x + 4f, rect.y + (rect.height - 14f) * 0.5f, 14f, 14f), icon);
            return clicked;
        }

        internal static void DrawIcon(Rect rect, string icon)
        {
            Texture2D texture = Icon(icon);
            if (texture == null || Event.current.type != EventType.Repaint)
                return;
            float size = Mathf.Min(IconSize, rect.width, rect.height);
            var iconRect = new Rect(rect.center.x - size * 0.5f, rect.center.y - size * 0.5f, size, size);
            Color previous = GUI.color;
            GUI.color = IconTint * previous;
            GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit);
            GUI.color = previous;
        }

        internal static void Section(string title)
        {
            EditorGUILayout.Space(6f);
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(rect, title, EditorStyles.boldLabel);
            Rect line = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(line, LineColor);
        }

        // Word-wrapped mini label. Its height comes from GUILayout's word-wrap sizing at the final width, so
        // indentation and the optional icon (reserved as left padding) are accounted for exactly.
        internal static void InfoLine(string text, string icon = null)
        {
            var style = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, richText = false };
            style.margin.left += (int)(EditorGUI.indentLevel * 15f);
            if (icon != null)
                style.padding.left += 18;
            var content = new GUIContent(text);
            Rect rect = GUILayoutUtility.GetRect(content, style);
            if (icon != null)
                DrawIcon(new Rect(rect.x + 1f, rect.y + 1f, 14f, 14f), icon);
            GUI.Label(rect, content, style);
        }

        internal static void Swatches(Rect rect, IReadOnlyList<Color> colors, int maxRows = 2)
        {
            if (colors == null || colors.Count == 0)
                return;
            int perRow = Mathf.Max(1, Mathf.CeilToInt(colors.Count / (float)maxRows));
            int rows = Mathf.CeilToInt(colors.Count / (float)perRow);
            float width = rect.width / perRow;
            float height = rect.height / rows;
            for (int index = 0; index < colors.Count; index++)
            {
                int row = index / perRow, column = index % perRow;
                var swatch = new Rect(rect.x + column * width, rect.y + row * height, Mathf.Ceil(width), Mathf.Ceil(height));
                Color color = colors[index];
                color.a = 1f;
                EditorGUI.DrawRect(swatch, color);
            }
        }
    }
}
