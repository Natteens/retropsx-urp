using UnityEngine;

namespace RetroPSX
{
    [CreateAssetMenu(menuName = "RetroPSX/Profiles/Fog", fileName = "RetroFogProfile")]
    public sealed class RetroFogProfile : ScriptableObject
    {
        [SerializeField] private RetroFogMode mode = RetroFogMode.DistanceColor;
        [SerializeField] private Color color = new(0.42f, 0.43f, 0.46f, 1f);
        [SerializeField, Min(0f)] private float nearDistance = 12f;
        [SerializeField, Min(0.01f)] private float farDistance = 42f;
        [SerializeField, Range(0, 64)] private int steps;
        [SerializeField, Range(0f, 2f)] private float strength = 1f;
        [SerializeField] private bool applyToWholeFrame;

        public RetroFogMode Mode => mode;
        public Color Color => color;
        public float NearDistance => nearDistance;
        public float FarDistance => farDistance;
        public int Steps => steps;
        public float Strength => strength;
        public bool ApplyToWholeFrame => applyToWholeFrame;
        public bool Enabled => mode != RetroFogMode.Off;

        private void OnValidate()
        {
            nearDistance = Mathf.Max(0f, nearDistance);
            farDistance = Mathf.Max(nearDistance + 0.01f, farDistance);
            steps = Mathf.Clamp(steps, 0, 64);
            strength = Mathf.Clamp(strength, 0f, 2f);
        }
    }
}
