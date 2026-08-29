using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace RetroPSX.Rendering
{
    /// <summary>Redraws the reserved native UI layer after RetroPSX presentation while retaining the camera depth buffer.</summary>
    internal sealed class RetroPSXNativeUIPass : ScriptableRenderPass
    {
        private static readonly List<ShaderTagId> ShaderTags = new()
        {
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("LightweightForward")
        };

        private RetroPSXPipelineProfile profile;

        internal void SetProfile(RetroPSXPipelineProfile value) => profile = value;

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (profile == null || !profile.IsComplete || profile.UI == null || !profile.UI.HasNativeWorldSpaceLayer)
                return;

            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            if (!resources.activeColorTexture.IsValid() || !resources.activeDepthTexture.IsValid())
                return;

            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            // PanelRenderer/UIRenderer may select an opaque or transparent UI material depending on panel content.
            // The profile reserves this layer exclusively for native world-space UI, so redraw the entire layer.
            FilteringSettings filtering = new(RenderQueueRange.all, 1 << profile.UI.NativeWorldSpaceLayer);
            DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(
                ShaderTags, renderingData, cameraData, lightData, SortingCriteria.CommonTransparent);
            NativeArray<ShaderTagId> tagValues = new(1, Allocator.Temp);
            NativeArray<RenderStateBlock> stateBlocks = new(1, Allocator.Temp);
            RendererListHandle rendererList;
            try
            {
                tagValues[0] = ShaderTagId.none;
                stateBlocks[0] = new RenderStateBlock(RenderStateMask.Depth)
                {
                    depthState = new DepthState(false, CompareFunction.LessEqual)
                };
                RendererListParams parameters = new(renderingData.cullResults, drawing, filtering)
                {
                    tagValues = tagValues,
                    stateBlocks = stateBlocks,
                    isPassTagName = false
                };
                rendererList = renderGraph.CreateRendererList(parameters);
            }
            finally
            {
                tagValues.Dispose();
                stateBlocks.Dispose();
            }

            using var builder = renderGraph.AddRasterRenderPass<PassData>("RetroPSX / Native World-Space UI", out PassData data);
            data.RendererList = rendererList;
            if (!data.RendererList.IsValid())
                return;

            builder.UseAllGlobalTextures(true);
            builder.UseRendererList(data.RendererList);
            builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.Write);
            // UI Toolkit uses stencil for clipping. ReadWrite retains world occlusion and its normal masking behavior.
            builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
            builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) => context.cmd.DrawRendererList(pass.RendererList));
        }

        private sealed class PassData
        {
            internal RendererListHandle RendererList;
        }
    }
}
