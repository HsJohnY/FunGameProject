using System;

namespace FunGame.Demo
{
    /// <summary>
    /// 与 Unity 场景无关的三章推进规则，集中约束继电器、战斗波次和校准门禁。
    /// </summary>
    public sealed class SinglePlayerDemoRules
    {
        private readonly int _requiredCoolingRuns;
        private readonly int _requiredRelays;
        private readonly int _stormWaveCount;

        public SinglePlayerDemoRules(int requiredRelays, int stormWaveCount)
            : this(1, requiredRelays, stormWaveCount)
        {
        }

        public SinglePlayerDemoRules(int requiredCoolingRuns, int requiredRelays, int stormWaveCount)
        {
            if (requiredCoolingRuns <= 0) throw new ArgumentOutOfRangeException(nameof(requiredCoolingRuns));
            if (requiredRelays <= 0) throw new ArgumentOutOfRangeException(nameof(requiredRelays));
            if (stormWaveCount <= 0) throw new ArgumentOutOfRangeException(nameof(stormWaveCount));
            _requiredCoolingRuns = requiredCoolingRuns;
            _requiredRelays = requiredRelays;
            _stormWaveCount = stormWaveCount;
        }

        public SinglePlayerDemoChapter Chapter { get; private set; } = SinglePlayerDemoChapter.CoolingEmergency;
        public int CoolingRunsCompleted { get; private set; }
        public int StabilizedRelays { get; private set; }
        public bool RelayDefenseCompleted { get; private set; }
        public int CurrentStormWave { get; private set; }
        public bool IsAwaitingCalibration { get; private set; }

        public bool CompleteCoolingChapter()
        {
            if (Chapter != SinglePlayerDemoChapter.CoolingEmergency)
            {
                return false;
            }

            CoolingRunsCompleted++;
            if (CoolingRunsCompleted >= _requiredCoolingRuns)
            {
                Chapter = SinglePlayerDemoChapter.RelaySurge;
            }

            return true;
        }

        public bool RegisterRelayStabilized()
        {
            if (Chapter != SinglePlayerDemoChapter.RelaySurge || StabilizedRelays >= _requiredRelays)
            {
                return false;
            }

            StabilizedRelays++;
            TryEnterStormChapter();
            return true;
        }

        public bool CompleteRelayDefense()
        {
            if (Chapter != SinglePlayerDemoChapter.RelaySurge || RelayDefenseCompleted)
            {
                return false;
            }

            RelayDefenseCompleted = true;
            TryEnterStormChapter();
            return true;
        }

        public bool CompleteCurrentStormWave()
        {
            if (Chapter != SinglePlayerDemoChapter.StormCalibration || IsAwaitingCalibration)
            {
                return false;
            }

            IsAwaitingCalibration = true;
            return true;
        }

        public bool ConfirmCalibration()
        {
            if (Chapter != SinglePlayerDemoChapter.StormCalibration || !IsAwaitingCalibration)
            {
                return false;
            }

            IsAwaitingCalibration = false;
            if (CurrentStormWave + 1 >= _stormWaveCount)
            {
                Chapter = SinglePlayerDemoChapter.Completed;
            }
            else
            {
                CurrentStormWave++;
            }

            return true;
        }

        public void RestartCurrentChapter()
        {
            if (Chapter == SinglePlayerDemoChapter.RelaySurge)
            {
                StabilizedRelays = 0;
                RelayDefenseCompleted = false;
            }
            else if (Chapter == SinglePlayerDemoChapter.StormCalibration)
            {
                CurrentStormWave = 0;
                IsAwaitingCalibration = false;
            }
        }

        private void TryEnterStormChapter()
        {
            if (StabilizedRelays == _requiredRelays && RelayDefenseCompleted)
            {
                Chapter = SinglePlayerDemoChapter.StormCalibration;
                CurrentStormWave = 0;
                IsAwaitingCalibration = false;
            }
        }
    }
}
