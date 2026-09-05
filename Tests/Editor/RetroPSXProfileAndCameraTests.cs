using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RetroPSX.Tests
{
    public sealed class RetroPSXProfileAndCameraTests
    {
        [Test]
        public void IncompleteRootFailsGracefully()
        {
            RetroPSXPipelineProfile profile = ScriptableObject.CreateInstance<RetroPSXPipelineProfile>();
            Assert.That(profile.IsComplete, Is.False);
            Object.DestroyImmediate(profile);
        }

        [TestCase(CameraType.Game, true, RetroSceneViewMode.Off, false, true, true)]
        [TestCase(CameraType.Game, false, RetroSceneViewMode.FullPipeline, false, false, false)]
        [TestCase(CameraType.SceneView, true, RetroSceneViewMode.Off, false, false, false)]
        [TestCase(CameraType.SceneView, false, RetroSceneViewMode.WorldEffects, false, true, false)]
        [TestCase(CameraType.SceneView, false, RetroSceneViewMode.FullPipeline, false, true, true)]
        [TestCase(CameraType.Preview, true, RetroSceneViewMode.FullPipeline, false, false, false)]
        [TestCase(CameraType.Reflection, true, RetroSceneViewMode.FullPipeline, false, false, false)]
        [TestCase(CameraType.Game, true, RetroSceneViewMode.FullPipeline, true, false, false)]
        public void CameraPolicyIsExplicit(
            CameraType type,
            bool gameCameras,
            RetroSceneViewMode sceneView,
            bool overlay,
            bool expectedWorldEffects,
            bool expectedFullPipeline)
        {
            RetroCameraPolicy policy = RetroCameraUtility.ResolvePolicy(type, gameCameras, sceneView, overlay);
            Assert.That(policy.WorldEffects, Is.EqualTo(expectedWorldEffects));
            Assert.That(policy.FullPipeline, Is.EqualTo(expectedFullPipeline));
        }

        [TestCase(CameraType.Game, RetroSceneViewMode.FullPipeline, false, false, false, true, true, false)]
        [TestCase(CameraType.Game, RetroSceneViewMode.FullPipeline, false, true, true, true, true, true)]
        [TestCase(CameraType.Game, RetroSceneViewMode.FullPipeline, false, true, false, true, true, false)]
        [TestCase(CameraType.SceneView, RetroSceneViewMode.FullPipeline, false, true, true, true, true, true)]
        [TestCase(CameraType.SceneView, RetroSceneViewMode.WorldEffects, false, true, true, true, false, true)]
        [TestCase(CameraType.SceneView, RetroSceneViewMode.Off, false, true, true, false, false, false)]
        [TestCase(CameraType.Game, RetroSceneViewMode.FullPipeline, true, false, true, false, false, false)]
        [TestCase(CameraType.Preview, RetroSceneViewMode.FullPipeline, false, true, true, false, false, false)]
        public void CameraTargetAndAlphaPolicyIsExplicit(
            CameraType type,
            RetroSceneViewMode sceneView,
            bool overlay,
            bool hasTargetTexture,
            bool alphaOutputEnabled,
            bool expectedWorldEffects,
            bool expectedFullPipeline,
            bool expectedPreserveAlpha)
        {
            RetroCameraPolicy policy = RetroCameraUtility.ResolvePolicy(
                type, true, sceneView, overlay, hasTargetTexture, alphaOutputEnabled);
            Assert.That(policy.WorldEffects, Is.EqualTo(expectedWorldEffects));
            Assert.That(policy.FullPipeline, Is.EqualTo(expectedFullPipeline));
            Assert.That(policy.PreserveAlpha, Is.EqualTo(expectedPreserveAlpha));
        }

        [Test]
        public void SceneViewWorldEffectsUsesNativeCameraDimensions()
        {
            RetroRasterProfile profile = ScriptableObject.CreateInstance<RetroRasterProfile>();
            RetroRasterContext context = RetroCameraUtility.BuildRasterContext(profile, 1000, 700, false);
            Assert.That(context.SourceSize, Is.EqualTo(new Vector2Int(1000, 700)));
            Assert.That(context.InternalSize, Is.EqualTo(new Vector2Int(1000, 700)));
            Assert.That(context.Viewport, Is.EqualTo(new RectInt(0, 0, 1000, 700)));
            Assert.That(context.IsNative, Is.True);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void FullPipelineUsesEachCamerasOwnDimensions()
        {
            RetroRasterProfile profile = ScriptableObject.CreateInstance<RetroRasterProfile>();
            RetroRasterContext game = RetroCameraUtility.BuildRasterContext(profile, 1920, 1080, true);
            RetroRasterContext scene = RetroCameraUtility.BuildRasterContext(profile, 1000, 700, true);
            Assert.That(game.InternalSize, Is.EqualTo(new Vector2Int(427, 240)));
            Assert.That(scene.InternalSize, Is.EqualTo(new Vector2Int(343, 240)));
            Assert.That(scene.SourceSize, Is.EqualTo(new Vector2Int(1000, 700)));
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void RenderTextureUsesProfileRasterWithoutChangingOutputDimensions()
        {
            RetroRasterProfile profile = ScriptableObject.CreateInstance<RetroRasterProfile>();
            try
            {
                RetroCameraPolicy policy = RetroCameraUtility.ResolvePolicy(
                    CameraType.Game, true, RetroSceneViewMode.Off, false, true, true);
                RetroRasterContext context = RetroCameraUtility.BuildRasterContext(profile, 1024, 576, policy.FullPipeline);
                Assert.That(context.SourceSize, Is.EqualTo(new Vector2Int(1024, 576)));
                Assert.That(context.InternalSize, Is.EqualTo(new Vector2Int(427, 240)));
                Assert.That(context.Viewport, Is.EqualTo(new RectInt(0, 0, 1024, 576)));
                Assert.That(context.IsNative, Is.False);
                Assert.That(policy.PreserveAlpha, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SceneViewOffDoesNotRequestPresentation()
        {
            RetroCameraPolicy policy = RetroCameraUtility.ResolvePolicy(
                CameraType.SceneView, true, RetroSceneViewMode.Off, false);
            Assert.That(policy.WorldEffects, Is.False);
            Assert.That(policy.FullPipeline, Is.False);
        }

        [TestCase(CameraType.Game, false, true)]
        [TestCase(CameraType.SceneView, false, true)]
        [TestCase(CameraType.Game, true, false)]
        [TestCase(CameraType.Preview, false, false)]
        [TestCase(CameraType.Reflection, false, false)]
        public void NativeWorldSpaceUIUsesOnlySupportedBaseCameras(
            CameraType cameraType,
            bool overlay,
            bool expected)
        {
            Assert.That(RetroCameraUtility.SupportsNativeWorldSpaceUI(cameraType, overlay), Is.EqualTo(expected));
        }

        [Test]
        public void RasterProfileClampsInvalidAuthoringValues()
        {
            RetroRasterProfile profile = ScriptableObject.CreateInstance<RetroRasterProfile>();
            JsonUtility.FromJsonOverwrite("{\"customResolution\":{\"x\":-10,\"y\":0},\"internalHeight\":-1,\"scaleFactor\":3}", profile);
            typeof(RetroRasterProfile).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(profile, null);
            Assert.That(profile.CustomResolution.x, Is.GreaterThanOrEqualTo(64));
            Assert.That(profile.CustomResolution.y, Is.GreaterThanOrEqualTo(64));
            Assert.That(profile.InternalHeight, Is.GreaterThanOrEqualTo(64));
            Assert.That(profile.ScaleFactor, Is.InRange(0.1f, 1f));
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void RasterDefaultsFillTheCameraViewport()
        {
            RetroRasterProfile profile = ScriptableObject.CreateInstance<RetroRasterProfile>();
            RetroRasterContext context = profile.BuildContext(3440, 1440);
            Assert.That(profile.Presentation, Is.EqualTo(RetroPresentationMode.Stretch));
            Assert.That(context.InternalSize, Is.EqualTo(new Vector2Int(573, 240)));
            Assert.That(context.Viewport, Is.EqualTo(new RectInt(0, 0, 3440, 1440)));
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void FogModesUseAlgorithmNames()
        {
            CollectionAssert.AreEqual(
                new[] { "Off", "DistanceColor", "DistanceModulation", "SteppedDistanceColor" },
                System.Enum.GetNames(typeof(RetroFogMode)));
        }

        [Test]
        public void FinalImageQuantizationIsAnExplicitOptIn()
        {
            RetroColorProfile profile = ScriptableObject.CreateInstance<RetroColorProfile>();
            Assert.That(profile.QuantizeFinalImage, Is.False);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void UIProfileDefaultsToNativeWithNoClaimedProjectLayer()
        {
            RetroUIProfile profile = ScriptableObject.CreateInstance<RetroUIProfile>();
            Assert.That(profile.WorldSpaceDefault, Is.EqualTo(RetroUIRenderMode.Native));
            Assert.That(profile.HasNativeWorldSpaceLayer, Is.False);
            Assert.That(profile.NativeWorldSpaceLayer, Is.EqualTo(-1));
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void UIMarkerDoesNotClaimALayerWithoutAConfiguredProfile()
        {
            GameObject panel = new("RetroPSX UI Marker Test");
            panel.layer = 8;
            RetroPSXUI marker = panel.AddComponent<RetroPSXUI>();
            Assert.That(marker.Mode, Is.EqualTo(RetroUIRenderMode.Native));
            Assert.That(panel.layer, Is.EqualTo(8));
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void UIMarkerRestoresOriginalLayerWhenSwitchingModesOrDisabling()
        {
            RetroUIProfile profile = ScriptableObject.CreateInstance<RetroUIProfile>();
            JsonUtility.FromJsonOverwrite("{\"nativeWorldSpaceLayer\":30}", profile);
            GameObject panel = new("RetroPSX UI Layer Restore Test");
            panel.layer = 8;
            RetroPSXUI marker = panel.AddComponent<RetroPSXUI>();
            SerializedObject serialized = new(marker);
            serialized.FindProperty("uiProfile").objectReferenceValue = profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            marker.SetMode(RetroUIRenderMode.Native);
            Assert.That(panel.layer, Is.EqualTo(30));
            marker.SetMode(RetroUIRenderMode.Retro);
            Assert.That(panel.layer, Is.EqualTo(8));
            marker.SetMode(RetroUIRenderMode.Native);
            marker.enabled = false;
            Assert.That(panel.layer, Is.EqualTo(8));
            marker.enabled = true;
            Assert.That(panel.layer, Is.EqualTo(30));

            JsonUtility.FromJsonOverwrite("{\"nativeWorldSpaceLayer\":29}", profile);
            marker.SetMode(RetroUIRenderMode.Native);
            Assert.That(panel.layer, Is.EqualTo(29));
            marker.SetMode(RetroUIRenderMode.Retro);
            Assert.That(panel.layer, Is.EqualTo(8));

            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void RemovingNativeUIMarkerRestoresOriginalLayerInEditMode()
        {
            RetroUIProfile profile = ScriptableObject.CreateInstance<RetroUIProfile>();
            JsonUtility.FromJsonOverwrite("{\"nativeWorldSpaceLayer\":30}", profile);
            GameObject panel = new("RetroPSX UI Remove Marker Test") { layer = 9 };
            RetroPSXUI marker = panel.AddComponent<RetroPSXUI>();
            SerializedObject serialized = new(marker);
            serialized.FindProperty("uiProfile").objectReferenceValue = profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            marker.SetMode(RetroUIRenderMode.Native);
            Assert.That(panel.layer, Is.EqualTo(30));

            Object.DestroyImmediate(marker);
            Assert.That(panel.layer, Is.EqualTo(9));

            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void VisibleVolumetricSelectionRejectsDisabledLights()
        {
            GameObject cameraObject = new("RetroPSX Test Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            GameObject lightObject = new("RetroPSX Test Light");
            lightObject.transform.position = new Vector3(0f, 0f, 3f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 5f;
            RetroVolumetricLight volume = lightObject.AddComponent<RetroVolumetricLight>();
            RetroVolumetricLight[] results = new RetroVolumetricLight[4];
            Assert.That(RetroVolumetricLightRegistry.CopyVisible(camera, results, 4), Is.EqualTo(1));
            light.enabled = false;
            Assert.That(RetroVolumetricLightRegistry.CopyVisible(camera, results, 4), Is.EqualTo(0));
            Object.DestroyImmediate(lightObject);
            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void LocalVolumetricLightsDefaultToUnityLightShadows()
        {
            GameObject lightObject = new("RetroPSX Shadow Policy Test");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            RetroVolumetricLight volume = lightObject.AddComponent<RetroVolumetricLight>();

            Assert.That(volume.VolumetricShadows, Is.EqualTo(RetroVolumetricShadowMode.UseLightShadows));
            Assert.That(volume.UsesLightShadows, Is.True);
            Assert.That(volume.RequiresRealtimeShadows, Is.True);

            light.shadows = LightShadows.Hard;
            Assert.That(volume.RequiresRealtimeShadows, Is.False);
            Object.DestroyImmediate(lightObject);
        }
    }
}
