using System;
using System.IO;
using FunGame.Audio;
using UnityEditor;
using UnityEngine;

namespace FunGame.Editor
{
    /// <summary>
    /// 将项目自有合成音乐固化为标准 PCM WAV，确保编辑器与玩家构建使用同一播放链路。
    /// </summary>
    public static class ProceduralBgmAssetBuilder
    {
        public const string AmbientPath = "Assets/Game/Content/Audio/BGM_CoolingBay_Ambient.wav";
        public const string PressurePath = "Assets/Game/Content/Audio/BGM_CoolingBay_Pressure.wav";
        private const int SampleRate = 24000;
        private const float DurationSeconds = 16f;

        public static AudioClip[] GenerateOrRefresh()
        {
            EnsureFolder("Assets/Game/Content/Audio");
            WriteWave(AmbientPath, ProceduralBgmSynthesis.RenderLoop(SampleRate, DurationSeconds, 0.15f));
            WriteWave(PressurePath, ProceduralBgmSynthesis.RenderCombatRhythmLoop(SampleRate, DurationSeconds));
            AssetDatabase.ImportAsset(AmbientPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(PressurePath, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(AmbientPath);
            ConfigureImporter(PressurePath);
            AssetDatabase.SaveAssets();

            AudioClip ambient = AssetDatabase.LoadAssetAtPath<AudioClip>(AmbientPath);
            AudioClip pressure = AssetDatabase.LoadAssetAtPath<AudioClip>(PressurePath);
            if (ambient == null || pressure == null)
            {
                throw new InvalidDataException("程序化 BGM WAV 生成后未能由 Unity 导入。");
            }

            return new[] { ambient, pressure };
        }

        private static void ConfigureImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
            {
                throw new InvalidDataException($"无法取得音频导入器：{assetPath}");
            }

            importer.forceToMono = false;
            importer.loadInBackground = true;
            importer.defaultSampleSettings = new AudioImporterSampleSettings
            {
                loadType = AudioClipLoadType.CompressedInMemory,
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = 0.72f,
                sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                preloadAudioData = true
            };
            importer.SaveAndReimport();
        }

        private static void WriteWave(string assetPath, float[] samples)
        {
            const short channelCount = 2;
            const short bitsPerSample = 16;
            int dataLength = samples.Length * sizeof(short);
            byte[] bytes = new byte[44 + dataLength];
            WriteAscii(bytes, 0, "RIFF");
            WriteInt32(bytes, 4, 36 + dataLength);
            WriteAscii(bytes, 8, "WAVE");
            WriteAscii(bytes, 12, "fmt ");
            WriteInt32(bytes, 16, 16);
            WriteInt16(bytes, 20, 1);
            WriteInt16(bytes, 22, channelCount);
            WriteInt32(bytes, 24, SampleRate);
            WriteInt32(bytes, 28, SampleRate * channelCount * bitsPerSample / 8);
            WriteInt16(bytes, 32, (short)(channelCount * bitsPerSample / 8));
            WriteInt16(bytes, 34, bitsPerSample);
            WriteAscii(bytes, 36, "data");
            WriteInt32(bytes, 40, dataLength);

            for (int index = 0; index < samples.Length; index++)
            {
                short sample = (short)Mathf.RoundToInt(Mathf.Clamp(samples[index], -1f, 1f) * short.MaxValue);
                WriteInt16(bytes, 44 + (index * sizeof(short)), sample);
            }

            string fullPath = Path.GetFullPath(assetPath);
            File.WriteAllBytes(fullPath, bytes);
        }

        private static void WriteAscii(byte[] target, int offset, string value)
        {
            for (int index = 0; index < value.Length; index++)
            {
                target[offset + index] = (byte)value[index];
            }
        }

        private static void WriteInt16(byte[] target, int offset, short value)
        {
            byte[] encoded = BitConverter.GetBytes(value);
            target[offset] = encoded[0];
            target[offset + 1] = encoded[1];
        }

        private static void WriteInt32(byte[] target, int offset, int value)
        {
            byte[] encoded = BitConverter.GetBytes(value);
            for (int index = 0; index < encoded.Length; index++)
            {
                target[offset + index] = encoded[index];
            }
        }

        private static void EnsureFolder(string assetPath)
        {
            string current = "Assets";
            string[] parts = assetPath.Split('/');
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
    }
}
