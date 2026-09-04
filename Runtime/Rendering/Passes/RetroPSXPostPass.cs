using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace RetroPSX.Rendering
{
    internal sealed class RetroPSXPostPass : ScriptableRenderPass
    {
        private const int MaximumLocalLights = 4;

        private readonly Material resolveMaterial;
        private readonly Material presentationMaterial;
        private readonly Material volumetricMaterial;
        private readonly Material volumetricCompositeMaterial;
        private readonly Material crtMaterial;
        private readonly RetroVolumetricLight[] visibleLights = new RetroVolumetricLight[MaximumLocalLights];
        private readonly Vector4[] lightPosRange = new Vector4[MaximumLocalLights];
        private readonly Vector4[] lightDirAngle = new Vector4[MaximumLocalLights];
        private readonly Vector4[] lightColorDensity = new Vector4[MaximumLocalLights];
        private readonly Vector4[] lightParams = new Vector4[MaximumLocalLights];
        private readonly Vector4[] patternTransform = new Vector4[MaximumLocalLights];
        private readonly Vector4[] patternParams = new Vector4[MaximumLocalLights];
        private readonly Vector4[] patternExtra = new Vector4[MaximumLocalLights];
        private readonly Vector4[] lightStylization = new Vector4[MaximumLocalLights];

        private RetroPSXPipelineProfile profile;
        private bool fullPipeline;
        private bool preserveAlpha;

        internal RetroPSXPostPass(Material resolve, Material presentation, Material volumetric, Material volumetricComposite, Material crt)
        {
            resolveMaterial = resolve;
            presentationMaterial = presentation;
            volumetricMaterial = volumetric;
            volumetricCompositeMaterial = volumetricComposite;
            crtMaterial = crt;
            requiresIntermediateTexture = true;
        }

        internal bool HasRequiredMaterials => resolveMaterial != null && presentationMaterial != null;
        internal void SetProfile(RetroPSXPipelineProfile value, bool useFullPipeline, bool preserveOutputAlpha)
        {
            profile = value;
            fullPipeline = useFullPipeline;
            preserveAlpha = preserveOutputAlpha;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (profile == null || !profile.IsComplete)
                return;

            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            if (resources.isActiveTargetBackBuffer || !resources.activeColorTexture.IsValid())
                return;

            TextureHandle source = resources.activeColorTexture;
            TextureDesc sourceDesc = renderGraph.GetTextureDesc(source);
            RetroRasterContext raster = RetroCameraUtility.BuildRasterContext(
                profile.Raster, sourceDesc.width, sourceDesc.height, fullPipeline);
            TextureDesc lowDesc = sourceDesc;
            lowDesc.name = "RetroPSX.CanonicalColor";
            lowDesc.width = raster.InternalSize.x;
            lowDesc.height = raster.InternalSize.y;
            lowDesc.depthBufferBits = DepthBits.None;
            lowDesc.msaaSamples = MSAASamples.None;
            lowDesc.bindTextureMS = false;
            lowDesc.useMipMap = false;
            lowDesc.filterMode = FilterMode.Point;
            lowDesc.clearBuffer = false;
            TextureHandle depth = resources.cameraDepthTexture.IsValid() ? resources.cameraDepthTexture : resources.activeDepthTexture;
            bool useVolumetrics = profile.Volumetrics.Enabled && depth.IsValid() && volumetricMaterial != null && volumetricCompositeMaterial != null;

            if (!fullPipeline)
            {
                if (useVolumetrics)
                    resources.cameraColor = AddVolumetrics(renderGraph, cameraData, lightData, raster, lowDesc, source, depth,
                        resources.mainShadowsTexture, resources.additionalShadowsTexture, false, preserveAlpha);
                return;
            }

            TextureHandle canonical = renderGraph.CreateTexture(lowDesc);
            ConfigureResolveMaterial(raster, !useVolumetrics, preserveAlpha);
            AddBlit(renderGraph, "RetroPSX / Canonical Resolve", source, canonical, resolveMaterial, 0, depth,
                TextureHandle.nullHandle, TextureHandle.nullHandle, false);

            TextureHandle lowFinal = canonical;
            if (useVolumetrics)
                lowFinal = AddVolumetrics(renderGraph, cameraData, lightData, raster, lowDesc, canonical, depth,
                    resources.mainShadowsTexture, resources.additionalShadowsTexture, true, preserveAlpha);

            TextureDesc outputDesc = sourceDesc;
            outputDesc.name = "RetroPSX.PresentedColor";
            outputDesc.depthBufferBits = DepthBits.None;
            outputDesc.msaaSamples = MSAASamples.None;
            outputDesc.bindTextureMS = false;
            outputDesc.useMipMap = false;
            outputDesc.filterMode = FilterMode.Point;
            outputDesc.clearBuffer = false;
            TextureHandle presented = lowFinal;
            if (!raster.IsNative || raster.PresentationMode != RetroPresentationMode.Stretch)
            {
                presented = renderGraph.CreateTexture(outputDesc);
                ConfigurePresentationMaterial(raster, preserveAlpha);
                AddBlit(renderGraph, "RetroPSX / Point Presentation", lowFinal, presented, presentationMaterial, 0,
                    TextureHandle.nullHandle, TextureHandle.nullHandle, TextureHandle.nullHandle, false);
            }

            TextureHandle final = presented;
            if (profile.Display.Enabled && crtMaterial != null)
            {
                TextureDesc crtDesc = outputDesc;
                crtDesc.name = "RetroPSX.DisplayColor";
                final = renderGraph.CreateTexture(crtDesc);
                ConfigureCRTMaterial(preserveAlpha);
                AddBlit(renderGraph, "RetroPSX / Display Simulation", presented, final, crtMaterial, 0,
                    TextureHandle.nullHandle, TextureHandle.nullHandle, TextureHandle.nullHandle, false);
            }

            resources.cameraColor = final;
        }

        private void ConfigureResolveMaterial(RetroRasterContext raster, bool applyFinalColor, bool preserveOutputAlpha)
        {
            RetroColorProfile color = profile.Color;
            Vector3Int bits = color.Bits;
            resolveMaterial.SetVector(RetroPSXShaderIDs.InternalSize, new Vector4(raster.InternalSize.x, raster.InternalSize.y, raster.TexelSize.x, raster.TexelSize.y));
            resolveMaterial.SetVector(RetroPSXShaderIDs.ColorBits, new Vector4(bits.x, bits.y, bits.z, color.Mode == RetroColorMode.Off ? 0f : 1f));
            resolveMaterial.SetVector(RetroPSXShaderIDs.FinalColorParams, new Vector4(
                applyFinalColor && color.QuantizeFinalImage ? 1f : 0f,
                (float)color.FinalImageDither,
                color.FinalImageDitherStrength,
                profile.Fog.Enabled && profile.Fog.ApplyToWholeFrame ? 1f : 0f));
            resolveMaterial.SetTexture(RetroPSXShaderIDs.CustomDither, color.CustomPattern != null ? color.CustomPattern : Texture2D.grayTexture);
            resolveMaterial.SetTexture(RetroPSXShaderIDs.BlueNoise, color.BlueNoise != null ? color.BlueNoise : Texture2D.grayTexture);
            resolveMaterial.SetFloat(RetroPSXShaderIDs.PreserveAlpha, preserveOutputAlpha ? 1f : 0f);
        }

        private TextureHandle AddVolumetrics(
            RenderGraph renderGraph,
            UniversalCameraData cameraData,
            UniversalLightData lightData,
            RetroRasterContext raster,
            TextureDesc lowDesc,
            TextureHandle canonical,
            TextureHandle depth,
            TextureHandle mainShadows,
            TextureHandle additionalShadows,
            bool applyFinalColor,
            bool preserveOutputAlpha)
        {
            profile.Volumetrics.GetQuality(out int divisor, out int steps);
            TextureDesc volumeDesc = lowDesc;
            volumeDesc.name = "RetroPSX.VolumetricBuffer";
            volumeDesc.width = Mathf.Max(1, raster.InternalSize.x / divisor);
            volumeDesc.height = Mathf.Max(1, raster.InternalSize.y / divisor);
            // Volumetric shadow edges are not necessarily camera-depth edges. Bilinear
            // filtering here can therefore mix a lit texel into shadowed media before
            // the depth-aware reconstruction gets a chance to reject it.
            volumeDesc.filterMode = FilterMode.Point;
            TextureHandle volume = renderGraph.CreateTexture(volumeDesc);

            ConfigureVolumetricMaterial(cameraData.camera, steps, lightData);
            AddBlit(renderGraph, "RetroPSX / Volumetric Raymarch", depth, volume, volumetricMaterial, 0, depth,
                mainShadows, additionalShadows, true, RetroPSXShaderIDs.VolumeTexture);

            TextureDesc compositeDesc = lowDesc;
            compositeDesc.name = "RetroPSX.VolumetricComposite";
            TextureHandle composite = renderGraph.CreateTexture(compositeDesc);
            volumetricCompositeMaterial.SetVector(RetroPSXShaderIDs.VolumeTexelSize, new Vector4(
                1f / volumeDesc.width, 1f / volumeDesc.height, volumeDesc.width, volumeDesc.height));
            volumetricCompositeMaterial.SetVector(RetroPSXShaderIDs.VolumeParams3, new Vector4(
                profile.Volumetrics.GeometryDepthBias, profile.Volumetrics.DepthEdgeSharpness, 0f, 0f));
            RetroColorProfile color = profile.Color;
            volumetricCompositeMaterial.SetVector(RetroPSXShaderIDs.FinalColorParams, new Vector4(
                applyFinalColor && color.QuantizeFinalImage ? 1f : 0f,
                (float)color.FinalImageDither,
                color.FinalImageDitherStrength,
                0f));
            volumetricCompositeMaterial.SetTexture(RetroPSXShaderIDs.CustomDither, color.CustomPattern != null ? color.CustomPattern : Texture2D.grayTexture);
            volumetricCompositeMaterial.SetTexture(RetroPSXShaderIDs.BlueNoise, color.BlueNoise != null ? color.BlueNoise : Texture2D.grayTexture);
            volumetricCompositeMaterial.SetFloat(RetroPSXShaderIDs.PreserveAlpha, preserveOutputAlpha ? 1f : 0f);
            AddBlit(renderGraph, "RetroPSX / Volumetric Composite", canonical, composite, volumetricCompositeMaterial, 0, volume,
                depth, TextureHandle.nullHandle, false);
            return composite;
        }

        private void ConfigureVolumetricMaterial(Camera camera, int steps, UniversalLightData lightData)
        {
            RetroVolumetricProfile volume = profile.Volumetrics;
            volumetricMaterial.SetVector(RetroPSXShaderIDs.VolumeParams0, new Vector4(volume.Density, volume.MaxDistance, volume.BaseHeight, volume.HeightFalloff));
            volumetricMaterial.SetVector(RetroPSXShaderIDs.VolumeParams1, new Vector4(volume.Extinction, volume.Scattering, volume.Anisotropy, volume.DirectionalContribution));
            volumetricMaterial.SetVector(RetroPSXShaderIDs.VolumeParams2, new Vector4(
                steps, volume.LightSteps, volume.Jitter ? volume.JitterStrength : 0f, volume.DensitySteps));
            volumetricMaterial.SetVector(RetroPSXShaderIDs.VolumeParams3, new Vector4(
                volume.GeometryDepthBias, volume.DepthEdgeSharpness, profile.Debug.VolumetricLightIndex, 0f));
            volumetricMaterial.SetColor(RetroPSXShaderIDs.VolumeAmbient, volume.Ambient.linear);

            int count = RetroVolumetricLightRegistry.CopyVisible(camera, visibleLights, volume.MaximumLocalLights);
            for (int index = 0; index < MaximumLocalLights; index++)
                PackLight(index, index < count ? visibleLights[index] : null, lightData);

            volumetricMaterial.SetInt(RetroPSXShaderIDs.LocalLightCount, count);
            volumetricMaterial.SetVectorArray(RetroPSXShaderIDs.LocalLightPosRange, lightPosRange);
            volumetricMaterial.SetVectorArray(RetroPSXShaderIDs.LocalLightDirAngle, lightDirAngle);
            volumetricMaterial.SetVectorArray(RetroPSXShaderIDs.LocalLightColorDensity, lightColorDensity);
            volumetricMaterial.SetVectorArray(RetroPSXShaderIDs.LocalLightParams, lightParams);
            volumetricMaterial.SetVectorArray(RetroPSXShaderIDs.LocalPatternTransform, patternTransform);
            volumetricMaterial.SetVectorArray(RetroPSXShaderIDs.LocalPatternParams, patternParams);
            volumetricMaterial.SetVectorArray(RetroPSXShaderIDs.LocalPatternExtra, patternExtra);
            volumetricMaterial.SetVectorArray(RetroPSXShaderIDs.LocalLightStylization, lightStylization);
        }

        private void PackLight(int index, RetroVolumetricLight volume, UniversalLightData lightData)
        {
            Texture2D texture = Texture2D.whiteTexture;
            if (volume == null || volume.Light == null)
            {
                lightPosRange[index] = Vector4.zero;
                lightDirAngle[index] = Vector4.zero;
                lightColorDensity[index] = Vector4.zero;
                lightParams[index] = Vector4.zero;
                patternTransform[index] = new Vector4(1f, 1f, 0f, 0f);
                patternParams[index] = new Vector4(0f, 1f, 0.5f, 0.1f);
                patternExtra[index] = Vector4.zero;
                lightStylization[index] = new Vector4(1f, 0f, -1f, 0f);
                SetPatternTexture(index, texture);
                return;
            }

            Light light = volume.Light;
            Transform transform = light.transform;
            Color linear = light.color.linear * (light.intensity * volume.Intensity);
            float outerCos = light.type == LightType.Spot ? Mathf.Cos(light.spotAngle * Mathf.Deg2Rad * 0.5f) : -1f;
            lightPosRange[index] = new Vector4(transform.position.x, transform.position.y, transform.position.z, light.range);
            Vector3 forward = transform.forward;
            lightDirAngle[index] = new Vector4(forward.x, forward.y, forward.z, outerCos);
            lightColorDensity[index] = new Vector4(linear.r, linear.g, linear.b, volume.Density);
            int shadowIndex = volume.UsesLightShadows ? FindAdditionalLightIndex(light, lightData) : -1;
            // The shadow index is only meaningful while URP has included the light for this
            // camera.  The shader also checks that an atlas slice was allocated: a missing
            // slice must never silently turn into unshadowed direct volumetric lighting.
            lightStylization[index] = new Vector4(
                volume.BeamSharpness,
                volume.PatternStrength,
                shadowIndex,
                volume.UsesLightShadows ? 1f : 0f);

            RetroVolumetricPattern pattern = volume.Pattern;
            RetroVolumetricPatternType type = pattern != null ? pattern.Type : RetroVolumetricPatternType.None;
            lightParams[index] = new Vector4(light.type == LightType.Spot ? 1f : 0f, volume.EdgeSoftness, (float)type, volume.NoiseDistortion);
            if (pattern != null)
            {
                Vector2 offset = pattern.Offset + pattern.ScrollVelocity * Time.time;
                patternTransform[index] = new Vector4(pattern.Scale.x, pattern.Scale.y, offset.x, offset.y);
                patternParams[index] = new Vector4(pattern.Rotation * Mathf.Deg2Rad, pattern.Contrast, pattern.Threshold, pattern.Softness);
                patternExtra[index] = new Vector4(pattern.Inverted ? 1f : 0f, (float)pattern.Mapping, volume.Blink ? volume.BlinkRate : 0f, volume.BlinkDuty);
                texture = pattern.Texture != null ? pattern.Texture : Texture2D.whiteTexture;
            }
            else
            {
                patternTransform[index] = new Vector4(1f, 1f, 0f, 0f);
                patternParams[index] = new Vector4(0f, 1f, 0.5f, 0.1f);
                patternExtra[index] = new Vector4(0f, 0f, volume.Blink ? volume.BlinkRate : 0f, volume.BlinkDuty);
            }
            SetPatternTexture(index, texture);
        }

        private static int FindAdditionalLightIndex(Light light, UniversalLightData lightData)
        {
            if (light == null || light.shadows == LightShadows.None)
                return -1;

            int additionalIndex = 0;
            for (int index = 0; index < lightData.visibleLights.Length; index++)
            {
                if (index == lightData.mainLightIndex)
                    continue;
                if (lightData.visibleLights[index].light == light)
                    return additionalIndex;
                additionalIndex++;
            }
            return -1;
        }

        private void SetPatternTexture(int index, Texture texture)
        {
            int id = index switch
            {
                0 => RetroPSXShaderIDs.Pattern0,
                1 => RetroPSXShaderIDs.Pattern1,
                2 => RetroPSXShaderIDs.Pattern2,
                _ => RetroPSXShaderIDs.Pattern3
            };
            volumetricMaterial.SetTexture(id, texture);
        }

        private void ConfigurePresentationMaterial(RetroRasterContext raster, bool preserveOutputAlpha)
        {
            RectInt rect = raster.Viewport;
            presentationMaterial.SetVector(RetroPSXShaderIDs.PresentationRect, new Vector4(
                rect.x / (float)raster.SourceSize.x,
                rect.y / (float)raster.SourceSize.y,
                rect.width / (float)raster.SourceSize.x,
                rect.height / (float)raster.SourceSize.y));
            presentationMaterial.SetColor(RetroPSXShaderIDs.LetterboxColor, profile.Raster.LetterboxColor.linear);
            presentationMaterial.SetFloat(RetroPSXShaderIDs.PreserveAlpha, preserveOutputAlpha ? 1f : 0f);
        }

        private void ConfigureCRTMaterial(bool preserveOutputAlpha)
        {
            RetroDisplayProfile display = profile.Display;
            crtMaterial.SetVector(RetroPSXShaderIDs.CRTParams0, new Vector4(display.Scanlines, display.MaskStrength, (float)display.MaskMode, display.Curvature));
            crtMaterial.SetVector(RetroPSXShaderIDs.CRTParams1, new Vector4(display.Overscan, display.Vignette, display.HorizontalBleed, display.ChromaBleed));
            crtMaterial.SetVector(RetroPSXShaderIDs.CRTParams2, new Vector4(display.ChromaticMisalignment, display.SignalNoise, display.Brightness, display.Interlacing ? 1f : 0f));
            crtMaterial.SetFloat(RetroPSXShaderIDs.PixelBloom, display.PixelBloom);
            crtMaterial.SetFloat(RetroPSXShaderIDs.PreserveAlpha, preserveOutputAlpha ? 1f : 0f);
        }

        private static void AddBlit(
            RenderGraph graph,
            string name,
            TextureHandle source,
            TextureHandle destination,
            Material material,
            int materialPass,
            TextureHandle extra0,
            TextureHandle extra1,
            TextureHandle extra2,
            bool useAllGlobalTextures,
            int globalAfterPass = 0)
        {
            using var builder = graph.AddRasterRenderPass<BlitPassData>(name, out BlitPassData data);
            data.Source = source;
            data.Material = material;
            data.MaterialPass = materialPass;
            builder.UseTexture(source, AccessFlags.Read);
            if (extra0.IsValid() && extra0 != source)
                builder.UseTexture(extra0, AccessFlags.Read);
            if (extra1.IsValid() && extra1 != source && extra1 != extra0)
                builder.UseTexture(extra1, AccessFlags.Read);
            if (extra2.IsValid() && extra2 != source && extra2 != extra0 && extra2 != extra1)
                builder.UseTexture(extra2, AccessFlags.Read);
            if (useAllGlobalTextures)
                builder.UseAllGlobalTextures(true);
            builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
            if (globalAfterPass != 0)
                builder.SetGlobalTextureAfterPass(destination, globalAfterPass);
            builder.SetRenderFunc(static (BlitPassData pass, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, pass.Source, new Vector4(1f, 1f, 0f, 0f), pass.Material, pass.MaterialPass);
            });
        }

        private sealed class BlitPassData
        {
            internal TextureHandle Source;
            internal Material Material;
            internal int MaterialPass;
        }
    }
}
