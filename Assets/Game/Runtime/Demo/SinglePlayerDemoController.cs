using System;
using System.Linq;
using FunGame.Combat;
using FunGame.Incident;
using FunGame.UI;
using UnityEngine;

namespace FunGame.Demo
{
    /// <summary>
    /// 协调约半小时单人演示版的三章流程；每章只组合维修、桥接、防卫和控制台确认四类基础动作。
    /// </summary>
    public sealed class SinglePlayerDemoController : MonoBehaviour
    {
        private const int RequiredCoolingRuns = 2;
        [SerializeField] private CoolingIncidentController coolingIncident;
        [SerializeField] private CombatEncounterController relayDefense;
        [SerializeField] private DemoRelayTarget[] relayTargets;
        [SerializeField] private CombatEncounterController[] stormWaves;
        [SerializeField] private DemoCalibrationConsole campaignConsole;
        private SinglePlayerDemoRules _rules;
        private bool _relayChapterStarted;
        private bool _stormChapterStarted;
        private bool _relayChapterFailed;
        private bool _stormChapterFailed;
        private float _elapsedSeconds;

        public event Action StateChanged;
        public SinglePlayerDemoChapter Chapter => Rules.Chapter;
        public int CoolingRunsCompleted => Rules.CoolingRunsCompleted;
        public int RequiredCoolingRunCount => RequiredCoolingRuns;
        public int StabilizedRelayCount => Rules.StabilizedRelays;
        public int RequiredRelayCount => relayTargets?.Length ?? 0;
        public int CurrentStormWave => Rules.CurrentStormWave;
        public int StormWaveCount => stormWaves?.Length ?? 0;
        public bool EasterEgg325Discovered { get; private set; }
        public float ElapsedSeconds => _elapsedSeconds;
        public int ChapterRestartCount { get; private set; }
        public bool IsCompleted => Chapter == SinglePlayerDemoChapter.Completed;
        public bool IsAwaitingCalibration => Rules.IsAwaitingCalibration;
        public bool IsCurrentChapterFailed => _relayChapterFailed || _stormChapterFailed;
        public CombatEncounterController RelayDefenseEncounter => relayDefense;
        public CombatEncounterController CurrentStormEncounter => GetCurrentStormWave();
        public bool IsCampaignConsoleAvailable =>
            _relayChapterFailed || _stormChapterFailed ||
            (Chapter == SinglePlayerDemoChapter.StormCalibration && Rules.IsAwaitingCalibration);
        public string CampaignConsoleAction =>
            _relayChapterFailed || _stormChapterFailed
                ? "重启当前章节"
                : Rules.IsAwaitingCalibration
                    ? CurrentStormWave + 1 >= StormWaveCount ? "提交最终校准" : "写入校准并进入下一波"
                    : "检查系统";

        public string ChapterTitle
        {
            get
            {
                switch (Chapter)
                {
                    case SinglePlayerDemoChapter.CoolingEmergency: return "第一章 · 冷却舱事故";
                    case SinglePlayerDemoChapter.RelaySurge: return "第二章 · 风暴继电器";
                    case SinglePlayerDemoChapter.StormCalibration: return "第三章 · 核心校准防卫";
                    default: return "演示完成 · 风暴航线恢复";
                }
            }
        }

        public string CurrentObjective
        {
            get
            {
                if (Chapter == SinglePlayerDemoChapter.CoolingEmergency)
                {
                    int activeRun = Mathf.Min(CoolingRunsCompleted + 1, RequiredCoolingRunCount);
                    return $"稳定冷却支路 {activeRun}/{RequiredCoolingRunCount}";
                }

                if (Chapter == SinglePlayerDemoChapter.RelaySurge)
                {
                    if (_relayChapterFailed) return "辅助设备离线 · 前往风暴控制台重启第二章";
                    int enemies = relayDefense != null ? relayDefense.RemainingEnemyCount : 0;
                    return $"稳定继电器 {StabilizedRelayCount}/{RequiredRelayCount} · 剩余干扰体 {enemies}";
                }

                if (Chapter == SinglePlayerDemoChapter.StormCalibration)
                {
                    if (_stormChapterFailed) return "风暴核心离线 · 前往控制台重启第三章";
                    if (Rules.IsAwaitingCalibration) return $"第 {CurrentStormWave + 1} 波已清除 · 前往控制台写入校准";
                    CombatEncounterController wave = GetCurrentStormWave();
                    int enemies = wave != null ? wave.RemainingEnemyCount : 0;
                    return $"抵御第 {CurrentStormWave + 1}/{StormWaveCount} 波 · 剩余干扰体 {enemies}";
                }

                return EasterEgg325Discovered
                    ? "完整演示完成 · 已发现隐藏维修队记录 325"
                    : "完整演示完成 · 舱内似乎还有一块积灰的旧铭牌";
            }
        }

        private SinglePlayerDemoRules Rules => _rules ?? (_rules = new SinglePlayerDemoRules(
            RequiredCoolingRuns,
            Mathf.Max(1, relayTargets?.Length ?? 0),
            Mathf.Max(1, stormWaves?.Length ?? 0)));

        public void Configure(
            CoolingIncidentController configuredIncident,
            CombatEncounterController configuredRelayDefense,
            DemoRelayTarget[] configuredRelays,
            CombatEncounterController[] configuredStormWaves,
            DemoCalibrationConsole configuredConsole)
        {
            coolingIncident = configuredIncident;
            relayDefense = configuredRelayDefense;
            relayTargets = configuredRelays;
            stormWaves = configuredStormWaves;
            campaignConsole = configuredConsole;
            _rules = new SinglePlayerDemoRules(
                RequiredCoolingRuns,
                Mathf.Max(1, relayTargets?.Length ?? 0),
                Mathf.Max(1, stormWaves?.Length ?? 0));
            campaignConsole?.Configure(this);
        }

        private void Start()
        {
            PrepareLaterChapters();
        }

        private void Update()
        {
            if (!IsCompleted && !GameMenuController.IsAnyMenuOpen)
            {
                _elapsedSeconds += Time.deltaTime;
            }

            if (Chapter == SinglePlayerDemoChapter.CoolingEmergency)
            {
                if (coolingIncident != null && coolingIncident.RunState == CoolingIncidentRunState.Succeeded &&
                    Rules.CompleteCoolingChapter())
                {
                    if (Rules.Chapter == SinglePlayerDemoChapter.RelaySurge)
                    {
                        BeginRelayChapter();
                    }
                    else
                    {
                        Debug.Log($"[Demo] chapter=1 event=cooling-branch-complete run={CoolingRunsCompleted}/{RequiredCoolingRunCount}", this);
                        coolingIncident.ResetIncident();
                        StateChanged?.Invoke();
                    }
                }

                return;
            }

            if (Chapter == SinglePlayerDemoChapter.RelaySurge)
            {
                MonitorRelayChapter();
                return;
            }

            if (Chapter == SinglePlayerDemoChapter.StormCalibration)
            {
                MonitorStormChapter();
            }
        }

        public bool ExecuteCampaignConsole()
        {
            if (_relayChapterFailed || _stormChapterFailed)
            {
                RestartCurrentChapter();
                return true;
            }

            if (Chapter != SinglePlayerDemoChapter.StormCalibration || !Rules.ConfirmCalibration())
            {
                return false;
            }

            if (Rules.Chapter == SinglePlayerDemoChapter.Completed)
            {
                CompleteDemo();
            }
            else
            {
                BeginCurrentStormWave();
            }

            StateChanged?.Invoke();
            return true;
        }

        public void DiscoverEasterEgg325()
        {
            if (EasterEgg325Discovered)
            {
                return;
            }

            EasterEgg325Discovered = true;
            StateChanged?.Invoke();
        }

        private void PrepareLaterChapters()
        {
            relayDefense?.PrepareDormant();
            if (relayTargets != null)
            {
                foreach (DemoRelayTarget relay in relayTargets)
                {
                    relay?.SetChapterActive(false, true);
                }
            }

            if (stormWaves != null)
            {
                foreach (CombatEncounterController wave in stormWaves)
                {
                    wave?.PrepareDormant();
                }
            }
        }

        private void BeginRelayChapter()
        {
            _relayChapterStarted = true;
            _relayChapterFailed = false;
            foreach (DemoRelayTarget relay in relayTargets)
            {
                relay?.SetChapterActive(true, true);
            }

            relayDefense?.BeginEncounter();
            Debug.Log("[Demo] chapter=2 event=relay-surge begin", this);
            StateChanged?.Invoke();
        }

        private void MonitorRelayChapter()
        {
            if (!_relayChapterStarted)
            {
                BeginRelayChapter();
            }

            if (relayDefense != null && relayDefense.State == CombatEncounterState.Failed)
            {
                _relayChapterFailed = true;
                return;
            }

            int stabilized = relayTargets.Count(relay => relay != null && relay.IsStabilized);
            while (Rules.StabilizedRelays < stabilized)
            {
                Rules.RegisterRelayStabilized();
            }

            if (relayDefense != null && relayDefense.State == CombatEncounterState.Succeeded)
            {
                Rules.CompleteRelayDefense();
            }

            if (Rules.Chapter == SinglePlayerDemoChapter.StormCalibration)
            {
                BeginStormChapter();
            }
        }

        private void BeginStormChapter()
        {
            _stormChapterStarted = true;
            _stormChapterFailed = false;
            if (relayTargets != null)
            {
                foreach (DemoRelayTarget relay in relayTargets)
                {
                    relay?.SetChapterActive(false, false);
                }
            }

            BeginCurrentStormWave();
            Debug.Log("[Demo] chapter=3 event=storm-calibration begin", this);
            StateChanged?.Invoke();
        }

        private void BeginCurrentStormWave()
        {
            _stormChapterFailed = false;
            for (int index = 0; index < stormWaves.Length; index++)
            {
                if (index == CurrentStormWave)
                {
                    stormWaves[index]?.BeginEncounter();
                }
                else
                {
                    stormWaves[index]?.PrepareDormant();
                }
            }

            Debug.Log($"[Demo] chapter=3 wave={CurrentStormWave + 1}/{StormWaveCount} begin", this);
        }

        private void MonitorStormChapter()
        {
            if (!_stormChapterStarted)
            {
                BeginStormChapter();
            }

            CombatEncounterController wave = GetCurrentStormWave();
            if (wave == null || Rules.IsAwaitingCalibration)
            {
                return;
            }

            if (wave.State == CombatEncounterState.Failed)
            {
                _stormChapterFailed = true;
            }
            else if (wave.State == CombatEncounterState.Succeeded && Rules.CompleteCurrentStormWave())
            {
                StateChanged?.Invoke();
            }
        }

        private void RestartCurrentChapter()
        {
            Rules.RestartCurrentChapter();
            ChapterRestartCount++;
            if (Chapter == SinglePlayerDemoChapter.RelaySurge)
            {
                BeginRelayChapter();
            }
            else if (Chapter == SinglePlayerDemoChapter.StormCalibration)
            {
                foreach (CombatEncounterController wave in stormWaves)
                {
                    wave?.PrepareDormant();
                }

                BeginStormChapter();
            }

            Debug.Log($"[Demo] chapter={Chapter} action=restart count={ChapterRestartCount}", this);
            StateChanged?.Invoke();
        }

        private CombatEncounterController GetCurrentStormWave()
        {
            return stormWaves != null && CurrentStormWave >= 0 && CurrentStormWave < stormWaves.Length
                ? stormWaves[CurrentStormWave]
                : null;
        }

        private void CompleteDemo()
        {
            foreach (CombatEncounterController wave in stormWaves)
            {
                wave?.PrepareDormant();
            }

            Debug.Log($"[Demo] result=completed duration={CoolingIncidentController.FormatDuration(_elapsedSeconds)} secret325={EasterEgg325Discovered} restarts={ChapterRestartCount}", this);
        }
    }
}
