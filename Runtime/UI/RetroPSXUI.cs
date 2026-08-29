using UnityEngine;

namespace RetroPSX
{
    /// <summary>Marks a world-space UI Toolkit renderer as Native or Retro presentation content.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("RetroPSX/UI Rendering")]
    public sealed class RetroPSXUI : MonoBehaviour
    {
        [SerializeField] private RetroUIRenderMode mode = RetroUIRenderMode.Native;
        [SerializeField, Tooltip("UI profile used by the active RetroPSX pipeline. Its Native World Space Layer must also be excluded from the Universal Renderer opaque and transparent masks.")]
        private RetroUIProfile uiProfile;
        [SerializeField, HideInInspector] private int originalLayer;
        [SerializeField, HideInInspector] private bool hasOriginalLayer;
        [SerializeField, HideInInspector] private bool nativeLayerApplied;

        public RetroUIRenderMode Mode => mode;
        public RetroUIProfile UIProfile => uiProfile;
        public int OriginalLayer => originalLayer;
        public bool IsNativeLayerApplied => nativeLayerApplied;

        public void SetMode(RetroUIRenderMode value)
        {
            mode = value;
            ApplyMode();
        }

        private void Reset()
        {
            CaptureOriginalLayer();
            ApplyMode();
        }

        private void OnEnable()
        {
            CaptureOriginalLayer();
            ApplyMode();
        }

        private void OnDisable() => RestoreOriginalLayer();
        private void OnDestroy() => RestoreOriginalLayer();

        private void OnValidate()
        {
            if (isActiveAndEnabled)
                ApplyMode();
        }

        private void ApplyMode()
        {
            if (mode == RetroUIRenderMode.Native && uiProfile != null && uiProfile.HasNativeWorldSpaceLayer)
            {
                CaptureOriginalLayer();
                gameObject.layer = uiProfile.NativeWorldSpaceLayer;
                nativeLayerApplied = true;
                return;
            }

            RestoreOriginalLayer();
        }

        private void CaptureOriginalLayer()
        {
            if (!hasOriginalLayer || (!nativeLayerApplied && originalLayer != gameObject.layer))
            {
                originalLayer = gameObject.layer;
                hasOriginalLayer = true;
            }
        }

        private void RestoreOriginalLayer()
        {
            if (hasOriginalLayer && nativeLayerApplied)
                gameObject.layer = originalLayer;
            nativeLayerApplied = false;
        }
    }
}
