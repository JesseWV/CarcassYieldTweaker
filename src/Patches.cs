using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.IntBackedUnit;
using JetBrains.Annotations;
using MelonLoader;
using System;
using UnityEngine;


namespace CarcassYieldTweaker
{
    internal static class Patches
    {
        internal static class HarvestState
        {
            internal static bool pendingChange = false;
            internal static string lastItemChanged = null;
            internal static float savedHarvestTime = 0f; // Holds the last known harvest time before switching tabs
            internal static float lastUnmodifiedTime = 0f; // Holds the last unmodified harvest time to use for tool changes

            internal static void ClearAll()
            {
                Main.DebugLog("Clearing HarvestState.");
                pendingChange = false;
                lastItemChanged = null;
                savedHarvestTime = 0f;
                lastUnmodifiedTime = 0f;
            }

        }
        private static string FormatTimeLog(float original, float adjusted, float multiplier)
        {
            return $"{original:F1}m -> {adjusted:F1}m ({multiplier:F1}x)";
        }
        internal static class Panel_BodyHarvest_Patches
        {



            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.GetHarvestDurationMinutes))]
            internal class Patch_HarvestDuration
            {
                static void Postfix(Il2Cpp.Panel_BodyHarvest __instance, ref float __result)
                {
                    if (__instance == null || string.IsNullOrEmpty(__instance.name)) return;

                    try
                    {
                        // First store unmodified total harvest time for future ratio calculations
                        float newUnmodifiedTime = __result;

                        // Get the animal type
                        string animalType = __instance.m_BodyHarvest.name;

                        // Check if the last change was a tool switch
                        if (HarvestState.pendingChange && HarvestState.lastItemChanged == "ToolSwitch")
                        {
                            if (HarvestState.lastUnmodifiedTime > 0) // Check if we have a previous unmodified time to compare against
                            {
                                float ratio = newUnmodifiedTime / HarvestState.lastUnmodifiedTime;
                                float toolAdjustedTotalHarvestTime = __instance.m_HarvestTimeMinutes * ratio;

                                __result = toolAdjustedTotalHarvestTime;
                                __instance.m_HarvestTimeMinutes = toolAdjustedTotalHarvestTime; // Update the instance variable

                                Main.DebugLog($"[Tool Switch] Ratio: {ratio:F2} - New time: {toolAdjustedTotalHarvestTime:F2}");
                            }

                            HarvestState.lastUnmodifiedTime = newUnmodifiedTime; // Update for next comparison
                        }
                        else if (HarvestState.pendingChange && !string.IsNullOrEmpty(HarvestState.lastItemChanged))
                        {
                            // If the change was an item change (Meat, Gut, Hide), use the item-based multiplier
                            float multiplier = GetRoundedMultiplier(HarvestState.lastItemChanged, animalType); // Pass item type and animal type
                            float itemAdjustedItemHarvestTime = __result * multiplier;

                            __instance.m_HarvestTimeMinutes = itemAdjustedItemHarvestTime;

                            Main.DebugLog($"[{animalType}:{HarvestState.lastItemChanged}] {__result:F2} -> {itemAdjustedItemHarvestTime:F2} (x{multiplier:F1})");

                            __result = itemAdjustedItemHarvestTime;

                            HarvestState.lastUnmodifiedTime = newUnmodifiedTime; // Update for next comparison
                        }
                        else if (__instance.m_HarvestTimeMinutes > 0f)
                        {
                            __result = __instance.m_HarvestTimeMinutes;
                        }

                        // Reset pending change and last item changed
                        HarvestState.pendingChange = false;
                        HarvestState.lastItemChanged = null;
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"Error in Patch_HarvestDuration: {ex}");
                    }
                }
            }

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
                        Main.DebugLog($"[UNKNOWN:{animalType}] GLOBAL multiplier: {rawMultiplier:F1}");
                        break;
                }

                return (float)Math.Round(rawMultiplier, 2);
            }

            // Button Press Panel_BodyHarvest_Patches: Set pendingChange and record which button
            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnIncreaseMeatHarvest))]
            internal class Patch_OnIncreaseMeatHarvest
            {
                static void Postfix()
                {
                    HarvestState.lastItemChanged = "Meat";
                    HarvestState.pendingChange = true;
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnDecreaseMeatHarvest))]
            internal class Patch_OnDecreaseMeatHarvest
            {
                static void Postfix()
                {
                    HarvestState.lastItemChanged = "Meat";
                    HarvestState.pendingChange = true;
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnIncreaseHideHarvest))]
            internal class Patch_OnIncreaseHideHarvest
            {
                static void Postfix()
                {
                    HarvestState.lastItemChanged = "Hide";
                    HarvestState.pendingChange = true;
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnDecreaseHideHarvest))]
            internal class Patch_OnDecreaseHideHarvest
            {
                static void Postfix()
                {
                    HarvestState.lastItemChanged = "Hide";
                    HarvestState.pendingChange = true;
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnIncreaseGutHarvest))]
            internal class Patch_OnIncreaseGutHarvest
            {
                static void Postfix()
                {
                    HarvestState.lastItemChanged = "Gut";
                    HarvestState.pendingChange = true;
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnDecreaseGutHarvest))]
            internal class Patch_OnDecreaseGutHarvest
            {
                static void Postfix()
                {
                    HarvestState.lastItemChanged = "Gut";
                    HarvestState.pendingChange = true;
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnTabQuarterSelected))]
            internal class Patch_OnTabQuarterSelected
            {
                static void Postfix(Il2Cpp.Panel_BodyHarvest __instance)
                {
                    if (__instance == null) return;
                    HarvestState.savedHarvestTime = __instance.m_HarvestTimeMinutes;
                    Main.DebugLog($"OnTabQuarterSelected called. Saved harvest time: {HarvestState.savedHarvestTime:F2}");
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnTabHarvestSelected))]
            internal class Patch_OnTabHarvestSelected
            {
                static void Postfix(Il2Cpp.Panel_BodyHarvest __instance)
                {
                    if (__instance == null) return;
                    __instance.m_HarvestTimeMinutes = HarvestState.savedHarvestTime;
                    Main.DebugLog($"OnTabHarvestSelected called. Restored harvest time: {HarvestState.savedHarvestTime:F2}");
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnToolNext))]
            internal class Patch_OnToolNext
            {
                static void Postfix()
                {
                    HarvestState.pendingChange = true;
                    HarvestState.lastItemChanged = "ToolSwitch";
                }
            }

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnToolPrev))]
            internal class Patch_OnToolPrev
            {
                static void Postfix()
                {
                    HarvestState.pendingChange = true;
                    HarvestState.lastItemChanged = "ToolSwitch";
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

            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.Enable), new Type[] { typeof(bool), typeof(Il2Cpp.BodyHarvest), typeof(bool), typeof(Il2Cpp.ComingFromScreenCategory) })]
            internal class Patch_ClearHarvestSettings
            {
                static void Prefix(Il2Cpp.Panel_BodyHarvest __instance, bool enable)
                {
                    // ON CLOSE PANEL
                    if (enable || __instance == null) return; // Exit if the panel is opening or if null

                    try
                    {
                        // Clear custom state and modified harvest times
                        Main.DebugLog("Panel_BodyHarvest closed. Clearing Settings.");
                        HarvestState.ClearAll();
                        __instance.m_HarvestTimeMinutes = 0f;
                    }
                    catch (Exception ex)
                    { 
                        MelonLogger.Error($"Error in ClearHarvestSettings: {ex}");
                    }
                }
            }

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
                                    Main.DebugLog("Panel_BodyHarvest Closed. Clearing ConditionLabel.");
                                }
                            }

                            if (Settings.instance.AlwaysShowPanelFrozenPercent)
                            {
                                // Destroy custom frozen label if it exists
                                var customFrozenLabel = frozenLabelParent.Find("CustomFrozenLabel");
                                if (customFrozenLabel != null)
                                {
                                    UnityEngine.Object.Destroy(customFrozenLabel.gameObject);
                                    Main.DebugLog("Panel_BodyHarvest Closed. Clearing CustomFrozenLabel.");
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


            private static readonly UnityEngine.Color Green = new UnityEngine.Color(0, 0.808f, 0.518f, 1);
            private static readonly UnityEngine.Color Yellow = new UnityEngine.Color(0.827f, 0.729f, 0, 1);
            private static readonly UnityEngine.Color Orange = new UnityEngine.Color(0.827f, 0.471f, 0, 1);
            private static readonly UnityEngine.Color Red = new UnityEngine.Color(0.639f, 0.204f, 0.231f, 1);
            private static readonly UnityEngine.Color White = new UnityEngine.Color(1, 1, 1, 1);
            private static readonly UnityEngine.Color Cyan = new UnityEngine.Color(0.447f, 0.765f, 0.765f, 1);
            private static readonly UnityEngine.Color Blue = new UnityEngine.Color(0, 0.251f, 0.502f, 1);



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
        } // End of Panel_BodyHarvest_Patches



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


        } // End of BodyHarvest_Patches

    } // End of Patches

} // End of namespace