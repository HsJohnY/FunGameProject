using System;
using UnityEngine;

namespace FunGame.Audio
{
    /// <summary>
    /// 生成可无缝循环的低成本合成音乐层，避免灰盒阶段依赖来源不明的外部音频。
    /// </summary>
    public static class ProceduralBgmSynthesis
    {
        private const double Tau = Math.PI * 2.0;

        public static float[] RenderLoop(int sampleRate, float durationSeconds, float energy)
        {
            if (sampleRate < 8000)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            }

            if (durationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            }

            int sampleCount = Math.Max(1, Mathf.RoundToInt(sampleRate * durationSeconds));
            float[] samples = new float[sampleCount * 2];
            double duration = sampleCount / (double)sampleRate;
            double layerEnergy = Math.Clamp(energy, 0f, 1f);

            // D 小调的低频机械嗡鸣与琶音，所有振荡器周期都量化到循环长度。
            double droneD = QuantizedFrequency(73.42, duration);
            double droneA = QuantizedFrequency(110.00, duration);
            double droneF = QuantizedFrequency(87.31, duration);
            double[] arpeggio =
            {
                QuantizedFrequency(146.83, duration),
                QuantizedFrequency(174.61, duration),
                QuantizedFrequency(220.00, duration),
                QuantizedFrequency(261.63, duration),
                QuantizedFrequency(220.00, duration),
                QuantizedFrequency(174.61, duration),
                QuantizedFrequency(164.81, duration),
                QuantizedFrequency(196.00, duration)
            };

            for (int index = 0; index < sampleCount; index++)
            {
                double time = index / (double)sampleRate;
                double beatPosition = time;
                double beatPhase = beatPosition - Math.Floor(beatPosition);
                double slowMotion = 0.72 + (0.28 * Math.Sin(Tau * time / duration));
                double pad =
                    (Math.Sin(Tau * droneD * time) * 0.12) +
                    (Math.Sin(Tau * droneA * time + 0.7) * 0.055) +
                    (Math.Sin(Tau * droneF * time + 1.4) * 0.045);

                int step = ((int)Math.Floor(beatPosition * 2.0)) % arpeggio.Length;
                double stepPhase = (beatPosition * 2.0) - Math.Floor(beatPosition * 2.0);
                double noteEnvelope = Math.Pow(Math.Sin(Math.PI * stepPhase), 2.0);
                double pulseEnvelope = Math.Exp(-beatPhase * 7.0);
                double melody = Math.Sin(Tau * arpeggio[step] * time) * noteEnvelope * 0.075 * layerEnergy;
                double lowPulse = Math.Sin(Tau * 55.0 * time) * pulseEnvelope * 0.085 * layerEnergy;
                double shimmer = Math.Sin(Tau * arpeggio[(step + 2) % arpeggio.Length] * time + 1.2)
                    * noteEnvelope * 0.025 * layerEnergy;

                double left = (pad * slowMotion) + melody + lowPulse;
                double right = (pad * (1.0 - ((slowMotion - 0.72) * 0.35))) + shimmer + lowPulse;
                const double outputGain = 2.35;
                samples[index * 2] = Mathf.Clamp((float)(left * outputGain), -0.8f, 0.8f);
                samples[(index * 2) + 1] = Mathf.Clamp((float)(right * outputGain), -0.8f, 0.8f);
            }

            return samples;
        }

        /// <summary>
        /// 生成不含主旋律的战斗节奏层。该层与维修主题同时起播并保持静音运行，
        /// 战斗发生时只需提高音量即可无缝增强鼓点和低音脉冲。
        /// </summary>
        public static float[] RenderCombatRhythmLoop(int sampleRate, float durationSeconds)
        {
            if (sampleRate < 8000)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            }

            if (durationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            }

            int sampleCount = Math.Max(1, Mathf.RoundToInt(sampleRate * durationSeconds));
            float[] samples = new float[sampleCount * 2];
            double duration = sampleCount / (double)sampleRate;
            double[] bassPattern =
            {
                QuantizedFrequency(73.42, duration),
                QuantizedFrequency(73.42, duration),
                QuantizedFrequency(87.31, duration),
                QuantizedFrequency(65.41, duration),
                QuantizedFrequency(73.42, duration),
                QuantizedFrequency(98.00, duration),
                QuantizedFrequency(87.31, duration),
                QuantizedFrequency(55.00, duration)
            };

            // 120 BPM，16 秒正好是 8 个四拍小节，循环边界落在强拍上。
            for (int index = 0; index < sampleCount; index++)
            {
                double time = index / (double)sampleRate;
                double beatPosition = time * 2.0;
                int beatIndex = (int)Math.Floor(beatPosition);
                double beatPhase = beatPosition - beatIndex;
                double eighthPosition = time * 4.0;
                double eighthPhase = eighthPosition - Math.Floor(eighthPosition);

                double kickEnvelope = Math.Exp(-beatPhase * 15.0);
                double kickPitch = 43.0 + (42.0 * Math.Exp(-beatPhase * 18.0));
                double kick = Math.Sin(Tau * kickPitch * time) * kickEnvelope * 0.34;

                bool backBeat = beatIndex % 4 == 1 || beatIndex % 4 == 3;
                double snareEnvelope = backBeat ? Math.Exp(-beatPhase * 24.0) : 0.0;
                double noise = HashNoise(index);
                double snareTone = Math.Sin(Tau * 178.0 * time) * 0.32;
                double snare = ((noise * 0.68) + snareTone) * snareEnvelope * 0.24;

                double hatEnvelope = Math.Exp(-eighthPhase * 42.0);
                double hatNoise = HashNoise(index * 3 + 17) - (HashNoise(index * 3 + 13) * 0.72);
                double hat = hatNoise * hatEnvelope * (beatIndex % 4 == 3 ? 0.11 : 0.075);

                int bassStep = (beatIndex / 2) % bassPattern.Length;
                double bassEnvelope = Math.Exp(-beatPhase * 5.5);
                double bass = Math.Sin(Tau * bassPattern[bassStep] * time) * bassEnvelope * 0.18;
                double urgency = Math.Sin(Tau * QuantizedFrequency(146.83, duration) * time)
                    * Math.Exp(-eighthPhase * 18.0) * 0.025;

                double left = kick + snare + bass + (hat * 0.72) + urgency;
                double right = kick + (snare * 0.92) + bass + hat - urgency;
                const double outputGain = 1.45;
                samples[index * 2] = Mathf.Clamp((float)(left * outputGain), -0.8f, 0.8f);
                samples[(index * 2) + 1] = Mathf.Clamp((float)(right * outputGain), -0.8f, 0.8f);
            }

            return samples;
        }

        private static double HashNoise(int value)
        {
            uint hash = unchecked((uint)value);
            hash ^= hash >> 16;
            hash *= 0x7feb352dU;
            hash ^= hash >> 15;
            hash *= 0x846ca68bU;
            hash ^= hash >> 16;
            return (hash / (double)uint.MaxValue * 2.0) - 1.0;
        }

        private static double QuantizedFrequency(double requestedFrequency, double durationSeconds)
        {
            return Math.Round(requestedFrequency * durationSeconds) / durationSeconds;
        }
    }
}
