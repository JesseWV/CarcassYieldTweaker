using MelonLoader;
using System;
using UnityEngine;

namespace CarcassYieldTweaker
{
    public class Main : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg($"Version {Info.Version} loaded!");
            Settings.OnLoad();
        }

        internal static void DebugLog(string message)
        {
            if (Settings.Instance.Extra_EnableDebug)
            {
                MelonLogger.Msg($"[Debug] {message}");
            }
        }

    }
}