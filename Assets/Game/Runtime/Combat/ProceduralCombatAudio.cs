using UnityEngine;

namespace FunGame.Combat
{
    /// <summary>
    /// 为无音频资产的灰盒生成极短提示音，后续可直接替换为正式 AudioClip。
    /// </summary>
    public static class ProceduralCombatAudio
    {
        public static AudioClip CreateTone(string name, float frequency, float duration, float volume)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * duration));
            var samples = new float[sampleCount];
            for (int index = 0; index < sampleCount; index++)
            {
                float time = (float)index / sampleRate;
                float envelope = 1f - (float)index / sampleCount;
                samples[index] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
