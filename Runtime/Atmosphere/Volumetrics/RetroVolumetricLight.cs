using System.Collections.Generic;
using UnityEngine;

namespace RetroPSX
{
    /// <summary>Adds bounded raymarched scattering to a normal point or spot Light.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Light))]
    public sealed class RetroVolumetricLight : MonoBehaviour
    {
        [SerializeField, Range(0f, 4f)] private float intensity = 1f;
        [SerializeField, Range(0f, 2f)] private float density = 1f;
        [SerializeField, Range(0.001f, 1f), Tooltip("Width of the edge transition. In Stylized Shaft mode this is a fraction of the cone radius.")]
        private float edgeSoftness = 0.15f;
        [SerializeField, Range(0.25f, 8f), Tooltip("How quickly the light falls off with distance from the source.")]
        private float beamSharpness = 1f;
        [SerializeField, Tooltip("For Spot Lights. Gives the cone its own scattering density, independent of the global atmosphere.")]
        private bool stylizedShaft;
        [SerializeField, Range(0f, 1f), Tooltip("How strongly the shaft concentrates light toward its centerline.")]
        private float radialFalloff;
        [SerializeField, Range(0f, 1f), Tooltip("Distance over which the shaft fades in, as a fraction of the light range.")]
        private float sourceFade;
        [SerializeField, Range(0f, 1f), Tooltip("Distance over which the shaft fades out, as a fraction of the light range.")]
        private float rangeFade;
        [SerializeField, Range(0f, 1f), Tooltip("Ordered dithering between the volumetric profile's Light Steps. Zero disables shaft dithering.")]
        private float bandDither;
        [SerializeField, Tooltip("Uses realtime shadows from the Unity Light. Legacy volumes require shadows to be enabled in this mode.")]
        private RetroVolumetricShadowMode volumetricShadows = RetroVolumetricShadowMode.UseLightShadows;
        [SerializeField, Range(0f, 2f)] private float noiseDistortion;
        [SerializeField] private RetroVolumetricPattern pattern;
        [SerializeField, Range(0f, 1f)] private float patternStrength = 1f;
        [SerializeField] private bool blink;
        [SerializeField, Min(0.01f)] private float blinkRate = 4f;
        [SerializeField, Range(0f, 1f)] private float blinkDuty = 0.75f;

        private Light cachedLight;

        public Light Light
        {
            get
            {
                if (cachedLight == null)
                    cachedLight = GetComponent<Light>();
                return cachedLight;
            }
        }

        public float Intensity => intensity;
        public float Density => density;
        public float EdgeSoftness => edgeSoftness;
        public float BeamSharpness => beamSharpness;
        internal bool IsStylizedShaft => stylizedShaft && Light.type == LightType.Spot;
        internal Vector4 ShaftShape => new(radialFalloff, sourceFade, rangeFade, bandDither);
        public RetroVolumetricShadowMode VolumetricShadows => volumetricShadows;
        public bool UsesLightShadows => volumetricShadows == RetroVolumetricShadowMode.UseLightShadows;
        public bool RequiresRealtimeShadows => UsesLightShadows && (Light == null || (!IsStylizedShaft && Light.shadows == LightShadows.None));
        public float NoiseDistortion => noiseDistortion;
        public float PatternStrength => patternStrength;
        public RetroVolumetricPattern Pattern
        {
            get => pattern;
            set => pattern = value;
        }
        public bool Blink => blink;
        public float BlinkRate => blinkRate;
        public float BlinkDuty => blinkDuty;

        // Existing serialized lights retain atmosphere-driven scattering.
        private void Reset() => stylizedShaft = true;

        private void OnEnable() => RetroVolumetricLightRegistry.Register(this);
        private void OnDisable() => RetroVolumetricLightRegistry.Unregister(this);

        private void OnValidate()
        {
            intensity = Mathf.Clamp(intensity, 0f, 4f);
            density = Mathf.Clamp(density, 0f, 2f);
            edgeSoftness = Mathf.Clamp(edgeSoftness, 0.001f, 1f);
            beamSharpness = Mathf.Clamp(beamSharpness, 0.25f, 8f);
            noiseDistortion = Mathf.Clamp(noiseDistortion, 0f, 2f);
            patternStrength = Mathf.Clamp01(patternStrength);
            blinkRate = Mathf.Max(0.01f, blinkRate);
            blinkDuty = Mathf.Clamp01(blinkDuty);
        }
    }

    internal static class RetroVolumetricLightRegistry
    {
        private static readonly List<RetroVolumetricLight> Lights = new(16);
        private static readonly Plane[] FrustumPlanes = new Plane[6];

        public static void Register(RetroVolumetricLight light)
        {
            if (light != null && !Lights.Contains(light))
                Lights.Add(light);
        }

        public static void Unregister(RetroVolumetricLight light) => Lights.Remove(light);

        public static int CopyVisible(Camera camera, RetroVolumetricLight[] destination, int maximum)
        {
            if (camera == null || destination == null || maximum <= 0)
                return 0;

            GeometryUtility.CalculateFrustumPlanes(camera, FrustumPlanes);
            int count = 0;
            int limit = Mathf.Min(maximum, destination.Length);
            for (int index = 0; index < Lights.Count && count < limit; index++)
            {
                RetroVolumetricLight volume = Lights[index];
                if (volume == null || !volume.isActiveAndEnabled)
                    continue;

                Light light = volume.Light;
                if (light == null || !light.enabled || (light.type != LightType.Point && light.type != LightType.Spot))
                    continue;

                Bounds bounds = new(light.transform.position, Vector3.one * (light.range * 2f));
                if (!GeometryUtility.TestPlanesAABB(FrustumPlanes, bounds))
                    continue;

                destination[count++] = volume;
            }

            for (int index = count; index < destination.Length; index++)
                destination[index] = null;
            return count;
        }
    }
}
