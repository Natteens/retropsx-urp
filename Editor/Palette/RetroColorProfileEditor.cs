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
        private const float NarrowWidth = 330f;
        private const string PreviewFoldoutKey = "RetroPSX.ColorProfile.Preview";

        private static readonly string[] ExamplePalettes =
        {
            "Olive", "101413 242D2B 414C43 68745D A5A184 D6CBB0",
            "Indigo", "0B0D19 1C2338 343E60 656F8F 999BB8 D9D6E4",
            "Rust", "181411 39251F 70432F A86A43 CF9A65 EBD5AB",
            "Green", "171A18 414A40 858C72 D8D6B5",
            "Grayscale", "000000 404040 808080 BFBFBF FFFFFF"
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

            RetroColorEditorGUI.Section("Material Color Precision");
            SerializedProperty mode = Property("mode", "Color Mode", "Per-channel bit depth used by RetroPSX materials.");
            if (mode.hasMultipleDifferentValues || mode.enumValueIndex == (int)RetroColorMode.Custom)
                Property("customBits", "Custom Bits", "Bits per channel (1-8).");
            SerializedProperty materialDither = Property("materialDither", "Material Dither", "Dither applied by RetroPSX materials before their quantization. Materials support PSX and Bayer patterns.");
            if (Differs(materialDither) || materialDither.enumValueIndex != (int)RetroDitherMode.Off)
                Property("materialDitherStrength", "Dither Strength", "Legacy amplitude scale (0-2).");
            if (single && profile.MaterialDither is RetroDitherMode.Custom or RetroDitherMode.BlueNoise)
                RetroColorEditorGUI.InfoLine("Materials only support PSX and Bayer patterns: no material dither is applied.", "triangle-exclamation-point");

            RetroColorEditorGUI.Section("Final Image Precision");
            bool colorModeOff = !mode.hasMultipleDifferentValues && mode.enumValueIndex == (int)RetroColorMode.Off;
            SerializedProperty quantize = Property("quantizeFinalImage", "Enabled", "Quantizes the whole image (plain URP materials included) with the Color Mode bit depth, before the artistic stages.");
            if (Differs(quantize) || quantize.boolValue)
            {
                using (new EditorGUI.DisabledScope(colorModeOff))
                {
                    SerializedProperty finalDither = Property("finalImageDither", "Dither", "Legacy dither, scaled for RGB bit quantization.");
                    if (Differs(finalDither) || finalDither.enumValueIndex != (int)RetroDitherMode.Off)
                        Property("finalImageDitherStrength", "Dither Strength", "Legacy amplitude scale (0-2).");
                }
                if (colorModeOff)
                    RetroColorEditorGUI.InfoLine("Needs a Color Mode: no effect while Color Mode is Off.", "triangle-exclamation-point");
            }

            RetroColorEditorGUI.Section("Posterization");
            SerializedProperty posterize = Property("posterizeMode", "Mode", "RGB: N levels per channel (up to N³ colors). Luminance: N brightness levels, hue and saturation kept where the gamut allows.");
            if (Differs(posterize) || posterize.enumValueIndex != (int)RetroPosterizeMode.Off)
            {
                IntSlider("posterizeLevels", "Levels", "Exact number of levels (2-64), not limited to powers of two.");
                Property("posterizeStrength", "Strength", "0 bypasses, 1 is full posterization; values in between mix with the incoming color.");
                bool paletteOwnsDither = single && profile.PaletteActive;
                string ownedTooltip = paletteOwnsDither ? " Disabled: Palette Mapping is active and dithers instead (the value is kept)." : string.Empty;
                using (new EditorGUI.DisabledScope(paletteOwnsDither))
                {
                    SerializedProperty posterizeDither = Property("posterizeDither", "Dither", "Ordered dither between adjacent levels, in internal-resolution pixels." + ownedTooltip);
                    if (Differs(posterizeDither) || posterizeDither.enumValueIndex != (int)RetroDitherMode.Off)
                        Property("posterizeDitherStrength", "Dither Strength", "0 rounds to the nearest level, 1 distributes pixels fully between the two levels." + ownedTooltip);
                }
                if (paletteOwnsDither && profile.PosterizeDither != RetroDitherMode.Off)
                    RetroColorEditorGUI.InfoLine("Dithered by Palette Mapping.");
            }

            RetroColorEditorGUI.Section("Palette Mapping");
            SerializedProperty paletteEnabled = Property("paletteEnabled", "Enabled", "Maps every pixel to the nearest palette color (OKLab). URP post-processing, CRT and UI run later and can change displayed colors.");
            if (Differs(paletteEnabled) || paletteEnabled.boolValue)
            {
                DrawPaletteField(single);
                Property("paletteStrength", "Strength", "0 bypasses; 1 outputs only palette colors at this stage; values in between mix with the incoming color.");
                SerializedProperty paletteDither = Property("paletteDither", "Dither", "Ordered dither between two palette colors; never adds off-palette colors.");
                if (Differs(paletteDither) || paletteDither.enumValueIndex != (int)RetroDitherMode.Off)
                    Property("paletteDitherStrength", "Dither Strength", "0 uses the nearest color only, 1 distributes pixels fully between the two colors.");
                if (single)
                    DrawPaletteWarning(profile.Palette);
            }

            DrawDitherTextures(profile, single);
            serializedObject.ApplyModifiedProperties();
            if (single && (profile.PosterizeActive || profile.PaletteActive))
                DrawPreview(profile);
        }

        private static bool Differs(SerializedProperty property) => property.hasMultipleDifferentValues;

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

        // A texture field is shown only when a stage that is actually running uses its pattern. Hidden
        // textures stay assigned.
        private void DrawDitherTextures(RetroColorProfile profile, bool single)
        {
            bool multi = !single;
            bool custom = multi || StageUses(profile, RetroDitherMode.Custom);
            bool blueNoise = multi || StageUses(profile, RetroDitherMode.BlueNoise);
            if (!custom && !blueNoise)
                return;

            RetroColorEditorGUI.Section("Dither Textures");
            if (custom)
            {
                Property("customPattern", "Custom Pattern", "Threshold texture tiled per internal pixel (red channel). Import it with sRGB off, Point filtering and no compression.");
                if (single && profile.CustomPattern == null)
                    RetroColorEditorGUI.InfoLine("No texture: Custom dither is skipped.", "triangle-exclamation-point");
            }
            if (blueNoise)
            {
                Property("blueNoise", "Blue Noise", "Optional override. When empty, the built-in 64×64 blue-noise texture is used.");
                if (single && profile.BlueNoise == null)
                {
                    Texture2D fallback = RetroColorProfile.DefaultBlueNoise;
                    RetroColorEditorGUI.InfoLine(fallback != null ? $"Default · {fallback.width}×{fallback.height}" : "Built-in blue noise is missing: Blue Noise dither is skipped.",
                        fallback != null ? null : "triangle-exclamation-point");
                }
            }
        }

        private static bool StageUses(RetroColorProfile profile, RetroDitherMode pattern)
        {
            return (profile.FinalImagePrecisionActive && profile.FinalImageDither == pattern)
                || (profile.PosterizeActive && !profile.PaletteActive && profile.PosterizeDither == pattern)
                || (profile.PaletteEnabled && profile.PaletteStrength > 0f && profile.PaletteDither == pattern);
        }

        private void DrawPaletteField(bool single)
        {
            SerializedProperty palette = serializedObject.FindProperty("palette");
            var content = new GUIContent("Palette", "A reusable Retro Palette Profile asset.");
            const float buttons = 2f * 22f + 2f;
            bool narrow = EditorGUIUtility.currentViewWidth < NarrowWidth;
            Rect row = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            Rect field = row;
            if (!narrow)
                field.xMax -= buttons;
            EditorGUI.PropertyField(field, palette, content);
            Rect icons = narrow ? EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight)) : row;
            var create = new Rect(icons.xMax - 20f, icons.y, 20f, icons.height);
            var ping = new Rect(create.x - 24f, icons.y, 20f, icons.height);
            using (new EditorGUI.DisabledScope(palette.objectReferenceValue == null || palette.hasMultipleDifferentValues))
                if (RetroColorEditorGUI.IconButton(ping, "locate", "Ping the palette asset"))
                    EditorGUIUtility.PingObject(palette.objectReferenceValue);
            using (new EditorGUI.DisabledScope(!single))
                if (RetroColorEditorGUI.IconButton(create, "plus", "Create a palette asset"))
                    ShowCreateMenu();

            if (single && palette.objectReferenceValue is RetroPaletteProfile asset && asset.Count > 0)
            {
                Rect strip = EditorGUILayout.GetControlRect(true, 12f);
                strip.xMin += EditorGUIUtility.labelWidth + 2f;
                if (!narrow)
                    strip.xMax -= buttons;
                RetroColorEditorGUI.Swatches(strip, asset.GetColors(), asset.Count > 32 ? 2 : 1);
            }
        }

        private void ShowCreateMenu()
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

        private static void DrawPaletteWarning(RetroPaletteProfile palette)
        {
            if (palette == null)
                EditorGUILayout.HelpBox("No palette assigned: Palette Mapping is skipped.", MessageType.Warning);
            else if (!palette.IsValid)
                EditorGUILayout.HelpBox($"The palette has {palette.Count} colors (valid: 1-{RetroPaletteProfile.MaxColors}). Palette Mapping is skipped.", MessageType.Warning);
        }

        private void DrawPreview(RetroColorProfile profile)
        {
            EditorGUILayout.Space(6f);
            bool expanded = SessionState.GetBool(PreviewFoldoutKey, true);
            bool next = EditorGUILayout.Foldout(expanded, "Preview", true, EditorStyles.foldoutHeader);
            if (next != expanded)
                SessionState.SetBool(PreviewFoldoutKey, next);
            if (!next)
                return;

            string key = PreviewKey(profile);
            if (preview == null || key != previewKey)
            {
                previewKey = key;
                BuildPreview(profile);
            }
            const float gap = 6f;
            // Height from the view width so Layout and Repaint agree; images keep a 2:1 aspect up to 96 px.
            float imageHeight = Mathf.Clamp((EditorGUIUtility.currentViewWidth - 40f) * 0.25f, 32f, 96f);
            Rect images = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, imageHeight));
            float half = Mathf.Max(1f, (images.width - gap) * 0.5f);
            var original = new Rect(images.x, images.y, half, images.height);
            var processed = new Rect(images.x + half + gap, images.y, half, images.height);
            Rect labels = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTextureWithTexCoords(original, preview, new Rect(0f, 0f, 0.5f, 1f));
                GUI.DrawTextureWithTexCoords(processed, preview, new Rect(0.5f, 0f, 0.5f, 1f));
            }
            GUI.Label(new Rect(original.x, labels.y, original.width, labels.height), "Original", EditorStyles.centeredGreyMiniLabel);
            GUI.Label(new Rect(processed.x, labels.y, processed.width, labels.height), "Processed", EditorStyles.centeredGreyMiniLabel);
            if (UsesUnreadablePattern(profile))
                RetroColorEditorGUI.InfoLine("The dither texture is not CPU-readable: previewed without its pattern.");
        }

        private static bool UsesUnreadablePattern(RetroColorProfile profile)
        {
            RetroDitherMode mode = profile.PaletteActive
                ? profile.EffectiveDither(profile.PaletteDither)
                : profile.PosterizeDitherActive ? profile.EffectiveDither(profile.PosterizeDither) : RetroDitherMode.Off;
            Texture2D pattern = mode == RetroDitherMode.Custom ? profile.CustomPattern : mode == RetroDitherMode.BlueNoise ? profile.EffectiveBlueNoise : null;
            return pattern != null && !pattern.isReadable;
        }

        private static string PreviewKey(RetroColorProfile profile)
        {
            var builder = new StringBuilder();
            builder.Append(profile.PosterizeMode).Append(profile.PosterizeLevels).Append(profile.PosterizeStrength)
                .Append(profile.PosterizeDither).Append(profile.PosterizeDitherStrength).Append(profile.PaletteActive)
                .Append(profile.PaletteStrength).Append(profile.PaletteDither).Append(profile.PaletteDitherStrength)
                .Append(profile.CustomPattern != null ? profile.CustomPattern.GetHashCode() : 0).Append('/')
                .Append(profile.EffectiveBlueNoise != null ? profile.EffectiveBlueNoise.GetHashCode() : 0);
            if (profile.PaletteActive)
                foreach (Color color in profile.Palette.GetColors())
                    builder.Append(RetroPaletteImport.ToHex(color));
            return builder.ToString();
        }

        // Rows: gray ramp, hue sweep, dark hue sweep, saturated-to-white ramp. Same math as the shader
        // (RetroPSXColorMath), one texel per internal pixel.
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
