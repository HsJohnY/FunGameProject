using FunGame.Audio;
using FunGame.Incident;
using NUnit.Framework;

namespace FunGame.Tests.EditMode
{
    public sealed class ProceduralBgmTests
    {
        [Test]
        public void RenderLoop_ProducesBoundedStereoSamples()
        {
            float[] samples = ProceduralBgmSynthesis.RenderLoop(8000, 2f, 1f);

            Assert.That(samples.Length, Is.EqualTo(32000));
            foreach (float sample in samples)
            {
                Assert.That(float.IsNaN(sample), Is.False);
                Assert.That(sample, Is.InRange(-0.8f, 0.8f));
            }
        }

        [Test]
        public void RenderCombatRhythmLoop_ProducesBoundedEnergeticStereoStem()
        {
            float[] samples = ProceduralBgmSynthesis.RenderCombatRhythmLoop(8000, 2f);
            double squaredEnergy = 0.0;

            Assert.That(samples.Length, Is.EqualTo(32000));
            foreach (float sample in samples)
            {
                Assert.That(float.IsNaN(sample), Is.False);
                Assert.That(sample, Is.InRange(-0.8f, 0.8f));
                squaredEnergy += sample * sample;
            }

            double rootMeanSquare = System.Math.Sqrt(squaredEnergy / samples.Length);
            Assert.That(rootMeanSquare, Is.GreaterThan(0.08), "战斗节奏层需要有足够清晰的鼓点能量");
        }

        [Test]
        public void TargetIntensity_RisesDuringRepairAndPeaksDuringCombat()
        {
            float start = CoolingBayBgmController.GetTargetIntensity(
                CoolingIncidentPhase.ContainLeak, CoolingIncidentRunState.Active, false);
            float lateRepair = CoolingBayBgmController.GetTargetIntensity(
                CoolingIncidentPhase.ResetPump, CoolingIncidentRunState.Active, false);
            float combat = CoolingBayBgmController.GetTargetIntensity(
                CoolingIncidentPhase.LoosenConnection, CoolingIncidentRunState.Active, true);
            float completed = CoolingBayBgmController.GetTargetIntensity(
                CoolingIncidentPhase.Stabilized, CoolingIncidentRunState.Succeeded, false);

            Assert.That(lateRepair, Is.GreaterThan(start));
            Assert.That(combat, Is.EqualTo(1f));
            Assert.That(completed, Is.Zero);
        }

        [Test]
        public void RhythmLayerGain_BecomesClearlyStrongerDuringCombat()
        {
            float repair = CoolingBayBgmController.GetRhythmLayerGain(0.68f, 0f);
            float combat = CoolingBayBgmController.GetRhythmLayerGain(1f, 1f);

            Assert.That(repair, Is.EqualTo(0.14f).Within(0.001f));
            Assert.That(combat, Is.EqualTo(0.72f).Within(0.001f));
            Assert.That(combat, Is.GreaterThan(repair * 5f));
        }
    }
}
