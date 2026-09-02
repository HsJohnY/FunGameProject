using System;
using FunGame.Audio;
using FunGame.Player;
using UnityEngine;

namespace FunGame.Settings
{
    /// <summary>
    /// 负责加载、保存并应用单机玩家设置；不持有菜单表现状态。
    /// </summary>
    public static class GameSettingsStore
    {
        private const string Prefix = "FunGame.Settings.";
        private static bool _loaded;
        private static GameSettingsValues _current;

        public static event Action SettingsApplied;

        public static GameSettingsValues Current
        {
            get
            {
                EnsureLoaded();
                return _current;
            }
        }

        public static GameSettingsValues Load()
        {
            int defaultWidth = Screen.width > 0 ? Screen.width : 1920;
            int defaultHeight = Screen.height > 0 ? Screen.height : 1080;
            GameSettingsValues defaults = GameSettingsValues.CreateDefault(
                defaultWidth, defaultHeight, QualitySettings.GetQualityLevel());
            _current = new GameSettingsValues
            {
                MasterVolume = PlayerPrefs.GetFloat(Prefix + "MasterVolume", defaults.MasterVolume),
                MusicVolume = PlayerPrefs.GetFloat(Prefix + "MusicVolume", defaults.MusicVolume),
                SoundEffectsVolume = PlayerPrefs.GetFloat(Prefix + "SoundEffectsVolume", defaults.SoundEffectsVolume),
                MouseSensitivity = PlayerPrefs.GetFloat(Prefix + "MouseSensitivity", defaults.MouseSensitivity),
                FieldOfView = PlayerPrefs.GetFloat(Prefix + "FieldOfView", defaults.FieldOfView),
                InvertYAxis = PlayerPrefs.GetInt(Prefix + "InvertYAxis", defaults.InvertYAxis ? 1 : 0) != 0,
                ResolutionWidth = PlayerPrefs.GetInt(Prefix + "ResolutionWidth", defaults.ResolutionWidth),
                ResolutionHeight = PlayerPrefs.GetInt(Prefix + "ResolutionHeight", defaults.ResolutionHeight),
                Fullscreen = PlayerPrefs.GetInt(Prefix + "Fullscreen", defaults.Fullscreen ? 1 : 0) != 0,
                QualityLevel = PlayerPrefs.GetInt(Prefix + "QualityLevel", defaults.QualityLevel),
                VerticalSync = PlayerPrefs.GetInt(Prefix + "VerticalSync", defaults.VerticalSync ? 1 : 0) != 0,
                FrameRateLimit = PlayerPrefs.GetInt(Prefix + "FrameRateLimit", defaults.FrameRateLimit)
            }.Sanitized(QualitySettings.names.Length);
            _loaded = true;
            return _current;
        }

        public static void Apply(GameSettingsValues values, FirstPersonController player, bool save)
        {
            _current = values.Sanitized(QualitySettings.names.Length);
            _loaded = true;
            AudioListener.volume = _current.MasterVolume;
            QualitySettings.SetQualityLevel(_current.QualityLevel, true);
            QualitySettings.vSyncCount = _current.VerticalSync ? 1 : 0;
            Application.targetFrameRate = _current.VerticalSync ? -1 : _current.FrameRateLimit;
            Screen.SetResolution(
                _current.ResolutionWidth,
                _current.ResolutionHeight,
                _current.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            player?.ApplyLookSettings(_current.MouseSensitivity, _current.FieldOfView, _current.InvertYAxis);
            ApplySoundEffectsVolume(_current.SoundEffectsVolume);
            SettingsApplied?.Invoke();

            if (save)
            {
                Save(_current);
            }
        }

        public static GameSettingsValues RestoreDefaults()
        {
            return GameSettingsValues.CreateDefault(
                Screen.currentResolution.width,
                Screen.currentResolution.height,
                Mathf.Min(3, Mathf.Max(0, QualitySettings.names.Length - 1)));
        }

        private static void EnsureLoaded()
        {
            if (!_loaded)
            {
                Load();
            }
        }

        private static void ApplySoundEffectsVolume(float volume)
        {
            AudioSource[] sources = UnityEngine.Object.FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (AudioSource source in sources)
            {
                if (source != null && source.GetComponentInParent<CoolingBayBgmController>() == null)
                {
                    source.volume = volume;
                }
            }
        }

        private static void Save(GameSettingsValues values)
        {
            PlayerPrefs.SetFloat(Prefix + "MasterVolume", values.MasterVolume);
            PlayerPrefs.SetFloat(Prefix + "MusicVolume", values.MusicVolume);
            PlayerPrefs.SetFloat(Prefix + "SoundEffectsVolume", values.SoundEffectsVolume);
            PlayerPrefs.SetFloat(Prefix + "MouseSensitivity", values.MouseSensitivity);
            PlayerPrefs.SetFloat(Prefix + "FieldOfView", values.FieldOfView);
            PlayerPrefs.SetInt(Prefix + "InvertYAxis", values.InvertYAxis ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "ResolutionWidth", values.ResolutionWidth);
            PlayerPrefs.SetInt(Prefix + "ResolutionHeight", values.ResolutionHeight);
            PlayerPrefs.SetInt(Prefix + "Fullscreen", values.Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "QualityLevel", values.QualityLevel);
            PlayerPrefs.SetInt(Prefix + "VerticalSync", values.VerticalSync ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "FrameRateLimit", values.FrameRateLimit);
            PlayerPrefs.Save();
        }
    }
}
