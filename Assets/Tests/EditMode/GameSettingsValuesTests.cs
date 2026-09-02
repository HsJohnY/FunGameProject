using FunGame.Settings;
using NUnit.Framework;

namespace FunGame.Tests.EditMode
{
    public sealed class GameSettingsValuesTests
    {
        [Test]
        public void Sanitized_ClampsUnsafeValuesAndRepairsFrameLimit()
        {
            var values = new GameSettingsValues
            {
                MasterVolume = 2f,
                MusicVolume = -1f,
                SoundEffectsVolume = 4f,
                MouseSensitivity = 1f,
                FieldOfView = 180f,
                ResolutionWidth = 100,
                ResolutionHeight = 100,
                QualityLevel = 99,
                FrameRateLimit = 17
            };

            GameSettingsValues result = values.Sanitized(4);

            Assert.That(result.MasterVolume, Is.EqualTo(1f));
            Assert.That(result.MusicVolume, Is.Zero);
            Assert.That(result.SoundEffectsVolume, Is.EqualTo(1f));
            Assert.That(result.MouseSensitivity, Is.EqualTo(0.3f));
            Assert.That(result.FieldOfView, Is.EqualTo(110f));
            Assert.That(result.ResolutionWidth, Is.EqualTo(640));
            Assert.That(result.ResolutionHeight, Is.EqualTo(480));
            Assert.That(result.QualityLevel, Is.EqualTo(3));
            Assert.That(result.FrameRateLimit, Is.EqualTo(60));
        }
    }
}
