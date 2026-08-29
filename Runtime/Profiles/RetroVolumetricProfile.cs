using UnityEngine;

namespace RetroPSX
{
    [CreateAssetMenu(menuName = "RetroPSX/Profiles/Volumetrics", fileName = "RetroVolumetricProfile")]
    public sealed class RetroVolumetricProfile : ScriptableObject
    {
        [SerializeField] private bool enabled;
        [SerializeField] private RetroVolumetricQuality quality = RetroVolumetricQuality.Medium;
        [SerializeField, Range(1, 8)] private int resolutionDivisor = 2;
        [SerializeField, Range(4, 128)] private int raySteps = 28;
        [SerializeField, Min(0.01f)] private float maxDistance = 40f;
        [SerializeField, Range(0f, 2f)] private float density = 0.045f;
        [SerializeField] private float baseHeight;
        [SerializeField, Range(0f, 4f)] private float heightFalloff = 0.35f;
        [SerializeField, Range(0f, 4f)] private float extinction = 1f;
        [SerializeField, Range(0f, 4f)] private float scattering = 1f;
        [SerializeField, Range(-0.8f, 0.8f)] private float anisotropy = 0.15f;
        [SerializeField] private Color ambient = new(0.08f, 0.09f, 0.12f, 1f);
        [SerializeField, Range(0f, 1f)] private float directionalContribution = 0.75f;
        [SerializeField] private bool jitter = true;
        [SerializeField, Range(0f, 1f)] private float jitterStrength = 0.65f;
        [SerializeField, Range(0, 32)] private int densitySteps = 8;
        [SerializeField, Range(0, 32)] private int lightSteps = 8;
        [SerializeField, Min(0f)] private float geometryDepthBias = 0.05f;
        [SerializeField, Range(0.25f, 16f)] private float depthEdgeSharpness = 4f;
        [SerializeField, Range(1, 4)] private int maximumLocalLights = 4;

        public bool Enabled => enabled;
        public RetroVolumetricQuality Quality => quality;
        public float MaxDistance => maxDistance;
        public float Density => density;
        public float BaseHeight => baseHeight;
        public float HeightFalloff => heightFalloff;
        public float Extinction => extinction;
        public float Scattering => scattering;
        public float Anisotropy => anisotropy;
        public Color Ambient => ambient;
        public float DirectionalContribution => directionalContribution;
        public bool Jitter => jitter;
        public float JitterStrength => jitterStrength;
        public int DensitySteps => densitySteps;
        public int LightSteps => lightSteps;
        public float GeometryDepthBias => geometryDepthBias;
        public float DepthEdgeSharpness => depthEdgeSharpness;
        public int MaximumLocalLights => maximumLocalLights;

        public void GetQuality(out int divisor, out int steps)
        {
            divisor = resolutionDivisor;
            steps = raySteps;
            RetroPSXMath.VolumetricQuality(quality, ref divisor, ref steps);
        }

        private void OnValidate()
        {
            resolutionDivisor = Mathf.Clamp(resolutionDivisor, 1, 8);
            raySteps = Mathf.Clamp(raySteps, 4, 128);
            maxDistance = Mathf.Max(0.01f, maxDistance);
            density = Mathf.Clamp(density, 0f, 2f);
            heightFalloff = Mathf.Clamp(heightFalloff, 0f, 4f);
            extinction = Mathf.Clamp(extinction, 0f, 4f);
            scattering = Mathf.Clamp(scattering, 0f, 4f);
            anisotropy = Mathf.Clamp(anisotropy, -0.8f, 0.8f);
            directionalContribution = Mathf.Clamp01(directionalContribution);
            jitterStrength = Mathf.Clamp01(jitterStrength);
            densitySteps = Mathf.Clamp(densitySteps, 0, 32);
            lightSteps = Mathf.Clamp(lightSteps, 0, 32);
            geometryDepthBias = Mathf.Max(0f, geometryDepthBias);
            depthEdgeSharpness = Mathf.Clamp(depthEdgeSharpness, 0.25f, 16f);
            maximumLocalLights = Mathf.Clamp(maximumLocalLights, 1, 4);
        }
    }
}
