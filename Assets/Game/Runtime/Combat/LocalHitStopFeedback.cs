using System.Collections;
using UnityEngine;

namespace FunGame.Combat
{
    /// <summary>
    /// 单机灰盒的极短命中顿帧。未来进入联网运行时可关闭此组件，避免修改共享模拟时钟。
    /// </summary>
    public sealed class LocalHitStopFeedback : MonoBehaviour
    {
        [SerializeField, Range(0.01f, 0.25f)] private float slowedTimeScale = 0.06f;
        [SerializeField, Min(0.01f)] private float realTimeDuration = 0.045f;
        [SerializeField] private bool enableTimeScaleHitStop = true;
        private Coroutine _routine;
        private float _originalTimeScale = 1f;
        private float _originalFixedDeltaTime = 0.02f;
        private bool _timeModified;

        public void Play()
        {
            if (!enableTimeScaleHitStop || Time.timeScale < 0.99f)
            {
                return;
            }

            if (_routine != null)
            {
                StopCoroutine(_routine);
                RestoreTime();
            }

            _routine = StartCoroutine(HitStopRoutine());
        }

        public void Configure(bool enabledForCurrentSession)
        {
            enableTimeScaleHitStop = enabledForCurrentSession;
        }

        private IEnumerator HitStopRoutine()
        {
            _originalTimeScale = Time.timeScale;
            _originalFixedDeltaTime = Time.fixedDeltaTime;
            Time.timeScale = slowedTimeScale;
            Time.fixedDeltaTime = _originalFixedDeltaTime * slowedTimeScale;
            _timeModified = true;
            yield return new WaitForSecondsRealtime(realTimeDuration);
            RestoreTime();
            _routine = null;
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            RestoreTime();
        }

        private void RestoreTime()
        {
            if (!_timeModified)
            {
                return;
            }

            Time.timeScale = _originalTimeScale;
            Time.fixedDeltaTime = _originalFixedDeltaTime;
            _timeModified = false;
        }
    }
}
