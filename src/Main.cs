using MelonLoader;
using System;
using UnityEngine;

namespace CarcassYieldTweaker
{
    public class Main : MelonMod
    {

        internal static bool debug_mode = false; // Set to false to disable debug logging
        internal static void DebugLog(string message)
        {
            if (debug_mode)
            {
                MelonLogger.Msg($"[Debug] {message}");
            }
        }

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg($"Version {Info.Version} loaded!");
            Settings.OnLoad();
        }
    }
}