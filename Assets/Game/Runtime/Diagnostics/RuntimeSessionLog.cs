using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FunGame.Diagnostics
{
    /// <summary>
    /// 将 Unity Console 的运行消息同步保存为独立会话日志，方便复现联机和玩法问题。
    /// 每次启动创建一个文件，只保留最近五次；日志写入失败不会影响游戏运行。
    /// </summary>
    public static class RuntimeSessionLog
    {
        private const int RetainedSessionCount = 5;
        private const string LogDirectoryName = "Logs";
        private static readonly object WriteLock = new object();

        private static StreamWriter writer;
        private static bool initialized;

        public static string CurrentLogPath { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            Shutdown();
            initialized = false;
            CurrentLogPath = string.Empty;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (initialized) return;
            initialized = true;

            try
            {
                string directory = Path.Combine(Application.persistentDataPath, LogDirectoryName);
                Directory.CreateDirectory(directory);
                RemoveExpiredLogs(directory);

                string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
                CurrentLogPath = Path.Combine(directory, $"FunGame-{timestamp}.log");
                writer = new StreamWriter(
                    new FileStream(CurrentLogPath, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite),
                    new UTF8Encoding(false))
                {
                    AutoFlush = true
                };

                WriteDirect($"# FunGame runtime log | started={DateTime.Now:O}");
                WriteDirect($"# unity={Application.unityVersion} platform={Application.platform} version={Application.version}");
                Application.logMessageReceivedThreaded += HandleLog;
                Application.quitting += Shutdown;
                Debug.Log($"[RuntimeLog] 会话日志已启用：{CurrentLogPath}");
            }
            catch (Exception)
            {
                // 日志是诊断辅助，磁盘不可写时不能阻止游戏启动，也不能再次写 Console 造成递归。
                Shutdown();
            }
        }

        private static void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (writer == null) return;

            string timestamp = DateTime.Now.ToString("O");
            WriteDirect($"{timestamp} [{type}] {condition}");
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                && !string.IsNullOrWhiteSpace(stackTrace))
            {
                WriteDirect(stackTrace.TrimEnd());
            }
        }

        private static void WriteDirect(string message)
        {
            try
            {
                lock (WriteLock)
                {
                    writer?.WriteLine(message);
                }
            }
            catch (IOException)
            {
                // 运行中磁盘被移除或文件被占用时静默停写，避免影响主线程。
            }
            catch (ObjectDisposedException)
            {
                // 退出阶段可能与后台日志回调竞争，已关闭即可。
            }
        }

        private static void RemoveExpiredLogs(string directory)
        {
            FileInfo[] logs = new DirectoryInfo(directory)
                .GetFiles("FunGame-*.log")
                .OrderByDescending(file => file.CreationTimeUtc)
                .ToArray();

            // 为本次即将创建的日志预留一个名额。
            foreach (FileInfo expired in logs.Skip(RetainedSessionCount - 1))
            {
                try { expired.Delete(); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void Shutdown()
        {
            Application.logMessageReceivedThreaded -= HandleLog;
            Application.quitting -= Shutdown;
            lock (WriteLock)
            {
                writer?.Dispose();
                writer = null;
            }
        }
    }
}
