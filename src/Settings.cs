﻿using ModSettings;
using System.Reflection;
using System.Collections.Generic;
using Il2CppSystem.Linq.Expressions;

namespace CarcassYieldTweaker
{
    public class Settings : JsonModSettings
    {
        internal static Settings Instance { get; } = new();

        internal static void OnLoad()
        {
            Instance.AddToModSettings("Carcass Yield Tweaker");
            Instance.UpdateVisibility();
            Instance.RefreshGUI();

        }


        // Constants for preset values.
        private const int PRESET_VANILLA = 0;
        private const int PRESET_REALISTIC = 1;
        private const int PRESET_BALANCED = 2;
        private const int PRESET_CUSTOM = 3;

        // Constants for settings categories.
        private const int SETTINGS_GLOBAL = 0;
        private const int SETTINGS_ANIMAL = 1;
        private const int SETTINGS_EXTRA = 2;

        // A helper function to map the selected animal (an int) to its name.
        // These names must match the text in your [Choice] attribute.
        private string GetAnimalFieldName(int animal)
        {
            switch (animal)
            {
                case 0: return "Rabbit";
                case 1: return "Ptarmigan";
                case 2: return "Doe";
                case 3: return "Stag";
                case 4: return "Moose";
                case 5: return "RegularWolf";
                case 6: return "TimberWolf";
                case 7: return "PoisonedWolf";
                case 8: return "Bear";
                case 9: return "Cougar";
                default: return "";
            }
        }

        static bool isApplyingPreset = false; // Flag to suppress OnChange during Selection_Preset application
        private readonly Dictionary<string, object> customSettingsBackup = new();

        bool UpdateVisibilityDebug = false;
        public void UpdateVisibility()
        {
            FieldInfo[] fields = this.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // If the mod is disabled, only show enableMod.
            if (!enableMod)
            {
                if (UpdateVisibilityDebug) Main.DebugLog("Mod is disabled. Hiding all fields except enableMod.");
                foreach (FieldInfo field in fields)
                {
                    if (!field.IsDefined(typeof(NameAttribute), false))
                        continue;
                    this.SetFieldVisible(field, field.Name == nameof(enableMod));
                }
                if (UpdateVisibilityDebug) Main.DebugLog($"[UpdateVisibility] Hiding all fields except enableMod (mod disabled)");
                return;
            }

            // If mod is enabled, decide visibility for each field.
            foreach (FieldInfo field in fields)
            {
                // Skip fields without a NameAttribute.
                if (!field.IsDefined(typeof(NameAttribute), false))
                    continue;

                bool visible = false;

                if (field.Name == nameof(enableMod))
                {
                    visible = true;
                    if (visible && UpdateVisibilityDebug) Main.DebugLog($"[UpdateVisibility] Showing enableMod field: {field.Name}");
                }
                else if (field.Name == nameof(Selection_SettingsCategory) || field.Name == nameof(Selection_Preset))
                {
                    visible = true;
                    if (visible && UpdateVisibilityDebug) Main.DebugLog($"[UpdateVisibility] Showing selection field: {field.Name}");
                }
                else if (field.Name == nameof(Selection_Animal))
                {
                    visible = (Selection_SettingsCategory == SETTINGS_ANIMAL);
                    if (visible && UpdateVisibilityDebug) Main.DebugLog($"[UpdateVisibility] Animal selector {field.Name}");
                }
                else if (field.Name.StartsWith("Animal_"))
                {
                    visible = (Selection_SettingsCategory == SETTINGS_ANIMAL && field.Name.Contains(GetAnimalFieldName(Selection_Animal)));
                    if (visible && UpdateVisibilityDebug) Main.DebugLog($"[UpdateVisibility] Animal-specific field {field.Name}");
                }
                else if (field.Name.StartsWith("Global_"))
                {
                    visible = (Selection_SettingsCategory == SETTINGS_GLOBAL);
                    if (visible && UpdateVisibilityDebug) Main.DebugLog($"[UpdateVisibility] Global field {field.Name}");
                }
                else if (field.Name.StartsWith("Extra_"))
                {
                    visible = (Selection_SettingsCategory == SETTINGS_EXTRA);
                    if (visible && UpdateVisibilityDebug) Main.DebugLog($"[UpdateVisibility] Extra field {field.Name}");
                }
                else
                {
                    visible = false;
                }

                this.SetFieldVisible(field, visible);
            }
        }


        bool OnChangeDebug = false;

        // --------------------------------------------------------------------
        // Change handling & visibility logic.
        // --------------------------------------------------------------------
        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {

            if (OnChangeDebug) Main.DebugLog($"OnChange triggered: Field={field.Name}, OldValue={oldValue}, NewValue={newValue}");

            if (isApplyingPreset)
            {
                if (OnChangeDebug) Main.DebugLog("OnChange suppressed: Preset is being applied.");
                return;
            }

            if (field.Name == nameof(Selection_Preset))
            {
                // For preset changes, update confirmedValues and apply the preset.
                UpdateConfirmedValue(field);
                isApplyingPreset = true;
                ApplyPreset(Selection_Preset);
                RefreshGUI();
                isApplyingPreset = false;
                if (OnChangeDebug) Main.DebugLog($"Preset changed. {oldValue} -> {newValue}");
            }
            else if (field.Name == nameof(enableMod) ||
                     field.Name == nameof(Selection_SettingsCategory) ||
                     field.Name == nameof(Selection_Animal))
            {
                UpdateConfirmedValue(field);
                UpdateVisibility();
            }
            else if ( field.Name.StartsWith("Animal_") || field.Name.StartsWith("Global_") )
            {
                if (OnChangeDebug) Main.DebugLog($"Animal, Global, or Extra Field changed: {field.Name}");
                // If any of these fields change while not in Custom, switch the preset to Custom.
                if (Selection_Preset != PRESET_CUSTOM)
                {
                    if (OnChangeDebug) Main.DebugLog($"Read-only preset value changed. Switching to custom preset.");
                    isApplyingPreset = true;
                    try
                    {
                        Selection_Preset = PRESET_CUSTOM;
                        // Update confirmedValues for Selection_Preset so the change persists.
                        UpdateConfirmedValue(GetType().GetField(nameof(Selection_Preset)));
                        RefreshGUI();
                    }
                    finally
                    {
                        isApplyingPreset = false;
                    }
                }
                if (OnChangeDebug) Main.DebugLog($"Updating temporary custom value for: {field.Name} = {newValue}");
            }
            base.OnChange(field, oldValue, newValue);
        }


        private void UpdateConfirmedValue(FieldInfo field)
        {
            // Use reflection to update the internal confirmedValues dictionary in ModSettingsBase.
            var baseType = typeof(ModSettingsBase);
            var confirmedValuesField = baseType.GetField("confirmedValues", BindingFlags.NonPublic | BindingFlags.Instance);
            if (confirmedValuesField != null)
            {
                var confirmed = (Dictionary<FieldInfo, object>)confirmedValuesField.GetValue(this);
                confirmed[field] = field.GetValue(this);
                if (OnChangeDebug) Main.DebugLog($"[UpdateConfirmedValue] Field {field.Name} updated in confirmedValues.");
            }
        }


        private void ApplyPreset(int presetOption)
        {
            try
            {
                switch (presetOption)
                {
                    case PRESET_VANILLA: ApplyVanillaPreset(); break;
                    case PRESET_REALISTIC: ApplyRealisticPreset(); break;
                    case PRESET_BALANCED: ApplyBalancedPreset(); break;
                    case PRESET_CUSTOM: LoadCustomSettings(); break;
                }
            }
            finally
            {
                Main.DebugLog($"[ApplyPreset] Preset {presetOption} applied!");
            }
        }


        protected override void OnConfirm()
        {
            Main.DebugLog("OnConfirm triggered.");

            // Save instance only if the preset is "Custom".
            if (Selection_Preset == PRESET_CUSTOM)
            {
                Main.DebugLog("Saving Custom preset instance to backup.");
                foreach (var field in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    customSettingsBackup[field.Name] = field.GetValue(this);
                }
            }
            base.OnConfirm();
            Main.DebugLog("Settings confirmed and saved.");
        }

        private void LoadCustomSettings()
        {
            Main.DebugLog("Loading Custom preset from backup.");
            foreach (var entry in customSettingsBackup)
            {
                var field = GetType().GetField(entry.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(this, entry.Value);
                    Main.DebugLog($"Custom preset loaded: {entry.Key} = {entry.Value}");
                }
            }
        }

        /// ===================================================================================================================================================
        // Settings
        [Name("Enable Mod")]
        [Description("Toggle mod functionality.")]
        public bool enableMod = true;

        [Name("Preset")]
        [Description("\n\nChoose a Preset. " +
            "\nVanilla, Realistic, and Balanced are read-only!" +
            "\nChanging any Animal or Global setting while in these presets" +
            "\nwill copy those settings to Custom, which can then be modified." +
          "\n\nIMPORTANT: Custom settings are ONLY SAVED by CLICKING CONFIRM! .")]
        [Choice(new string[] { "Vanilla", "Realistic", "Balanced", "Custom" })]
        public int Selection_Preset;

        [Name("Settings")]
        [Description("Choose which settings to modify: Global, Animal, or Extra.")]
        [Choice(new string[] { "Global", "Animal", "Extra" })]
        public int Selection_SettingsCategory;

        [Name("Quarter Waste Weight Multiplier")]
        [Description("Changes the amount of unharvestable waste in quarters. Vanilla value is 2" +
            "\nwhich means quarters weigh twice as much as the meat you'll get from them.")]
        [Slider(0.5f, 4.00f, NumberFormat = "{0:F1} x")]
        public float Global_QuarterWasteSlider = VanillaSettings.Global_QuarterWasteMultiplier;

        [Name("Maximum Harvest Time")]
        [Description("Maximum time allowed in hours to harvest meat from a carcass. Vanilla value is 5 hours.")]
        [Slider(1f, 24f, NumberFormat = "{0:F1} hrs.")]
        public float Global_MaxHarvestTimeSlider = VanillaSettings.Global_MaxHarvestTimeSliderGlobal;

        // Description values taken from https://thelongdark.fandom.com/wiki/Carcass_Harvesting 2024-12-22
        [Name("Meat (Thawed Carcass)")]
        [Description("Global meat harvest time multiplier for a thawed carcass. Vanilla value is 1.\n" +
                    "\nBase harvest rates are:\n" +
                    "30 min/kg with Bare Hands.\n" +
                    "20 min/kg with Improvised Hatchet.\n" +
                    "15 min/kg with Hacksaw or Hatchet.\n" +
                    "12 min/kg with Improvised Knife.\n" +
                    "8 min/kg with Hunting Knife, Survival Knife, or Scrap Metal Shard.\n" +
                    "7 min/kg with Cougar Claw Knife.\n" +
                    "\nCarcass Harvesting Skill reduces meat harvesting times by:\n" +
                    "10% at level  2\n" +
                    "25% at level  3\n" +
                    "30% at level  4\n" +
                    "50% at  level 5")]
        [Slider(0.01f, 3.00f, NumberFormat = "{0:F2}x")]
        public float Global_MeatTimeSlider = VanillaSettings.Global_MeatTimeSlider;

        [Name("Meat (Frozen Carcass)")]
        [Description("Global meat harvest time multiplier for a frozen carcass. Vanilla value is 1.\n" +
                    "\nBase harvest rates are:\n" +
                    "Cannot harvest frozen meat with Bare Hands!\n" +
                    "30 min/kg with Improvised Knife.\n" +
                    "20 min/kg with Hunting Knife, Scrap Metal Shard, or Cougar Claw Knife.\n" +
                    "18 min/kg with Cougar Claw Knife.\n" +
                    "15 min/kg with Improvised Hatchet.\n" +
                    "10 min/kg with Hacksaw, Hatchet, or Survival Knife.\n" +
                    "\nCarcass Harvesting Skill reduces meat harvesting times by:\n" +
                    " 10% at level  2\n" +
                    " 25% at  level 3\n" +
                    " 30% at  level 4\n" +
                    " 50% at  level 5\n" +
                    "\nCarcass Harvesting Skill allows frozen caracasses to be harvested by hand:\n" +
                    " 50% frozen at Level  3\n" +
                    " 75% frozen at level  4\n" +
                    "100% frozen at level  5")]
        [Slider(0.01f, 3.00f, NumberFormat = "{0:F2}x")]
        public float Global_FrozenMeatTimeSlider = VanillaSettings.Global_FrozenMeatTimeSlider;

        [Name("Gut")]
        [Description("Global Gut harvest time multiplier. Vanilla value is 1\n" +
                    "\nBase harvest rates are:\n" +
                    "40 min/unit with Bare Hands.\n" +
                    "30 min/unit with Hacksaw or Improvised Hatchet.\n" +
                    "20 min/unit with Hatchet.\n" +
                    "15 min/unit with Improvised Knife.\n" +
                    "10 min/unit with Hunting Knife, Cougar Claw Knife, Survival Knife, or Scrap Metal Shard.\n" +
                    "\nCarcass Harvesting Skill reduces gut harvesting times by:\n" +
                    "10% at Level  3\n" +
                    "20% at Level  4\n" +
                    "30% at Level  5")]
        [Slider(0.01f, 3.00f, NumberFormat = "{0:F2}x")]
        public float Global_GutTimeSlider = VanillaSettings.Global_GutTimeSlider;


        //=====================================================================================================================================================
        //[Section("Extra Settings")]

        [Name("Disable Carcass Decay")]
        [Description("Completely disable the decay of animal carcasses. Vanilla is No")]
        public bool Extra_DisableCarcassDecayGlobal = false;

        [Name("Show Condition Percent")]
        [Description("Show the condition of the carcass in the harvest Panel. Vanilla is No")]
        public bool Extra_ShowPanelCondition = false;

        [Name("Condition Text Color")]
        [Description("Color the condition text according to the carcass condition percentage. Vanilla is No\n" +
            "\n100% to 66% - Green" +
            "\n 66% to 33% - Yellow" +
            "\n 33% to  1% - Red")]
        public bool Extra_ShowPanelConditionColors = false;

        [Name("Always Show Frozen Percent")]
        [Description("Always show the frozen percentage in the harvest Panel, even if the carcass is not frozen. Vanilla is No")]
        public bool Extra_AlwaysShowPanelFrozenPercent = false;

        [Name("Frozen Text Color")]
        [Description("Color the frozen text according to the carcass frozen percentage. Vanilla is No\n" +
            "\n  0% to  25% - Orange - Warm" +
            "\n 25% to  50% - White - Cold" +
            "\n 50% to  75% - Cyan - Frozen" +
            "\n 75% to 100% - Blue - Frozen Solid")]
        public bool Extra_ShowPanelFrozenColors = false;

        [Name("Enable Debug Output")]
        [Description("Toggle debug output to the log for troubleshooting. Vanilla is No")]
        public bool Extra_EnableDebug = false;


        /// ===================================================================================================================================================
        // Animal selection 
        [Name("Select Animal")]
        [Description("Choose an animal's to change it's harvest settings")]
        [Choice(new string[] { "Rabbit", "Ptarmigan", "Doe", "Stag", "Moose", "Wolf", "Timber Wolf", "Poisoned Wolf", "Bear", "Cougar" })]
        public int Selection_Animal;

        // ===============================================================================
        //[Section("Rabbit")]
        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed Rabbit. Vanilla value is 0.75")]
        [Slider(0f, 5f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMinRabbit = VanillaSettings.MeatSliderMinRabbit;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed Rabbit. Vanilla value is 1.5")]
        [Slider(0f, 5f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMaxRabbit = VanillaSettings.MeatSliderMaxRabbit;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Rabbit. Vanilla value is 1")]
        [Slider(0, 3)]
        public int Animal_HideCountSliderRabbit = VanillaSettings.HideCountSliderRabbit;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Rabbit. Vanilla value is 1")]
        [Slider(0, 10)]
        public int Animal_GutCountSliderRabbit = VanillaSettings.GutCountSliderRabbit;

        [Name("Hide Time")]
        [Description("Rabbit Hide harvest time multiplier. Vanilla value is 1.\n" +
            "\nBase harvest times are:\n" +
            "60 min with Hacksaw or Improvised Hatchet.\n" +
            "45 min with Hatchet.\n" +
            "40 min with Bare Hands or Improvised Knife.\n" +
            "30 min with Scrap Metal Shard, Survival Knife, Hunting Knife, or Cougar Claw Knife.\n" +
            "\nCarcass Harvesting Skill reduces time by:\n" +
            "10% at level  3\n" +
            "20% at level 4\n" +
            "30% at level 5")]
        [Slider(0.01f, 2.0f, NumberFormat = "{0:F2}x")]
        public float Animal_HideTimeSliderRabbit = VanillaSettings.HideTimeSliderRabbit;


        // ===============================================================================
        //[Section("Ptarmigan (DLC)")]
        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed Ptarmigan. Vanilla value is 0.75")]
        [Slider(0f, 5f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMinPtarmigan = VanillaSettings.MeatSliderMinPtarmigan;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed Ptarmigan. Vanilla value is 1.5")]
        [Slider(0.1f, 5f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMaxPtarmigan = VanillaSettings.MeatSliderMaxPtarmigan;

        [Name("Down Feather Count")]
        [Description("Number of harvestable down feathers from a Ptarmigan. Vanilla value is 4")]
        [Slider(0, 12)]
        public int Animal_HideCountSliderPtarmigan = VanillaSettings.HideCountSliderPtarmigan;

        [Name("Down Feather Time")]
        [Description("Ptarmigan down feathers harvest time multiplier. Vanilla value is 1\n" +
            "\nBase harvest times are:\n" +
            "60 min with Hacksaw or Improvised Hatchet.\n" +
            "45 min with Hatchet.\n" +
            "40 min with Bare Hands or Improvised Knife.\n" +
            "30 min with Scrap Metal Shard, Survival Knife, Hunting Knife, or Cougar Claw Knife.\n" +
            "\nCarcass Harvesting Skill reduces time by:\n" +
            "10% at level  3\n" +
            "20% at level 4\n" +
            "30% at level 5")]
        [Slider(0.01f, 2.0f, NumberFormat = "{0:F2}x")]
        public float Animal_HideTimeSliderPtarmigan = VanillaSettings.HideTimeSliderPtarmigan;


        // ===============================================================================
        //[Section("Doe")]
        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed Doe. Vanilla value is 7")]
        [Slider(0f, 100f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMinDoe = VanillaSettings.MeatSliderMinDoe;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed Doe. Vanilla value is 9")]
        [Slider(0f, 100f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMaxDoe = VanillaSettings.MeatSliderMaxDoe;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Doe. Vanilla value is 1")]
        [Slider(0, 4)]
        public int Animal_HideCountSliderDoe = VanillaSettings.HideCountSliderDoe;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Doe. Vanilla value is 2")]
        [Slider(0, 20)]
        public int Animal_GutCountSliderDoe = VanillaSettings.GutCountSliderDoe;

        [Name("Quarter Size")]
        [Description("Size of each quarter in Kg from a Doe. Vanilla value is 2.5")]
        [Slider(1f, 50f, NumberFormat = "{0:F1} Kg")]
        public float Animal_QuarterSizeSliderDoe = VanillaSettings.QuarterSizeSliderDoe;

        [Name("Fat to Meat Percentage (%)")]
        [Description("Fat to meat percentage for a Doe. Vanilla value is 20%")]
        [Slider(0, 40, NumberFormat = "{0:#} %")]
        public int Animal_FatToMeatPercentSliderDoe = VanillaSettings.FatToMeatPercentSliderDoe;

        [Name("Hide Time")]
        [Description("Doe Hide harvest time multiplier. Vanilla value is 1\n" +
            "\nBase harvest times are:\n" +
            "60 min with Hacksaw or Improvised Hatchet.\n" +
            "45 min with Hatchet.\n" +
            "40 min with Bare Hands or Improvised Knife.\n" +
            "30 min with Scrap Metal Shard, Survival Knife, Hunting Knife, or Cougar Claw Knife.\n" +
            "\nCarcass Harvesting Skill reduces time by:\n" +
            "10% at level  3\n" +
            "20% at level 4\n" +
            "30% at level 5")]
        [Slider(0.01f, 2.0f, NumberFormat = "{0:F2}x")]
        public float Animal_HideTimeSliderDoe = VanillaSettings.HideTimeSliderDoe;

        [Name("Quarter Time")]
        [Description("Time to quarter a Doe. Vanilla value is 60m")]
        [Slider(1, 180, NumberFormat = "{0:#}m")]
        public int Animal_QuarterDurationMinutesSliderDoe = VanillaSettings.QuarterDurationMinutesSliderDoe;


        // ===============================================================================
        //[Section("Stag")]
        [Name("Minimum Meat")]
        [Description("Minimum amount of harvestable meat in Kg from a Stag. Vanilla value is 11")]
        [Slider(0f, 150f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMinStag = VanillaSettings.MeatSliderMinStag;

        [Name("Maximum Meat")]
        [Description("Maximum amount of harvestable meat in Kg from a Stag. Vanilla value is 13")]
        [Slider(0f, 150f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMaxStag = VanillaSettings.MeatSliderMaxStag;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Stag. Vanilla value is 1")]
        [Slider(0, 5)]
        public int Animal_HideCountSliderStag = VanillaSettings.HideCountSliderStag;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Stag. Vanilla value is 2")]
        [Slider(0, 20)]
        public int Animal_GutCountSliderStag = VanillaSettings.GutCountSliderStag;

        [Name("Quarter Size")]
        [Description("Size of each quarter in Kg from a Stag. Vanilla value is 2.5")]
        [Slider(1f, 50f, NumberFormat = "{0:F1} Kg")]
        public float Animal_QuarterSizeSliderStag = VanillaSettings.QuarterSizeSliderStag;

        [Name("Fat to Meat Percentage (%)")]
        [Description("Fat to meat percentage for a Stag. Vanilla value is 20%")]
        [Slider(0, 40, NumberFormat = "{0:#} %")]
        public int Animal_FatToMeatPercentSliderStag = VanillaSettings.FatToMeatPercentSliderStag;

        [Name("Hide Time")]
        [Description("Stag Hide harvest time multiplier. Vanilla value is 1\n" +
            "\nBase harvest times are:\n" +
            "60 min with Hacksaw or Improvised Hatchet.\n" +
            "45 min with Hatchet.\n" +
            "40 min with Bare Hands or Improvised Knife.\n" +
            "30 min with Scrap Metal Shard, Survival Knife, Hunting Knife, or Cougar Claw Knife.\n" +
           "\nCarcass Harvesting Skill reduces time by:\n" +
            "10% at level  3\n" +
            "20% at level 4\n" +
            "30% at level 5")]
        [Slider(0.01f, 2.0f, NumberFormat = "{0:F2}x")]
        public float Animal_HideTimeSliderStag = VanillaSettings.HideTimeSliderStag;

        [Name("Quarter Time")]
        [Description("Time to quarter a Stag. Vanilla value is 75m")]
        [Slider(1, 180, NumberFormat = "{0:#}m")]
        public int Animal_QuarterDurationMinutesSliderStag = VanillaSettings.QuarterDurationMinutesSliderStag;



        // ===============================================================================
        //[Section("Moose")]

        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed Moose. Vanilla value is 30")]
        [Slider(0f, 600f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMinMoose = VanillaSettings.MeatSliderMinMoose;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed Moose. Vanilla value is 45")]
        [Slider(0f, 600f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMaxMoose = VanillaSettings.MeatSliderMaxMoose;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Moose. Vanilla value is 1")]
        [Slider(0, 4)]
        public int Animal_HideCountSliderMoose = VanillaSettings.HideCountSliderMoose;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Moose. Vanilla value is 12")]
        [Slider(0, 48)]
        public int Animal_GutCountSliderMoose = VanillaSettings.GutCountSliderMoose;

        [Name("Quarter Size")]
        [Description("Size of each quarter in Kg from a Moose. Vanilla value is 5")]
        [Slider(1f, 50f, NumberFormat = "{0:F1} Kg")]
        public float Animal_QuarterSizeSliderMoose = VanillaSettings.QuarterSizeSliderMoose;

        [Name("Fat to Meat Percentage (%)")]
        [Description("Fat to meat percentage for a Moose. Vanilla value is 15%")]
        [Slider(0, 40, NumberFormat = "{0:#} %")]
        public int Animal_FatToMeatPercentSliderMoose = VanillaSettings.FatToMeatPercentSliderMoose;

        [Name("Hide Time")]
        [Description("Moose Hide harvest time multiplier. Vanilla value is 1\n" +
            "\nBase harvest times are:\n" +
            "60 min with Hacksaw or Improvised Hatchet.\n" +
            "45 min with Hatchet.\n" +
            "40 min with Bare Hands or Improvised Knife.\n" +
            "30 min with Scrap Metal Shard, Survival Knife, Hunting Knife, or Cougar Claw Knife.\n" +
           "\nCarcass Harvesting Skill reduces time by:\n" +
            "10% at level  3\n" +
            "20% at level 4\n" +
            "30% at level 5")]
        [Slider(0.01f, 2.0f, NumberFormat = "{0:F2}x")]
        public float Animal_HideTimeSliderMoose = VanillaSettings.HideTimeSliderMoose;

        [Name("Quarter Time")]
        [Description("Time to quarter a Moose. Vanilla value is 120m")]
        [Slider(1, 180, NumberFormat = "{0:#}m")]
        public int Animal_QuarterDurationMinutesSliderMoose = VanillaSettings.QuarterDurationMinutesSliderMoose;



        // ===============================================================================
        //[Section("Wolf")]

        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed Wolf. Vanilla value is 3")]
        [Slider(0f, 50f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMinRegularWolf = VanillaSettings.MeatSliderMinRegularWolf;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed Wolf. Vanilla value is 6")]
        [Slider(0f, 50f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMaxRegularWolf = VanillaSettings.MeatSliderMaxRegularWolf;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Wolf. Vanilla value is 1")]
        [Slider(0, 2)]
        public int Animal_HideCountSliderRegularWolf = VanillaSettings.HideCountSliderRegularWolf;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Wolf. Vanilla value is 2")]
        [Slider(0, 20)]
        public int Animal_GutCountSliderRegularWolf = VanillaSettings.GutCountSliderRegularWolf;

        [Name("Quarter Size")]
        [Description("Size of each Quarter from a Wolf. Vanilla value is 2.5")]
        [Slider(1f, 50f, NumberFormat = "{0:F1} Kg")]
        public float Animal_QuarterSizeSliderRegularWolf = VanillaSettings.QuarterSizeSliderRegularWolf;

        [Name("Fat to Meat Percentage (%)")]
        [Description("Fat to meat percentage for a Wolf. Vanilla value is 10%")]
        [Slider(0, 40, NumberFormat = "{0:#} %")]
        public int Animal_FatToMeatPercentSliderRegularWolf = VanillaSettings.FatToMeatPercentSliderRegularWolf;

        [Name("Hide Time")]
        [Description("Wolf Hide harvest time multiplier. Vanilla value is 1\n" +
                    "\nBase harvest times are:\n" +
                    "60 min with Hacksaw or Improvised Hatchet.\n" +
                    "45 min with Hatchet.\n" +
                    "40 min with Bare Hands or Improvised Knife.\n" +
                    "30 min with Scrap Metal Shard, Survival Knife, Hunting Knife, or Cougar Claw Knife.\n" +
                    "\nCarcass Harvesting Skill reduces time by:\n" +
                    "10% at level  3\n" +
                    "20% at level 4\n" +
                    "30% at level 5")]
        [Slider(0.01f, 2.0f, NumberFormat = "{0:F2}x")]
        public float Animal_HideTimeSliderRegularWolf = VanillaSettings.HideTimeSliderRegularWolf;

        [Name("Quarter Time")]
        [Description("Time to quarter a Wolf. Vanilla value is 60m")]
        [Slider(1, 180, NumberFormat = "{0:#}m")]
        public int Animal_QuarterDurationMinutesSliderRegularWolf = VanillaSettings.QuarterDurationMinutesSliderRegularWolf;


        // ===============================================================================
        //[Section("TimberWolf")]

        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed TimberWolf. Vanilla value is 4")]
        [Slider(0f, 70f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMinTimberWolf = VanillaSettings.MeatSliderMinTimberWolf;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed TimberWolf. Vanilla value is 7")]
        [Slider(0f, 70f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMaxTimberWolf = VanillaSettings.MeatSliderMaxTimberWolf;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed TimberWolf. Vanilla value is 1")]
        [Slider(0, 3)]
        public int Animal_HideCountSliderTimberWolf = VanillaSettings.HideCountSliderTimberWolf;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed TimberWolf. Vanilla value is 2")]
        [Slider(0, 20)]
        public int Animal_GutCountSliderTimberWolf = VanillaSettings.GutCountSliderTimberWolf;

        [Name("Quarter Size")]
        [Description("Size of each Quarter from a TimberWolf. Vanilla value is 2.5")]
        [Slider(1f, 50f, NumberFormat = "{0:F1} Kg")]
        public float Animal_QuarterSizeSliderTimberWolf = VanillaSettings.QuarterSizeSliderTimberWolf;

        [Name("Fat to Meat Percentage (%)")]
        [Description("Fat to meat percentage for a TimberWolf. Vanilla value is 10%")]
        [Slider(0, 40, NumberFormat = "{0:#} %")]
        public int Animal_FatToMeatPercentSliderTimberWolf = VanillaSettings.FatToMeatPercentSliderTimberWolf;

        [Name("Hide Time")]
        [Description("TimberWolf Hide harvest time multiplier. Vanilla value is 1\n" +
            "\nBase harvest times are:\n" +
            "60 min with Hacksaw or Improvised Hatchet.\n" +
            "45 min with Hatchet.\n" +
            "40 min with Bare Hands or Improvised Knife.\n" +
            "30 min with Scrap Metal Shard, Survival Knife, Hunting Knife, or Cougar Claw Knife.\n" +
            "\nCarcass Harvesting Skill reduces time by:\n" +
            "10% at level  3\n" +
            "20% at level 4\n" +
            "30% at level 5")]
        [Slider(0.01f, 2.0f, NumberFormat = "{0:F2}x")]
        public float Animal_HideTimeSliderTimberWolf = VanillaSettings.HideTimeSliderTimberWolf;

        [Name("Quarter Time")]
        [Description("Time to quarter a TimberWolf. Vanilla value is 60m")]
        [Slider(1, 180, NumberFormat = "{0:#}m")]
        public int Animal_QuarterDurationMinutesSliderTimberWolf = VanillaSettings.QuarterDurationMinutesSliderTimberWolf;


        // ===============================================================================
        //[Section("Poisoned Wolf (DLC)")]

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Poisoned Wolf. Vanilla value is 1")]
        [Slider(0, 2)]
        public int Animal_HideCountSliderPoisonedWolf = VanillaSettings.HideCountSliderPoisonedWolf;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Poisoned Wolf. Vanilla value is 2")]
        [Slider(0, 10)]
        public int Animal_GutCountSliderPoisonedWolf = VanillaSettings.GutCountSliderPoisonedWolf;

        [Name("Hide Time")]
        [Description("Poisoned Wolf Hide harvest time multiplier. Vanilla value is 1\n" +
            "\nBase harvest times are:\n" +
            "60 min with Hacksaw or Improvised Hatchet.\n" +
            "45 min with Hatchet.\n" +
            "40 min with Bare Hands or Improvised Knife.\n" +
            "30 min with Scrap Metal Shard, Survival Knife, Hunting Knife, or Cougar Claw Knife.\n" +
            "\nCarcass Harvesting Skill reduces time by:\n" +
            "10% at level  3\n" +
            "20% at level 4\n" +
            "30% at level 5")]
        [Slider(0.01f, 2.00f, NumberFormat = "{0:F2}x")]
        public float Animal_HideTimeSliderPoisonedWolf = VanillaSettings.HideTimeSliderPoisonedWolf;


        // ===============================================================================
        //[Section("Bear")]

        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed Bear. Vanilla value is 25")]
        [Slider(0f, 300f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMinBear = VanillaSettings.MeatSliderMinBear;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed Bear. Vanilla value is 40")]
        [Slider(0f, 300f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMaxBear = VanillaSettings.MeatSliderMaxBear;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Bear. Vanilla value is 1")]
        [Slider(0, 3)]
        public int Animal_HideCountSliderBear = VanillaSettings.HideCountSliderBear;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Bear. Vanilla value is 10")]
        [Slider(0, 40)]
        public int Animal_GutCountSliderBear = VanillaSettings.GutCountSliderBear;

        [Name("Quarter Size")]
        [Description("Size of each Quarter from a Bear. Vanilla value is 5")]
        [Slider(1f, 50f, NumberFormat = "{0:F1} Kg")]
        public float Animal_QuarterSizeSliderBear = VanillaSettings.QuarterSizeSliderBear;

        [Name("Fat to Meat Percentage (%)")]
        [Description("Fat to meat percentage for a Bear. Vanilla value is 10%")]
        [Slider(0, 40, NumberFormat = "{0:#} %")]
        public int Animal_FatToMeatPercentSliderBear = VanillaSettings.FatToMeatPercentSliderBear;

        [Name("Hide Time")]
        [Description("Bear Hide harvest time multiplier. Vanilla value is 1\n" +
                    "\nBase harvest times are:\n" +
                    "60 min with Hacksaw or Improvised Hatchet.\n" +
                    "45 min with Hatchet.\n" +
                    "40 min with Bare Hands or Improvised Knife.\n" +
                    "30 min with Scrap Metal Shard, Survival Knife, Hunting Knife, or Cougar Claw Knife.\n" +
                    "\nCarcass Harvesting Skill reduces time by:\n" +
                    "10% at level  3\n" +
                    "20% at level 4\n" +
                    "30% at level 5")]
        [Slider(0.01f, 3.00f, NumberFormat = "{0:F2}x")]
        public float Animal_HideTimeSliderBear = VanillaSettings.HideTimeSliderBear;

        [Name("Quarter Time")]
        [Description("Time to quarter a Bear. Vanilla value is 120m")]
        [Slider(1, 180, NumberFormat = "{0:#}m")]
        public int Animal_QuarterDurationMinutesSliderBear = VanillaSettings.QuarterDurationMinutesSliderBear;


        // ===============================================================================
        //[Section("Cougar (DLC)")]

        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed Cougar. Vanilla value is 4")]
        [Slider(0f, 100f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMinCougar = VanillaSettings.MeatSliderMinCougar;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed Cougar. Vanilla value is 5")]
        [Slider(0f, 100f, NumberFormat = "{0:F1} Kg")]
        public float Animal_MeatSliderMaxCougar = VanillaSettings.MeatSliderMaxCougar;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Cougar. Vanilla value is 1")]
        [Slider(0, 2)]
        public int Animal_HideCountSliderCougar = VanillaSettings.HideCountSliderCougar;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Cougar. Vanilla value is 2")]
        [Slider(0, 50)]
        public int Animal_GutCountSliderCougar = VanillaSettings.GutCountSliderCougar;

        [Name("Quarter Size")]
        [Description("Size of each Quarter from a Cougar. Vanilla value is 2.5")]
        [Slider(1f, 50f, NumberFormat = "{0:F1} Kg")]
        public float Animal_QuarterSizeSliderCougar = VanillaSettings.QuarterSizeSliderCougar;

        [Name("Fat to Meat Percentage (%)")]
        [Description("Fat to meat percentage for a Cougar. Vanilla value is 10%")]
        [Slider(0, 40, NumberFormat = "{0:#} %")]
        public int Animal_FatToMeatPercentSliderCougar = VanillaSettings.FatToMeatPercentSliderCougar;

        [Name("Hide Time")]
        [Description("Cougar Hide harvest time multiplier. Vanilla value is 1\n" +
            "\nBase harvest times are:\n" +
            "60 min with Hacksaw or Improvised Hatchet.\n" +
            "45 min with Hatchet.\n" +
            "40 min with Bare Hands or Improvised Knife.\n" +
            "30 min with Scrap Metal Shard, Survival Knife, Hunting Knife, or Cougar Claw Knife.\n" +
            "\nCarcass Harvesting Skill reduces time by:\n" +
            "10% at level  3\n" +
            "20% at level 4\n" +
            "30% at level 5")]
        [Slider(0.01f, 2.00f, NumberFormat = "{0:F2}x")]
        public float Animal_HideTimeSliderCougar = VanillaSettings.HideTimeSliderCougar;

        [Name("Quarter Time")]
        [Description("Time to quarter a Cougar. Vanilla value is 120m")]
        [Slider(1, 180, NumberFormat = "{0:#}m")]
        public int Animal_QuarterDurationMinutesSliderCougar = VanillaSettings.QuarterDurationMinutesSliderCougar;

       

        private void ApplyVanillaPreset()
        {
            // Set Vanilla Values from the VanillaSettings class

            // Global Settings
            this.Global_QuarterWasteSlider = VanillaSettings.Global_QuarterWasteMultiplier;
            this.Global_MeatTimeSlider = VanillaSettings.Global_MeatTimeSlider;
            this.Global_FrozenMeatTimeSlider = VanillaSettings.Global_FrozenMeatTimeSlider;
            this.Global_GutTimeSlider = VanillaSettings.Global_GutTimeSlider;
            this.Global_MaxHarvestTimeSlider = VanillaSettings.Global_MaxHarvestTimeSliderGlobal;

            //// Extra Settings
            //this.Extra_ShowPanelCondition = VanillaSettings.ShowPanelCondition;
            //this.Extra_ShowPanelConditionColors = VanillaSettings.ShowPanelConditionColors;
            //this.Extra_AlwaysShowPanelFrozenPercent = VanillaSettings.AlwaysShowPanelFrozenPercent;
            //this.Extra_ShowPanelFrozenColors = VanillaSettings.ShowPanelFrozenColors;
            //this.Extra_DisableCarcassDecayGlobal = VanillaSettings.DisableCarcassDecayGlobal;

            // Rabbit
            this.Animal_MeatSliderMinRabbit = VanillaSettings.MeatSliderMinRabbit;
            this.Animal_MeatSliderMaxRabbit = VanillaSettings.MeatSliderMaxRabbit;
            this.Animal_HideCountSliderRabbit = VanillaSettings.HideCountSliderRabbit;
            this.Animal_GutCountSliderRabbit = VanillaSettings.GutCountSliderRabbit;
            this.Animal_HideTimeSliderRabbit = VanillaSettings.HideTimeSliderRabbit;

            // Ptarmigan (DLC)
            this.Animal_MeatSliderMinPtarmigan = VanillaSettings.MeatSliderMinPtarmigan;
            this.Animal_MeatSliderMaxPtarmigan = VanillaSettings.MeatSliderMaxPtarmigan;
            this.Animal_HideCountSliderPtarmigan = VanillaSettings.HideCountSliderPtarmigan;
            this.Animal_HideTimeSliderPtarmigan = VanillaSettings.HideTimeSliderPtarmigan;

            // Doe
            this.Animal_MeatSliderMinDoe = VanillaSettings.MeatSliderMinDoe;
            this.Animal_MeatSliderMaxDoe = VanillaSettings.MeatSliderMaxDoe;
            this.Animal_HideCountSliderDoe = VanillaSettings.HideCountSliderDoe;
            this.Animal_GutCountSliderDoe = VanillaSettings.GutCountSliderDoe;
            this.Animal_QuarterSizeSliderDoe = VanillaSettings.QuarterSizeSliderDoe;
            this.Animal_FatToMeatPercentSliderDoe = VanillaSettings.FatToMeatPercentSliderDoe;
            this.Animal_HideTimeSliderDoe = VanillaSettings.HideTimeSliderDoe;
            this.Animal_QuarterDurationMinutesSliderDoe = VanillaSettings.QuarterDurationMinutesSliderDoe;

            // Stag
            this.Animal_MeatSliderMinStag = VanillaSettings.MeatSliderMinStag;
            this.Animal_MeatSliderMaxStag = VanillaSettings.MeatSliderMaxStag;
            this.Animal_HideCountSliderStag = VanillaSettings.HideCountSliderStag;
            this.Animal_GutCountSliderStag = VanillaSettings.GutCountSliderStag;
            this.Animal_QuarterSizeSliderStag = VanillaSettings.QuarterSizeSliderStag;
            this.Animal_FatToMeatPercentSliderStag = VanillaSettings.FatToMeatPercentSliderStag;
            this.Animal_HideTimeSliderStag = VanillaSettings.HideTimeSliderStag;
            this.Animal_QuarterDurationMinutesSliderStag = VanillaSettings.QuarterDurationMinutesSliderStag;

            // Moose
            this.Animal_MeatSliderMinMoose = VanillaSettings.MeatSliderMinMoose;
            this.Animal_MeatSliderMaxMoose = VanillaSettings.MeatSliderMaxMoose;
            this.Animal_HideCountSliderMoose = VanillaSettings.HideCountSliderMoose;
            this.Animal_GutCountSliderMoose = VanillaSettings.GutCountSliderMoose;
            this.Animal_QuarterSizeSliderMoose = VanillaSettings.QuarterSizeSliderMoose;
            this.Animal_FatToMeatPercentSliderMoose = VanillaSettings.FatToMeatPercentSliderMoose;
            this.Animal_HideTimeSliderMoose = VanillaSettings.HideTimeSliderMoose;
            this.Animal_QuarterDurationMinutesSliderMoose = VanillaSettings.QuarterDurationMinutesSliderMoose;

            // Wolf
            this.Animal_MeatSliderMinRegularWolf = VanillaSettings.MeatSliderMinRegularWolf;
            this.Animal_MeatSliderMaxRegularWolf = VanillaSettings.MeatSliderMaxRegularWolf;
            this.Animal_HideCountSliderRegularWolf = VanillaSettings.HideCountSliderRegularWolf;
            this.Animal_GutCountSliderRegularWolf = VanillaSettings.GutCountSliderRegularWolf;
            this.Animal_QuarterSizeSliderRegularWolf = VanillaSettings.QuarterSizeSliderRegularWolf;
            this.Animal_FatToMeatPercentSliderRegularWolf = VanillaSettings.FatToMeatPercentSliderRegularWolf;
            this.Animal_HideTimeSliderRegularWolf = VanillaSettings.HideTimeSliderRegularWolf;
            this.Animal_QuarterDurationMinutesSliderRegularWolf = VanillaSettings.QuarterDurationMinutesSliderRegularWolf;

            // TimberWolf
            this.Animal_MeatSliderMinTimberWolf = VanillaSettings.MeatSliderMinTimberWolf;
            this.Animal_MeatSliderMaxTimberWolf = VanillaSettings.MeatSliderMaxTimberWolf;
            this.Animal_HideCountSliderTimberWolf = VanillaSettings.HideCountSliderTimberWolf;
            this.Animal_GutCountSliderTimberWolf = VanillaSettings.GutCountSliderTimberWolf;
            this.Animal_QuarterSizeSliderTimberWolf = VanillaSettings.QuarterSizeSliderTimberWolf;
            this.Animal_FatToMeatPercentSliderTimberWolf = VanillaSettings.FatToMeatPercentSliderTimberWolf;
            this.Animal_HideTimeSliderTimberWolf = VanillaSettings.HideTimeSliderTimberWolf;
            this.Animal_QuarterDurationMinutesSliderTimberWolf = VanillaSettings.QuarterDurationMinutesSliderTimberWolf;

            // Poisoned Wolf (DLC)
            this.Animal_HideCountSliderPoisonedWolf = VanillaSettings.HideCountSliderPoisonedWolf;
            this.Animal_GutCountSliderPoisonedWolf = VanillaSettings.GutCountSliderPoisonedWolf;
            this.Animal_HideTimeSliderPoisonedWolf = VanillaSettings.HideTimeSliderPoisonedWolf;

            // Bear
            this.Animal_MeatSliderMinBear = VanillaSettings.MeatSliderMinBear;
            this.Animal_MeatSliderMaxBear = VanillaSettings.MeatSliderMaxBear;
            this.Animal_HideCountSliderBear = VanillaSettings.HideCountSliderBear;
            this.Animal_GutCountSliderBear = VanillaSettings.GutCountSliderBear;
            this.Animal_QuarterSizeSliderBear = VanillaSettings.QuarterSizeSliderBear;
            this.Animal_FatToMeatPercentSliderBear = VanillaSettings.FatToMeatPercentSliderBear;
            this.Animal_HideTimeSliderBear = VanillaSettings.HideTimeSliderBear;
            this.Animal_QuarterDurationMinutesSliderBear = VanillaSettings.QuarterDurationMinutesSliderBear;

            // Cougar
            this.Animal_MeatSliderMinCougar = VanillaSettings.MeatSliderMinCougar;
            this.Animal_MeatSliderMaxCougar = VanillaSettings.MeatSliderMaxCougar;
            this.Animal_HideCountSliderCougar = VanillaSettings.HideCountSliderCougar;
            this.Animal_GutCountSliderCougar = VanillaSettings.GutCountSliderCougar;
            this.Animal_QuarterSizeSliderCougar = VanillaSettings.QuarterSizeSliderCougar;
            this.Animal_FatToMeatPercentSliderCougar = VanillaSettings.FatToMeatPercentSliderCougar;
            this.Animal_HideTimeSliderCougar = VanillaSettings.HideTimeSliderCougar;
            this.Animal_QuarterDurationMinutesSliderCougar = VanillaSettings.QuarterDurationMinutesSliderCougar;
        }
        private void ApplyRealisticPreset()
        {
            // Realistic Preset - Meat values are based on data from Canadian encyclopedia (see DATA.xlsx)
            //
            // ********** WARNING - ALL VALUES MUST HAVE A MAXIUMUM OF 2 DECIMALS! *************************
            //
            // Global
            this.Global_QuarterWasteSlider = 1.2f; // Less waste
            this.Global_MeatTimeSlider = 1f; // Unchanged
            this.Global_FrozenMeatTimeSlider = 1f; // Unchanged
            this.Global_GutTimeSlider = 1f; // Unchanged

            // Rabbit
            this.Animal_MeatSliderMinRabbit = 0.75f;
            this.Animal_MeatSliderMaxRabbit = 1.5f;
            this.Animal_HideCountSliderRabbit = 1;
            this.Animal_GutCountSliderRabbit = 2;
            this.Animal_HideTimeSliderRabbit = 0.13f; // A rabbit can be skinned in less than a minute

            // Ptarmigan (DLC)
            this.Animal_MeatSliderMinPtarmigan = 0.43f;
            this.Animal_MeatSliderMaxPtarmigan = 0.81f;
            this.Animal_HideCountSliderPtarmigan = 4;
            this.Animal_HideTimeSliderPtarmigan = 0.25f; // A Ptarmigan can be plucked in less than 30 minutes

            // Doe
            this.Animal_MeatSliderMinDoe = 16f;
            this.Animal_MeatSliderMaxDoe = 36f;
            this.Animal_HideCountSliderDoe = 1;
            this.Animal_GutCountSliderDoe = 12;
            this.Animal_QuarterSizeSliderDoe = 10f;
            this.Animal_FatToMeatPercentSliderDoe = 3; //Doe are very lean
            this.Animal_HideTimeSliderDoe = 0.75f; // Realistic time for processing a doe hide
            this.Animal_QuarterDurationMinutesSliderDoe = 30;

            // Stag
            this.Animal_MeatSliderMinStag = 38f;
            this.Animal_MeatSliderMaxStag = 57f;
            this.Animal_HideCountSliderStag = 1;
            this.Animal_GutCountSliderStag = 15;
            this.Animal_QuarterSizeSliderStag = 15f;
            this.Animal_FatToMeatPercentSliderStag = 4; // Stags have a bit more fat
            this.Animal_HideTimeSliderStag = 1.13f; // Realistic time for processing a stag hide
            this.Animal_QuarterDurationMinutesSliderStag = 60;

            // Moose
            this.Animal_MeatSliderMinMoose = 121f;
            this.Animal_MeatSliderMaxMoose = 270f;
            this.Animal_HideCountSliderMoose = 1;
            this.Animal_GutCountSliderMoose = 40;
            this.Animal_QuarterSizeSliderMoose = 30f;
            this.Animal_FatToMeatPercentSliderMoose = 5;
            this.Animal_HideTimeSliderMoose = 2.25f; // Realistic time for processing a moose hide
            this.Animal_QuarterDurationMinutesSliderMoose = 150;

            // Wolf
            this.Animal_MeatSliderMinRegularWolf = 7f;
            this.Animal_MeatSliderMaxRegularWolf = 26f;
            this.Animal_HideCountSliderRegularWolf = 1;
            this.Animal_GutCountSliderRegularWolf = 6;
            this.Animal_QuarterSizeSliderRegularWolf = 7f;
            this.Animal_FatToMeatPercentSliderRegularWolf = 2;
            this.Animal_HideTimeSliderRegularWolf = 0.63f; // Realistic time for processing a wolf hide
            this.Animal_QuarterDurationMinutesSliderRegularWolf = 20;

            // TimberWolf
            this.Animal_MeatSliderMinTimberWolf = 9f; // Larger minimum meat yield due to increased size.
            this.Animal_MeatSliderMaxTimberWolf = 32f; // Higher maximum meat yield for a larger wolf.
            this.Animal_HideCountSliderTimberWolf = 1; // Still yields only 1 hide.
            this.Animal_GutCountSliderTimberWolf = 8; // More guts due to its larger body size.
            this.Animal_QuarterSizeSliderTimberWolf = 9f; // Larger quarters for a bigger wolf.
            this.Animal_FatToMeatPercentSliderTimberWolf = 3; // Slightly higher fat-to-meat ratio than regular wolves.
            this.Animal_HideTimeSliderTimberWolf = 0.75f; // Slightly longer hide processing time due to size.
            this.Animal_QuarterDurationMinutesSliderTimberWolf = 30; // Longer quartering time than regular wolves.

            // Poisoned Wolf (DLC)
            this.Animal_HideCountSliderPoisonedWolf = 1; // Default value
            this.Animal_GutCountSliderPoisonedWolf = 2; // Default value

            // Bear
            this.Animal_MeatSliderMinBear = 16f;
            this.Animal_MeatSliderMaxBear = 135f;
            this.Animal_HideCountSliderBear = 1;
            this.Animal_GutCountSliderBear = 25;
            this.Animal_QuarterSizeSliderBear = 25f;
            this.Animal_FatToMeatPercentSliderBear = 25;
            this.Animal_HideTimeSliderBear = 2.25f; // Realistic time for processing a bear hide
            this.Animal_QuarterDurationMinutesSliderBear = 180;

            // Cougar (DLC)
            this.Animal_MeatSliderMinCougar = 13f;
            this.Animal_MeatSliderMaxCougar = 54f;
            this.Animal_HideCountSliderCougar = 1;
            this.Animal_GutCountSliderCougar = 6;
            this.Animal_QuarterSizeSliderCougar = 18f;
            this.Animal_FatToMeatPercentSliderCougar = 4;
            this.Animal_HideTimeSliderCougar = 0.75f; // Realistic time for processing a cougar hide
            this.Animal_QuarterDurationMinutesSliderCougar = 60;

        }
        private void ApplyBalancedPreset()
        {
            // Realistic (Balanced) Preset - Meat values are based on data from Canadian encyclopedia (see DATA.xlsx)
            //
            // ********** WARNING - ALL VALUES MUST HAVE A MAXIUMUM OF 2 DECIMALS! *************************
            //
            // Rabbit
            this.Animal_MeatSliderMinRabbit = 0.75f; // Realistic unchanged
            this.Animal_MeatSliderMaxRabbit = 1.5f; // Realistic unchanged
            this.Animal_HideCountSliderRabbit = 1;
            this.Animal_GutCountSliderRabbit = 2; // Realistic unchanged
            this.Animal_HideTimeSliderRabbit = 0.25f; // Doubled Realistic time for rabbit hide processing

            // Ptarmigan (DLC)
            this.Animal_MeatSliderMinPtarmigan = 0.43f; // Realistic unchanged
            this.Animal_MeatSliderMaxPtarmigan = 0.81f; // Realistic unchanged
            this.Animal_HideCountSliderPtarmigan = 4;
            this.Animal_HideTimeSliderPtarmigan = 0.50f; // Doubled Realistic time for ptarmigan feather plucking

            // Doe
            this.Animal_MeatSliderMinDoe = 11f; // Realistic -33%
            this.Animal_MeatSliderMaxDoe = 18f; // Realistic -50%
            this.Animal_HideCountSliderDoe = 1;
            this.Animal_GutCountSliderDoe = 3; // Arbitrary value
            this.Animal_QuarterSizeSliderDoe = 6f; // Arbitrary value
            this.Animal_FatToMeatPercentSliderDoe = 6;
            this.Animal_HideTimeSliderDoe = 0.75f; // Realistic time for processing a doe hide
            this.Animal_QuarterDurationMinutesSliderDoe = 30;

            //// Stag
            this.Animal_MeatSliderMinStag = 25f; // Realistic -33%
            this.Animal_MeatSliderMaxStag = 37f; // Realistic -50%
            this.Animal_HideCountSliderStag = 1;
            this.Animal_GutCountSliderStag = 5; // Arbitrary value
            this.Animal_QuarterSizeSliderStag = 8f; // Arbitrary value
            this.Animal_FatToMeatPercentSliderStag = 8;
            this.Animal_HideTimeSliderStag = 1f; // faster Realistic time for processing a stag hide
            this.Animal_QuarterDurationMinutesSliderStag = 70;

            // Moose
            this.Animal_MeatSliderMinMoose = 80f; // Realistic -33%
            this.Animal_MeatSliderMaxMoose = 135f; // Realistic -50%
            this.Animal_HideCountSliderMoose = 1;
            this.Animal_GutCountSliderMoose = 16; // Arbitrary value
            this.Animal_QuarterSizeSliderMoose = 20f; // Arbitrary value
            this.Animal_FatToMeatPercentSliderMoose = 15;
            this.Animal_HideTimeSliderMoose = 1.5f; // faster Realistic time for processing a moose hide
            this.Animal_QuarterDurationMinutesSliderMoose = 120;

            // Wolf
            this.Animal_MeatSliderMinRegularWolf = 5f; // Realistic -33%
            this.Animal_MeatSliderMaxRegularWolf = 13f; // Realistic -50%
            this.Animal_HideCountSliderRegularWolf = 1;
            this.Animal_GutCountSliderRegularWolf = 2; // Arbitrary value
            this.Animal_QuarterSizeSliderRegularWolf = 5f; // Arbitrary value
            this.Animal_FatToMeatPercentSliderRegularWolf = 4;
            this.Animal_HideTimeSliderRegularWolf = 0.75f; // slower Realistic time for processing a wolf hide
            this.Animal_QuarterDurationMinutesSliderRegularWolf = 40;


            // TimberWolf
            this.Animal_MeatSliderMinTimberWolf = 7f; // smaller Larger minimum meat yield due to increased size.
            this.Animal_MeatSliderMaxTimberWolf = 19f; // smaller Higher maximum meat yield for a larger wolf.
            this.Animal_HideCountSliderTimberWolf = 1; // Still yields only 1 hide.
            this.Animal_GutCountSliderTimberWolf = 7; // More guts due to its larger body size.
            this.Animal_QuarterSizeSliderTimberWolf = 8f; // Larger quarters for a bigger wolf.
            this.Animal_FatToMeatPercentSliderTimberWolf = 3; // Slightly higher fat-to-meat ratio than regular wolves.
            this.Animal_HideTimeSliderTimberWolf = 0.90f; // Slightly longer hide processing time due to size.
            this.Animal_QuarterDurationMinutesSliderTimberWolf = 45; // Longer quartering time than regular wolves.

            // Poisoned Wolf (DLC)
            this.Animal_HideCountSliderPoisonedWolf = 1; // Default value
            this.Animal_GutCountSliderPoisonedWolf = 2; // Default value

            // Bear
            this.Animal_MeatSliderMinBear = 16f; // Realistic unchanged
            this.Animal_MeatSliderMaxBear = 68f; // Realistic -50%
            this.Animal_HideCountSliderBear = 1;
            this.Animal_GutCountSliderBear = 12; // Vanilla value 12
            this.Animal_QuarterSizeSliderBear = 15f; // Realistic -10
            this.Animal_FatToMeatPercentSliderBear = 10;
            this.Animal_HideTimeSliderBear = 1.5f; // Realistic time for processing a bear hide
            this.Animal_QuarterDurationMinutesSliderBear = 120;

            // Cougar (DLC)
            this.Animal_MeatSliderMinCougar = 8f; // Realistic -33%
            this.Animal_MeatSliderMaxCougar = 27f; // Realistic -50%
            this.Animal_HideCountSliderCougar = 1;
            this.Animal_GutCountSliderCougar = 5; // Arbitrary value
            this.Animal_QuarterSizeSliderCougar = 7f; // Arbitrary value
            this.Animal_FatToMeatPercentSliderCougar = 10;
            this.Animal_HideTimeSliderCougar = 0.90f; // Realistic time for processing a cougar hide   
            this.Animal_QuarterDurationMinutesSliderCougar = 90;

            // Quarter Waste Multiplier
            this.Global_QuarterWasteSlider = 1.2f;
        } 

    } // End of Settings Class




    internal static class VanillaSettings // Define Vanilla Settings (only once) - only the descriptions will need updated if something changes
    {
        //
        // ********** WARNING - ALL VALUES MUST HAVE A MAXIUMUM OF 2 DECIMALS! *************************
        //
        // Global
        internal static float Global_QuarterWasteMultiplier = 2.0f;
        internal static float Global_MeatTimeSlider = 1f;
        internal static float Global_FrozenMeatTimeSlider = 1f;
        internal static float Global_GutTimeSlider = 1f;
        internal static float Global_MaxHarvestTimeSliderGlobal = 5f;

        //// Extra Settings
        //internal static bool AlwaysShowPanelFrozenPercent = false;
        //internal static bool ShowPanelFrozenColors = false;
        //internal static bool ShowPanelCondition = false;
        //internal static bool ShowPanelConditionColors = false;
        //internal static bool DisableCarcassDecayGlobal = false;

        // Rabbit
        internal static float MeatSliderMinRabbit = 0.75f;
        internal static float MeatSliderMaxRabbit = 1.5f;
        internal static int HideCountSliderRabbit = 1;
        internal static int GutCountSliderRabbit = 1;
        internal static float HideTimeSliderRabbit = 1f;
        internal static float DecayRateMultiplierSliderRabbit = 1f;

        // Ptarmigan (DLC)
        internal static float MeatSliderMinPtarmigan = 0.75f;
        internal static float MeatSliderMaxPtarmigan = 1.5f;
        internal static int HideCountSliderPtarmigan = 4;
        internal static float HideTimeSliderPtarmigan = 1f;
        internal static float DecayRateMultiplierSliderPtarmigan = 1f;

        // Doe
        internal static float MeatSliderMinDoe = 7f;
        internal static float MeatSliderMaxDoe = 9f;
        internal static int HideCountSliderDoe = 1;
        internal static int GutCountSliderDoe = 2;
        internal static float QuarterSizeSliderDoe = 2.5f;
        internal static int FatToMeatPercentSliderDoe = 20;
        internal static float HideTimeSliderDoe = 1f;
        internal static int QuarterDurationMinutesSliderDoe = 60;
        internal static float DecayRateMultiplierSliderDoe = 1f;

        // Stag
        internal static float MeatSliderMinStag = 11f;
        internal static float MeatSliderMaxStag = 13f;
        internal static int HideCountSliderStag = 1;
        internal static int GutCountSliderStag = 2;
        internal static float QuarterSizeSliderStag = 2.5f;
        internal static int FatToMeatPercentSliderStag = 20;
        internal static float HideTimeSliderStag = 1f;
        internal static int QuarterDurationMinutesSliderStag = 75;
        internal static float DecayRateMultiplierSliderStag = 1f;

        // Moose
        internal static float MeatSliderMinMoose = 30f;
        internal static float MeatSliderMaxMoose = 45f;
        internal static int HideCountSliderMoose = 1;
        internal static int GutCountSliderMoose = 12;
        internal static float QuarterSizeSliderMoose = 5f;
        internal static float FrozenMeatTimeSliderMoose = 1f;
        internal static float HideTimeSliderMoose = 1f;
        internal static int FatToMeatPercentSliderMoose = 15;
        internal static int QuarterDurationMinutesSliderMoose = 120;
        internal static float DecayRateMultiplierSliderMoose = 1f;

        // Wolf
        internal static float MeatSliderMinRegularWolf = 3f;
        internal static float MeatSliderMaxRegularWolf = 6f;
        internal static int HideCountSliderRegularWolf = 1;
        internal static int GutCountSliderRegularWolf = 2;
        internal static float QuarterSizeSliderRegularWolf = 2.5f;
        internal static int FatToMeatPercentSliderRegularWolf = 10;
        internal static float HideTimeSliderRegularWolf = 1f;
        internal static int QuarterDurationMinutesSliderRegularWolf = 60;
        internal static float DecayRateMultiplierSliderWolf = 1f;

        // TimberWolf
        internal static float MeatSliderMinTimberWolf = 4f;
        internal static float MeatSliderMaxTimberWolf = 7f;
        internal static int HideCountSliderTimberWolf = 1;
        internal static int GutCountSliderTimberWolf = 2;
        internal static float QuarterSizeSliderTimberWolf = 2.5f;
        internal static int FatToMeatPercentSliderTimberWolf = 10;
        internal static float HideTimeSliderTimberWolf = 1f;
        internal static int QuarterDurationMinutesSliderTimberWolf = 60;
        internal static float DecayRateMultiplierSliderTimberWolf = 1f;

        // Poisoned Wolf (DLC)
        internal static int HideCountSliderPoisonedWolf = 1;
        internal static int GutCountSliderPoisonedWolf = 2;
        internal static float HideTimeSliderPoisonedWolf = 1f;
        internal static float DecayRateMultiplierSliderPoisonedWolf = 1f;

        // Bear
        internal static float MeatSliderMinBear = 25f;
        internal static float MeatSliderMaxBear = 40f;
        internal static int HideCountSliderBear = 1;
        internal static int GutCountSliderBear = 10;
        internal static float QuarterSizeSliderBear = 5f;
        internal static int FatToMeatPercentSliderBear = 10;
        internal static float HideTimeSliderBear = 1f;
        internal static int QuarterDurationMinutesSliderBear = 120;
        internal static float DecayRateMultiplierSliderBear = 1f;

        // Cougar (DLC)
        internal static float MeatSliderMinCougar = 4f;
        internal static float MeatSliderMaxCougar = 5f;
        internal static int HideCountSliderCougar = 1;
        internal static int GutCountSliderCougar = 2;
        internal static float QuarterSizeSliderCougar = 2.5f;
        internal static int FatToMeatPercentSliderCougar = 10;
        internal static float HideTimeSliderCougar = 1f;
        internal static int QuarterDurationMinutesSliderCougar = 120;
        internal static float DecayRateMultiplierSliderCougar = 1f;

    } // End of VanillaSettings namespace

} // End of CarcassHarvestSettings namespace