using System;
using UnityEngine;

namespace FunGame.Diagnostics
{
    /// <summary>
    /// 标识可运行开发检查点，并为自动化玩家程序冒烟检查提供正常退出入口。
    /// </summary>
    public sealed class DevelopmentCheckpoint : MonoBehaviour
    {
        [SerializeField] private string checkpointId = "unconfigured";
        [SerializeField] private string smokeArgument = "--checkpoint-smoke";

        /// <summary>
        /// 由场景生成器设置检查点名称和专用命令行参数。
        /// </summary>
        public void Configure(string id, string argument)
        {
            checkpointId = id;
            smokeArgument = argument;
        }

        private void Start()
        {
            Debug.Log($"[Checkpoint] 已进入运行场景：{checkpointId}。", this);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), smokeArgument) < 0)
            {
                return;
            }

            Debug.Log($"[Checkpoint] {checkpointId} 冒烟检查通过，程序正常退出。", this);
            Application.Quit(0);
        }
    }
}
