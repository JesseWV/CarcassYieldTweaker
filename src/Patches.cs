using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.IntBackedUnit;
using MelonLoader;
using System;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace CarcassYieldTweaker
{
    internal static class Patches
    {

        internal static class Panel_BodyHarvest_Time_Patches
        {


            // Helper method to retrieve the rounded multiplier based on the item type and animal type
            private static float GetRoundedMultiplier(string itemType, string animalType)
            {
                float rawMultiplier = 1f; // Default multiplier

                // Dictionary for animal-specific Hide multipliers
                var animalHideMultipliers = new Dictionary<string, float>
                {
                    { "GEAR_RabbitCarcass", Settings.instance.HideTimeSliderRabbit },
                    { "GEAR_PtarmiganCarcass", Settings.instance.HideTimeSliderPtarmigan },
                    { "WILDLIFE_Doe", Settings.instance.HideTimeSliderDoe },
                    { "WILDLIFE_Stag", Settings.instance.HideTimeSliderStag },
                    { "WILDLIFE_Moose", Settings.instance.HideTimeSliderMoose },
                    { "WILDLIFE_Wolf", Settings.instance.HideTimeSliderWolf },
                    { "WILDLIFE_Wolf_grey", Settings.instance.HideTimeSliderTimberWolf },
                    { "WILDLIFE_Wolf_Starving", Settings.instance.HideTimeSliderPoisonedWolf },
                    { "WILDLIFE_Bear", Settings.instance.HideTimeSliderBear },
                    { "WILDLIFE_Cougar", Settings.instance.HideTimeSliderCougar }
                };

                // Default to global multipliers for Meat, FrozenMeat, and Gut - can be expanded by adding additional dictionaries if needed
                var globalMultipliers = new Dictionary<string, float>
                {
                    { "Meat", Settings.instance.MeatTimeSliderGlobal },
                    { "FrozenMeat", Settings.instance.FrozenMeatTimeSliderGlobal },
                    { "Gut", Settings.instance.GutTimeSliderGlobal }
                };

                // Determine the multiplier based on item type
                if (itemType == "Hide")
                {
                    if (!animalHideMultipliers.TryGetValue(animalType, out rawMultiplier))
                    {
                        rawMultiplier = 1.0f; // Default for unknown animals
                        Main.DebugLog($"[UNKNOWN ANIMAL!: {animalType}] Using default Hide multiplier: {rawMultiplier:F2}");
                    }
                }
                else if (globalMultipliers.TryGetValue(itemType, out float globalMultiplier))
                {
                    rawMultiplier = globalMultiplier;
                }
                else
                {
                    Main.DebugLog($"[UNKNOWN ITEM TYPE!: {itemType}] Defaulting to 1.0x");
                }

                return (float)Math.Round(rawMultiplier, 2);
            }


            // Generic function to retrieve a field value using reflection
            private static T GetFieldValue<T>(Il2Cpp.BodyHarvestItem item, string methodName)
            {
                var getter = item.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return getter != null ? (T)getter.Invoke(item, null) : default;
            }

            // Generic function to modify a field value using reflection
            private static void ModifyFieldValue(Il2Cpp.BodyHarvestItem item, string methodName, float newValue)
            {
                var setter = item.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                setter?.Invoke(item, new object[] { newValue });
            }


            // Store original values to reset them later
            private static Dictionary<Il2Cpp.BodyHarvestItem, (float meat, float frozenMeat, float gut, float hide)> originalValues =
                new Dictionary<Il2Cpp.BodyHarvestItem, (float, float, float, float)>();


            // ********************** Patch to change the harvest times for each item type based on the animal type ****************************
            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Il2Cpp.Panel_BodyHarvest.Enable),
                new Type[] { typeof(bool), typeof(Il2Cpp.BodyHarvest), typeof(bool), typeof(Il2Cpp.ComingFromScreenCategory) })]
            internal class Patch_BodyHarvestItems
            {
                static void Postfix(Il2Cpp.Panel_BodyHarvest __instance, bool enable)
                {
                    if (!enable || __instance == null) return;
                    try
                    {
                        if (__instance.m_BodyHarvest == null)
                        {
                            MelonLoader.MelonLogger.Error("[HarvestItems] m_BodyHarvest is null, skipping patch.");
                            return;
                        }

                        string animalType = __instance.m_BodyHarvest?.name ?? string.Empty;
                        var items = UnityEngine.Resources.FindObjectsOfTypeAll<Il2Cpp.BodyHarvestItem>();

                        float meatMultiplier = GetRoundedMultiplier("Meat", animalType);
                        float frozenMeatMultiplier = GetRoundedMultiplier("FrozenMeat", animalType);
                        float gutMultiplier = GetRoundedMultiplier("Gut", animalType);
                        float hideMultiplier = GetRoundedMultiplier("Hide", animalType);

                        Main.DebugLog($"[HarvestItems] Animal: {animalType}, Tools: {items.Count} ");

                        foreach (var item in items)
                        {
                            // --- Store Original Values Before Modifying ---
                            if (!originalValues.ContainsKey(item))
                            {
                                originalValues[item] = (
                                    GetFieldValue<float>(item, "get_m_HarvestMeatMinutesPerKG"),
                                    GetFieldValue<float>(item, "get_m_HarvestFrozenMeatMinutesPerKG"),
                                    GetFieldValue<float>(item, "get_m_HarvestGutMinutesPerUnit"),
                                    GetFieldValue<float>(item, "get_m_HarvestHideMinutesPerUnit")
                                );
                            }

                            // --- Update Harvest Times ---
                            ModifyFieldValue(item, "set_m_HarvestMeatMinutesPerKG", originalValues[item].meat * meatMultiplier);
                            ModifyFieldValue(item, "set_m_HarvestFrozenMeatMinutesPerKG", originalValues[item].frozenMeat * frozenMeatMultiplier);
                            ModifyFieldValue(item, "set_m_HarvestGutMinutesPerUnit", originalValues[item].gut * gutMultiplier);
                            ModifyFieldValue(item, "set_m_HarvestHideMinutesPerUnit", originalValues[item].hide * hideMultiplier);

                            Main.DebugLog($"" +
                                $"Meat {originalValues[item].meat:F1} -> {originalValues[item].meat * meatMultiplier:F1}m/kg ({meatMultiplier:F2}x), " +
                                $"FrozenMeat {originalValues[item].frozenMeat:F1} -> {originalValues[item].frozenMeat * frozenMeatMultiplier:F1}m/kg ({frozenMeatMultiplier:F2}x), " +
                                $"Gut {originalValues[item].gut:F1} -> {originalValues[item].gut * gutMultiplier:F1}m/unit ({gutMultiplier:F2}x), " +
                                $"Hide {originalValues[item].hide:F1} -> {originalValues[item].hide * hideMultiplier:F1}m/unit ({hideMultiplier:F2}x)" +
                                $" - {item.name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLoader.MelonLogger.Error($"Error in Patch_BodyHarvestItems: {ex}");
                    }
                }
            } // End of Patch_BodyHarvestItems



            // Reset the modified values back to their original values
            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Il2Cpp.Panel_BodyHarvest.Enable),
                new Type[] { typeof(bool), typeof(Il2Cpp.BodyHarvest), typeof(bool), typeof(Il2Cpp.ComingFromScreenCategory) })]
            internal class Patch_ResetHarvestValues
            {
                static void Prefix(Il2Cpp.Panel_BodyHarvest __instance, bool enable)
                {
                    if (enable || __instance == null) return; // Exit if the panel is opening

                    try
                    {
                        foreach (var item in originalValues.Keys.ToList())
                        {
                            if (item == null) continue;

                            // Restore original values
                            if (originalValues.TryGetValue(item, out var values))
                            {
                                ModifyFieldValue(item, "set_m_HarvestMeatMinutesPerKG", values.meat);
                                ModifyFieldValue(item, "set_m_HarvestFrozenMeatMinutesPerKG", values.frozenMeat);
                                ModifyFieldValue(item, "set_m_HarvestGutMinutesPerUnit", values.gut);
                                ModifyFieldValue(item, "set_m_HarvestHideMinutesPerUnit", values.hide);
                            }

                        }

                        Main.DebugLog($"[Close] Restored initial harvest time values for {originalValues.Count} tools.");
                        // Clear stored original values after reset
                        originalValues.Clear();
                    }
                    catch (Exception ex)
                    {
                        MelonLoader.MelonLogger.Error($"Error in Patch_ResetHarvestValues: {ex}");
                    }
                }
            } // End of Patch_ResetHarvestValues



            // Patch to change the maxiumum harvest time 
            [HarmonyPatch(typeof(Il2Cpp.Panel_BodyHarvest), nameof(Panel_BodyHarvest.Enable), new Type[] { typeof(bool), typeof(Il2Cpp.BodyHarvest), typeof(bool), typeof(Il2Cpp.ComingFromScreenCategory) })]
            internal class Patch_MaxHarvestTime
            {
                static void Prefix(Il2Cpp.Panel_BodyHarvest __instance, bool enable)
                {
                    // ON OPEN PANEL
                    if (!enable || __instance == null) return;// Exit if panel is closing or if null
                    try
                    {
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
        } // End of Panel_BodyHarvest_Time_Patches





        internal class Panel_BodyHarvest_Display_Patches
        {

            private static readonly UnityEngine.Color Green = new UnityEngine.Color(0, 0.808f, 0.518f, 1);
            private static readonly UnityEngine.Color Yellow = new UnityEngine.Color(0.827f, 0.729f, 0, 1);
            private static readonly UnityEngine.Color Orange = new UnityEngine.Color(0.827f, 0.471f, 0, 1);
            private static readonly UnityEngine.Color Red = new UnityEngine.Color(0.639f, 0.204f, 0.231f, 1);
            private static readonly UnityEngine.Color White = new UnityEngine.Color(1, 1, 1, 1);
            private static readonly UnityEngine.Color Cyan = new UnityEngine.Color(0.447f, 0.765f, 0.765f, 1);
            private static readonly UnityEngine.Color Blue = new UnityEngine.Color(0, 0.251f, 0.502f, 1);


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
