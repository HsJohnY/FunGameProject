using UnityEngine;

namespace FunGame.Content
{
    [CreateAssetMenu(menuName = "FunGame/Content/Cooling Incident Definition")]
    public sealed class CoolingIncidentDefinition : ScriptableObject
    {
        [SerializeField] private float startingTemperature = 65f;
        [SerializeField] private float failureTemperature = 100f;
        [SerializeField] private float temperatureRisePerSecond = 0.07f;
        [SerializeField] private bool diagnosticChecksEnabled = true;
        public float StartingTemperature => startingTemperature;
        public float FailureTemperature => failureTemperature;
        public float TemperatureRisePerSecond => temperatureRisePerSecond;
        public bool DiagnosticChecksEnabled => diagnosticChecksEnabled;
    }
}
