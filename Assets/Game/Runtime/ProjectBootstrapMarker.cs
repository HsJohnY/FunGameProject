using System;
using UnityEngine;

namespace FunGame
{
    /// <summary>
    /// Identifies the generated M0 validation scene without owning gameplay state.
    /// </summary>
    public sealed class ProjectBootstrapMarker : MonoBehaviour
    {
        public const string BaselineId = "m0-technical-baseline";

        /// <summary>
        /// Returns whether a built player was launched for the automated M0 smoke check.
        /// Normal editor and player launches do not include this flag and remain open.
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
