using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.IntBackedUnit;
using MelonLoader;
using System;
using UnityEngine;
using static CarcassYieldTweaker.Patches.Panel_BodyHarvest_Time_Patches;


namespace CarcassYieldTweaker
{
    internal static class Patches
    {
        internal static class HarvestState
        {
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

                    case "WILDLIFE_TimberWolf":
                        if (itemType == "Hide")
                            rawMultiplier = Settings.instance.HideTimeSliderTimberWolf;
                        else if (itemType == "Meat")
                            rawMultiplier = Settings.instance.MeatTimeSliderGlobal;
                        else if (itemType == "Gut")
                            rawMultiplier = Settings.instance.GutTimeSliderGlobal;
                        break;

                    case "WILDLIFE_StarvingWolf":
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


            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.GetHarvestDurationMinutes))]
            internal class Patch_HarvestDuration
            {
                private static float previousUnmodifiedTotalTime = -1f;
                private static bool baseMeatTimeDiscovered = false;
                private static bool baseHideTimeDiscovered = false;
                private static bool baseGutTimeDiscovered = false;
                private static float baseMeatTimePerKg = 0f;
                private static float baseHideTimePerUnit = 0f;
                private static float baseGutTimePerUnit = 0f;

                private static float lastMeatAmount = -1f;
                private static int lastHideUnits = -1;
                private static int lastGutUnits = -1;

                public static void ResetHarvestVariables()
                {
                    previousUnmodifiedTotalTime = -1f;
                    baseMeatTimeDiscovered = false;
                    baseHideTimeDiscovered = false;
                    baseGutTimeDiscovered = false;
                    baseMeatTimePerKg = 0f;
                    baseHideTimePerUnit = 0f;
                    baseGutTimePerUnit = 0f;

                    lastMeatAmount = -1f;
                    lastHideUnits = -1;
                    lastGutUnits = -1;

                    Main.DebugLog("Patch_HarvestDuration: Static variables reset.");
                }

                static void Postfix(Il2Cpp.Panel_BodyHarvest __instance, ref float __result)
                {
                    if (__instance == null || string.IsNullOrEmpty(__instance.name)) return;

                    try
                    {
                        string animalType = __instance.m_BodyHarvest?.name ?? string.Empty;
                        float currentUnmodifiedTotalTime = __result;

                        if (HarvestState.logPending)
                        {
                            Main.DebugLog($"[DEBUG] Start Patch_HarvestDuration: {animalType}");
                            Main.DebugLog($"[DEBUG] Current Unmodified Total Time: {currentUnmodifiedTotalTime:F2}m");
                            Main.DebugLog($"[DEBUG] Previous Unmodified Total Time: {previousUnmodifiedTotalTime:F2}m");
                        }

                        // Preserve previous total before applying tool changes
                        float lastRecordedTotalTime = previousUnmodifiedTotalTime;
                        previousUnmodifiedTotalTime = currentUnmodifiedTotalTime;

                        // Get current amounts
                        float meatAmount = ConvertItemWeightToFloat(__instance.m_MenuItem_Meat.HarvestAmount);
                        int hideUnits = __instance.m_MenuItem_Hide.HarvestUnits;
                        int gutUnits = __instance.m_MenuItem_Gut.HarvestUnits;

                        bool anyAmountChanged = false;
                        string changedItem = null;

                        if (meatAmount != lastMeatAmount)
                        {
                            changedItem = "Meat";
                            anyAmountChanged = true;
                        }
                        else if (hideUnits != lastHideUnits)
                        {
                            changedItem = "Hide";
                            anyAmountChanged = true;
                        }
                        else if (gutUnits != lastGutUnits)
                        {
                            changedItem = "Gut";
                            anyAmountChanged = true;
                        }

                        if (HarvestState.logPending)
                        {
                            Main.DebugLog($"[DEBUG] Changed Item Detected: {changedItem}");
                            Main.DebugLog($"[DEBUG] Current Amounts: Meat={meatAmount:F2}kg, Hide={hideUnits}, Gut={gutUnits}");
                            Main.DebugLog($"[DEBUG] Last Known Amounts: Meat={lastMeatAmount:F2}kg, Hide={lastHideUnits}, Gut={lastGutUnits}");
                        }

                        // Update tracking variables
                        lastMeatAmount = meatAmount;
                        lastHideUnits = hideUnits;
                        lastGutUnits = gutUnits;

                        // Compute previous expected time contributions (before tool change)
                        float previousMeatTime = baseMeatTimeDiscovered ? baseMeatTimePerKg * meatAmount : 0f;
                        float previousHideTime = baseHideTimeDiscovered ? baseHideTimePerUnit * hideUnits : 0f;
                        float previousGutTime = baseGutTimeDiscovered ? baseGutTimePerUnit * gutUnits : 0f;

                        float previousTotalExpected = previousMeatTime + previousHideTime + previousGutTime;
                        float remainingTimeBeforeToolChange = Math.Max(0, lastRecordedTotalTime - previousTotalExpected);

                        if (HarvestState.logPending)
                        {
                            Main.DebugLog($"[DEBUG] Expected Time Contributions: Meat={previousMeatTime:F2}m, Hide={previousHideTime:F2}m, Gut={previousGutTime:F2}m");
                            Main.DebugLog($"[DEBUG] Remaining Time Before Tool Change: {remainingTimeBeforeToolChange:F2}m");
                        }

                        // **Handle Tool Switch**
                        if (HarvestState.toolChanged)
                        {
                            if (HarvestState.logPending)
                                Main.DebugLog($"[DEBUG] Tool change detected.");

                            if (anyAmountChanged)
                            {
                                if (HarvestState.logPending)
                                    Main.DebugLog($"[DEBUG] Amount changed after tool switch. Attempting base time rediscovery.");

                                // **If an amount changed, discover the actual base time for that item**
                                if (changedItem == "Meat" && meatAmount > 0)
                                {
                                    baseMeatTimePerKg = (currentUnmodifiedTotalTime - remainingTimeBeforeToolChange) / meatAmount;
                                    baseMeatTimeDiscovered = true;
                                    Main.DebugLog($"[NEW BaseTime Discovered: Meat] Amount: {meatAmount:F2} kg, Base Time per kg: {baseMeatTimePerKg:F2} minutes.");
                                }
                                else if (changedItem == "Hide" && hideUnits > 0)
                                {
                                    baseHideTimePerUnit = (currentUnmodifiedTotalTime - remainingTimeBeforeToolChange) / hideUnits;
                                    baseHideTimeDiscovered = true;
                                    Main.DebugLog($"[NEW BaseTime Discovered: Hide] Units: {hideUnits}, Base Time per unit: {baseHideTimePerUnit:F2} minutes.");
                                }
                                else if (changedItem == "Gut" && gutUnits > 0)
                                {
                                    baseGutTimePerUnit = (currentUnmodifiedTotalTime - remainingTimeBeforeToolChange) / gutUnits;
                                    baseGutTimeDiscovered = true;
                                    Main.DebugLog($"[NEW BaseTime Discovered: Gut] Units: {gutUnits}, Base Time per unit: {baseGutTimePerUnit:F2} minutes.");
                                }
                            }
                            else
                            {
                                // **If no amount changed, apply a global ratio adjustment**
                                float toolRatio = currentUnmodifiedTotalTime / lastRecordedTotalTime;
                                baseMeatTimePerKg *= toolRatio;
                                baseHideTimePerUnit *= toolRatio;
                                baseGutTimePerUnit *= toolRatio;

                                Main.DebugLog($"[Tool Switch] {animalType}: Ratio {toolRatio:F3}");
                            }

                            HarvestState.toolChanged = false;
                        }

                        // **Apply Multipliers and Compute Adjusted Time**
                        float meatMultiplier = GetRoundedMultiplier("Meat", animalType);
                        float hideMultiplier = GetRoundedMultiplier("Hide", animalType);
                        float gutMultiplier = GetRoundedMultiplier("Gut", animalType);

                        float originalMeatTime = baseMeatTimeDiscovered ? meatAmount * baseMeatTimePerKg : 0f;
                        float adjustedMeatTime = originalMeatTime * meatMultiplier;

                        float originalHideTime = baseHideTimeDiscovered ? hideUnits * baseHideTimePerUnit : 0f;
                        float adjustedHideTime = originalHideTime * hideMultiplier;

                        float originalGutTime = baseGutTimeDiscovered ? gutUnits * baseGutTimePerUnit : 0f;
                        float adjustedGutTime = originalGutTime * gutMultiplier;

                        // Compute total adjusted time
                        float totalAdjustedHarvestTime = adjustedMeatTime + adjustedHideTime + adjustedGutTime;

                        // ****** ASSIGN FINAL ADJUSTED HARVEST TIME ******
                        __result = totalAdjustedHarvestTime;

                        // Log if changes were detected
                        if (HarvestState.logPending)
                        {
                            string logDetails = $"{animalType}: ";
                            logDetails += $"{meatAmount:F1}kg MEAT {originalMeatTime:F1}m -> {adjustedMeatTime:F1}m, ";
                            logDetails += $"{hideUnits}x HIDE {originalHideTime:F1}m -> {adjustedHideTime:F1}m, ";
                            logDetails += $"{gutUnits}x GUT {originalGutTime:F1}m -> {adjustedGutTime:F1}m, ";
                            logDetails += $"- Total {currentUnmodifiedTotalTime:F1}m -> {totalAdjustedHarvestTime:F1}m";

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
                        Main.DebugLog("Panel_BodyHarvest opened.");
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
                                    Main.DebugLog("Patch_ClearConditionAndFrozenLabels: Panel_BodyHarvest Closed. Clearing ConditionLabel.");
                                }
                            }

                            if (Settings.instance.AlwaysShowPanelFrozenPercent)
                            {
                                // Destroy custom frozen label if it exists
                                var customFrozenLabel = frozenLabelParent.Find("CustomFrozenLabel");
                                if (customFrozenLabel != null)
                                {
                                    UnityEngine.Object.Destroy(customFrozenLabel.gameObject);
                                    Main.DebugLog("Patch_ClearConditionAndFrozenLabels: Panel_BodyHarvest Closed. Clearing CustomFrozenLabel.");
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
            [HarmonyPatch(typeof(Il2Cpp.BodyHarvest), nameof(BodyHarvest.InitializeResourcesAndConditions))]
            internal class Patch_HarvestQuantities
            {
                private static void Prefix(Il2Cpp.BodyHarvest __instance)
                {
                    if (__instance == null || string.IsNullOrEmpty(__instance.name)) return;
                    try
                    {
                        //Main.DebugLog($"{__instance.name} Original fat ratio: " + __instance.m_FatToMeatRatio);
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

                        //Main.DebugLog($"{__instance.name} New fat ratio: " + __instance.m_FatToMeatRatio);

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
