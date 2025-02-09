using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.IntBackedUnit;
using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarcassYieldTweaker
{
    internal static class Patches
    {
        internal static class HarvestState
        {
            internal static bool panelOpened = false;
            internal static bool toolChanged = false; 
            internal static bool logPending = false;
        }

        internal static class Panel_BodyHarvest_Time_Patches
        {

            private static float GetRoundedMultiplier(string itemType, string animalType)
            {
                float rawMultiplier = 1f;  // Default multiplier

                // Use a switch statement for each animal type and item type
                switch (animalType)
                {
                    case "GEAR_RabbitCarcass":
                        if (itemType == "Hide")
                            rawMultiplier = Settings.instance.HideTimeSliderRabbit;
                        else if (itemType == "Meat")
                            rawMultiplier = Settings.instance.MeatTimeSliderGlobal;
                        else if (itemType == "Gut")
                            rawMultiplier = Settings.instance.GutTimeSliderGlobal;
                        break;

                    case "GEAR_PtarmiganCarcass":
                        if (itemType == "Hide")
                            rawMultiplier = Settings.instance.HideTimeSliderPtarmigan;
                        else if (itemType == "Meat")
                            rawMultiplier = Settings.instance.MeatTimeSliderGlobal;
                        break;

                    case "WILDLIFE_Doe":
                        if (itemType == "Hide")
                            rawMultiplier = Settings.instance.HideTimeSliderDoe;
                        else if (itemType == "Meat")
                            rawMultiplier = Settings.instance.MeatTimeSliderGlobal;
                        else if (itemType == "Gut")
                            rawMultiplier = Settings.instance.GutTimeSliderGlobal;
                        break;

                    case "WILDLIFE_Stag":
                        if (itemType == "Hide")
                            rawMultiplier = Settings.instance.HideTimeSliderStag;
                        else if (itemType == "Meat")
                            rawMultiplier = Settings.instance.MeatTimeSliderGlobal;
                        else if (itemType == "Gut")
                            rawMultiplier = Settings.instance.GutTimeSliderGlobal;
                        break;


                    case "WILDLIFE_Moose":
                        if (itemType == "Hide")
                            rawMultiplier = Settings.instance.HideTimeSliderMoose;
                        else if (itemType == "Meat")
                            rawMultiplier = Settings.instance.MeatTimeSliderGlobal;
                        else if (itemType == "Gut")
                            rawMultiplier = Settings.instance.GutTimeSliderGlobal;
                        break;

                    case "WILDLIFE_Wolf":
                        if (itemType == "Hide")
                            rawMultiplier = Settings.instance.HideTimeSliderWolf;
                        else if (itemType == "Meat")
                            rawMultiplier = Settings.instance.MeatTimeSliderGlobal;
                        else if (itemType == "Gut")
                            rawMultiplier = Settings.instance.GutTimeSliderGlobal;
                        break;

                    case "WILDLIFE_Wolf_grey":
                        if (itemType == "Hide")
                            rawMultiplier = Settings.instance.HideTimeSliderTimberWolf;
                        else if (itemType == "Meat")
                            rawMultiplier = Settings.instance.MeatTimeSliderGlobal;
                        else if (itemType == "Gut")
                            rawMultiplier = Settings.instance.GutTimeSliderGlobal;
                        break;

                    case "WILDLIFE_Wolf_Starving":
                        if (itemType == "Hide")
                            rawMultiplier = Settings.instance.HideTimeSliderPoisonedWolf;
                        else if (itemType == "Gut")
                            rawMultiplier = Settings.instance.GutTimeSliderGlobal;
                        break;

                    case "WILDLIFE_Bear":
                        if (itemType == "Hide")
                            rawMultiplier = Settings.instance.HideTimeSliderBear;
                        else if (itemType == "Meat")
                            rawMultiplier = Settings.instance.MeatTimeSliderGlobal;
                        else if (itemType == "Gut")
                            rawMultiplier = Settings.instance.GutTimeSliderGlobal;
                        break;

                    case "WILDLIFE_Cougar":
                        if (itemType == "Hide")
                            rawMultiplier = Settings.instance.HideTimeSliderCougar;
                        else if (itemType == "Meat")
                            rawMultiplier = Settings.instance.MeatTimeSliderGlobal;
                        else if (itemType == "Gut")
                            rawMultiplier = Settings.instance.GutTimeSliderGlobal;
                        break;

                    default:
                        // Fallback to global multipliers if animal type not found
                        if (itemType == "Hide")
                            rawMultiplier = 1.0f;
                        else if (itemType == "Meat")
                            rawMultiplier = Settings.instance.MeatTimeSliderGlobal;
                        else if (itemType == "FrozenMeat")
                            rawMultiplier = Settings.instance.FrozenMeatTimeSliderGlobal;
                        else if (itemType == "Gut")
                            rawMultiplier = Settings.instance.GutTimeSliderGlobal;
                        Main.DebugLog($"[UNKNOWN:{animalType}] GLOBAL multiplier: {rawMultiplier:F2}");
                        break;
                }

                return (float)Math.Round(rawMultiplier, 2);
            }
            internal static float ConvertItemWeightToFloat(Il2CppTLD.IntBackedUnit.ItemWeight itemWeight)
            {
                // Convert the scaled value in m_Units to a float in kilograms
                return (float)itemWeight.m_Units / 1_000_000_000f;
            }


            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.GetHarvestDurationMinutes)), HarmonyPriority(Priority.Low)]
            internal class Patch_HarvestDuration
            {
                private static float previousUnmodifiedTotalTime = -1f;
                private static Dictionary<int, (float Meat, float Hide, float Gut)> toolBaseTimes = new();
                private static Dictionary<int, (bool Meat, bool Hide, bool Gut)> toolKnownFlags = new();
                private static float lastMeatAmount;
                private static int lastHideUnits;
                private static int lastGutUnits;
                private static int previousToolIndex = -1;  // Stores the last-used tool index


                public static void ResetHarvestVariables()
                {
                    previousUnmodifiedTotalTime = -1f;
                    toolBaseTimes.Clear();
                    toolKnownFlags.Clear();
                    lastMeatAmount = -1f;
                    lastHideUnits = -1;
                    lastGutUnits = -1;
                    previousToolIndex = -1;

                    Main.DebugLog("[Close] Patch_HarvestDuration Static variables reset.");
                }

                static void Postfix(Il2Cpp.Panel_BodyHarvest __instance, ref float __result)
                {
                    if (__instance == null || string.IsNullOrEmpty(__instance.name))
                    {
                        Main.DebugLog("Panel_BodyHarvest instance is null or name is empty.");
                        return;
                    }

                    try
                    {
                        string animalType = __instance.m_BodyHarvest?.name ?? string.Empty;
                        int toolIndex = __instance.m_SelectedToolItemIndex;

                        float currentUnmodifiedTotalTime = __result;
                        float lastRecordedTotalTime = previousUnmodifiedTotalTime;
                        previousUnmodifiedTotalTime = currentUnmodifiedTotalTime;

                        float meatAmount = ConvertItemWeightToFloat(__instance.m_MenuItem_Meat.HarvestAmount);
                        int hideUnits = __instance.m_MenuItem_Hide.HarvestUnits;
                        int gutUnits = __instance.m_MenuItem_Gut.HarvestUnits;

                        float meatMultiplier = GetRoundedMultiplier("Meat", animalType);
                        float hideMultiplier = GetRoundedMultiplier("Hide", animalType);
                        float gutMultiplier = GetRoundedMultiplier("Gut", animalType);

                        bool meatAmountChanged = meatAmount != lastMeatAmount;
                        bool hideUnitsChanged = hideUnits != lastHideUnits;
                        bool gutUnitsChanged = gutUnits != lastGutUnits;
                        bool anyAmountChanged = meatAmountChanged || hideUnitsChanged || gutUnitsChanged;

                        if (HarvestState.panelOpened)
                        {
                            previousToolIndex = toolIndex;
                            lastMeatAmount = meatAmount;
                            lastHideUnits = hideUnits;
                            lastGutUnits = gutUnits;
                            toolBaseTimes[toolIndex] = (0f, 0f, 0f);
                            toolKnownFlags[toolIndex] = (false, false, false);
                            Main.DebugLog("[Open] -------- Panel_BodyHarvest Opened --------");
                            Main.DebugLog($"[Initialize] Animal: {animalType}, Tool:{toolIndex}, Time Multipliers - Meat: {meatMultiplier}x, Hide: {hideMultiplier}x, Gut: {gutMultiplier}x");
                            HarvestState.panelOpened = false;
                            HarvestState.logPending = false;
                        }

                        if (HarvestState.logPending)
                        {
                            Main.DebugLog($"[Unmodified Times] {lastRecordedTotalTime:F2}m -> {currentUnmodifiedTotalTime:F2}m ");
                        }


                        if (HarvestState.toolChanged)  // ** Detect tool change **
                        {
                            float threeItemRatio = currentUnmodifiedTotalTime / lastRecordedTotalTime;
                            // **Log previous and new tool index**
                            if (HarvestState.logPending)
                            {
                                Main.DebugLog($"[ToolSwitch] {previousToolIndex} -> {toolIndex}");
                            }

                            // If this tool has never been seen at all, initialize it with the previous tool's base times and a 3-item scaling ratio
                            if (!toolBaseTimes.ContainsKey(toolIndex))
                            {
                                if (lastRecordedTotalTime > 0)
                                {
                                    // Use the previous tool's base times as a starting point. Then apply the ratio of the previous tool's total time and the new tool's total time to the new tool's base times to ensure proper scaling  
                                    toolBaseTimes[toolIndex] = (
                                        toolBaseTimes.ContainsKey(previousToolIndex) ? toolBaseTimes[previousToolIndex].Meat * threeItemRatio : 0f,
                                        toolBaseTimes.ContainsKey(previousToolIndex) ? toolBaseTimes[previousToolIndex].Hide * threeItemRatio : 0f,
                                        toolBaseTimes.ContainsKey(previousToolIndex) ? toolBaseTimes[previousToolIndex].Gut * threeItemRatio : 0f
                                    );

                                    // Set the known flags to false for the new tool, since base times are only estimates
                                    toolKnownFlags[toolIndex] = (false, false, false);
                                    Main.DebugLog($"[ToolSwitch] BaseTime Estimation - Tool:{toolIndex} - 3-item Ratio: {threeItemRatio:F3} - " +
                                        $"Meat: {toolBaseTimes[previousToolIndex].Meat:F2} -> {toolBaseTimes[toolIndex].Meat:F2} min/kg, " +
                                        $"Hide: {toolBaseTimes[previousToolIndex].Hide:F2} -> {toolBaseTimes[toolIndex].Hide:F2} min/unit, " +
                                        $"Gut: {toolBaseTimes[previousToolIndex].Gut:F2} -> {toolBaseTimes[toolIndex].Gut:F2} min/unit");
                                }
                                else
                                {
                                    // If a tool switch happens with all amounts set to 0, initialize the tool with 0 base times and unknown flags
                                    toolBaseTimes[toolIndex] = (0f, 0f, 0f);
                                    toolKnownFlags[toolIndex] = (false, false, false);
                                    Main.DebugLog($"[ToolSwitch] New Tool: {toolIndex}, initialized.");
                                }


                            }

                            // **Update the previous tool index AFTER handling the switch**
                            previousToolIndex = toolIndex;
                            HarvestState.toolChanged = false;
                        }


                        if (anyAmountChanged)
                        {
                            // Calculate the time difference between the last recorded total time and the current total time
                            float deltaTime = currentUnmodifiedTotalTime - lastRecordedTotalTime;

                            List<string> discoveredItems = new();

                            if (meatAmountChanged && meatAmount > lastMeatAmount && !toolKnownFlags[toolIndex].Meat)
                            {
                                // Determine the amount of meat added
                                float addedAmount = meatAmount - lastMeatAmount;
                                // Calculate the base time for the meat item
                                float baseTime = deltaTime / addedAmount;

                                Main.DebugLog($"[BaseTime] Meat: {deltaTime:F2}m / {addedAmount:F2}kg = {baseTime:F2} min/kg");

                                // Update the base time for the meat item
                                toolBaseTimes[toolIndex] = (baseTime, toolBaseTimes[toolIndex].Hide, toolBaseTimes[toolIndex].Gut);
                                // Set the meat flag to true, since we now know the base time
                                toolKnownFlags[toolIndex] = (true, toolKnownFlags[toolIndex].Hide, toolKnownFlags[toolIndex].Gut);
                            }

                            if (hideUnitsChanged && hideUnits > lastHideUnits && !toolKnownFlags[toolIndex].Hide)
                            {
                                // Determine the amount of hide added
                                float addedAmount = hideUnits - lastHideUnits;
                                // Calculate the base time for the hide item
                                float baseTime = deltaTime / addedAmount;

                                Main.DebugLog($"[BaseTime] Hide:{deltaTime:F2}m / {addedAmount:F2} = {baseTime:F2} min/unit");

                                // Update the base time for the hide item
                                toolBaseTimes[toolIndex] = (toolBaseTimes[toolIndex].Meat, baseTime, toolBaseTimes[toolIndex].Gut);
                                // Set the hide flag to true, since we now know the base time
                                toolKnownFlags[toolIndex] = (toolKnownFlags[toolIndex].Meat, true, toolKnownFlags[toolIndex].Gut);
                            }

                            if (gutUnitsChanged && gutUnits > lastGutUnits && !toolKnownFlags[toolIndex].Gut)
                            {
                                // Determine the amount of gut added
                                float addedAmount = gutUnits - lastGutUnits;
                                // Calculate the base time for the gut item
                                float baseTime = deltaTime / addedAmount;

                                Main.DebugLog($"[BaseTime] Gut: {deltaTime:F2}m / {addedAmount:F2} = {baseTime:F2} min/unit");

                                // Update the base time for the gut item
                                toolBaseTimes[toolIndex] = (toolBaseTimes[toolIndex].Meat, toolBaseTimes[toolIndex].Hide, baseTime);
                                // Set the gut flag to true, since we now know the base time
                                toolKnownFlags[toolIndex] = (toolKnownFlags[toolIndex].Meat, toolKnownFlags[toolIndex].Hide, true);
                            }


                            int currentToolBaseTimeKnownCount = (toolKnownFlags[toolIndex].Meat ? 1 : 0) + (toolKnownFlags[toolIndex].Hide ? 1 : 0) + (toolKnownFlags[toolIndex].Gut ? 1 : 0);

                            // Determine how many base times are known for the current tool
                            int knownCount = (toolKnownFlags[toolIndex].Meat ? 1 : 0) +
                                             (toolKnownFlags[toolIndex].Hide ? 1 : 0) +
                                             (toolKnownFlags[toolIndex].Gut ? 1 : 0);

                            // Start with all unknowns then subtract the knowns to get the unknown count
                            int unknownCount = 3 - knownCount;

                            // Calculate the contributions of confirmed items only
                            float knownContribution = 0f;
                            if (toolKnownFlags[toolIndex].Meat && meatAmount > 0)
                                knownContribution += toolBaseTimes[toolIndex].Meat * meatAmount;
                            if (toolKnownFlags[toolIndex].Hide && hideUnits > 0)
                                knownContribution += toolBaseTimes[toolIndex].Hide * hideUnits;
                            if (toolKnownFlags[toolIndex].Gut && gutUnits > 0)
                                knownContribution += toolBaseTimes[toolIndex].Gut * gutUnits;

                            // The remaining time that must come from the unknown items
                            float remainingTime = currentUnmodifiedTotalTime - knownContribution;

                            if (unknownCount == 2)
                            {
                                // For two unknown items, we want to distribute the remaining time
                                // using the previous tool's base times for those unknown items.
                                float previousContribution = 0f;
                                if (!toolKnownFlags[toolIndex].Meat && meatAmount > 0 && toolBaseTimes.ContainsKey(previousToolIndex))
                                    previousContribution += toolBaseTimes[previousToolIndex].Meat * meatAmount;
                                if (!toolKnownFlags[toolIndex].Hide && hideUnits > 0 && toolBaseTimes.ContainsKey(previousToolIndex))
                                    previousContribution += toolBaseTimes[previousToolIndex].Hide * hideUnits;
                                if (!toolKnownFlags[toolIndex].Gut && gutUnits > 0 && toolBaseTimes.ContainsKey(previousToolIndex))
                                    previousContribution += toolBaseTimes[previousToolIndex].Gut * gutUnits;

                                if (previousContribution > 0)
                                {
                                    float newRatio = remainingTime / previousContribution;
                                    Main.DebugLog($"[Algebraic] Total Previous Contribution for Unknown Items: {previousContribution:F2}m, New 2-Item Ratio: {newRatio:F3}");

                                    // For each unknown item, update its base time using the previous tool's value scaled by newRatio.
                                    if (!toolKnownFlags[toolIndex].Meat && meatAmount > 0)
                                    {
                                        float newBaseTime = toolBaseTimes[previousToolIndex].Meat * newRatio;
                                        toolBaseTimes[toolIndex] = (newBaseTime, toolBaseTimes[toolIndex].Hide, toolBaseTimes[toolIndex].Gut);
                                        Main.DebugLog($"[BaseTime] Meat [Algebraic Estimate] -> {newBaseTime:F2} min/kg");
                                    }
                                    if (!toolKnownFlags[toolIndex].Hide && hideUnits > 0)
                                    {
                                        float newBaseTime = toolBaseTimes[previousToolIndex].Hide * newRatio;
                                        toolBaseTimes[toolIndex] = (toolBaseTimes[toolIndex].Meat, newBaseTime, toolBaseTimes[toolIndex].Gut);
                                        Main.DebugLog($"[BaseTime] Hide [Algebraic Estimate] -> {newBaseTime:F2} min/unit");
                                    }
                                    if (!toolKnownFlags[toolIndex].Gut && gutUnits > 0)
                                    {
                                        float newBaseTime = toolBaseTimes[previousToolIndex].Gut * newRatio;
                                        toolBaseTimes[toolIndex] = (toolBaseTimes[toolIndex].Meat, toolBaseTimes[toolIndex].Hide, newBaseTime);
                                        Main.DebugLog($"[BaseTime] Gut [Algebraic Estimate] -> {newBaseTime:F2} min/unit");
                                    }
                                }
                                else if (previousContribution == 0 && meatAmount > 0 && hideUnits > 0 && gutUnits > 0)
                                {
                                    Main.DebugLog("[WARNING] Total previous contribution for unknown items is zero!");
                                }
                            }
                            else if (unknownCount == 1)
                            {
                                // If exactly one base time is unknown, assign it directly from the remaining time.
                                if (!toolKnownFlags[toolIndex].Meat && meatAmount > 0)
                                {
                                    float newBaseTime = remainingTime / meatAmount;
                                    toolBaseTimes[toolIndex] = (newBaseTime, toolBaseTimes[toolIndex].Hide, toolBaseTimes[toolIndex].Gut);
                                    toolKnownFlags[toolIndex] = (true, toolKnownFlags[toolIndex].Hide, toolKnownFlags[toolIndex].Gut);
                                    Main.DebugLog($"[BaseTime] Meat [Algebraic] -> {newBaseTime:F2} min/kg");
                                }
                                else if (!toolKnownFlags[toolIndex].Hide && hideUnits > 0)
                                {
                                    float newBaseTime = remainingTime / hideUnits;
                                    toolBaseTimes[toolIndex] = (toolBaseTimes[toolIndex].Meat, newBaseTime, toolBaseTimes[toolIndex].Gut);
                                    toolKnownFlags[toolIndex] = (toolKnownFlags[toolIndex].Meat, true, toolKnownFlags[toolIndex].Gut);
                                    Main.DebugLog($"[BaseTime] Hide [Algebraic] -> {newBaseTime:F2} min/unit");
                                }
                                else if (!toolKnownFlags[toolIndex].Gut && gutUnits > 0)
                                {
                                    float newBaseTime = remainingTime / gutUnits;
                                    toolBaseTimes[toolIndex] = (toolBaseTimes[toolIndex].Meat, toolBaseTimes[toolIndex].Hide, newBaseTime);
                                    toolKnownFlags[toolIndex] = (toolKnownFlags[toolIndex].Meat, toolKnownFlags[toolIndex].Hide, true);
                                    Main.DebugLog($"[BaseTime] Gut [Algebraic] -> {newBaseTime:F2} min/unit");
                                }
                            } 
                        } // End of anyAmountChanged block
                        /*                            // Algebraic Calculation for when only 1 base time is known (i.e. 2 items remain unknown)
                                                    if (currentToolBaseTimeKnownCount == 1) // Only 1 exact base time known, fix the other two
                                                    {
                                                        // Sum the contribution from the known item
                                                        float knownBaseTimeItemsCurrentToolContributions = 0f;
                                                        bool meatIsKnown = toolKnownFlags[toolIndex].Meat;
                                                        bool hideIsKnown = toolKnownFlags[toolIndex].Hide;
                                                        bool gutIsKnown = toolKnownFlags[toolIndex].Gut;

                                                        if (meatIsKnown && meatAmount > 0)
                                                            knownBaseTimeItemsCurrentToolContributions += toolBaseTimes[toolIndex].Meat * meatAmount;
                                                        if (hideIsKnown && hideUnits > 0)
                                                            knownBaseTimeItemsCurrentToolContributions += toolBaseTimes[toolIndex].Hide * hideUnits;
                                                        if (gutIsKnown && gutUnits > 0)
                                                            knownBaseTimeItemsCurrentToolContributions += toolBaseTimes[toolIndex].Gut * gutUnits;

                                                        // Calculate the adjusted remaining time for the unknown items
                                                        float unknownBaseTimeItemsCurrentToolTimeContribution = currentUnmodifiedTotalTime - knownBaseTimeItemsCurrentToolContributions;
                                                        Main.DebugLog($"[Algebraic] Known BaseTime Contribution: {knownBaseTimeItemsCurrentToolContributions:F2}m, Unknown BaseTime Contribution: {unknownBaseTimeItemsCurrentToolTimeContribution:F2}m");

                                                        // Identify the unknown items and sum their previous tool contributions
                                                        float unknownBaseTimeItemsPreviousToolContributions = 0f;
                                                        if (!meatIsKnown && meatAmount > 0 && toolBaseTimes.ContainsKey(previousToolIndex))
                                                            unknownBaseTimeItemsPreviousToolContributions += toolBaseTimes[previousToolIndex].Meat * meatAmount;
                                                        if (!hideIsKnown && hideUnits > 0 && toolBaseTimes.ContainsKey(previousToolIndex))
                                                            unknownBaseTimeItemsPreviousToolContributions += toolBaseTimes[previousToolIndex].Hide * hideUnits;
                                                        if (!gutIsKnown && gutUnits > 0 && toolBaseTimes.ContainsKey(previousToolIndex))
                                                            unknownBaseTimeItemsPreviousToolContributions += toolBaseTimes[previousToolIndex].Gut * gutUnits;

                                                        if (unknownBaseTimeItemsPreviousToolContributions > 0)
                                                        {
                                                            float unknownBaseTimeItemsPreviousToCurrentToolRatio = unknownBaseTimeItemsCurrentToolTimeContribution / unknownBaseTimeItemsPreviousToolContributions;
                                                            Main.DebugLog($"[Algebraic] Total Previous Contribution: {unknownBaseTimeItemsPreviousToolContributions:F2}m, 2-Item Ratio: {unknownBaseTimeItemsPreviousToCurrentToolRatio:F3}");

                                                            // For each unknown item, update its base time using the previous tool's value scaled by the new twoItemRatio.
                                                            if (!meatIsKnown && meatAmount > 0)
                                                            {
                                                                // Calculate the new base time for the meat item by scaling the previous tool's base time with the overall ratio between the previous and current tool's total time
                                                                float newBaseTime = toolBaseTimes[previousToolIndex].Meat * unknownBaseTimeItemsPreviousToCurrentToolRatio;
                                                                // Update the new tool base time for the meat item
                                                                toolBaseTimes[toolIndex] = (newBaseTime, toolBaseTimes[toolIndex].Hide, toolBaseTimes[toolIndex].Gut);
                                                                //toolKnownFlags[toolIndex] = (true, toolKnownFlags[toolIndex].Hide, toolKnownFlags[toolIndex].Gut);
                                                                Main.DebugLog($"[BaseTime] Meat [Algebraic] -> {newBaseTime:F2} min/kg");
                                                            }
                                                            if (!hideIsKnown && hideUnits > 0)
                                                            {
                                                                // Calculate the new base time for the meat item by scaling the previous tool's base time with the overall ratio between the previous and current tool's total time
                                                                float newBaseTime = toolBaseTimes[previousToolIndex].Hide * unknownBaseTimeItemsPreviousToCurrentToolRatio;
                                                                // Update the new tool base time for the hide item
                                                                toolBaseTimes[toolIndex] = (toolBaseTimes[toolIndex].Meat, newBaseTime, toolBaseTimes[toolIndex].Gut);
                                                                //toolKnownFlags[toolIndex] = (toolKnownFlags[toolIndex].Meat, true, toolKnownFlags[toolIndex].Gut);
                                                                Main.DebugLog($"[BaseTime] Hide [Algebraic] -> {newBaseTime:F2} min/unit");
                                                            }
                                                            if (!gutIsKnown && gutUnits > 0)
                                                            {
                                                                // Calculate the new base time for the meat item by scaling the previous tool's base time with the overall ratio between the previous and current tool's total time
                                                                float newBaseTime = toolBaseTimes[previousToolIndex].Gut * unknownBaseTimeItemsPreviousToCurrentToolRatio;
                                                                // Update the new tool base time for the gut item
                                                                toolBaseTimes[toolIndex] = (toolBaseTimes[toolIndex].Meat, toolBaseTimes[toolIndex].Hide, newBaseTime);
                                                                //toolKnownFlags[toolIndex] = (toolKnownFlags[toolIndex].Meat, toolKnownFlags[toolIndex].Hide, true);
                                                                Main.DebugLog($"[BaseTime] Gut [Algebraic] -> {newBaseTime:F2} min/unit");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            Main.DebugLog("[WARNING] Total previous contribution for unknown items is zero!");
                                                        }
                                                    }


                                                    // Final Algebraic Calculation: Solve for the last unknown base time
                                                    if (currentToolBaseTimeKnownCount == 2) // If 2 base times are known, calculate the 3rd
                                                    {
                                                        // Sum the contributions of the items whose base times are known.
                                                        float knownContributions = 0f;
                                                        if (toolKnownFlags[toolIndex].Meat && meatAmount > 0)
                                                            knownContributions += toolBaseTimes[toolIndex].Meat * meatAmount;
                                                        if (toolKnownFlags[toolIndex].Hide && hideUnits > 0)
                                                            knownContributions += toolBaseTimes[toolIndex].Hide * hideUnits;
                                                        if (toolKnownFlags[toolIndex].Gut && gutUnits > 0)
                                                            knownContributions += toolBaseTimes[toolIndex].Gut * gutUnits;

                                                        // The remaining time to be allocated to the unknown item is the total minus the known contributions.
                                                        float remainingTime = currentUnmodifiedTotalTime - knownContributions;

                                                        // Now, update only the unknown item's base time.
                                                        if (!toolKnownFlags[toolIndex].Meat && meatAmount > 0)
                                                        {
                                                            toolBaseTimes[toolIndex] = (remainingTime / meatAmount, toolBaseTimes[toolIndex].Hide, toolBaseTimes[toolIndex].Gut);
                                                            toolKnownFlags[toolIndex] = (true, toolKnownFlags[toolIndex].Hide, toolKnownFlags[toolIndex].Gut);
                                                            Main.DebugLog($"[BaseTime] Meat [Algebraic] -> {toolBaseTimes[toolIndex].Meat:F2} min/kg");
                                                        }
                                                        else if (!toolKnownFlags[toolIndex].Hide && hideUnits > 0)
                                                        {
                                                            toolBaseTimes[toolIndex] = (toolBaseTimes[toolIndex].Meat, remainingTime / hideUnits, toolBaseTimes[toolIndex].Gut);
                                                            toolKnownFlags[toolIndex] = (toolKnownFlags[toolIndex].Meat, true, toolKnownFlags[toolIndex].Gut);
                                                            Main.DebugLog($"[BaseTime] Hide [Algebraic] -> {toolBaseTimes[toolIndex].Hide:F2} min/unit");
                                                        }
                                                        else if (!toolKnownFlags[toolIndex].Gut && gutUnits > 0)
                                                        {
                                                            toolBaseTimes[toolIndex] = (toolBaseTimes[toolIndex].Meat, toolBaseTimes[toolIndex].Hide, remainingTime / gutUnits);
                                                            toolKnownFlags[toolIndex] = (toolKnownFlags[toolIndex].Meat, toolKnownFlags[toolIndex].Hide, true);
                                                            Main.DebugLog($"[BaseTime] Gut [Algebraic] -> {toolBaseTimes[toolIndex].Gut:F2} min/unit");
                                                        }
                                                    }
                                                }
                        */



                        lastMeatAmount = meatAmount;
                        lastHideUnits = hideUnits;
                        lastGutUnits = gutUnits;

                        float originalMeatTime = meatAmount * toolBaseTimes[toolIndex].Meat;
                        float originalHideTime = hideUnits * toolBaseTimes[toolIndex].Hide;
                        float originalGutTime = gutUnits * toolBaseTimes[toolIndex].Gut;

                        float adjustedMeatTime = originalMeatTime * meatMultiplier;
                        float adjustedHideTime = originalHideTime * hideMultiplier;
                        float adjustedGutTime = originalGutTime * gutMultiplier;

                        float totalAdjustedHarvestTime = adjustedMeatTime + adjustedHideTime + adjustedGutTime;
                        float totalUnadjustedHarvestTime = originalMeatTime + originalHideTime + originalGutTime;

                        __result = totalAdjustedHarvestTime;

                        if (HarvestState.logPending)
                        {
                            string logDetails = $"Tool: {toolIndex} - ";
                            logDetails += $"{meatAmount:F1}kg Meat {originalMeatTime:F1}m -> {adjustedMeatTime:F1}m ({meatMultiplier:F2}x), ";
                            logDetails += $"{hideUnits}x Hide {originalHideTime:F1}m -> {adjustedHideTime:F1}m ({hideMultiplier:F2}x), ";
                            logDetails += $"{gutUnits}x Gut {originalGutTime:F1}m -> {adjustedGutTime:F1}m ({gutMultiplier:F2}x), ";
                            logDetails += $"- Total {totalUnadjustedHarvestTime:F1}m -> {totalAdjustedHarvestTime:F1}m ";
                            logDetails += $"({(totalUnadjustedHarvestTime > 0 ? (float.IsNaN(totalAdjustedHarvestTime / totalUnadjustedHarvestTime) ? 0f : totalAdjustedHarvestTime / totalUnadjustedHarvestTime) : 0f):F2}x)";
                            Main.DebugLog(logDetails);
                            HarvestState.logPending = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"Error in Patch_HarvestDuration: {ex}");
                    }
                }
            }


            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnIncreaseMeatHarvest))]
            internal class Patch_OnIncreaseMeatHarvest
            {
                static void Postfix(Il2Cpp.Panel_BodyHarvest __instance)
                {
                    if (__instance == null) return;

                    HarvestState.logPending = true;
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnDecreaseMeatHarvest))]
            internal class Patch_OnDecreaseMeatHarvest
            {
                static void Postfix(Il2Cpp.Panel_BodyHarvest __instance)
                {
                    if (__instance == null) return;

                    HarvestState.logPending = true;
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnIncreaseHideHarvest))]
            internal class Patch_OnIncreaseHideHarvest
            {
                static void Postfix(Il2Cpp.Panel_BodyHarvest __instance)
                {
                    if (__instance == null) return;

                    HarvestState.logPending = true;
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnDecreaseHideHarvest))]
            internal class Patch_OnDecreaseHideHarvest
            {
                static void Postfix(Il2Cpp.Panel_BodyHarvest __instance)
                {
                    if (__instance == null) return;

                    HarvestState.logPending = true;
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnIncreaseGutHarvest))]
            internal class Patch_OnIncreaseGutHarvest
            {
                static void Postfix(Il2Cpp.Panel_BodyHarvest __instance)
                {
                    if (__instance == null) return;

                    HarvestState.logPending = true;
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnDecreaseGutHarvest))]
            internal class Patch_OnDecreaseGutHarvest
            {
                static void Postfix(Il2Cpp.Panel_BodyHarvest __instance)
                {
                    if (__instance == null) return;

                    HarvestState.logPending = true;
                }
            }


            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnToolNext))]
            internal class Patch_OnToolNext
            {
                static void Postfix()
                {
                    HarvestState.toolChanged = true;  // Mark tool switch detected
                    HarvestState.logPending = true;
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnToolPrev))]
            internal class Patch_OnToolPrev
            {
                static void Postfix()
                {
                    HarvestState.toolChanged = true;  // Mark tool switch detected
                    HarvestState.logPending = true;
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.Enable), new Type[] { typeof(bool), typeof(Il2Cpp.BodyHarvest), typeof(bool), typeof(Il2Cpp.ComingFromScreenCategory) })]
            internal class Patch_MaxHarvestTime
            {
                static void Prefix(Il2Cpp.Panel_BodyHarvest __instance, bool enable)
                {
                    // ON OPEN PANEL
                    if (!enable || __instance == null) return;// Exit if panel is closing or if null
                    try
                    {
                        HarvestState.panelOpened = true;  // Mark log pending for initial values
                        // Override the max harvest time if the global setting is not the default value
                        if (__instance.m_MaxTimeHours != Settings.instance.MaxHarvestTimeSliderGlobal)
                        {
                            __instance.m_MaxTimeHours = Settings.instance.MaxHarvestTimeSliderGlobal;
                            Main.DebugLog($"Updated m_MaxTimeHours to {Settings.instance.MaxHarvestTimeSliderGlobal}.");
                        }
                    }
                    catch (Exception ex) { MelonLogger.Error($"Error on Patch_MaxHarvestTime: {ex}"); }
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.Enable),
                new Type[] { typeof(bool), typeof(Il2Cpp.BodyHarvest), typeof(bool), typeof(Il2Cpp.ComingFromScreenCategory) })]
            internal class Patch_ClearHarvestSettings
            {
                static void Prefix(Il2Cpp.Panel_BodyHarvest __instance, bool enable)
                {
                    // ON CLOSE PANEL
                    if (enable || __instance == null) return; // Exit if the panel is opening or if null

                    try
                    {
                        // Clear custom state and modified harvest times
                        Main.DebugLog("Patch_ClearHarvestSettings: Panel_BodyHarvest closed, clearing state.");

                        __instance.m_HarvestTimeMinutes = 0f;

                        // Reset static variables in Patch_HarvestDuration
                        Patch_HarvestDuration.ResetHarvestVariables();
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"Error in ClearHarvestSettings: {ex}");
                    }
                }
            }


        } // End of Panel_BodyHarvest_TimePatches



        internal class Panel_BodyHarvest_Display_Patches
            {

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.Enable), new Type[] { typeof(bool), typeof(Il2Cpp.BodyHarvest), typeof(bool), typeof(Il2Cpp.ComingFromScreenCategory) })]
            internal class Patch_ClearConditionAndFrozenLabels
            {
                static void Prefix(Il2Cpp.Panel_BodyHarvest __instance, bool enable)
                {
                    // ON CLOSE PANEL
                    if (enable || __instance == null) return; // Exit if the panel is opening or if null

                    try 
                    { 
                        // Clean up custom UI elements
                        var frozenLabelParent = __instance.m_Label_FrozenInfo?.transform.parent;
                        if (frozenLabelParent != null)
                        {

                            if (Settings.instance.ShowPanelCondition) 
                            {
                                var conditionLabel = frozenLabelParent.Find("ConditionLabel");
                                if (conditionLabel != null)
                                {
                                    UnityEngine.Object.Destroy(conditionLabel.gameObject);
                                    Main.DebugLog("[Close] Patch_ClearConditionAndFrozenLabels: ConditionLabel Cleared");
                                }
                            }

                            if (Settings.instance.AlwaysShowPanelFrozenPercent)
                            {
                                // Destroy custom frozen label if it exists
                                var customFrozenLabel = frozenLabelParent.Find("CustomFrozenLabel");
                                if (customFrozenLabel != null)
                                {
                                    UnityEngine.Object.Destroy(customFrozenLabel.gameObject);
                                    Main.DebugLog("[Close] Patch_ClearConditionAndFrozenLabels: CustomFrozenLabel Cleared");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                      MelonLogger.Error($"Error in ClearConditionAndFrozenLabels: {ex}");
                    }
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.RefreshTitle))]
            public class PanelBodyHarvest_ConditionLabel_Patch
            {

                private static UnityEngine.Color GetConditionColor(int condition)
                {
                    return condition >= 66 ? Green : (condition >= 33 ? Yellow : Red);
                }

                static void Postfix(Il2Cpp.Panel_BodyHarvest __instance)
                {
                    if (!Settings.instance.ShowPanelCondition || __instance == null) return; // Exit if setting is disabled or if null

                    try
                    {
                        var bodyHarvest = __instance.m_BodyHarvest;
                        if (bodyHarvest == null) return;

                        var titleLabel = __instance.m_Label_Title;
                        if (titleLabel == null) return;

                        var parentTransform = titleLabel.transform.parent;
                        var conditionLabel = parentTransform.Find("ConditionLabel")?.GetComponent<UILabel>();

                        if (conditionLabel == null)
                        {
                            // Create the new condition label
                            var newLabelObject = UnityEngine.Object.Instantiate(titleLabel.gameObject, parentTransform);
                            newLabelObject.name = "ConditionLabel";
                            conditionLabel = newLabelObject.GetComponent<UILabel>();
                            conditionLabel.fontSize = 14;
                            conditionLabel.transform.localPosition = titleLabel.transform.localPosition + new UnityEngine.Vector3(0, -25, 0);
                        }

                        // Update condition label only if needed
                        int carcassCondition = Mathf.RoundToInt(bodyHarvest.m_Condition);
                        string newText = $"({carcassCondition}% CONDITION)";
                        if (conditionLabel.text != newText)
                        {
                            conditionLabel.text = newText;
                            if (Settings.instance.ShowPanelConditionColors) { conditionLabel.color = GetConditionColor(carcassCondition); }
                        }

                        if (!conditionLabel.gameObject.activeSelf)
                        {
                            conditionLabel.gameObject.SetActive(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"Error in PanelBodyHarvest_ConditionLabel_Patch: {ex}");
                    }
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.RefreshTitle))]
            public class PanelBodyHarvest_FrozenLabel_Patch
            {

                private static UnityEngine.Color GetFrozenColor(int frozen)
                {
                    return frozen >= 75 ? Blue : (frozen >= 50 ? Cyan : (frozen >= 25 ? White : Orange));
                }

                static void Postfix(Il2Cpp.Panel_BodyHarvest __instance)
                {
                    if (!Settings.instance.AlwaysShowPanelFrozenPercent || __instance == null) return; // Exit if setting is disabled or if null

                    try
                    {
                        var frozenInfo = __instance.m_Label_FrozenInfo?.gameObject;

                        // Hide the default frozen label only if it's active
                        if (frozenInfo != null && frozenInfo.activeSelf)
                        {
                            frozenInfo.SetActive(false);
                        }

                        var bodyHarvest = __instance.m_BodyHarvest;
                        if (bodyHarvest == null) return;

                        var titleLabel = __instance.m_Label_Title;
                        if (titleLabel == null) return;

                        var parentTransform = titleLabel.transform.parent;
                        var customFrozenLabel = parentTransform.Find("CustomFrozenLabel")?.GetComponent<UILabel>();

                        if (customFrozenLabel == null)
                        {
                            // Create the new custom frozen label
                            var newLabelObject = UnityEngine.Object.Instantiate(titleLabel.gameObject, parentTransform);
                            newLabelObject.name = "CustomFrozenLabel";
                            customFrozenLabel = newLabelObject.GetComponent<UILabel>();
                            customFrozenLabel.fontSize = 14;
                            customFrozenLabel.transform.localPosition = titleLabel.transform.localPosition + new UnityEngine.Vector3(0, -45, 0);
                        }

                        // Update the custom frozen label's current frozen percentage and color
                        int percentFrozen = Mathf.RoundToInt(bodyHarvest.m_PercentFrozen);
                        if (customFrozenLabel.text != $"({percentFrozen}% FROZEN)")
                        {
                            customFrozenLabel.text = $"({percentFrozen}% FROZEN)";
                            if (Settings.instance.ShowPanelFrozenColors) { customFrozenLabel.color = GetFrozenColor(percentFrozen); }
                        }

                        if (!customFrozenLabel.gameObject.activeSelf)
                        {
                            customFrozenLabel.gameObject.SetActive(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"Error in PanelBodyHarvest_FrozenLabel_Patch: {ex}");

                    }
                }
            }
                private static string FormatTimeLog(float original, float adjusted, float multiplier)
                {
                    return $"{original:F1}m -> {adjusted:F1}m ({multiplier:F1}x)";
                }

                private static readonly UnityEngine.Color Green = new UnityEngine.Color(0, 0.808f, 0.518f, 1);
                private static readonly UnityEngine.Color Yellow = new UnityEngine.Color(0.827f, 0.729f, 0, 1);
                private static readonly UnityEngine.Color Orange = new UnityEngine.Color(0.827f, 0.471f, 0, 1);
                private static readonly UnityEngine.Color Red = new UnityEngine.Color(0.639f, 0.204f, 0.231f, 1);
                private static readonly UnityEngine.Color White = new UnityEngine.Color(1, 1, 1, 1);
                private static readonly UnityEngine.Color Cyan = new UnityEngine.Color(0.447f, 0.765f, 0.765f, 1);
                private static readonly UnityEngine.Color Blue = new UnityEngine.Color(0, 0.251f, 0.502f, 1);

            } // End of Panel_BodyHarvest_TimePatches



            internal static class BodyHarvest_Patches
        {

            //Quantity and Quarter time Patching
            [HarmonyPatch(typeof(Il2Cpp.BodyHarvest), nameof(BodyHarvest.InitializeResourcesAndConditions)), HarmonyPriority(Priority.Low)]
            internal class Patch_HarvestQuantities
            {
                private static void Prefix(Il2Cpp.BodyHarvest __instance)
                {
                    if (__instance == null || string.IsNullOrEmpty(__instance.name)) return;
                    try
                    {
                        //Main.DebugLog($"{__instance.name} Original fat threeItemRatio: " + __instance.m_FatToMeatRatio);
                        if (__instance.name.StartsWith("WILDLIFE_Rabbit"))
                        {
                            __instance.m_MeatAvailableMin = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMinRabbit, 1));
                            __instance.m_MeatAvailableMax = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMaxRabbit, 1));
                            __instance.m_HideAvailableUnits = Settings.instance.HideCountSliderRabbit;
                            __instance.m_GutAvailableUnits = Settings.instance.GutCountSliderRabbit;
                        }

                        if (__instance.name.StartsWith("WILDLIFE_Ptarmigan"))
                        {
                            __instance.m_MeatAvailableMin = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMinPtarmigan, 1));
                            __instance.m_MeatAvailableMax = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMaxPtarmigan, 1));
                            __instance.m_HideAvailableUnits = Settings.instance.HideCountSliderPtarmigan;
                        }

                        if (__instance.name.StartsWith("WILDLIFE_Doe"))
                        {
                            __instance.m_MeatAvailableMin = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMinDoe, 1));
                            __instance.m_MeatAvailableMax = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMaxDoe, 1));
                            __instance.m_HideAvailableUnits = Settings.instance.HideCountSliderDoe;
                            __instance.m_GutAvailableUnits = Settings.instance.GutCountSliderDoe;
                            __instance.m_QuarterBagMeatCapacity = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.QuarterSizeSliderDoe,1));
                            __instance.m_QuarterDurationMinutes = (float)Settings.instance.QuarterDurationMinutesSliderDoe;
                            __instance.m_FatToMeatRatio = Settings.instance.FatToMeatPercentSliderDoe / 100f;

                        }

                        if (__instance.name.StartsWith("WILDLIFE_Stag"))
                        {
                            __instance.m_MeatAvailableMax = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMaxStag, 1));
                            __instance.m_MeatAvailableMin = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMinStag, 1));
                            __instance.m_HideAvailableUnits = Settings.instance.HideCountSliderStag;
                            __instance.m_GutAvailableUnits = Settings.instance.GutCountSliderStag;
                            __instance.m_QuarterBagMeatCapacity = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.QuarterSizeSliderStag, 1));
                            __instance.m_QuarterDurationMinutes = (float)Settings.instance.QuarterDurationMinutesSliderStag;
                            __instance.m_FatToMeatRatio = Settings.instance.FatToMeatPercentSliderStag / 100f;

                        }

                        if (__instance.name.StartsWith("WILDLIFE_Moose"))
                        {
                            __instance.m_MeatAvailableMax = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMaxMoose, 1));
                            __instance.m_MeatAvailableMin = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMinMoose, 1));
                            __instance.m_HideAvailableUnits = Settings.instance.HideCountSliderMoose;
                            __instance.m_GutAvailableUnits = Settings.instance.GutCountSliderMoose;
                            __instance.m_QuarterBagMeatCapacity = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.QuarterSizeSliderMoose, 1));
                            __instance.m_QuarterDurationMinutes = (float)Settings.instance.QuarterDurationMinutesSliderMoose;
                            __instance.m_FatToMeatRatio = Settings.instance.FatToMeatPercentSliderMoose / 100f;
                        }

                        // Extra logic for wolves to handle the different types
                        if (__instance.name.StartsWith("WILDLIFE_Wolf_Starving"))
                        {
                            __instance.m_HideAvailableUnits = Settings.instance.HideCountSliderPoisonedWolf;
                            __instance.m_GutAvailableUnits = Settings.instance.GutCountSliderPoisonedWolf;
                        }
                        else if (__instance.name.StartsWith("WILDLIFE_Wolf_grey"))
                        {
                            __instance.m_MeatAvailableMax = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMaxTimberWolf, 1));
                            __instance.m_MeatAvailableMin = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMinTimberWolf, 1));
                            __instance.m_HideAvailableUnits = Settings.instance.HideCountSliderTimberWolf;
                            __instance.m_GutAvailableUnits = Settings.instance.GutCountSliderTimberWolf;
                            __instance.m_QuarterBagMeatCapacity = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.QuarterSizeSliderTimberWolf, 1));
                            __instance.m_QuarterDurationMinutes = (float)Settings.instance.QuarterDurationMinutesSliderTimberWolf;
                            __instance.m_FatToMeatRatio = Settings.instance.FatToMeatPercentSliderTimberWolf / 100f;
                        }
                        else if (__instance.name.StartsWith("WILDLIFE_Wolf"))
                        {
                            __instance.m_MeatAvailableMax = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMaxWolf, 1));
                            __instance.m_MeatAvailableMin = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMinWolf, 1));
                            __instance.m_HideAvailableUnits = Settings.instance.HideCountSliderWolf;
                            __instance.m_GutAvailableUnits = Settings.instance.GutCountSliderWolf;
                            __instance.m_QuarterBagMeatCapacity = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.QuarterSizeSliderWolf, 1));
                            __instance.m_QuarterDurationMinutes = (float)Settings.instance.QuarterDurationMinutesSliderWolf;
                            __instance.m_FatToMeatRatio = Settings.instance.FatToMeatPercentSliderWolf / 100f;
                        }

                        if (__instance.name.StartsWith("WILDLIFE_Bear"))
                        {
                            __instance.m_MeatAvailableMax = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMaxBear, 1));
                            __instance.m_MeatAvailableMin = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMinBear, 1));
                            __instance.m_HideAvailableUnits = Settings.instance.HideCountSliderBear;
                            __instance.m_GutAvailableUnits = Settings.instance.GutCountSliderBear;
                            __instance.m_QuarterBagMeatCapacity = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.QuarterSizeSliderBear, 1));
                            __instance.m_QuarterDurationMinutes = (float)Settings.instance.QuarterDurationMinutesSliderBear;
                            __instance.m_FatToMeatRatio = Settings.instance.FatToMeatPercentSliderBear / 100f;
                        }

                        if (__instance.name.StartsWith("WILDLIFE_Cougar"))
                        {
                            __instance.m_MeatAvailableMax = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMaxCougar, 1));
                            __instance.m_MeatAvailableMin = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.MeatSliderMinCougar, 1));
                            __instance.m_HideAvailableUnits = Settings.instance.HideCountSliderCougar;
                            __instance.m_GutAvailableUnits = Settings.instance.GutCountSliderCougar;
                            __instance.m_QuarterBagMeatCapacity = ItemWeight.FromKilograms((float)Math.Round(Settings.instance.QuarterSizeSliderCougar, 1));
                            __instance.m_QuarterDurationMinutes = (float)Settings.instance.QuarterDurationMinutesSliderCougar;
                            __instance.m_FatToMeatRatio = Settings.instance.FatToMeatPercentSliderCougar / 100f;
                        }

                        //Main.DebugLog($"{__instance.name} New fat threeItemRatio: " + __instance.m_FatToMeatRatio);

                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"Error in Patch_HarvestQuantities: {ex}");
                    }
                }
            }


            // Decay Disable Patching
            [HarmonyPatch(typeof(Il2Cpp.BodyHarvest), nameof(BodyHarvest.Update))]
            internal class Patch_DisableCarcassDecay
            {
                //Default decay rate for every animal and carcass is 5. There must be something else on the backend that converts this into real game time.
                internal static float defaultDecay = 5f;
                private static void Prefix(Il2Cpp.BodyHarvest __instance)
                {
                    if (__instance == null || string.IsNullOrEmpty(__instance.name) || !Settings.instance.DisableCarcassDecayGlobal ) return;
                    try {__instance.m_AllowDecay = false;} catch (Exception ex) {MelonLogger.Error($"Error in Patch_DisableCarcassDecay: {ex}");}
                }
            }





        } // End of BodyHarvest_Patches

    } // End of Patches

} // End of namespace


//// Decay rate patching - ONLY WORKS DURING REALTIME GAMEPLAY, NOT DURING ACCELERATED TIME
//[HarmonyPatch(typeof(Il2Cpp.BodyHarvest), nameof(BodyHarvest.InitializeResourcesAndConditions))]
//internal class Patch_CarcassDecay
//{
//    //Default decay rate for every animal and carcass is 5. There must be something else on the backend that converts this into real game time.
//    internal static float defaultDecay = 5f;
//    private static void Prefix(Il2Cpp.BodyHarvest __instance)
//    {
//        if (__instance == null || string.IsNullOrEmpty(__instance.name)) return;
//        try
//        {
//            //ONLY WORKS DURING REALTIME GAMEPLAY, NOT DURING ACCELERATED TIME
//            //else
//            //{
//            //    Main.DebugLog($"{__instance.name} Orig Decay: " + __instance.m_DecayConditionPerHour);
//            //    if (__instance.name.StartsWith("WILDLIFE_Rabbit")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderRabbit, 2) * defaultDecay; }
//            //    if (__instance.name.StartsWith("WILDLIFE_Ptarmigan")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderPtarmigan, 2) * defaultDecay; }
//            //    if (__instance.name.StartsWith("WILDLIFE_Doe")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderDoe, 2) * defaultDecay; }
//            //    if (__instance.name.StartsWith("WILDLIFE_Stag")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderStag, 2) * defaultDecay; }
//            //    if (__instance.name.StartsWith("WILDLIFE_Moose")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderMoose, 2) * defaultDecay; }
//            //    if (__instance.name.StartsWith("WILDLIFE_Wolf_Starving")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderPoisonedWolf, 2) * defaultDecay; }
//            //    else if (__instance.name.StartsWith("WILDLIFE_Wolf_grey")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderTimberWolf, 2) * defaultDecay; }
//            //    else if (__instance.name.StartsWith("WILDLIFE_Wolf")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderWolf, 2) * defaultDecay; }
//            //    if (__instance.name.StartsWith("WILDLIFE_Bear")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderBear, 2) * defaultDecay; }
//            //    if (__instance.name.StartsWith("WILDLIFE_Cougar")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderCougar, 2) * defaultDecay; }

//            //    if (Settings.instance.AdjustExistingCarcasses)
//            //    {
//            //        if (__instance.name.StartsWith("CORPSE_Deer")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderStag, 2) * defaultDecay; }
//            //        if (__instance.name.StartsWith("CORPSE_Moose")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderMoose, 2) * defaultDecay; }
//            //        if (__instance.name.StartsWith("CORPSE_Wolf")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderWolf, 2) * defaultDecay; }
//            //        // Doesn't seem to be a CORPSE_Wolf_grey so we'll just use the WILDLIFE_Wolf_grey which seems to also be the corpse object
//            //        // Doesn't seem to be a CORPSE_Wolf_Starving so we'll just use the WILDLIFE_Wolf_Starving which seems to also be the corpse object
//            //        if (__instance.name.StartsWith("GEAR_PtarmiganCarcass")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderPtarmigan, 2) * defaultDecay; }
//            //        if (__instance.name.StartsWith("CORPSE_Doe")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderDoe, 2) * defaultDecay; }
//            //        if (__instance.name.StartsWith("CORPSE_Bear")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderBear, 2) * defaultDecay; }
//            //        if (__instance.name.StartsWith("CORPSE_Cougar")) { __instance.m_DecayConditionPerHour = (float)Math.Round(Settings.instance.DecayRateMultiplierSliderCougar, 2) * defaultDecay; }
//            //    }
//            //    Main.DebugLog($"{__instance.name}  New Decay: " + __instance.m_DecayConditionPerHour);
//            //}
//        }
//        catch (Exception ex)
//        {
//            MelonLogger.Error($"Error in Patch_CarcassDecay: {ex}");
//        }
//    } // End of Prefix
//} // End of Patch_CarcassDecay
