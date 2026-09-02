using FunGame.Combat;
using FunGame.Incident;
using FunGame.Settings;
using FunGame.UI;
using UnityEngine;

namespace FunGame.Audio
{
    /// <summary>
    /// 让维修主题持续播放，并在防卫状态下无缝推入同步节奏层；音乐只表达压力，不承担关键玩法提示。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CoolingBayBgmController : MonoBehaviour
    {
        private const float LoopDurationSeconds = 16f;

        [SerializeField] private CoolingIncidentController incident;
        [SerializeField] private CoolingCombatIntegrationController combatIntegration;
        [SerializeField] private AudioClip menuClipAsset;
        [SerializeField] private AudioClip ambientClipAsset;
        [SerializeField] private AudioClip pressureClipAsset;
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.55f;
        [SerializeField, Min(0.1f)] private float responseSpeed = 1.8f;
        [SerializeField, Min(0.1f)] private float combatResponseSpeed = 2.8f;
        private AudioSource _menuSource;
        private AudioSource _ambientSource;
        private AudioSource _pressureSource;
        private AudioClip _menuClip;
        private AudioClip _ambientClip;
        private AudioClip _pressureClip;
        private float _menuMix;

        public float CurrentIntensity { get; private set; }
        public float CurrentCombatMix { get; private set; }

        public void Configure(
            CoolingIncidentController configuredIncident,
            CoolingCombatIntegrationController configuredCombatIntegration = null)
        {
            incident = configuredIncident;
            combatIntegration = configuredCombatIntegration;
        }

        public void ConfigureMusicAssets(
            AudioClip configuredMenuClip,
            AudioClip configuredAmbientClip,
            AudioClip configuredPressureClip)
        {
            menuClipAsset = configuredMenuClip;
            ambientClipAsset = configuredAmbientClip;
            pressureClipAsset = configuredPressureClip;
        }

        private void Awake()
        {
            int sampleRate = Mathf.Max(8000, AudioSettings.outputSampleRate);
            _menuSource = CreateLayer(
                "BGM - Wacky Main Menu", menuClipAsset, ref _menuClip, sampleRate, 0.5f);
            _ambientSource = CreateLayer(
                "BGM - Future Power Repair", ambientClipAsset, ref _ambientClip, sampleRate, 0.15f);
            _pressureSource = CreateLayer(
                "BGM - Synchronized Combat Rhythm", pressureClipAsset, ref _pressureClip, sampleRate, 0.95f, true);
            CurrentIntensity = EvaluateIntensity();
            CurrentCombatMix = EvaluateCombatActive() ? 1f : 0f;
            _menuMix = GameMenuController.IsAnyMenuOpen ? 1f : 0f;
            double synchronizedStartTime = AudioSettings.dspTime + 0.05;
            _menuSource.PlayScheduled(synchronizedStartTime);
            _ambientSource.PlayScheduled(synchronizedStartTime);
            _pressureSource.PlayScheduled(synchronizedStartTime);
            ApplyVolumes(CurrentIntensity, CurrentCombatMix);
            Debug.Log(
                $"[BGM] playback=started menu={_menuSource.clip.name} ambient={_ambientSource.clip.name} " +
                $"pressure={_pressureSource.clip.name} " +
                $"sampleRate={_ambientSource.clip.frequency} masterVolume={masterVolume:F2}", this);
        }

        private void Update()
        {
            float target = EvaluateIntensity();
            CurrentIntensity = Mathf.MoveTowards(CurrentIntensity, target, responseSpeed * Time.unscaledDeltaTime);
            float combatTarget = EvaluateCombatActive() ? 1f : 0f;
            CurrentCombatMix = Mathf.MoveTowards(
                CurrentCombatMix,
                combatTarget,
                combatResponseSpeed * Time.unscaledDeltaTime);
            float menuTarget = GameMenuController.IsAnyMenuOpen ? 1f : 0f;
            _menuMix = Mathf.MoveTowards(_menuMix, menuTarget, 1.6f * Time.unscaledDeltaTime);
            ApplyVolumes(CurrentIntensity, CurrentCombatMix);
        }

        private void OnDestroy()
        {
            if (_menuClip != null)
            {
                Destroy(_menuClip);
            }

            if (_ambientClip != null)
            {
                Destroy(_ambientClip);
            }

            if (_pressureClip != null)
            {
                Destroy(_pressureClip);
            }
        }

        public static float GetTargetIntensity(
            CoolingIncidentPhase phase,
            CoolingIncidentRunState runState,
            bool combatActive)
        {
            if (runState == CoolingIncidentRunState.Succeeded)
            {
                return 0f;
            }

            if (runState == CoolingIncidentRunState.Failed)
            {
                return 0.2f;
            }

            if (combatActive)
            {
                return 1f;
            }

            return Mathf.Lerp(0.15f, 0.68f, (int)phase / (float)(int)CoolingIncidentPhase.Stabilized);
        }

        public static float GetRhythmLayerGain(float intensity, float combatMix)
        {
            float normalizedRepairPressure = Mathf.Clamp01(intensity / 0.68f);
            float repairRhythmVolume = Mathf.Lerp(0.025f, 0.14f, normalizedRepairPressure);
            return Mathf.Lerp(
                repairRhythmVolume,
                0.72f,
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(combatMix)));
        }

        private float EvaluateIntensity()
        {
            if (incident == null)
            {
                return 0.15f;
            }

            bool combatActive = combatIntegration != null && combatIntegration.IsInterferenceActive;
            return GetTargetIntensity(incident.Phase, incident.RunState, combatActive);
        }

        private AudioSource CreateLayer(
            string layerName,
            AudioClip configuredClip,
            ref AudioClip clip,
            int sampleRate,
            float energy,
            bool rhythmOnly = false)
        {
            var layerObject = new GameObject(layerName);
            layerObject.transform.SetParent(transform, false);
            var source = layerObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.priority = 32;

            if (configuredClip != null)
            {
                source.clip = configuredClip;
            }
            else
            {
                float[] samples = rhythmOnly
                    ? ProceduralBgmSynthesis.RenderCombatRhythmLoop(sampleRate, LoopDurationSeconds)
                    : ProceduralBgmSynthesis.RenderLoop(sampleRate, LoopDurationSeconds, energy);
                clip = AudioClip.Create(layerName, samples.Length / 2, 2, sampleRate, false);
                clip.SetData(samples, 0);
                source.clip = clip;
            }

            return source;
        }

        private bool EvaluateCombatActive()
        {
            return incident != null &&
                incident.RunState == CoolingIncidentRunState.Active &&
                combatIntegration != null &&
                combatIntegration.IsInterferenceActive;
        }

        private void ApplyVolumes(float intensity, float combatMix)
        {
            if (_menuSource == null || _ambientSource == null || _pressureSource == null)
            {
                return;
            }

            float completionFade = incident != null && incident.RunState == CoolingIncidentRunState.Succeeded ? 0.55f : 1f;
            float musicVolume = GameSettingsStore.Current.MusicVolume;
            float gameplayMix = 1f - _menuMix;
            float rhythmVolume = GetRhythmLayerGain(intensity, combatMix);
            _menuSource.volume = masterVolume * musicVolume * _menuMix * 0.78f;
            _ambientSource.volume = masterVolume * musicVolume * gameplayMix * completionFade *
                Mathf.Lerp(0.92f, 0.78f, intensity);
            _pressureSource.volume = masterVolume * musicVolume * gameplayMix * completionFade *
                rhythmVolume;
        }
    }
}
