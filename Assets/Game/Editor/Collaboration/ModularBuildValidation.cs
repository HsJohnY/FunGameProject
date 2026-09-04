using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace FunGame.Editor
{
    public static class ModularBuildValidation
    {
        public static void VerifyAndBuild()
        {
            Dictionary<string, string> baseline = Snapshot();
            ModularContentBuilder.GenerateEnvironmentScenes();
            AssertUnchanged(baseline, "first generation");
            ModularContentBuilder.GenerateEnvironmentScenes();
            AssertUnchanged(baseline, "second generation");
            ModularContentBuilder.BuildWindows("Builds/M4-Coop-Windows/FunGame-M4-Coop.exe");
            AssertUnchanged(baseline, "Windows build");
            Debug.Log("[ModuleBuildCheck] PASS: two generations and Windows build left " + baseline.Count + " source files unchanged.");
        }

        private static Dictionary<string, string> Snapshot()
        {
            using var sha = SHA256.Create();
            return new[] { "Assets", "Packages", "ProjectSettings" }
                .SelectMany(directory => Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToDictionary(path => path, path => Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))), StringComparer.Ordinal);
        }

        private static void AssertUnchanged(Dictionary<string, string> baseline, string stage)
        {
            Dictionary<string, string> actual = Snapshot();
            string[] changed = baseline.Keys.Union(actual.Keys).Where(path => !baseline.TryGetValue(path, out string before)
                || !actual.TryGetValue(path, out string after) || before != after).ToArray();
            if (changed.Length != 0) throw new InvalidDataException(stage + " rewrote source files: " + string.Join(", ", changed));
        }
    }
}
