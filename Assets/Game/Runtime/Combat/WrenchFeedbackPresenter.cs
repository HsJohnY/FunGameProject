using FunGame.Tools;
using UnityEngine;

namespace FunGame.Combat
{
    /// <summary>
    /// 响应三种工具动作：扳手挥击、喷枪后坐和桥接器脉冲，并提供战斗命中反馈。
    /// </summary>
    [RequireComponent(typeof(ToolController), typeof(AudioSource))]
    public sealed class WrenchFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private Transform wrenchVisual;
        [SerializeField] private Transform sealantVisual;
        [SerializeField] private Transform circuitBridgerVisual;
        [SerializeField] private CombatCameraFeedback cameraFeedback;
        [SerializeField] private LocalHitStopFeedback hitStopFeedback;
        [SerializeField, Min(0.05f)] private float swingDuration = 0.18f;
        private ToolController _toolController;
        private AudioSource _audioSource;
        private AudioClip _hitClip;
        private AudioClip _sealantClip;
        private AudioClip _bridgerClip;
        private Quaternion _restRotation;
        private Quaternion _sealantRestRotation;
        private Quaternion _bridgerRestRotation;
        private float _swingRemaining;
        private float _sealantPulseRemaining;
        private float _bridgerPulseRemaining;

        private void Awake()
        {
            _toolController = GetComponent<ToolController>();
            _audioSource = GetComponent<AudioSource>();
            _audioSource.spatialBlend = 0f;
            _audioSource.playOnAwake = false;
            _hitClip = ProceduralCombatAudio.CreateTone("WrenchHit", 180f, 0.09f, 0.35f);
            _sealantClip = ProceduralCombatAudio.CreateTone("SealantPulse", 360f, 0.08f, 0.18f);
            _bridgerClip = ProceduralCombatAudio.CreateTone("BridgerPulse", 880f, 0.12f, 0.22f);
            if (wrenchVisual != null)
            {
                _restRotation = wrenchVisual.localRotation;
            }

            if (sealantVisual != null)
            {
                _sealantRestRotation = sealantVisual.localRotation;
            }

            if (circuitBridgerVisual != null)
            {
                _bridgerRestRotation = circuitBridgerVisual.localRotation;
            }
        }

        private void OnEnable()
        {
            if (_toolController != null)
            {
                _toolController.ToolActionExecuted += HandleToolAction;
            }
        }

        private void OnDisable()
        {
            if (_toolController != null)
            {
                _toolController.ToolActionExecuted -= HandleToolAction;
            }

            if (wrenchVisual != null)
            {
                wrenchVisual.localRotation = _restRotation;
            }

            if (sealantVisual != null)
            {
                sealantVisual.localRotation = _sealantRestRotation;
            }

            if (circuitBridgerVisual != null)
            {
                circuitBridgerVisual.localRotation = _bridgerRestRotation;
            }
        }

        private void Update()
        {
            if (wrenchVisual != null && _swingRemaining > 0f)
            {
                _swingRemaining = Mathf.Max(0f, _swingRemaining - Time.unscaledDeltaTime);
                float normalized = swingDuration <= 0f ? 1f : 1f - _swingRemaining / swingDuration;
                float angle = Mathf.Sin(normalized * Mathf.PI) * -42f;
                wrenchVisual.localRotation = _restRotation * Quaternion.Euler(angle, 0f, 10f);
                if (_swingRemaining <= 0f)
                {
                    wrenchVisual.localRotation = _restRotation;
                }
            }

            AnimatePulse(sealantVisual, _sealantRestRotation, ref _sealantPulseRemaining, 9f);
            AnimatePulse(circuitBridgerVisual, _bridgerRestRotation, ref _bridgerPulseRemaining, -13f);
        }

        private static void AnimatePulse(Transform visual, Quaternion restRotation, ref float remaining, float angle)
        {
            if (visual == null || remaining <= 0f)
            {
                return;
            }

            remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
            float normalized = 1f - remaining / 0.14f;
            visual.localRotation = restRotation * Quaternion.Euler(Mathf.Sin(normalized * Mathf.PI) * angle, 0f, 0f);
            if (remaining <= 0f)
            {
                visual.localRotation = restRotation;
            }
        }

        public void Configure(Transform configuredWrenchVisual, CombatCameraFeedback configuredCameraFeedback, LocalHitStopFeedback configuredHitStop)
        {
            Configure(configuredWrenchVisual, null, null, configuredCameraFeedback, configuredHitStop);
        }

        public void Configure(
            Transform configuredWrenchVisual,
            Transform configuredSealantVisual,
            Transform configuredCircuitBridgerVisual,
            CombatCameraFeedback configuredCameraFeedback,
            LocalHitStopFeedback configuredHitStop)
        {
            wrenchVisual = configuredWrenchVisual;
            sealantVisual = configuredSealantVisual;
            circuitBridgerVisual = configuredCircuitBridgerVisual;
            cameraFeedback = configuredCameraFeedback;
            hitStopFeedback = configuredHitStop;
            if (wrenchVisual != null)
            {
                _restRotation = wrenchVisual.localRotation;
            }

            if (sealantVisual != null)
            {
                _sealantRestRotation = sealantVisual.localRotation;
            }

            if (circuitBridgerVisual != null)
            {
                _bridgerRestRotation = circuitBridgerVisual.localRotation;
            }
        }

        private void HandleToolAction(ToolActionFeedback feedback)
        {
            switch (feedback.Tool)
            {
                case ToolKind.ImpactWrench:
                    _swingRemaining = swingDuration;
                    break;
                case ToolKind.SealantGun:
                    _sealantPulseRemaining = 0.14f;
                    break;
                case ToolKind.CircuitBridger:
                    _bridgerPulseRemaining = 0.14f;
                    break;
            }

            if (!feedback.Succeeded)
            {
                return;
            }

            bool hitEnemy = feedback.Target is InterferenceEnemy || feedback.Target is FunGame.Networking.NetworkCombatEnemy;
            if (hitEnemy)
            {
                cameraFeedback?.Play();
                if (feedback.Tool == ToolKind.ImpactWrench)
                {
                    hitStopFeedback?.Play();
                }
            }

            AudioClip clip = feedback.Tool == ToolKind.SealantGun
                ? _sealantClip
                : feedback.Tool == ToolKind.CircuitBridger
                    ? _bridgerClip
                    : _hitClip;
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
    }
}
