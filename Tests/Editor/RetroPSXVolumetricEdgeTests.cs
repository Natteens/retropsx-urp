using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace RetroPSX.Tests
{
    public sealed class RetroPSXVolumetricEdgeTests
    {
        [Test]
        public void BackgroundVolumeDoesNotBleedOntoForegroundSilhouette()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("This regression requires GPU rendering.");

            var shader = Shader.Find("Hidden/RetroPSX/VolumetricComposite");
            Assert.That(shader, Is.Not.Null);
            VerifySilhouette(shader);
        }

        private static void VerifySilhouette(Shader shader)
        {
            const int width = 32, height = 8;
            var material = new Material(shader);
            var color = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true) { filterMode = FilterMode.Point };
            var depth = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true) { filterMode = FilterMode.Point };
            var volume = new Texture2D(width / 4, height / 4, TextureFormat.RGBAFloat, false, true) { filterMode = FilterMode.Point };
            var output = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
            var previousTarget = RenderTexture.active;
            var previousDepth = Shader.GetGlobalTexture("_CameraDepthTexture");
            var previousZ = Shader.GetGlobalVector("_ZBufferParams");
            var previousScreen = Shader.GetGlobalVector("_ScreenSize");
            var previousScale = Shader.GetGlobalVector("_RTHandleScale");
            var previousDepthSize = Shader.GetGlobalVector("_CameraDepthTexture_TexelSize");
            var previousDebug = Shader.GetGlobalFloat("_RetroPSXDebugMode");
            try
            {
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    bool foreground = x >= 11 && x <= 20;
                    color.SetPixel(x, y, new Color(0.1f, 0.1f, 0.1f, foreground ? 0.65f : 0f));
                    depth.SetPixel(x, y, new Color(foreground ? 1f / 6f : 1f / 100f, 0, 0, 1));
                }
                for (int y = 0; y < height / 4; y++)
                for (int x = 0; x < width / 4; x++)
                {
                    int sourceX = x * 4 + 2;
                    bool foreground = sourceX >= 11 && sourceX <= 20;
                    volume.SetPixel(x, y, foreground ? new Color(0.04f, 0.04f, 0.04f, 0.8f) : new Color(0.7f, 0.7f, 0.7f, 0.2f));
                }
                color.Apply(); depth.Apply(); volume.Apply(); output.Create();
                material.SetTexture("_BlitTexture", color);
                material.SetVector("_BlitScaleBias", new Vector4(1, 1, 0, 0));
                material.SetTexture("_RetroVolumeTexture", volume);
                material.SetVector("_RetroVolumeTexelSize", new Vector4(4f / width, 4f / height, width / 4, height / 4));
                material.SetVector("_RetroVolumeParams3", new Vector4(0.05f, 4, 0, 0));
                material.SetVector("_RetroFinalColorParams", Vector4.zero);
                material.SetFloat("_RetroPreserveAlpha", 1);
                Shader.SetGlobalTexture("_CameraDepthTexture", depth);
                Shader.SetGlobalVector("_ZBufferParams", new Vector4(0, 0, 1, 0));
                Shader.SetGlobalVector("_ScreenSize", new Vector4(width, height, 1f / width, 1f / height));
                Shader.SetGlobalVector("_RTHandleScale", Vector4.one);
                Shader.SetGlobalVector("_CameraDepthTexture_TexelSize", new Vector4(1f / width, 1f / height, width, height));
                Shader.SetGlobalFloat("_RetroPSXDebugMode", 0);
                using (var cmd = new CommandBuffer())
                {
                    cmd.SetRenderTarget(output);
                    cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
                    Graphics.ExecuteCommandBuffer(cmd);
                }
                RenderTexture.active = output;
                readback.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readback.Apply();
                for (int x = 11; x <= 20; x++)
                {
                    Color pixel = readback.GetPixel(x, height / 2);
                    Assert.That(pixel.r, Is.EqualTo(0.12f).Within(0.005f), $"Incorrect foreground scattering at x={x}.");
                    Assert.That(pixel.a, Is.EqualTo(0.65f).Within(0.005f), $"Coverage changed at x={x}.");
                }
            }
            finally
            {
                RenderTexture.active = previousTarget;
                Shader.SetGlobalTexture("_CameraDepthTexture", previousDepth);
                Shader.SetGlobalVector("_ZBufferParams", previousZ);
                Shader.SetGlobalVector("_ScreenSize", previousScreen);
                Shader.SetGlobalVector("_RTHandleScale", previousScale);
                Shader.SetGlobalVector("_CameraDepthTexture_TexelSize", previousDepthSize);
                Shader.SetGlobalFloat("_RetroPSXDebugMode", previousDebug);
                Object.DestroyImmediate(material); Object.DestroyImmediate(color); Object.DestroyImmediate(depth);
                Object.DestroyImmediate(volume); Object.DestroyImmediate(output); Object.DestroyImmediate(readback);
            }
        }
    }
}
