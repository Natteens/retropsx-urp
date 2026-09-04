using UnityEngine;

namespace RetroPSX
{
    public readonly struct RetroCameraPolicy
    {
        public RetroCameraPolicy(bool worldEffects, bool fullPipeline)
            : this(worldEffects, fullPipeline, false)
        {
        }

        public RetroCameraPolicy(bool worldEffects, bool fullPipeline, bool preserveAlpha)
        {
            WorldEffects = worldEffects;
            FullPipeline = fullPipeline;
            PreserveAlpha = preserveAlpha;
        }

        public bool WorldEffects { get; }
        public bool FullPipeline { get; }
        public bool PreserveAlpha { get; }
    }

    public static class RetroCameraUtility
    {
        public static RetroCameraPolicy ResolvePolicy(
            CameraType cameraType,
            bool gameCamerasEnabled,
            RetroSceneViewMode sceneViewMode,
            bool isOverlay)
        {
            return ResolvePolicy(cameraType, gameCamerasEnabled, sceneViewMode, isOverlay, false, false);
        }

        public static RetroCameraPolicy ResolvePolicy(
            CameraType cameraType,
            bool gameCamerasEnabled,
            RetroSceneViewMode sceneViewMode,
            bool isOverlay,
            bool hasTargetTexture,
            bool isAlphaOutputEnabled)
        {
            if (isOverlay)
                return default;

            if (cameraType == CameraType.Game && gameCamerasEnabled)
                return new RetroCameraPolicy(true, !hasTargetTexture, isAlphaOutputEnabled);
            if (cameraType == CameraType.SceneView && sceneViewMode != RetroSceneViewMode.Off)
                return new RetroCameraPolicy(true, sceneViewMode == RetroSceneViewMode.FullPipeline, isAlphaOutputEnabled);
            return default;
        }

        public static bool SupportsNativeWorldSpaceUI(CameraType cameraType, bool isOverlay)
        {
            if (isOverlay)
                return false;

            return cameraType is CameraType.Game or CameraType.SceneView;
        }

        public static RetroRasterContext BuildRasterContext(
            RetroRasterProfile raster,
            int sourceWidth,
            int sourceHeight,
            bool fullPipeline)
        {
            if (fullPipeline)
                return raster.BuildContext(sourceWidth, sourceHeight);

            Vector2Int size = new(Mathf.Max(1, sourceWidth), Mathf.Max(1, sourceHeight));
            return new RetroRasterContext(size, size, new RectInt(0, 0, size.x, size.y), RetroPresentationMode.Stretch);
        }
    }
}
