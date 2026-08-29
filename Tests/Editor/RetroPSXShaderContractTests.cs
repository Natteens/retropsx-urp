using System.IO;
using NUnit.Framework;
using UnityEditor.PackageManager;

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
    }
}
