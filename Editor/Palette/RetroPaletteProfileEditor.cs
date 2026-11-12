using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RetroPSX.Editor
{
    [CustomEditor(typeof(RetroPaletteProfile))]
    public sealed class RetroPaletteProfileEditor : UnityEditor.Editor
    {
        private const float HexWidth = 64f;

        private SerializedProperty colors;
        private ReorderableList list;
        private RetroPaletteImportResult pending;
        private readonly Dictionary<int, string> hexDrafts = new();

        private void OnEnable()
        {
            colors = serializedObject.FindProperty("colors");
            list = new ReorderableList(serializedObject, colors, true, false, true, true)
            {
                elementHeight = EditorGUIUtility.singleLineHeight + 4f,
                drawElementCallback = DrawElement,
                onAddCallback = AddColor,
                onCanAddCallback = _ => colors.arraySize < RetroPaletteProfile.MaxColors,
                onReorderCallback = _ => hexDrafts.Clear(),
                onRemoveCallback = RemoveColor
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPaletteHeader();
            EditorGUILayout.Space(4f);
            list.DoLayoutList();
            DrawActions();
            if (pending != null)
                DrawPending();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPaletteHeader()
        {
            int count = colors.arraySize;
            Rect strip = EditorGUILayout.GetControlRect(false, count > 16 ? 36f : 22f);
            var values = new List<Color>(count);
            for (int index = 0; index < count; index++)
                values.Add(colors.GetArrayElementAtIndex(index).colorValue);
            if (count == 0)
                EditorGUI.DrawRect(strip, RetroColorEditorGUI.LineColor);
            RetroColorEditorGUI.Swatches(strip, values);

            EditorGUILayout.LabelField($"{count} / {RetroPaletteProfile.MaxColors} colors", EditorStyles.miniLabel);
            if (count == 0)
                EditorGUILayout.HelpBox("The palette is empty. Palette Mapping is skipped until it has at least one color.", MessageType.Warning);
            else if (count > RetroPaletteProfile.MaxColors)
                EditorGUILayout.HelpBox($"A palette holds at most {RetroPaletteProfile.MaxColors} colors. Palette Mapping is skipped.", MessageType.Error);
            else if (count == 1)
                RetroColorEditorGUI.InfoLine("A single color maps the whole image to that color.", "triangle-exclamation-point");
            RetroColorEditorGUI.InfoLine("Colors are sRGB and opaque: palette alpha is not used and never changes image transparency.");
        }

        private void DrawElement(Rect rect, int index, bool active, bool focused)
        {
            SerializedProperty element = colors.GetArrayElementAtIndex(index);
            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;
            var indexRect = new Rect(rect.x, rect.y, 22f, rect.height);
            var copyRect = new Rect(rect.xMax - 20f, rect.y, 20f, rect.height);
            var hexRect = new Rect(copyRect.x - HexWidth - 4f, rect.y, HexWidth, rect.height);
            var colorRect = new Rect(indexRect.xMax + 2f, rect.y, hexRect.x - indexRect.xMax - 6f, rect.height);
            EditorGUI.LabelField(indexRect, index.ToString(), EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
            Color color = EditorGUI.ColorField(colorRect, GUIContent.none, element.colorValue, true, false, false);
            if (EditorGUI.EndChangeCheck())
            {
                element.colorValue = RetroPaletteProfile.Sanitize(color);
                hexDrafts.Remove(index);
            }

            string hex = "#" + RetroPaletteImport.ToHex(element.colorValue);
            string draft = hexDrafts.TryGetValue(index, out string text) ? text : hex;
            bool valid = RetroPaletteImport.TryParseHex(draft, out Color32 parsed);
            Color previousColor = GUI.color;
            if (!valid)
                GUI.color = new Color(1f, 0.55f, 0.5f);
            EditorGUI.BeginChangeCheck();
            draft = EditorGUI.DelayedTextField(hexRect, draft);
            GUI.color = previousColor;
            if (EditorGUI.EndChangeCheck())
            {
                if (RetroPaletteImport.TryParseHex(draft, out parsed))
                {
                    element.colorValue = (Color)parsed;
                    hexDrafts.Remove(index);
                }
                else
                    hexDrafts[index] = draft;
            }
            if (RetroColorEditorGUI.IconButton(copyRect, "copy", "Copy hex"))
                EditorGUIUtility.systemCopyBuffer = hex;
        }

        private void AddColor(ReorderableList reorderable)
        {
            int index = colors.arraySize;
            colors.arraySize++;
            colors.GetArrayElementAtIndex(index).colorValue = index > 0 ? colors.GetArrayElementAtIndex(index - 1).colorValue : Color.black;
            reorderable.index = index;
            hexDrafts.Clear();
        }

        private void RemoveColor(ReorderableList reorderable)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(reorderable);
            hexDrafts.Clear();
        }

        private void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (RetroColorEditorGUI.IconTextButton("plus", "Import PNG…", "Read unique colors from a palette image (strip, column, swatch grid). The texture's import settings are not changed."))
                {
                    string path = EditorUtility.OpenFilePanelWithFilters("Import Palette Image", LastFolder(), new[] { "PNG image", "png" });
                    if (!string.IsNullOrEmpty(path))
                        pending = RetroPaletteImport.ParsePng(File.ReadAllBytes(path), Path.GetFileName(path));
                }
                if (RetroColorEditorGUI.IconTextButton("plus", "Import HEX…", "Read a .hex/.txt file with one RRGGBB color per line."))
                {
                    string path = EditorUtility.OpenFilePanelWithFilters("Import Palette HEX", LastFolder(), new[] { "HEX palette", "hex,txt", "All files", "*" });
                    if (!string.IsNullOrEmpty(path))
                        pending = RetroPaletteImport.ParseHex(File.ReadAllText(path), Path.GetFileName(path));
                }
                if (RetroColorEditorGUI.IconTextButton("copy", "Paste HEX", "Read colors from the clipboard, one RRGGBB per line."))
                    pending = RetroPaletteImport.ParseHex(EditorGUIUtility.systemCopyBuffer, "Clipboard");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(colors.arraySize == 0))
                {
                    if (RetroColorEditorGUI.IconTextButton("copy", "Copy HEX", "Copy every color as #RRGGBB lines."))
                        EditorGUIUtility.systemCopyBuffer = RetroPaletteImport.ToHexText(CurrentColors(), true);
                    if (RetroColorEditorGUI.IconTextButton("locate", "Export HEX…", "Save a .hex file (one RRGGBB per line)."))
                    {
                        string path = EditorUtility.SaveFilePanel("Export Palette HEX", LastFolder(), target.name, "hex");
                        if (!string.IsNullOrEmpty(path))
                            File.WriteAllText(path, RetroPaletteImport.ToHexText(CurrentColors(), false));
                    }
                }
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawPending()
        {
            RetroColorEditorGUI.Section($"Import preview · {pending.Source}");
            if (pending.Colors.Count > 0 && !pending.OverLimit)
            {
                Rect strip = EditorGUILayout.GetControlRect(false, pending.Colors.Count > 16 ? 36f : 22f);
                var preview = new List<Color>(pending.Colors.Count);
                foreach (Color32 color in pending.Colors)
                    preview.Add(color);
                RetroColorEditorGUI.Swatches(strip, preview);
                EditorGUILayout.LabelField($"{pending.Colors.Count} colors, in reading order.", EditorStyles.miniLabel);
            }
            foreach (string warning in pending.Warnings)
                RetroColorEditorGUI.InfoLine(warning, "triangle-exclamation-point");
            foreach (string error in pending.Errors)
                EditorGUILayout.HelpBox(error, MessageType.Error);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(!pending.CanApply))
                {
                    if (GUILayout.Button(new GUIContent("Replace", "Replace every color (undoable)."), EditorStyles.miniButtonLeft, GUILayout.Width(70f)))
                        Apply(false);
                    using (new EditorGUI.DisabledScope(colors.arraySize + pending.Colors.Count > RetroPaletteProfile.MaxColors))
                        if (GUILayout.Button(new GUIContent("Append", "Add after the current colors (undoable)."), EditorStyles.miniButtonMid, GUILayout.Width(70f)))
                            Apply(true);
                }
                if (GUILayout.Button("Cancel", EditorStyles.miniButtonRight, GUILayout.Width(70f)))
                    pending = null;
            }
        }

        private void Apply(bool append)
        {
            int start = append ? colors.arraySize : 0;
            colors.arraySize = start + pending.Colors.Count;
            for (int index = 0; index < pending.Colors.Count; index++)
                colors.GetArrayElementAtIndex(start + index).colorValue = pending.Colors[index];
            Undo.SetCurrentGroupName(append ? "Append Palette Colors" : "Import Palette");
            pending = null;
            hexDrafts.Clear();
            GUI.FocusControl(null);
        }

        private List<Color> CurrentColors()
        {
            var result = new List<Color>(colors.arraySize);
            for (int index = 0; index < colors.arraySize; index++)
                result.Add(colors.GetArrayElementAtIndex(index).colorValue);
            return result;
        }

        private string LastFolder()
        {
            string path = AssetDatabase.GetAssetPath(target);
            return string.IsNullOrEmpty(path) ? "Assets" : Path.GetDirectoryName(path);
        }
    }
}
