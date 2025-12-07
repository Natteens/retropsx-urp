using UnityEngine;
using UnityEngine. Rendering;
using UnityEngine. Rendering.Universal;
using UnityEngine. Rendering.RenderGraphModule;

public class TestSimpleFeature : ScriptableRendererFeature
{
    TestPass testPass;

    public override void Create()
    {
        testPass = new TestPass();
        testPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        Debug.Log("TestSimpleFeature Created!");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData. cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(testPass);
        }
    }

    protected override void Dispose(bool disposing) { }
}

public class TestPass : ScriptableRenderPass
{
    private class PassData
    {
        internal TextureHandle source;
    }

    private static void ExecutePass(PassData data, RasterGraphContext context)
    {
        // SÓ LIMPA A TELA COM VERMELHO - NADA MAIS
        context.cmd.ClearRenderTarget(false, true, Color.red);
        Debug.Log("ExecutePass - Tela deveria estar VERMELHA agora!");
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        TextureHandle cameraTex = resourceData. activeColorTexture;
        
        if (! cameraTex.IsValid())
        {
            Debug.LogError("Camera texture INVALID!");
            return;
        }

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Test Red Pass", out var passData))
        {
            passData.source = cameraTex;
            
            // Renderiza direto no cameraColor
            builder.SetRenderAttachment(cameraTex, 0, AccessFlags.Write);
            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
        }
    }
}