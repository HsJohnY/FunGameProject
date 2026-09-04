using System;
using System.Linq;
using UnityEngine;

namespace FunGame.Diagnostics
{
    public static class AutomationAudioMute
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void MuteAutomatedRuns()
        {
            if (Application.isBatchMode || Environment.GetCommandLineArgs().Any(a => a.StartsWith("--m4-check-output=") || a == "--mute-audio"))
                AudioListener.pause = true;
        }
    }
}
