using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace RetroPSX.Rendering
{
    internal sealed class RetroPSXGlobalSetupPass : ScriptableRenderPass
    {
        private readonly string label;
        private RetroPSXPipelineProfile profile;
        private bool fullPipeline;

        internal RetroPSXGlobalSetupPass(string label) => this.label = label;
        internal void SetProfile(RetroPSXPipelineProfile value, bool useFullPipeline)
        {
            profile = value;
            fullPipeline = useFullPipeline;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            bool enabled = profile != null && profile.IsComplete;
            int sourceWidth = enabled ? Mathf.Max(1, cameraData.cameraTargetDescriptor.width) : 1;
            int sourceHeight = enabled ? Mathf.Max(1, cameraData.cameraTargetDescriptor.height) : 1;
            RetroRasterContext raster = enabled
                ? RetroCameraUtility.BuildRasterContext(profile.Raster, sourceWidth, sourceHeight, fullPipeline)
                : new RetroRasterContext(
                    new Vector2Int(sourceWidth, sourceHeight),
                    new Vector2Int(sourceWidth, sourceHeight),
                    new RectInt(0, 0, sourceWidth, sourceHeight),
                    RetroPresentationMode.Stretch);
            Vector3Int bits = enabled ? profile.Color.Bits : new Vector3Int(8, 8, 8);

            Vector3 mainDirection = Vector3.down;
            Color mainColor = Color.black;
            if (enabled && lightData.mainLightIndex >= 0 && lightData.mainLightIndex < lightData.visibleLights.Length)
            {
                VisibleLight main = lightData.visibleLights[lightData.mainLightIndex];
                mainDirection = -main.localToWorldMatrix.GetColumn(2);
                mainColor = main.finalColor;
            }

            using var builder = renderGraph.AddRasterRenderPass<PassData>(label, out PassData data);
            data.InternalSize = new Vector4(raster.InternalSize.x, raster.InternalSize.y, raster.TexelSize.x, raster.TexelSize.y);
            data.SourceSize = new Vector4(raster.SourceSize.x, raster.SourceSize.y, 1f / raster.SourceSize.x, 1f / raster.SourceSize.y);
            data.GeometryParams = enabled ? new Vector4((float)profile.Geometry.PrecisionMode, profile.Geometry.SnapStrength, profile.Geometry.PrecisionScale, profile.Geometry.DistanceInfluence) : Vector4.zero;
            data.GeometryNearFade = enabled ? profile.Geometry.NearCameraFade : 0f;
            data.AffineParams = enabled ? new Vector4((float)profile.Geometry.AffineMode, profile.Geometry.AffineStrength, 0f, 0f) : Vector4.zero;
            data.ColorBits = new Vector4(bits.x, bits.y, bits.z, enabled && profile.Color.Mode != RetroColorMode.Off ? 1f : 0f);
            data.MaterialDither = enabled ? new Vector4((float)profile.Color.MaterialDither, profile.Color.MaterialDitherStrength, 0f, 0f) : Vector4.zero;
            data.LightingParams = enabled ? new Vector4((float)profile.Lighting.DefaultMode, profile.Lighting.Intensity, profile.Lighting.AdditionalLightLimit, profile.Lighting.VertexLightExaggeration) : Vector4.zero;
            data.AmbientColor = enabled ? profile.Lighting.AmbientColor.linear : Color.white;
            data.FogParams = enabled ? new Vector4((float)profile.Fog.Mode, profile.Fog.NearDistance, profile.Fog.FarDistance, profile.Fog.Steps) : Vector4.zero;
            data.FogStrength = enabled ? profile.Fog.Strength : 0f;
            data.FogColor = enabled ? profile.Fog.Color.linear : Color.black;
            data.DebugMode = enabled && (fullPipeline ||
                profile.Debug.Mode == RetroDebugMode.VolumetricDensity ||
                profile.Debug.Mode == RetroDebugMode.VolumetricBuffer ||
                profile.Debug.Mode == RetroDebugMode.VolumetricLightVisibility)
                ? (int)profile.Debug.Mode
                : 0;
            data.MainLightDirection = mainDirection;
            data.MainLightColor = mainColor;
            builder.AllowGlobalStateModification(true);
            builder.AllowPassCulling(false);
            builder.SetGlobalTextureAfterPass(renderGraph.defaultResources.blackTexture, RetroPSXShaderIDs.VolumeTexture);
            builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) => Execute(pass, context));
        }

        private static void Execute(PassData data, RasterGraphContext context)
        {
            context.cmd.SetGlobalVector(RetroPSXShaderIDs.InternalSize, data.InternalSize);
            context.cmd.SetGlobalVector(RetroPSXShaderIDs.SourceSize, data.SourceSize);
            context.cmd.SetGlobalVector(RetroPSXShaderIDs.GeometryParams, data.GeometryParams);
            context.cmd.SetGlobalFloat(RetroPSXShaderIDs.GeometryNearFade, data.GeometryNearFade);
            context.cmd.SetGlobalVector(RetroPSXShaderIDs.AffineParams, data.AffineParams);
            context.cmd.SetGlobalVector(RetroPSXShaderIDs.ColorBits, data.ColorBits);
            context.cmd.SetGlobalVector(RetroPSXShaderIDs.MaterialDither, data.MaterialDither);
            context.cmd.SetGlobalVector(RetroPSXShaderIDs.LightingParams, data.LightingParams);
            context.cmd.SetGlobalColor(RetroPSXShaderIDs.AmbientColor, data.AmbientColor);
            context.cmd.SetGlobalVector(RetroPSXShaderIDs.FogParams, data.FogParams);
            context.cmd.SetGlobalFloat(RetroPSXShaderIDs.FogStrength, data.FogStrength);
            context.cmd.SetGlobalColor(RetroPSXShaderIDs.FogColor, data.FogColor);
            context.cmd.SetGlobalInt(RetroPSXShaderIDs.DebugMode, data.DebugMode);
            context.cmd.SetGlobalVector(RetroPSXShaderIDs.MainLightDirection, data.MainLightDirection);
            context.cmd.SetGlobalColor(RetroPSXShaderIDs.MainLightColor, data.MainLightColor);
        }

        private sealed class PassData
        {
            internal Vector4 InternalSize;
            internal Vector4 SourceSize;
            internal Vector4 GeometryParams;
            internal float GeometryNearFade;
            internal Vector4 AffineParams;
            internal Vector4 ColorBits;
            internal Vector4 MaterialDither;
            internal Vector4 LightingParams;
            internal Color AmbientColor;
            internal Vector4 FogParams;
            internal float FogStrength;
            internal Color FogColor;
            internal int DebugMode;
            internal Vector4 MainLightDirection;
            internal Color MainLightColor;
        }
    }
}
