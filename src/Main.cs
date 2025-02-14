using MelonLoader;
using System;
using UnityEngine;

namespace CarcassYieldTweaker
{
    public class Main : MelonMod
    {
        internal static void DebugLog(string message)
        {
            if (Settings.instance.Extra_EnableDebug)
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