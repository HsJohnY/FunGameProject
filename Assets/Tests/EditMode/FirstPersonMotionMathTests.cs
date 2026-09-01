using FunGame.Player;
using NUnit.Framework;
using UnityEngine;

namespace FunGame.Tests.EditMode
{
    public sealed class FirstPersonMotionMathTests
    {
        [TestCase(-120f, FirstPersonMotionMath.MinimumPitch)]
        [TestCase(120f, FirstPersonMotionMath.MaximumPitch)]
        [TestCase(25f, 25f)]
        public void ClampPitch_限制在安全俯仰范围(float input, float expected)
        {
            Assert.That(FirstPersonMotionMath.ClampPitch(input), Is.EqualTo(expected));
        }

        [Test]
        public void ClampMoveInput_斜向输入不会超过单位长度()
        {
            Vector2 result = FirstPersonMotionMath.ClampMoveInput(new Vector2(1f, 1f));

            Assert.That(result.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
