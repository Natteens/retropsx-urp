using UnityEngine;

namespace RetroPSX
{
    [CreateAssetMenu(menuName = "RetroPSX/Profiles/Geometry", fileName = "RetroGeometryProfile")]
    public sealed class RetroGeometryProfile : ScriptableObject
    {
        [SerializeField] private RetroGeometryPrecisionMode precisionMode = RetroGeometryPrecisionMode.AuthenticInteger;
        [SerializeField, Range(0f, 1f)] private float snapStrength = 1f;
        [SerializeField, Range(0.25f, 4f)] private float precisionScale = 1f;
        [SerializeField, Range(0f, 1f)] private float distanceInfluence;
        [SerializeField, Min(0f)] private float nearCameraFade = 0.2f;
        [SerializeField] private RetroAffineMode affineMode = RetroAffineMode.Authentic;
        [SerializeField, Range(0f, 1f)] private float affineStrength = 1f;

        public RetroGeometryPrecisionMode PrecisionMode => precisionMode;
        public float SnapStrength => snapStrength;
        public float PrecisionScale => precisionScale;
        public float DistanceInfluence => distanceInfluence;
        public float NearCameraFade => nearCameraFade;
        public RetroAffineMode AffineMode => affineMode;
        public float AffineStrength => affineStrength;

        private void OnValidate()
        {
            snapStrength = Mathf.Clamp01(snapStrength);
            precisionScale = Mathf.Clamp(precisionScale, 0.25f, 4f);
            distanceInfluence = Mathf.Clamp01(distanceInfluence);
            nearCameraFade = Mathf.Max(0f, nearCameraFade);
            affineStrength = Mathf.Clamp01(affineStrength);
        }
    }
}
