using UnityEngine;

namespace RetroPSX
{
    [CreateAssetMenu(menuName = "RetroPSX/Profiles/Volumetrics", fileName = "RetroVolumetricProfile")]
    public sealed class RetroVolumetricProfile : ScriptableObject
    {
        [SerializeField] private bool enabled;
        [Header("Sampling")]
        [SerializeField] private RetroVolumetricQuality quality = RetroVolumetricQuality.Medium;
        [SerializeField, Range(1, 8)] private int resolutionDivisor = 2;
        [SerializeField, Range(4, 128)] private int raySteps = 28;
        [SerializeField, Tooltip("Keeps the volumetric texels crisp. Resolution is set by Quality or the Custom divisor.")]
        private bool pixelated;
        [SerializeField, Tooltip("Offsets raymarch samples. Pixelated and stylized directional volumes use centered samples.")]
        private bool jitter = true;
        [SerializeField, Range(0f, 1f)] private float jitterStrength = 0.65f;
        [Header("Atmosphere")]
        [SerializeField, Min(0.01f)] private float maxDistance = 40f;
        [SerializeField, Range(0f, 2f)] private float density = 0.045f;
        [SerializeField] private float baseHeight;
        [SerializeField, Range(0f, 4f)] private float heightFalloff = 0.35f;
        [SerializeField, Range(0f, 4f)] private float extinction = 1f;
        [SerializeField, Range(0f, 4f)] private float scattering = 1f;
        [SerializeField, Range(-0.8f, 0.8f)] private float anisotropy = 0.15f;
        [SerializeField] private Color ambient = new(0.08f, 0.09f, 0.12f, 1f);
        [SerializeField, Min(0f), Tooltip("Distance over which the atmosphere fades in from the camera. Sun shafts are not faded.")]
        private float nearFade;
        [Header("Directional Light")]
        [SerializeField, Range(0f, 1f)] private float directionalContribution = 0.75f;
        [SerializeField, Tooltip("Styles the main directional light's scattering through shadowed atmosphere.")]
        private bool stylizedDirectional;
        [SerializeField, Tooltip("Multiplies the main directional light color.")]
        private Color directionalTint = Color.white;
        [SerializeField, Range(0.5f, 4f), Tooltip("Curve on the integrated sun visibility, applied before Light Steps. One is physical; higher narrows partially shadowed rays, lower lifts them.")]
        private float directionalContrast = 1f;
        [SerializeField, Range(0f, 1f), Tooltip("Ordered dithering of directional Light Steps and of sun shaft sampling.")]
        private float directionalDither;
        [Header("Directional Sun Shafts")]
        [SerializeField, Range(0f, 1f), Tooltip("Scattering density of sunlit air, independent of the atmosphere. Shafts follow the main light and its shadows; open, fully lit air adds no veil. Zero disables them.")]
        private float sunShafts;
        [Header("Retro Stylization")]
        [SerializeField, Range(0, 32), Tooltip("Quantizes the height density into layers. Zero or one leaves it continuous.")]
        private int densitySteps = 8;
        [SerializeField, Range(0, 32), Tooltip("Quantizes local light and stylized directional scattering into bands. Zero or one leaves it continuous.")]
        private int lightSteps = 8;
        [Header("Depth and Limits")]
        [SerializeField, Min(0f)] private float geometryDepthBias = 0.05f;
        [SerializeField, Range(0.25f, 16f), Tooltip("How strictly a volumetric sample must lie on the pixel's surface during reconstruction, including Pixelated.")]
        private float depthEdgeSharpness = 4f;
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
        internal bool Pixelated => pixelated;
        internal bool StylizedDirectional => stylizedDirectional;
        internal float DirectionalContrast => directionalContrast;
        internal float DirectionalDither => directionalDither;
        internal float SunShafts => sunShafts;
        internal float NearFade => nearFade;
        internal Color DirectionalTint => directionalTint.linear;
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
            sunShafts = Mathf.Clamp01(sunShafts);
            nearFade = Mathf.Max(0f, nearFade);
        }
    }
}
