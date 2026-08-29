using UnityEngine;

namespace RetroPSX
{
    public readonly struct RetroCameraPolicy
    {
        public RetroCameraPolicy(bool worldEffects, bool fullPipeline)
        {
            WorldEffects = worldEffects;
            FullPipeline = fullPipeline;
        }

        public bool WorldEffects { get; }
        public bool FullPipeline { get; }
    }

    public static class RetroCameraUtility
    {
        public static RetroCameraPolicy ResolvePolicy(
            CameraType cameraType,
            bool gameCamerasEnabled,
            RetroSceneViewMode sceneViewMode,
            bool isOverlay)
        {
            if (isOverlay)
                return default;

            if (cameraType == CameraType.Game && gameCamerasEnabled)
                return new RetroCameraPolicy(true, true);
            if (cameraType == CameraType.SceneView && sceneViewMode != RetroSceneViewMode.Off)
                return new RetroCameraPolicy(true, sceneViewMode == RetroSceneViewMode.FullPipeline);
            return default;
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
