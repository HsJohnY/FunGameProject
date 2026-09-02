using FunGame.Tools;
using UnityEngine;

namespace FunGame.Combat
{
    /// <summary>
    /// 响应扳手动作，播放占位挥动、命中音、轻震和本地短顿帧。
    /// </summary>
    [RequireComponent(typeof(ToolController), typeof(AudioSource))]
    public sealed class WrenchFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private Transform wrenchVisual;
        [SerializeField] private CombatCameraFeedback cameraFeedback;
        [SerializeField] private LocalHitStopFeedback hitStopFeedback;
        [SerializeField, Min(0.05f)] private float swingDuration = 0.18f;
        private ToolController _toolController;
        private AudioSource _audioSource;
        private AudioClip _hitClip;
        private Quaternion _restRotation;
        private float _swingRemaining;

        private void Awake()
        {
            _toolController = GetComponent<ToolController>();
            _audioSource = GetComponent<AudioSource>();
            _audioSource.spatialBlend = 0f;
            _audioSource.playOnAwake = false;
            _hitClip = ProceduralCombatAudio.CreateTone("WrenchHit", 180f, 0.09f, 0.35f);
            if (wrenchVisual != null)
            {
                _restRotation = wrenchVisual.localRotation;
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
        }

        private void Update()
        {
            if (wrenchVisual == null || _swingRemaining <= 0f)
            {
                return;
            }

            _swingRemaining = Mathf.Max(0f, _swingRemaining - Time.unscaledDeltaTime);
            float normalized = swingDuration <= 0f ? 1f : 1f - _swingRemaining / swingDuration;
            float angle = Mathf.Sin(normalized * Mathf.PI) * -42f;
            wrenchVisual.localRotation = _restRotation * Quaternion.Euler(angle, 0f, 10f);
            if (_swingRemaining <= 0f)
            {
                wrenchVisual.localRotation = _restRotation;
            }
        }

        public void Configure(Transform configuredWrenchVisual, CombatCameraFeedback configuredCameraFeedback, LocalHitStopFeedback configuredHitStop)
        {
            wrenchVisual = configuredWrenchVisual;
            cameraFeedback = configuredCameraFeedback;
            hitStopFeedback = configuredHitStop;
            if (wrenchVisual != null)
            {
                _restRotation = wrenchVisual.localRotation;
            }
        }

        private void HandleToolAction(ToolActionFeedback feedback)
        {
            if (feedback.Tool != ToolKind.ImpactWrench)
            {
                return;
            }

            _swingRemaining = swingDuration;
            if (!feedback.Succeeded || !(feedback.Target is InterferenceEnemy))
            {
                return;
            }

            cameraFeedback?.Play();
            hitStopFeedback?.Play();
            if (_audioSource != null && _hitClip != null)
            {
                _audioSource.PlayOneShot(_hitClip);
            }
        }
    }
}
