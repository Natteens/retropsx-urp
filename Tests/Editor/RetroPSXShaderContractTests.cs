using System.IO;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEngine;
using RetroPSX.Rendering;

namespace RetroPSX.Tests
{
    public sealed class RetroPSXShaderContractTests
    {
        [Test]
        public void ShadowCasterUsesUrpDirectionalOnlyNearPlaneClamping()
        {
            PackageInfo package = PackageInfo.FindForAssembly(typeof(RetroPSXShaderContractTests).Assembly);
            Assert.That(package, Is.Not.Null, "Could not resolve the RetroPSX package path.");
            string path = Path.Combine(package.resolvedPath, "Runtime", "Shaders", "Includes", "RetroPSXDepthPass.hlsl");
            string source = File.ReadAllText(path);

            StringAssert.Contains("ApplyShadowClamping(clipPosition)", source);
            StringAssert.DoesNotContain("clipPosition.z = min", source,
                "Punctual shadow casters must not be clamped to the near plane on reversed-Z platforms.");
            StringAssert.DoesNotContain("clipPosition.z = max", source,
                "Punctual shadow casters must not be clamped to the near plane on forward-Z platforms.");
        }

        [Test]
        public void RuntimeShaderResourcesContainEveryPostShader()
        {
            RetroPSXShaderResources shaders = Resources.Load<RetroPSXShaderResources>(RetroPSXShaderResources.ResourcePath);

            Assert.That(shaders, Is.Not.Null);
            Assert.That(shaders.Resolve?.name, Is.EqualTo("Hidden/RetroPSX/Resolve"));
            Assert.That(shaders.Presentation?.name, Is.EqualTo("Hidden/RetroPSX/Presentation"));
            Assert.That(shaders.Volumetric?.name, Is.EqualTo("Hidden/RetroPSX/VolumetricRaymarch"));
            Assert.That(shaders.VolumetricComposite?.name, Is.EqualTo("Hidden/RetroPSX/VolumetricComposite"));
            Assert.That(shaders.CRT?.name, Is.EqualTo("Hidden/RetroPSX/CRT"));
        }

        [TestCase("Raster/RetroPSXResolve.shader", "_RetroPreserveAlpha > 0.5 ? original.a : 1.0")]
        [TestCase("Raster/RetroPSXPresentation.shader", "_RetroPreserveAlpha > 0.5 ? color.a : 1.0")]
        [TestCase("Atmosphere/RetroPSXVolumetricComposite.shader", "_RetroPreserveAlpha > 0.5 ? baseColor.a : 1.0")]
        [TestCase("Display/RetroPSXCRT.shader", "_RetroPreserveAlpha > 0.5 ? centerSample.a : 1.0")]
        public void FinalImageShadersUseExplicitAlphaPolicy(string relativePath, string expectedExpression)
        {
            PackageInfo package = PackageInfo.FindForAssembly(typeof(RetroPSXShaderContractTests).Assembly);
            Assert.That(package, Is.Not.Null);
            string path = Path.Combine(package.resolvedPath, "Runtime", "Shaders", relativePath);
            string source = File.ReadAllText(path);

            StringAssert.Contains(expectedExpression, source);
        }

        [Test]
        public void CrtFilteringIgnoresRgbFromTransparentTexels()
        {
            PackageInfo package = PackageInfo.FindForAssembly(typeof(RetroPSXShaderContractTests).Assembly);
            Assert.That(package, Is.Not.Null);
            string path = Path.Combine(package.resolvedPath, "Runtime", "Shaders", "Display", "RetroPSXCRT.shader");
            string source = File.ReadAllText(path);

            StringAssert.Contains("half4 SampleDisplay(float2 uv)", source);
            StringAssert.Contains("sample00.rgb * sample00.a", source);
            StringAssert.Contains("premultiplied / alpha", source);
            StringAssert.Contains("half3 SampleCoveredColor(float2 uv, half4 centerSample)", source);
            StringAssert.Contains("sample.a / max(centerSample.a", source);
            StringAssert.Contains("centerSample.a <= 0.0001", source);
        }
    }
}
