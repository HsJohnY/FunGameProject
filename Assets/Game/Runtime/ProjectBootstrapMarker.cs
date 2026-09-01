using System;
using UnityEngine;

namespace FunGame
{
    /// <summary>
    /// 标识自动生成的 M0 验证场景，本组件不持有任何玩法状态。
    /// </summary>
    public sealed class ProjectBootstrapMarker : MonoBehaviour
    {
        public const string BaselineId = "m0-technical-baseline";

        /// <summary>
        /// 判断构建后的玩家程序是否由 M0 自动冒烟检查启动。
        /// 编辑器和玩家的正常启动不包含此标记，因此不会自动退出。
        /// </summary>
        public static bool ContainsSmokeRunFlag(string[] arguments)
        {
            return Array.IndexOf(arguments, "--m0-smoke") >= 0;
        }

        private void Start()
        {
            Debug.Log($"[M0] Runtime scene started: {BaselineId}.");
            if (ContainsSmokeRunFlag(Environment.GetCommandLineArgs()))
            {
                Debug.Log("[M0] Runtime smoke check passed; exiting normally.");
                Application.Quit(0);
            }
        }
    }
}
