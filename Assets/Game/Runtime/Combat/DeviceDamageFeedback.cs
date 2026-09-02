using UnityEngine;

namespace FunGame.Combat
{
    /// <summary>
    /// 设备受损时播放短促警报音；离线时使用更低频的长提示音。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class DeviceDamageFeedback : MonoBehaviour
    {
        [SerializeField] private DefendableSystemTarget target;
        private AudioSource _audioSource;
        private AudioClip _damageClip;
        private AudioClip _offlineClip;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f;
            _audioSource.maxDistance = 18f;
            _damageClip = ProceduralCombatAudio.CreateTone("DeviceDamage", 620f, 0.16f, 0.28f);
            _offlineClip = ProceduralCombatAudio.CreateTone("DeviceOffline", 120f, 0.42f, 0.35f);
        }

        private void OnEnable()
        {
            if (target != null)
            {
                target.Damaged += HandleDamaged;
                target.Offline += HandleOffline;
            }
        }

        private void OnDisable()
        {
            if (target != null)
            {
                target.Damaged -= HandleDamaged;
                target.Offline -= HandleOffline;
            }
        }

        public void Configure(DefendableSystemTarget configuredTarget)
        {
            target = configuredTarget;
        }

        private void HandleDamaged(int damage)
        {
            if (_audioSource != null && _damageClip != null)
            {
                _audioSource.PlayOneShot(_damageClip);
            }
        }

        private void HandleOffline()
        {
            if (_audioSource != null && _offlineClip != null)
            {
                _audioSource.PlayOneShot(_offlineClip);
            }
        }
    }
}
