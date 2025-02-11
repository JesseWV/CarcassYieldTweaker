using ModSettings;
using System.Reflection;
using System;
using MelonLoader;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace CarcassYieldTweaker
{
    internal static class Settings
    {
        internal static CarcassYieldTweakerSettings instance = new();
        internal static void OnLoad()
        {
            instance.AddToModSettings("Carcass Yield Tweaker");
            instance.RefreshGUI();
        }
    }
    internal class CarcassYieldTweakerSettings : JsonModSettings
    {
        private bool isApplyingPreset = false; // Flag to suppress OnChange during preset application
        private readonly Dictionary<string, object> customSettingsBackup = new();

        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            //Main.DebugLog($"OnChange triggered: Field={field.Name}, OldValue={oldValue}, NewValue={newValue}");

            if (isApplyingPreset)
            {
                Main.DebugLog("OnChange suppressed: Preset is being applied.");
                return;
            }

            if (field.Name == nameof(preset))
            {
                // User selected a preset
                Main.DebugLog($"Preset changed to: {newValue}");
                ApplyPreset((int)newValue);
            }
            else
            {
                // User modified a instance; switch to "Custom" if not already
                if (preset != 3) // 3 = "Custom"
                {
                    Main.DebugLog("Switching to Custom preset due to instance modification.");

                    isApplyingPreset = true; // Temporarily suppress OnChange for the preset change
                    try
                    {
                        preset = 3; // Set preset to "Custom"
                        RefreshGUI(); // Ensure the UI reflects the change
                    }
                    finally
                    {
                        isApplyingPreset = false; // Reset flag after change
                    }
                }
                //Main.DebugLog($"Updating temporary value for: {field.Name} = {newValue}");
            }

            base.OnChange(field, oldValue, newValue);

        }

        private void ApplyPreset(int presetIndex)
        {
            isApplyingPreset = true;
            Main.DebugLog($"Applying preset: {presetIndex}");

            try
            {
                switch (presetIndex)
                {
                    case 0: ApplyVanillaPreset(); break;
                    case 1: ApplyRealisticPreset(); break;
                    case 2: ApplyBalancedPreset(); break;
                    case 3: LoadCustomSettings(); break; // Load saved "Custom" instance
                }

                RefreshGUI(); // Update UI to reflect preset changes
            }
            finally
            {
                isApplyingPreset = false;
                Main.DebugLog("Preset application complete.");
            }
        }

        private void LoadCustomSettings()
        {
            Main.DebugLog("Loading Custom instance from backup.");
            foreach (var entry in customSettingsBackup)
            {
                var field = GetType().GetField(entry.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(this, entry.Value);
                    Main.DebugLog($"Custom instance loaded: {entry.Key} = {entry.Value}");
                }
            }
        }

        protected override void OnConfirm()
        {
            Main.DebugLog("OnConfirm triggered.");

            // Save instance only if the preset is "Custom"
            if (preset == 3) // 3 = "Custom"
            {
                Main.DebugLog("Saving Custom preset instance to backup.");
                foreach (var field in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    customSettingsBackup[field.Name] = field.GetValue(this);
                    //Main.DebugLog($"Saved Custom instance: {field.Name} = {customSettingsBackup[field.Name]}");
                }
            }

            base.OnConfirm();
            Main.DebugLog("Settings confirmed and saved.");
        }

        private void ApplyVanillaPreset()
        {
            // Set Vanilla Values from the VanillaSettings class
            Main.DebugLog("Applying Vanilla preset.");


            // Global
            this.QuarterWasteSliderGlobal = VanillaSettings.QuarterWasteMultiplier;
            this.MeatTimeSliderGlobal = VanillaSettings.MeatTimeSliderGlobal;
            this.FrozenMeatTimeSliderGlobal = VanillaSettings.FrozenMeatTimeSliderGlobal;
            this.GutTimeSliderGlobal = VanillaSettings.GutTimeSliderGlobal;
            this.MaxHarvestTimeSliderGlobal = VanillaSettings.MaxHarvestTimeSliderGlobal;
            //this.AdjustExistingCarcasses = VanillaSettings.ModifyNativeCarcassesGlobal;
            this.DisableCarcassDecayGlobal = VanillaSettings.DisableCarcassDecayGlobal;

            // Settings
            this.ShowPanelCondition = VanillaSettings.ShowPanelCondition;
            this.ShowPanelConditionColors = VanillaSettings.ShowPanelConditionColors;
            this.AlwaysShowPanelFrozenPercent = VanillaSettings.AlwaysShowPanelFrozenPercent;
            this.ShowPanelFrozenColors = VanillaSettings.ShowPanelFrozenColors;


            // Rabbit
            this.MeatSliderMinRabbit = VanillaSettings.MeatSliderMinRabbit;
            this.MeatSliderMaxRabbit = VanillaSettings.MeatSliderMaxRabbit;
            this.HideCountSliderRabbit = VanillaSettings.HideCountSliderRabbit;
            this.GutCountSliderRabbit = VanillaSettings.GutCountSliderRabbit;
            this.HideTimeSliderRabbit = VanillaSettings.HideTimeSliderRabbit;
            //this.DecayRateMultiplierSliderRabbit = VanillaSettings.DecayRateMultiplierSliderRabbit;

            // Ptarmigan (DLC)
            this.MeatSliderMinPtarmigan = VanillaSettings.MeatSliderMinPtarmigan;
            this.MeatSliderMaxPtarmigan = VanillaSettings.MeatSliderMaxPtarmigan;
            this.HideCountSliderPtarmigan = VanillaSettings.HideCountSliderPtarmigan;
            this.HideTimeSliderPtarmigan = VanillaSettings.HideTimeSliderPtarmigan;
            //this.DecayRateMultiplierSliderPtarmigan = VanillaSettings.DecayRateMultiplierSliderPtarmigan;

            // Doe
            this.MeatSliderMinDoe = VanillaSettings.MeatSliderMinDoe;
            this.MeatSliderMaxDoe = VanillaSettings.MeatSliderMaxDoe;
            this.HideCountSliderDoe = VanillaSettings.HideCountSliderDoe;
            this.GutCountSliderDoe = VanillaSettings.GutCountSliderDoe;
            this.QuarterSizeSliderDoe = VanillaSettings.QuarterSizeSliderDoe;
            this.FatToMeatPercentSliderDoe = VanillaSettings.FatToMeatPercentSliderDoe;
            this.HideTimeSliderDoe = VanillaSettings.HideTimeSliderDoe;
            this.QuarterDurationMinutesSliderDoe = VanillaSettings.QuarterDurationMinutesSliderDoe;
            //this.DecayRateMultiplierSliderDoe = VanillaSettings.DecayRateMultiplierSliderDoe;

            // Stag
            this.MeatSliderMinStag = VanillaSettings.MeatSliderMinStag;
            this.MeatSliderMaxStag = VanillaSettings.MeatSliderMaxStag;
            this.HideCountSliderStag = VanillaSettings.HideCountSliderStag;
            this.GutCountSliderStag = VanillaSettings.GutCountSliderStag;
            this.QuarterSizeSliderStag = VanillaSettings.QuarterSizeSliderStag;
            this.FatToMeatPercentSliderStag = VanillaSettings.FatToMeatPercentSliderStag;
            this.HideTimeSliderStag = VanillaSettings.HideTimeSliderStag;
            this.QuarterDurationMinutesSliderStag = VanillaSettings.QuarterDurationMinutesSliderStag;
            //this.DecayRateMultiplierSliderStag = VanillaSettings.DecayRateMultiplierSliderStag;    

            // Moose
            this.MeatSliderMinMoose = VanillaSettings.MeatSliderMinMoose;
            this.MeatSliderMaxMoose = VanillaSettings.MeatSliderMaxMoose;
            this.HideCountSliderMoose = VanillaSettings.HideCountSliderMoose;
            this.GutCountSliderMoose = VanillaSettings.GutCountSliderMoose;
            this.QuarterSizeSliderMoose = VanillaSettings.QuarterSizeSliderMoose;
            this.FatToMeatPercentSliderMoose = VanillaSettings.FatToMeatPercentSliderMoose;
            this.HideTimeSliderMoose = VanillaSettings.HideTimeSliderMoose;
            this.QuarterDurationMinutesSliderMoose = VanillaSettings.QuarterDurationMinutesSliderMoose;
            //this.DecayRateMultiplierSliderMoose = VanillaSettings.DecayRateMultiplierSliderMoose;

            // Wolf
            this.MeatSliderMinWolf = VanillaSettings.MeatSliderMinWolf;
            this.MeatSliderMaxWolf = VanillaSettings.MeatSliderMaxWolf;
            this.HideCountSliderWolf = VanillaSettings.HideCountSliderWolf;
            this.GutCountSliderWolf = VanillaSettings.GutCountSliderWolf;
            this.QuarterSizeSliderWolf = VanillaSettings.QuarterSizeSliderWolf;
            this.FatToMeatPercentSliderWolf = VanillaSettings.FatToMeatPercentSliderWolf;
            this.HideTimeSliderWolf = VanillaSettings.HideTimeSliderWolf;
            this.QuarterDurationMinutesSliderWolf = VanillaSettings.QuarterDurationMinutesSliderWolf;
            //this.DecayRateMultiplierSliderWolf = VanillaSettings.DecayRateMultiplierSliderWolf;

            // TimberWolf
            this.MeatSliderMinTimberWolf = VanillaSettings.MeatSliderMinTimberWolf;
            this.MeatSliderMaxTimberWolf = VanillaSettings.MeatSliderMaxTimberWolf;
            this.HideCountSliderTimberWolf = VanillaSettings.HideCountSliderTimberWolf;
            this.GutCountSliderTimberWolf = VanillaSettings.GutCountSliderTimberWolf;
            this.QuarterSizeSliderTimberWolf = VanillaSettings.QuarterSizeSliderTimberWolf;
            this.FatToMeatPercentSliderTimberWolf = VanillaSettings.FatToMeatPercentSliderTimberWolf;
            this.HideTimeSliderTimberWolf = VanillaSettings.HideTimeSliderTimberWolf;
            this.QuarterDurationMinutesSliderTimberWolf = VanillaSettings.QuarterDurationMinutesSliderTimberWolf;
            //this.DecayRateMultiplierSliderTimberWolf = VanillaSettings.DecayRateMultiplierSliderTimberWolf;

            // Poisoned Wolf (DLC)
            this.HideCountSliderPoisonedWolf = VanillaSettings.HideCountSliderPoisonedWolf;
            this.GutCountSliderPoisonedWolf = VanillaSettings.GutCountSliderPoisonedWolf;
            this.HideTimeSliderPoisonedWolf = VanillaSettings.HideTimeSliderPoisonedWolf;
            //this.DecayRateMultiplierSliderPoisonedWolf = VanillaSettings.DecayRateMultiplierSliderPoisonedWolf;

            // Bear
            this.MeatSliderMinBear = VanillaSettings.MeatSliderMinBear;
            this.MeatSliderMaxBear = VanillaSettings.MeatSliderMaxBear;
            this.HideCountSliderBear = VanillaSettings.HideCountSliderBear;
            this.GutCountSliderBear = VanillaSettings.GutCountSliderBear;
            this.QuarterSizeSliderBear = VanillaSettings.QuarterSizeSliderBear;
            this.FatToMeatPercentSliderBear = VanillaSettings.FatToMeatPercentSliderBear;
            this.HideTimeSliderBear = VanillaSettings.HideTimeSliderBear;
            this.QuarterDurationMinutesSliderBear = VanillaSettings.QuarterDurationMinutesSliderBear;
            //this.DecayRateMultiplierSliderBear = VanillaSettings.DecayRateMultiplierSliderBear;

            // Cougar
            this.MeatSliderMinCougar = VanillaSettings.MeatSliderMinCougar;
            this.MeatSliderMaxCougar = VanillaSettings.MeatSliderMaxCougar;
            this.HideCountSliderCougar = VanillaSettings.HideCountSliderCougar;
            this.GutCountSliderCougar = VanillaSettings.GutCountSliderCougar;
            this.QuarterSizeSliderCougar = VanillaSettings.QuarterSizeSliderCougar;
            this.FatToMeatPercentSliderCougar = VanillaSettings.FatToMeatPercentSliderCougar;
            this.HideTimeSliderCougar = VanillaSettings.HideTimeSliderCougar;
            this.QuarterDurationMinutesSliderCougar = VanillaSettings.QuarterDurationMinutesSliderCougar;
            //this.DecayRateMultiplierSliderCougar = VanillaSettings.DecayRateMultiplierSliderCougar;
        }

        private void ApplyRealisticPreset()
        {
            // Realistic Preset - Meat values are based on data from Canadian encyclopedia (see DATA.xlsx)
            Main.DebugLog("Applying Realistic preset.");

            // Global
            this.QuarterWasteSliderGlobal = 1.2f; // Less waste
            this.MeatTimeSliderGlobal = 1f; // Unchanged
            this.FrozenMeatTimeSliderGlobal = 1f; // Unchanged
            this.GutTimeSliderGlobal = 1f; // Unchanged

            // Rabbit
            this.MeatSliderMinRabbit = 0.75f;
            this.MeatSliderMaxRabbit = 1.5f;
            this.HideCountSliderRabbit = 1;
            this.GutCountSliderRabbit = 2;
            this.HideTimeSliderRabbit = 0.13f; // A rabbit can be skinned in less than a minute

            // Ptarmigan (DLC)
            this.MeatSliderMinPtarmigan = 0.43f;
            this.MeatSliderMaxPtarmigan = 0.81f;
            this.HideCountSliderPtarmigan = 4;
            this.HideTimeSliderPtarmigan = 0.25f; // A Ptarmigan can be plucked in less than 30 minutes

            // Doe
            this.MeatSliderMinDoe = 16f;
            this.MeatSliderMaxDoe = 36f;
            this.HideCountSliderDoe = 1;
            this.GutCountSliderDoe = 12;
            this.QuarterSizeSliderDoe = 10f;
            this.FatToMeatPercentSliderDoe = 3; //Doe are very lean
            this.HideTimeSliderDoe = 0.75f; // Realistic time for processing a doe hide
            this.QuarterDurationMinutesSliderDoe = 30;

            //// Stag
            this.MeatSliderMinStag = 38f;
            this.MeatSliderMaxStag = 57f;
            this.HideCountSliderStag = 1;
            this.GutCountSliderStag = 15;
            this.QuarterSizeSliderStag = 15f;
            this.FatToMeatPercentSliderStag = 4; // Stags have a bit more fat
            this.HideTimeSliderStag = 1.125f; // Realistic time for processing a stag hide
            this.QuarterDurationMinutesSliderStag = 60;

            // Moose
            this.MeatSliderMinMoose = 121f;
            this.MeatSliderMaxMoose = 270f;
            this.HideCountSliderMoose = 1;
            this.GutCountSliderMoose = 40;
            this.QuarterSizeSliderMoose = 30f;
            this.FatToMeatPercentSliderMoose = 5;
            this.HideTimeSliderMoose = 2.25f; // Realistic time for processing a moose hide
            this.QuarterDurationMinutesSliderMoose = 150;

            // Wolf
            this.MeatSliderMinWolf = 7f;
            this.MeatSliderMaxWolf = 26f;
            this.HideCountSliderWolf = 1;
            this.GutCountSliderWolf = 6;
            this.QuarterSizeSliderWolf = 7f;
            this.FatToMeatPercentSliderWolf = 2;
            this.HideTimeSliderWolf = 0.625f; // Realistic time for processing a wolf hide
            this.QuarterDurationMinutesSliderWolf = 20;

            // TimberWolf
            this.MeatSliderMinTimberWolf = 9f; // Larger minimum meat yield due to increased size.
            this.MeatSliderMaxTimberWolf = 32f; // Higher maximum meat yield for a larger wolf.
            this.HideCountSliderTimberWolf = 1; // Still yields only 1 hide.
            this.GutCountSliderTimberWolf = 8; // More guts due to its larger body size.
            this.QuarterSizeSliderTimberWolf = 9f; // Larger quarters for a bigger wolf.
            this.FatToMeatPercentSliderTimberWolf = 3; // Slightly higher fat-to-meat ratio than regular wolves.
            this.HideTimeSliderTimberWolf = 0.75f; // Slightly longer hide processing time due to size.
            this.QuarterDurationMinutesSliderTimberWolf = 30; // Longer quartering time than regular wolves.

            // Poisoned Wolf (DLC)
            this.HideCountSliderPoisonedWolf = 1; // Default value
            this.GutCountSliderPoisonedWolf = 2; // Default value

            // Bear
            this.MeatSliderMinBear = 16f;
            this.MeatSliderMaxBear = 135f;
            this.HideCountSliderBear = 1;
            this.GutCountSliderBear = 25;
            this.QuarterSizeSliderBear = 25f;
            this.FatToMeatPercentSliderBear = 25;
            this.HideTimeSliderBear = 2.25f; // Realistic time for processing a bear hide
            this.QuarterDurationMinutesSliderBear = 180;

            // Cougar (DLC)
            this.MeatSliderMinCougar = 13f;
            this.MeatSliderMaxCougar = 54f;
            this.HideCountSliderCougar = 1;
            this.GutCountSliderCougar = 6;
            this.QuarterSizeSliderCougar = 18f;
            this.FatToMeatPercentSliderCougar = 4;
            this.HideTimeSliderCougar = 0.75f; // Realistic time for processing a cougar hide
            this.QuarterDurationMinutesSliderCougar = 60;

        }

        private void ApplyBalancedPreset()
        {
            // Realistic (Balanced) Preset - Meat values are based on data from Canadian encyclopedia (see DATA.xlsx)
            Main.DebugLog("Applying Balanced preset.");
            // Rabbit
            this.MeatSliderMinRabbit = 0.75f; // Realistic unchanged
            this.MeatSliderMaxRabbit = 1.5f; // Realistic unchanged
            this.HideCountSliderRabbit = 1;
            this.GutCountSliderRabbit = 2; // Realistic unchanged
            this.HideTimeSliderRabbit = 0.25f; // Doubled Realistic time for rabbit hide processing

            // Ptarmigan (DLC)
            this.MeatSliderMinPtarmigan = 0.43f; // Realistic unchanged
            this.MeatSliderMaxPtarmigan = 0.81f; // Realistic unchanged
            this.HideCountSliderPtarmigan = 4;
            this.HideTimeSliderPtarmigan = 0.50f; // Doubled Realistic time for ptarmigan feather plucking

            // Doe
            this.MeatSliderMinDoe = 11f; // Realistic -33%
            this.MeatSliderMaxDoe = 18f; // Realistic -50%
            this.HideCountSliderDoe = 1;
            this.GutCountSliderDoe = 3; // Arbitrary value
            this.QuarterSizeSliderDoe = 6f; // Arbitrary value
            this.FatToMeatPercentSliderDoe = 6;
            this.HideTimeSliderDoe = 0.75f; // Realistic time for processing a doe hide
            this.QuarterDurationMinutesSliderDoe = 30;

            //// Stag
            this.MeatSliderMinStag = 25f; // Realistic -33%
            this.MeatSliderMaxStag = 37f; // Realistic -50%
            this.HideCountSliderStag = 1;
            this.GutCountSliderStag = 5; // Arbitrary value
            this.QuarterSizeSliderStag = 8f; // Arbitrary value
            this.FatToMeatPercentSliderStag = 8;
            this.HideTimeSliderStag = 1f; // faster Realistic time for processing a stag hide
            this.QuarterDurationMinutesSliderStag = 70;

            // Moose
            this.MeatSliderMinMoose = 80f; // Realistic -33%
            this.MeatSliderMaxMoose = 135f; // Realistic -50%
            this.HideCountSliderMoose = 1;
            this.GutCountSliderMoose = 16; // Arbitrary value
            this.QuarterSizeSliderMoose = 20f; // Arbitrary value
            this.FatToMeatPercentSliderMoose = 15;
            this.HideTimeSliderMoose = 1.5f; // faster Realistic time for processing a moose hide
            this.QuarterDurationMinutesSliderMoose = 120;

            // Wolf
            this.MeatSliderMinWolf = 5f; // Realistic -33%
            this.MeatSliderMaxWolf = 13f; // Realistic -50%
            this.HideCountSliderWolf = 1;
            this.GutCountSliderWolf = 2; // Arbitrary value
            this.QuarterSizeSliderWolf = 5f; // Arbitrary value
            this.FatToMeatPercentSliderWolf = 4;
            this.HideTimeSliderWolf = 0.75f; // slower Realistic time for processing a wolf hide
            this.QuarterDurationMinutesSliderWolf = 40;


            // TimberWolf
            this.MeatSliderMinTimberWolf = 7f; // smaller Larger minimum meat yield due to increased size.
            this.MeatSliderMaxTimberWolf = 19f; // smaller Higher maximum meat yield for a larger wolf.
            this.HideCountSliderTimberWolf = 1; // Still yields only 1 hide.
            this.GutCountSliderTimberWolf = 7; // More guts due to its larger body size.
            this.QuarterSizeSliderTimberWolf = 8f; // Larger quarters for a bigger wolf.
            this.FatToMeatPercentSliderTimberWolf = 3; // Slightly higher fat-to-meat ratio than regular wolves.
            this.HideTimeSliderTimberWolf = 0.90f; // Slightly longer hide processing time due to size.
            this.QuarterDurationMinutesSliderTimberWolf = 45; // Longer quartering time than regular wolves.

            // Poisoned Wolf (DLC)
            this.HideCountSliderPoisonedWolf = 1; // Default value
            this.GutCountSliderPoisonedWolf = 2; // Default value

            // Bear
            this.MeatSliderMinBear = 16f; // Realistic unchanged
            this.MeatSliderMaxBear = 68f; // Realistic -50%
            this.HideCountSliderBear = 1;
            this.GutCountSliderBear = 12; // Vanilla value 12
            this.QuarterSizeSliderBear = 15f; // Realistic -10
            this.FatToMeatPercentSliderBear = 10;
            this.HideTimeSliderBear = 1.5f; // Realistic time for processing a bear hide
            this.QuarterDurationMinutesSliderBear = 120;

            // Cougar (DLC)
            this.MeatSliderMinCougar = 8f; // Realistic -33%
            this.MeatSliderMaxCougar = 27f; // Realistic -50%
            this.HideCountSliderCougar = 1;
            this.GutCountSliderCougar = 5; // Arbitrary value
            this.QuarterSizeSliderCougar = 7f; // Arbitrary value
            this.FatToMeatPercentSliderCougar = 10;
            this.HideTimeSliderCougar = 0.90f; // Realistic time for processing a cougar hide   
            this.QuarterDurationMinutesSliderCougar = 90;

            // Quarter Waste Multiplier
            this.QuarterWasteSliderGlobal = 1.2f;
        }

        [Section("Presets")]

        [Name("Select a Preset")]
        [Description("Choose a preset to apply instance. Modifying other instance will switch this to 'Custom'.")]
        [Choice(new string[] { "Vanilla", "Realistic", "Balanced", "Custom" })]
        public int preset = 0;

        [Name("Quarter Waste Weight Multiplier")]
        [Description("Changes the amount of unharvestable waste in quarters. Vanilla value is 2 which means your quarters weigh twice as much as the meat you'll get from them.")]
        [Slider(0.5f, 4.00f, NumberFormat = "{0:F1} x")]
        public float QuarterWasteSliderGlobal = VanillaSettings.QuarterWasteMultiplier;

        [Name("Maximum Harvest Time")]
        [Description("Maximum time allowed in hours to harvest meat from a carcass. Vanilla value is 5 hours.")]
        [Slider(1f, 24f, NumberFormat = "{0:F1} hrs.")]
        public float MaxHarvestTimeSliderGlobal = VanillaSettings.MaxHarvestTimeSliderGlobal;


        [Section("Rabbit")]

        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed Rabbit. Vanilla value is 0.75")]
        [Slider(0f, 5f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMinRabbit = VanillaSettings.MeatSliderMinRabbit;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed Rabbit. Vanilla value is 1.5")]
        [Slider(0f, 5f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMaxRabbit = VanillaSettings.MeatSliderMaxRabbit;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Rabbit. Vanilla value is 1")]
        [Slider(0, 3)]
        public int HideCountSliderRabbit = VanillaSettings.HideCountSliderRabbit;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Rabbit. Vanilla value is 1")]
        [Slider(0, 10)]
        public int GutCountSliderRabbit = VanillaSettings.GutCountSliderRabbit;



        [Section("Ptarmigan (DLC)")]

        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed Ptarmigan. Vanilla value is 0.75")]
        [Slider(0f, 5f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMinPtarmigan = VanillaSettings.MeatSliderMinPtarmigan;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed Ptarmigan. Vanilla value is 1.5")]
        [Slider(0.1f, 5f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMaxPtarmigan = VanillaSettings.MeatSliderMaxPtarmigan;

        [Name("Down Feather Count")]
        [Description("Number of harvestable down feathers from a Ptarmigan. Vanilla value is 4")]
        [Slider(0, 12)]
        public int HideCountSliderPtarmigan = VanillaSettings.HideCountSliderPtarmigan;



        [Section("Doe")]

        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed Doe. Vanilla value is 7")]
        [Slider(0f, 100f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMinDoe = VanillaSettings.MeatSliderMinDoe;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed Doe. Vanilla value is 9")]
        [Slider(0f, 100f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMaxDoe = VanillaSettings.MeatSliderMaxDoe;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Doe. Vanilla value is 1")]
        [Slider(0, 4)]
        public int HideCountSliderDoe = VanillaSettings.HideCountSliderDoe;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Doe. Vanilla value is 2")]
        [Slider(0, 20)]
        public int GutCountSliderDoe = VanillaSettings.GutCountSliderDoe;

        [Name("Quarter Size")]
        [Description("Size of each quarter in Kg from a Doe. Vanilla value is 2.5")]
        [Slider(1f, 50f, NumberFormat = "{0:F1} Kg")]
        public float QuarterSizeSliderDoe = VanillaSettings.QuarterSizeSliderDoe;

        [Name("Fat to Meat Percentage (%)")]
        [Description("Fat to meat percentage for a Doe. Vanilla value is 20%")]
        [Slider(0, 40, NumberFormat = "{0:#} %")]
        public int FatToMeatPercentSliderDoe = VanillaSettings.FatToMeatPercentSliderDoe;



        [Section("Stag")]

        [Name("Minimum Meat")]
        [Description("Minimum amount of harvestable meat in Kg from a Stag. Vanilla value is 11")]
        [Slider(0f, 150f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMinStag = VanillaSettings.MeatSliderMinStag;

        [Name("Maximum Meat")]
        [Description("Maximum amount of harvestable meat in Kg from a Stag. Vanilla value is 13")]
        [Slider(0f, 150f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMaxStag = VanillaSettings.MeatSliderMaxStag;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Stag. Vanilla value is 1")]
        [Slider(0, 5)]
        public int HideCountSliderStag = VanillaSettings.HideCountSliderStag;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Stag. Vanilla value is 2")]
        [Slider(0, 20)]
        public int GutCountSliderStag = VanillaSettings.GutCountSliderStag;

        [Name("Quarter Size")]
        [Description("Size of each quarter in Kg from a Stag. Vanilla value is 2.5")]
        [Slider(1f, 50f, NumberFormat = "{0:F1} Kg")]
        public float QuarterSizeSliderStag = VanillaSettings.QuarterSizeSliderStag;

        [Name("Fat to Meat Percentage (%)")]
        [Description("Fat to meat percentage for a Stag. Vanilla value is 20%")]
        [Slider(0, 40, NumberFormat = "{0:#} %")]
        public int FatToMeatPercentSliderStag = VanillaSettings.FatToMeatPercentSliderStag;



        [Section("Moose")]

        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed Moose. Vanilla value is 30")]
        [Slider(0f, 600f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMinMoose = VanillaSettings.MeatSliderMinMoose;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed Moose. Vanilla value is 45")]
        [Slider(0f, 600f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMaxMoose = VanillaSettings.MeatSliderMaxMoose;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Moose. Vanilla value is 1")]
        [Slider(0, 4)]
        public int HideCountSliderMoose = VanillaSettings.HideCountSliderMoose;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Moose. Vanilla value is 12")]
        [Slider(0, 48)]
        public int GutCountSliderMoose = VanillaSettings.GutCountSliderMoose;

        [Name("Quarter Size")]
        [Description("Size of each quarter in Kg from a Moose. Vanilla value is 5")]
        [Slider(1f, 50f, NumberFormat = "{0:F1} Kg")]
        public float QuarterSizeSliderMoose = VanillaSettings.QuarterSizeSliderMoose;

        [Name("Fat to Meat Percentage (%)")]
        [Description("Fat to meat percentage for a Moose. Vanilla value is 15%")]
        [Slider(0, 40, NumberFormat = "{0:#} %")]
        public int FatToMeatPercentSliderMoose = VanillaSettings.FatToMeatPercentSliderMoose;




        [Section("Wolf")]

        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed Wolf. Vanilla value is 3")]
        [Slider(0f, 50f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMinWolf = VanillaSettings.MeatSliderMinWolf;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed Wolf. Vanilla value is 6")]
        [Slider(0f, 50f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMaxWolf = VanillaSettings.MeatSliderMaxWolf;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Wolf. Vanilla value is 1")]
        [Slider(0, 2)]
        public int HideCountSliderWolf = VanillaSettings.HideCountSliderWolf;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Wolf. Vanilla value is 2")]
        [Slider(0, 20)]
        public int GutCountSliderWolf = VanillaSettings.GutCountSliderWolf;

        [Name("Quarter Size")]
        [Description("Size of each Quarter from a Wolf. Vanilla value is 2.5")]
        [Slider(1f, 50f, NumberFormat = "{0:F1} Kg")]
        public float QuarterSizeSliderWolf = VanillaSettings.QuarterSizeSliderWolf;

        [Name("Fat to Meat Percentage (%)")]
        [Description("Fat to meat percentage for a Wolf. Vanilla value is 10%")]
        [Slider(0, 40, NumberFormat = "{0:#} %")]
        public int FatToMeatPercentSliderWolf = VanillaSettings.FatToMeatPercentSliderWolf;



        [Section("TimberWolf")]

        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed TimberWolf. Vanilla value is 4")]
        [Slider(0f, 70f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMinTimberWolf = VanillaSettings.MeatSliderMinTimberWolf;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed TimberWolf. Vanilla value is 7")]
        [Slider(0f, 70f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMaxTimberWolf = VanillaSettings.MeatSliderMaxTimberWolf;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed TimberWolf. Vanilla value is 1")]
        [Slider(0, 3)]
        public int HideCountSliderTimberWolf = VanillaSettings.HideCountSliderTimberWolf;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed TimberWolf. Vanilla value is 2")]
        [Slider(0, 20)]
        public int GutCountSliderTimberWolf = VanillaSettings.GutCountSliderTimberWolf;

        [Name("Quarter Size")]
        [Description("Size of each Quarter from a TimberWolf. Vanilla value is 2.5")]
        [Slider(1f, 50f, NumberFormat = "{0:F1} Kg")]
        public float QuarterSizeSliderTimberWolf = VanillaSettings.QuarterSizeSliderTimberWolf;

        [Name("Fat to Meat Percentage (%)")]
        [Description("Fat to meat percentage for a TimberWolf. Vanilla value is 10%")]
        [Slider(0, 40, NumberFormat = "{0:#} %")]
        public int FatToMeatPercentSliderTimberWolf = VanillaSettings.FatToMeatPercentSliderTimberWolf;



        [Section("Poisoned Wolf (DLC)")]

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Poisoned Wolf. Vanilla value is 1")]
        [Slider(0, 2)]
        public int HideCountSliderPoisonedWolf = VanillaSettings.HideCountSliderPoisonedWolf;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Poisoned Wolf. Vanilla value is 2")]
        [Slider(0, 10)]
        public int GutCountSliderPoisonedWolf = VanillaSettings.GutCountSliderPoisonedWolf;



        [Section("Bear")]

        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed Bear. Vanilla value is 25")]
        [Slider(0f, 300f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMinBear = VanillaSettings.MeatSliderMinBear;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed Bear. Vanilla value is 40")]
        [Slider(0f, 300f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMaxBear = VanillaSettings.MeatSliderMaxBear;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Bear. Vanilla value is 1")]
        [Slider(0, 3)]
        public int HideCountSliderBear = VanillaSettings.HideCountSliderBear;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Bear. Vanilla value is 10")]
        [Slider(0, 40)]
        public int GutCountSliderBear = VanillaSettings.GutCountSliderBear;

        [Name("Quarter Size")]
        [Description("Size of each Quarter from a Bear. Vanilla value is 5")]
        [Slider(1f, 50f, NumberFormat = "{0:F1} Kg")]
        public float QuarterSizeSliderBear = VanillaSettings.QuarterSizeSliderBear;

        [Name("Fat to Meat Percentage (%)")]
        [Description("Fat to meat percentage for a Bear. Vanilla value is 10%")]
        [Slider(0, 40, NumberFormat = "{0:#} %")]
        public int FatToMeatPercentSliderBear = VanillaSettings.FatToMeatPercentSliderBear;



        [Section("Cougar (DLC)")]

        [Name("Minimum Meat")]
        [Description("Minimum meat from a freshly killed Cougar. Vanilla value is 4")]
        [Slider(0f, 100f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMinCougar = VanillaSettings.MeatSliderMinCougar;

        [Name("Maximum Meat")]
        [Description("Maximum meat from a freshly killed Cougar. Vanilla value is 5")]
        [Slider(0f, 100f, NumberFormat = "{0:F1} Kg")]
        public float MeatSliderMaxCougar = VanillaSettings.MeatSliderMaxCougar;

        [Name("Hide Count")]
        [Description("Hides from a freshly killed Cougar. Vanilla value is 1")]
        [Slider(0, 2)]
        public int HideCountSliderCougar = VanillaSettings.HideCountSliderCougar;

        [Name("Gut Count")]
        [Description("Guts from a freshly killed Cougar. Vanilla value is 2")]
        [Slider(0, 50)]
        public int GutCountSliderCougar = VanillaSettings.GutCountSliderCougar;

        [Name("Quarter Size")]
        [Description("Size of each Quarter from a Cougar. Vanilla value is 2.5")]
        [Slider(1f, 50f, NumberFormat = "{0:F1} Kg")]
        public float QuarterSizeSliderCougar = VanillaSettings.QuarterSizeSliderCougar;

        [Name("Fat to Meat Percentage (%)")]
        [Description("Fat to meat percentage for a Cougar. Vanilla value is 10%")]
        [Slider(0, 40, NumberFormat = "{0:#} %")]
        public int FatToMeatPercentSliderCougar = VanillaSettings.FatToMeatPercentSliderCougar;



        // Description values taken from https://thelongdark.fandom.com/wiki/Carcass_Harvesting 2024-12-22
        [Section("Global Harvest Times")]

        [Name("Meat (Thawed Carcass)")]
        [Description("Global Meat harvest time multiplier. Vanilla value is 1.\n" +
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
        public float MeatTimeSliderGlobal = VanillaSettings.MeatTimeSliderGlobal;

        [Name("Meat (Frozen Carcass)")]
        [Description("Global Frozen Meat harvest time multiplier. Vanilla value is 1.\n" +
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
        public float FrozenMeatTimeSliderGlobal = VanillaSettings.FrozenMeatTimeSliderGlobal;

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
        public float GutTimeSliderGlobal = VanillaSettings.GutTimeSliderGlobal;

        [Section("Hide/Feathers Harvest Times")]
        
        [Name("Rabbit")]
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
        public float HideTimeSliderRabbit = VanillaSettings.HideTimeSliderRabbit;

        [Name("Ptarmigan (DLC)")]
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
        public float HideTimeSliderPtarmigan = VanillaSettings.HideTimeSliderPtarmigan;

        [Name("Doe")]
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
        public float HideTimeSliderDoe = VanillaSettings.HideTimeSliderDoe;

        [Name("Stag")]
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
        public float HideTimeSliderStag = VanillaSettings.HideTimeSliderStag;

        [Name("Moose")]
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
        public float HideTimeSliderMoose = VanillaSettings.HideTimeSliderMoose;

        [Name("Wolf")]
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
        public float HideTimeSliderWolf = VanillaSettings.HideTimeSliderWolf;

        [Name("TimberWolf")]
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
        public float HideTimeSliderTimberWolf = VanillaSettings.HideTimeSliderTimberWolf;

        [Name("Poisoned Wolf (DLC)")]
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
        [Slider(0.01f, 2.0f, NumberFormat = "{0:F2}x")]
        public float HideTimeSliderPoisonedWolf = VanillaSettings.HideTimeSliderPoisonedWolf;

        [Name("Bear")]
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
        [Slider(0.01f, 2.0f, NumberFormat = "{0:F2}x")]
        public float HideTimeSliderBear = VanillaSettings.HideTimeSliderBear;

        [Name("Cougar (DLC)")]
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
        [Slider(0.01f, 2.0f, NumberFormat = "{0:F2}x")]
        public float HideTimeSliderCougar = VanillaSettings.HideTimeSliderCougar;



        [Section("Quartering Times")]

        [Name("Doe")]
        [Description("Time to quarter a Doe. Vanilla value is 60m")]
        [Slider(1, 180, NumberFormat = "{0:#}m")]
        public int QuarterDurationMinutesSliderDoe = VanillaSettings.QuarterDurationMinutesSliderDoe;

        [Name("Stag")]
        [Description("Time to quarter a Stag. Vanilla value is 75m")]
        [Slider(1, 180, NumberFormat = "{0:#}m")]
        public int QuarterDurationMinutesSliderStag = VanillaSettings.QuarterDurationMinutesSliderStag;

        [Name("Moose")]
        [Description("Time to quarter a Moose. Vanilla value is 120m")]
        [Slider(1, 180, NumberFormat = "{0:#}m")]
        public int QuarterDurationMinutesSliderMoose = VanillaSettings.QuarterDurationMinutesSliderMoose;

        [Name("Wolf")]
        [Description("Time to quarter a Wolf. Vanilla value is 60m")]
        [Slider(1, 180, NumberFormat = "{0:#}m")]
        public int QuarterDurationMinutesSliderWolf = VanillaSettings.QuarterDurationMinutesSliderWolf;

        [Name("TimberWolf")]
        [Description("Time to quarter a TimberWolf. Vanilla value is 60m")]
        [Slider(1, 180, NumberFormat = "{0:#}m")]
        public int QuarterDurationMinutesSliderTimberWolf = VanillaSettings.QuarterDurationMinutesSliderTimberWolf;

        [Name("Bear")]
        [Description("Time to quarter a Bear. Vanilla value is 120m")]
        [Slider(1, 180, NumberFormat = "{0:#}m")]
        public int QuarterDurationMinutesSliderBear = VanillaSettings.QuarterDurationMinutesSliderBear;

        [Name("Cougar (DLC)")]
        [Description("Time to quarter a Cougar. Vanilla value is 120m")]
        [Slider(1, 180, NumberFormat = "{0:#}m")]
        public int QuarterDurationMinutesSliderCougar = VanillaSettings.QuarterDurationMinutesSliderCougar;


        [Section("Carcass Decay")]

        [Name("Disable Carcass Decay")]
        [Description("Completely disable the decay of animal carcasses.")]
        public bool DisableCarcassDecayGlobal = false;

        //[Name("Adjust Existing Carcasses")]
        //[Description("Adjust the decay rate for game generated animal carcasses which were already dead. \n" +
        //    "(Uses per animal settings)")]
        //public bool AdjustExistingCarcasses = false;

        //[Name("Rabbit")]
        //[Description("Decay rate multiplier for a Rabbit. Vanilla value is 1.\n" +
        //    "Normal daily decay rate is 10%")]
        //[Slider(0.01f, 3f, NumberFormat = "{0:F2}x")]
        //public float DecayRateMultiplierSliderRabbit = VanillaSettings.DecayRateMultiplierSliderRabbit;

        //[Name("Ptarmigan")]
        //[Description("Decay rate multiplier for a Ptarmigan. Vanilla value is 1.\n" +
        //    "Normal daily decay rate is 10%")]
        //[Slider(0.01f, 3f, NumberFormat = "{0:F2}x")]
        //public float DecayRateMultiplierSliderPtarmigan = VanillaSettings.DecayRateMultiplierSliderPtarmigan;

        //[Name("Doe")]
        //[Description("Decay rate multiplier for a Doe. Vanilla value is 1.\n" +
        //    "Normal daily decay rate is 33.3%.")]
        //[Slider(0.01f, 3f, NumberFormat = "{0:F2}x")]
        //public float DecayRateMultiplierSliderDoe = VanillaSettings.DecayRateMultiplierSliderDoe;

        //[Name("Stag")]
        //[Description("Decay rate multiplier for a Stag. Vanilla value is 1.\n" +
        //    "Normal daily decay rate is 33.3%.")]
        //[Slider(0.01f, 3f, NumberFormat = "{0:F2}x")]
        //public float DecayRateMultiplierSliderStag = VanillaSettings.DecayRateMultiplierSliderStag;

        //[Name("Moose")]
        //[Description("Decay rate multiplier for a Moose. Vanilla value is 1.\n" +
        //    "Normal daily decay rate is 33.3%.")]
        //[Slider(0.01f, 3f, NumberFormat = "{0:F2}x")]
        //public float DecayRateMultiplierSliderMoose = VanillaSettings.DecayRateMultiplierSliderMoose;

        //[Name("Wolf")]
        //[Description("Decay rate multiplier for a Wolf. Vanilla value is 1.\n" +
        //    "Normal daily decay rate is 33.3%.")]
        //[Slider(0.01f, 3f, NumberFormat = "{0:F2}x")]
        //public float DecayRateMultiplierSliderWolf = VanillaSettings.DecayRateMultiplierSliderWolf;

        //[Name("Timberwolf")]
        //[Description("Decay rate multiplier for a TimberWolf. Vanilla value is 1.\n" +
        //    "Normal daily decay rate is 33.3%.")]
        //[Slider(0.01f, 3f, NumberFormat = "{0:F2}x")]
        //public float DecayRateMultiplierSliderTimberWolf = VanillaSettings.DecayRateMultiplierSliderTimberWolf;

        //[Name("Poisoned Wolf")]
        //[Description("Decay rate multiplier for a Poisoned Wolf. Vanilla value is 1.\n" +
        //    "Normal daily decay rate is 33.3%.")]
        //[Slider(0.01f, 3f, NumberFormat = "{0:F2}x")]
        //public float DecayRateMultiplierSliderPoisonedWolf = VanillaSettings.DecayRateMultiplierSliderPoisonedWolf;

        //[Name("Bear")]
        //[Description("Decay rate multiplier for a Bear. Vanilla value is 1.\n" +
        //    "Normal daily decay rate is 33.3%.")]
        //[Slider(0.01f, 3f, NumberFormat = "{0:F2}x")]
        //public float DecayRateMultiplierSliderBear = VanillaSettings.DecayRateMultiplierSliderBear;

        //[Name("Cougar")]
        //[Description("Decay rate multiplier for a Cougar. Vanilla value is 1.\n" +
        //    "Normal daily decay rate is 33.3%.")]
        //[Slider(0.01f, 3f, NumberFormat = "{0:F2}x")]
        //public float DecayRateMultiplierSliderCougar = VanillaSettings.DecayRateMultiplierSliderCougar;


        [Section("Harvest Panel Settings")]

        [Name("Show Condition Percent")]
        [Description("Show the condition of the carcass in the harvest Panel.")]
        public bool ShowPanelCondition = false;

        [Name("Condition Text Color")]
        [Description("Color the condition text according to the carcass condition percentage.\n" +
            "\n100% to 66% - Green" +
            "\n 66% to 33% - Yellow" +
            "\n 33% to  1% - Red")]
        public bool ShowPanelConditionColors = false;

        [Name("Always Show Frozen Percent")]
        [Description("Always show the frozen percentage in the harvest Panel, even if the carcass is not frozen.")]
        public bool AlwaysShowPanelFrozenPercent = false;

        [Name("Frozen Text Color")]
        [Description("Color the frozen text according to the carcass frozen percentage.\n" +
            "\n  0% to  25% - Orange - Warm" +
            "\n 25% to  50% - White - Cold" +
            "\n 50% to  75% - Cyan - Frozen" +
            "\n 75% to 100% - Blue - Frozen Solid")]
        public bool ShowPanelFrozenColors = false;


    }

    // Define Vanilla Settings (only once) - only the descriptions will need updated if something changes
    internal static class VanillaSettings
    {
        // Global
        internal static float QuarterWasteMultiplier = 2.0f;
        internal static float MeatTimeSliderGlobal = 1f;
        internal static float FrozenMeatTimeSliderGlobal = 1f;
        internal static float GutTimeSliderGlobal = 1f;
        //internal static float DecayRateMultiplierSliderGlobal = 1f;
        internal static float MaxHarvestTimeSliderGlobal = 5f;
        internal static bool ModifyNativeCarcassesGlobal = false;
        internal static bool DisableCarcassDecayGlobal = false;

        // Settings
        internal static bool AlwaysShowPanelFrozenPercent = false;
        internal static bool ShowPanelFrozenColors = false;
        internal static bool ShowPanelCondition = false;
        internal static bool ShowPanelConditionColors = false;

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
        internal static float MeatSliderMinWolf = 3f;
        internal static float MeatSliderMaxWolf = 6f;
        internal static int HideCountSliderWolf = 1;
        internal static int GutCountSliderWolf = 2;
        internal static float QuarterSizeSliderWolf = 2.5f;
        internal static int FatToMeatPercentSliderWolf = 10;
        internal static float HideTimeSliderWolf = 1f;
        internal static int QuarterDurationMinutesSliderWolf = 60;
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

    }
}
