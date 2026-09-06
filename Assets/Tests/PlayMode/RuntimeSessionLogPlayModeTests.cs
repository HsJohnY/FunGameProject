using System.Collections;
using System.IO;
using FunGame.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class RuntimeSessionLogPlayModeTests
    {
        [UnityTest]
        public IEnumerator 运行消息会写入当前会话日志()
        {
            Assert.That(RuntimeSessionLog.CurrentLogPath, Is.Not.Empty);
            Assert.That(File.Exists(RuntimeSessionLog.CurrentLogPath), Is.True);

            string marker = $"runtime-log-test-{Time.frameCount}";
            Debug.Log(marker);
            yield return null;

            string content;
            using (var stream = new FileStream(RuntimeSessionLog.CurrentLogPath, FileMode.Open,
                       FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
                content = reader.ReadToEnd();
            Assert.That(content, Does.Contain(marker));
            Assert.That(content, Does.Contain(Application.unityVersion));
        }
    }
}
