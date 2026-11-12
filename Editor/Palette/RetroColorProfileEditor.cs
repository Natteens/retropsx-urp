using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RetroPSX.Editor
{
    [CustomEditor(typeof(RetroColorProfile)), CanEditMultipleObjects]
    public sealed class RetroColorProfileEditor : UnityEditor.Editor
    {
        private const int PreviewWidth = 96;
        private const int PreviewRowHeight = 6;

        private static readonly string[] ExamplePalettes =
        {
            "Industrial Horror", "101413 242D2B 414C43 68745D A5A184 D6CBB0",
            "Night", "0B0D19 1C2338 343E60 656F8F 999BB8 D9D6E4",
            "Rust", "181411 39251F 70432F A86A43 CF9A65 EBD5AB",
            "Monochrome", "171A18 414A40 858C72 D8D6B5"
        };

        private Texture2D preview;
        private string previewKey;

        private void OnDisable()
        {
            if (preview != null)
                DestroyImmediate(preview);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var profile = (RetroColorProfile)target;
            bool single = targets.Length == 1;
            if (single)
                DrawPipelineSummary(profile);

            RetroColorEditorGUI.Section("Material Color Precision");
            SerializedProperty mode = Property("mode", "Color Mode", "Per-channel bit depth used by RetroPSX materials (PSX vertex/texture color precision).");
            if (mode.hasMultipleDifferentValues || mode.enumValueIndex == (int)RetroColorMode.Custom)
                Property("customBits", "Custom Bits", "Bits per channel (1-8) for Custom mode.");
            SerializedProperty materialDither = Property("materialDither", "Material Dither", "Dither applied by RetroPSX materials before their precision quantization.");
            if (materialDither.hasMultipleDifferentValues || materialDither.enumValueIndex != (int)RetroDitherMode.Off)
                Property("materialDitherStrength", "Dither Strength", "Legacy amplitude scale (0-2).");

            RetroColorEditorGUI.Section("Final Image Precision");
            SerializedProperty quantize = Property("quantizeFinalImage", "Quantize Final Image", "Legacy: quantizes the whole final image (plain URP materials included) with the Color Mode bit depth.");
            if (quantize.hasMultipleDifferentValues || quantize.boolValue)
            {
                SerializedProperty finalDither = Property("finalImageDither", "Dither", "Legacy final-image dither, scaled for RGB bit quantization.");
                if (finalDither.hasMultipleDifferentValues || finalDither.enumValueIndex != (int)RetroDitherMode.Off)
                    Property("finalImageDitherStrength", "Dither Strength", "Legacy amplitude scale (0-2).");
                if (single && profile.Mode == RetroColorMode.Off)
                    RetroColorEditorGUI.InfoLine("Color Mode is Off, so Final Image Precision has no effect.", "triangle-exclamation-point");
            }

            RetroColorEditorGUI.Section("Artistic Posterization");
            SerializedProperty posterize = Property("posterizeMode", "Mode", "RGB: each channel gets N levels. Luminance: brightness gets N levels while hue and saturation are kept where the gamut allows.");
            if (posterize.hasMultipleDifferentValues || posterize.enumValueIndex != (int)RetroPosterizeMode.Off)
            {
                IntSlider("posterizeLevels", "Levels", "Exact number of levels (2-64), not limited to powers of two.");
                Property("posterizeStrength", "Strength", "0 bypasses, 1 is full posterization; values in between mix with the original.");
                SerializedProperty posterizeDither = Property("posterizeDither", "Dither", "Ordered dither between adjacent levels, in internal-resolution pixels.");
                if (posterizeDither.hasMultipleDifferentValues || posterizeDither.enumValueIndex != (int)RetroDitherMode.Off)
                    Property("posterizeDitherStrength", "Dither Strength", "0 rounds to the nearest level, 1 distributes pixels fully between the two levels.");
                if (single)
                    DrawPosterizeInfo(profile);
            }

            RetroColorEditorGUI.Section("Palette Mapping");
            SerializedProperty paletteEnabled = Property("paletteEnabled", "Enabled", "Remaps every pixel of the final image to the nearest palette color (OKLab).");
            if (paletteEnabled.hasMultipleDifferentValues || paletteEnabled.boolValue)
            {
                DrawPaletteField(single);
                Property("paletteStrength", "Strength", "0 bypasses, 1 outputs only palette colors; values in between mix with the original.");
                SerializedProperty paletteDither = Property("paletteDither", "Dither", "Ordered dither between two palette colors; never adds off-palette colors.");
                if (paletteDither.hasMultipleDifferentValues || paletteDither.enumValueIndex != (int)RetroDitherMode.Off)
                    Property("paletteDitherStrength", "Dither Strength", "0 uses the nearest color only, 1 distributes pixels fully between the two colors.");
                if (single)
                    DrawPaletteInfo(profile);
            }

            if (UsesTexturePattern())
            {
                RetroColorEditorGUI.Section("Dither Textures");
                Property("customPattern", "Custom Pattern", "Threshold texture for Custom dither. Import it with sRGB off and Point filtering; artistic stages tile it per internal pixel.");
                Property("blueNoise", "Blue Noise", "Threshold texture for Blue Noise dither. Import it with sRGB off; artistic stages tile it per internal pixel.");
            }

            serializedObject.ApplyModifiedProperties();
            if (single && (profile.PosterizeActive || profile.PaletteActive))
                DrawPreview(profile);
        }

        private SerializedProperty Property(string name, string label, string tooltip)
        {
            SerializedProperty property = serializedObject.FindProperty(name);
            EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip));
            return property;
        }

        private void IntSlider(string name, string label, string tooltip)
        {
            SerializedProperty property = serializedObject.FindProperty(name);
            EditorGUILayout.IntSlider(property, RetroColorProfile.MinPosterizeLevels, RetroColorProfile.MaxPosterizeLevels, new GUIContent(label, tooltip));
        }

        private bool UsesTexturePattern()
        {
            foreach (string name in new[] { "finalImageDither", "posterizeDither", "paletteDither" })
            {
                SerializedProperty property = serializedObject.FindProperty(name);
                if (property.hasMultipleDifferentValues || property.enumValueIndex == (int)RetroDitherMode.Custom || property.enumValueIndex == (int)RetroDitherMode.BlueNoise)
                    return true;
            }
            return serializedObject.FindProperty("customPattern").objectReferenceValue != null
                || serializedObject.FindProperty("blueNoise").objectReferenceValue != null;
        }

        private static void DrawPipelineSummary(RetroColorProfile profile)
        {
            var stages = new List<string>();
            if (profile.Mode != RetroColorMode.Off)
                stages.Add($"Materials {ModeName(profile)}");
            if (profile.FinalImagePrecisionActive)
                stages.Add($"Final {ModeName(profile)}");
            if (profile.PosterizeActive)
                stages.Add($"{profile.PosterizeMode} Posterize {profile.PosterizeLevels}");
            if (profile.PaletteActive)
                stages.Add($"Palette {profile.Palette.Count}");
            string text = stages.Count == 0 ? "No color stage is active." : "Active color stages: " + string.Join(" → ", stages);
            RetroColorEditorGUI.InfoLine(text);
            if (profile.FinalImagePrecisionActive && (profile.PosterizeActive || profile.PaletteActive))
                RetroColorEditorGUI.InfoLine("Final Image Precision runs first; the artistic stages then process its quantized result.", "triangle-exclamation-point");
        }

        private static string ModeName(RetroColorProfile profile)
        {
            Vector3Int bits = profile.Bits;
            return profile.Mode == RetroColorMode.Custom ? $"Custom {bits.x}{bits.y}{bits.z}" : profile.Mode.ToString();
        }

        private static void DrawPosterizeInfo(RetroColorProfile profile)
        {
            int levels = profile.PosterizeLevels;
            if (profile.PosterizeMode == RetroPosterizeMode.RGB)
            {
                long combinations = (long)levels * levels * levels;
                RetroColorEditorGUI.InfoLine($"RGB Posterize: {levels} levels per channel, up to {combinations:N0} RGB combinations (not a {levels}-color palette).");
            }
            else
                RetroColorEditorGUI.InfoLine($"Luminance Posterize: {levels} brightness levels; hue and saturation are kept where the gamut allows.");
            if (profile.PosterizeStrength < 1f)
                RetroColorEditorGUI.InfoLine("Partial strength mixes with the original, so more than the listed levels appear.");
            if (profile.PosterizeDither != RetroDitherMode.Off && profile.PaletteActive)
                RetroColorEditorGUI.InfoLine("Posterize dither is off while Palette Mapping is active: the palette stage dithers.", "triangle-exclamation-point");
        }

        private void DrawPaletteField(bool single)
        {
            SerializedProperty palette = serializedObject.FindProperty("palette");
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(palette, new GUIContent("Palette", "A reusable Retro Palette Profile asset."));
                using (new EditorGUI.DisabledScope(palette.objectReferenceValue == null || palette.hasMultipleDifferentValues))
                    if (RetroColorEditorGUI.IconButton("locate", "Ping the palette asset"))
                        EditorGUIUtility.PingObject(palette.objectReferenceValue);
                using (new EditorGUI.DisabledScope(!single))
                    if (RetroColorEditorGUI.IconButton("plus", "Create a palette asset"))
                        ShowCreateMenu(palette);
            }
            if (single && palette.objectReferenceValue is RetroPaletteProfile asset && asset.Count > 0)
            {
                Rect strip = EditorGUILayout.GetControlRect(false, 14f);
                strip = EditorGUI.IndentedRect(strip);
                strip.xMin += EditorGUIUtility.labelWidth;
                RetroColorEditorGUI.Swatches(strip, asset.GetColors(), 1);
            }
        }

        private void ShowCreateMenu(SerializedProperty palette)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Black and White"), false, () => CreatePalette("RetroPaletteProfile", "000000 FFFFFF"));
            menu.AddSeparator(string.Empty);
            for (int index = 0; index < ExamplePalettes.Length; index += 2)
            {
                string name = ExamplePalettes[index], hex = ExamplePalettes[index + 1];
                menu.AddItem(new GUIContent("Examples/" + name), false, () => CreatePalette(name, hex));
            }
            menu.ShowAsContext();
        }

        private void CreatePalette(string name, string hex)
        {
            string folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(target)) ?? "Assets";
            string path = EditorUtility.SaveFilePanelInProject("Create Retro Palette", name.Replace(" ", string.Empty) + " Palette", "asset", "Choose where to save the palette.", folder);
            if (string.IsNullOrEmpty(path))
                return;
            RetroPaletteImportResult parsed = RetroPaletteImport.ParseHex(hex.Replace(' ', '\n'));
            var asset = CreateInstance<RetroPaletteProfile>();
            var colors = new Color[parsed.Colors.Count];
            for (int index = 0; index < colors.Length; index++)
                colors[index] = parsed.Colors[index];
            asset.SetColors(colors);
            AssetDatabase.CreateAsset(asset, path);
            serializedObject.Update();
            serializedObject.FindProperty("palette").objectReferenceValue = asset;
            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawPaletteInfo(RetroColorProfile profile)
        {
            RetroPaletteProfile palette = profile.Palette;
            if (palette == null)
            {
                EditorGUILayout.HelpBox("No palette assigned: Palette Mapping is skipped.", MessageType.Warning);
                return;
            }
            if (!palette.IsValid)
            {
                EditorGUILayout.HelpBox($"The palette has {palette.Count} colors (valid: 1-{RetroPaletteProfile.MaxColors}). Palette Mapping is skipped.", MessageType.Warning);
                return;
            }
            RetroColorEditorGUI.InfoLine($"Palette Mapping: {palette.Count} colors.");
            RetroColorEditorGUI.InfoLine(profile.PaletteStrength >= 1f
                ? "Strict palette output applies at 100% strength."
                : "Below 100% strength the output mixes with the original, so off-palette colors are expected.");
            RetroColorEditorGUI.InfoLine("URP post-processing (tonemapping, grading, bloom), CRT and UI run later and can change displayed colors.");
        }

        private void DrawPreview(RetroColorProfile profile)
        {
            RetroColorEditorGUI.Section("Preview");
            string key = PreviewKey(profile);
            if (preview == null || key != previewKey)
            {
                previewKey = key;
                BuildPreview(profile);
            }
            Rect rect = EditorGUILayout.GetControlRect(false, 96f);
            rect = EditorGUI.IndentedRect(rect);
            float half = (rect.width - 6f) * 0.5f;
            var original = new Rect(rect.x, rect.y, half, rect.height);
            var processed = new Rect(rect.x + half + 6f, rect.y, half, rect.height);
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTextureWithTexCoords(original, preview, new Rect(0f, 0f, 0.5f, 1f));
                GUI.DrawTextureWithTexCoords(processed, preview, new Rect(0.5f, 0f, 0.5f, 1f));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Original", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField("Artistic stages", EditorStyles.centeredGreyMiniLabel);
            }
            if (profile.PaletteDither is RetroDitherMode.Custom or RetroDitherMode.BlueNoise
                || (profile.PosterizeDitherActive && profile.PosterizeDither is RetroDitherMode.Custom or RetroDitherMode.BlueNoise))
                RetroColorEditorGUI.InfoLine("Texture dither patterns are previewed without dithering.");
        }

        private static string PreviewKey(RetroColorProfile profile)
        {
            var builder = new StringBuilder();
            builder.Append(profile.PosterizeMode).Append(profile.PosterizeLevels).Append(profile.PosterizeStrength)
                .Append(profile.PosterizeDither).Append(profile.PosterizeDitherStrength).Append(profile.PaletteActive)
                .Append(profile.PaletteStrength).Append(profile.PaletteDither).Append(profile.PaletteDitherStrength)
                .Append(EditorGUIUtility.isProSkin);
            if (profile.PaletteActive)
                foreach (Color color in profile.Palette.GetColors())
                    builder.Append(RetroPaletteImport.ToHex(color));
            return builder.ToString();
        }

        // Rows: gray ramp, hue sweep, dark hue sweep, saturated-to-white ramp. Same math as the shader (RetroPSXColorMath).
        private void BuildPreview(RetroColorProfile profile)
        {
            const int rows = 4;
            int height = rows * PreviewRowHeight * 2;
            if (preview == null)
                preview = new Texture2D(PreviewWidth * 2, height, TextureFormat.RGBA32, false, false)
                {
                    hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
                };
            var pixels = new Color[preview.width * preview.height];
            for (int y = 0; y < height; y++)
            {
                int row = rows - 1 - y / (PreviewRowHeight * 2);
                for (int x = 0; x < PreviewWidth; x++)
                {
                    float u = x / (PreviewWidth - 1f);
                    Color source = row switch
                    {
                        0 => new Color(u, u, u),
                        1 => Color.HSVToRGB(u, 0.75f, 0.85f),
                        2 => Color.HSVToRGB(u, 0.6f, 0.35f),
                        _ => Color.Lerp(new Color(0.55f, 0.08f, 0.05f), Color.white, u)
                    };
                    Vector3 linear = RetroPSXColorMath.SrgbToLinear(new Vector3(source.r, source.g, source.b));
                    Vector3 result = RetroPSXColorMath.LinearToSrgb(RetroPSXColorMath.ApplyArtistic(linear, profile, x, y));
                    pixels[y * preview.width + x] = source;
                    pixels[y * preview.width + PreviewWidth + x] = new Color(result.x, result.y, result.z, 1f);
                }
            }
            preview.SetPixels(pixels);
            preview.Apply(false, false);
        }
    }
}
