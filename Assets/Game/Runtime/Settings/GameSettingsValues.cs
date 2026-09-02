using System;
using UnityEngine;

namespace FunGame.Settings
{
    /// <summary>
    /// 可序列化且可独立校验的玩家设置快照。
    /// </summary>
    [Serializable]
    public struct GameSettingsValues
    {
        public float MasterVolume;
        public float MusicVolume;
        public float SoundEffectsVolume;
        public float MouseSensitivity;
        public float FieldOfView;
        public bool InvertYAxis;
        public int ResolutionWidth;
        public int ResolutionHeight;
        public bool Fullscreen;
        public int QualityLevel;
        public bool VerticalSync;
        public int FrameRateLimit;

        public static GameSettingsValues CreateDefault(int width, int height, int qualityLevel)
        {
            return new GameSettingsValues
            {
                MasterVolume = 0.85f,
                MusicVolume = 0.8f,
                SoundEffectsVolume = 0.9f,
                MouseSensitivity = 0.08f,
                FieldOfView = 80f,
                InvertYAxis = false,
                ResolutionWidth = width,
                ResolutionHeight = height,
                Fullscreen = true,
                QualityLevel = qualityLevel,
                VerticalSync = true,
                FrameRateLimit = 60
            };
        }

        public GameSettingsValues Sanitized(int qualityLevelCount)
        {
            GameSettingsValues result = this;
            result.MasterVolume = Mathf.Clamp01(result.MasterVolume);
            result.MusicVolume = Mathf.Clamp01(result.MusicVolume);
            result.SoundEffectsVolume = Mathf.Clamp01(result.SoundEffectsVolume);
            result.MouseSensitivity = Mathf.Clamp(result.MouseSensitivity, 0.02f, 0.3f);
            result.FieldOfView = Mathf.Clamp(result.FieldOfView, 65f, 110f);
            result.ResolutionWidth = Mathf.Clamp(result.ResolutionWidth, 640, 7680);
            result.ResolutionHeight = Mathf.Clamp(result.ResolutionHeight, 480, 4320);
            result.QualityLevel = Mathf.Clamp(result.QualityLevel, 0, Mathf.Max(0, qualityLevelCount - 1));
            if (result.FrameRateLimit != 30 && result.FrameRateLimit != 60 &&
                result.FrameRateLimit != 120 && result.FrameRateLimit != -1)
            {
                result.FrameRateLimit = 60;
            }

            return result;
        }
    }
}
