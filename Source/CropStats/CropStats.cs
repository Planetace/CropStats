
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;


namespace CropStats
{
    [StaticConstructorOnStartup]
    public static class CropStats
    {
        static CropStats() {
            try
            {
                var harmony = new Harmony("planetace.croptats");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                Log.Message(CropStatsMod.logID + "Patches have been successfully applied.");
            }
            catch (Exception e)
            {
                Log.Error(CropStatsMod.logID + "Patches have failed to initialise due to: " + e);
            }
        }
    }

    // Stores references to all plants and valid animals in-game for quick hyperlink generation and other data management.
    public static class CropStatsCache {

        public static List<ThingDef> cachedPlantList    = new List<ThingDef>();

        public static List<ThingDef> cachedThrumbolike  = new List<ThingDef>();
        public static List<ThingDef> cachedCowlike      = new List<ThingDef>();
        public static List<ThingDef> cachedGoatlike     = new List<ThingDef>();
        public static List<ThingDef> cachedChickenlike  = new List<ThingDef>();

        static CropStatsCache()
        {
            GenerateHyperlinks();
            CachePlantResults();
        }

        private static void CachePlantResults()
        {
            foreach(ThingDef plant in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (plant.plant != null) cachedPlantList.Add(plant);
            }
        }

        // Dynamically generate hyperlink lists for cow, goat, and chicken equivilents.
        private static void GenerateHyperlinks()
        {
            foreach (ThingDef animal in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (animal.race?.Animal == true && !animal.IsCorpse)
                {
                    // Determines if an animal comes from an Official DLC and ignores them if the content pack is disabled.
                    // Unlike community mods, all of Rimworlds animals are defined in-code in some way, this guarantees we don't accidentally link a DLC animal when the player has it disabled.
                    if (animal.modContentPack != null && !animal.modContentPack.IsCoreMod && animal.modContentPack.IsOfficialMod)
                    {
                        switch (animal.modContentPack.PackageId)
                        {
                            case ModContentPack.RoyaltyModPackageId:
                                if (!ModsConfig.RoyaltyActive) continue;
                                break;

                            case ModContentPack.IdeologyModPackageId:
                                if (!ModsConfig.IdeologyActive) continue;
                                break;

                            case ModContentPack.BiotechModPackageId:
                                if (!ModsConfig.BiotechActive) continue;
                                break;

                            case ModContentPack.AnomalyModPackageId:
                                if (!ModsConfig.AnomalyActive) continue;
                                break;

                            case ModContentPack.OdysseyModPackageId:
                                if (!ModsConfig.OdysseyActive) continue;
                                break;

                            // Until there is a new DLC announced and 1.7 unstable, this should hopefully NEVER be hit.
                            default:
                                break;
                        }
                    }

                    float hungerRate = Mathf.Round(animal.race.baseHungerRate * 1.6f * 100f) / 100f;

                    if (hungerRate >= 1.83f)
                    {
                        cachedThrumbolike.Add(animal);
                    }
                    else if (hungerRate < 1.83f && hungerRate >= 0.61f)
                    {
                        cachedCowlike.Add(animal);
                    }
                    else if (hungerRate < 0.61f && hungerRate >= 0.29f)
                    {
                        cachedGoatlike.Add(animal);
                    }
                    else
                    {
                        cachedChickenlike.Add(animal);
                    }
                }
            }
        }
    }


    public class CropStatsMod : Mod
    {
        public const string logID = "[Crop Stats] ";

        CropStatsSettings modSettings;

        public CropStatsMod(ModContentPack content) : base(content)
        {
            this.modSettings = GetSettings<CropStatsSettings>();
        }


        public override void DoSettingsWindowContents(Rect inRect)
        {

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);


            // Expanded Plant Information
            Text.Font = GameFont.Medium;
            listing.Label("settings_ExpandedCropStatsLabel".Translate());
            Text.Font = GameFont.Small;
            listing.GapLine(8);

            listing.CheckboxLabeled("setting_TrueGrowingTime".Translate(), ref modSettings.setting_TrueGrowingTime, "setting_TrueGrowingTimeDesc".Translate());
            listing.CheckboxLabeled("setting_DailyHarvest".Translate(), ref modSettings.setting_DailyHarvest, "setting_DailyHarvestDesc".Translate());
            listing.CheckboxLabeled("setting_DailyCash".Translate(), ref modSettings.setting_DailyCash, "setting_DailyCashDesc".Translate());
            listing.CheckboxLabeled("setting_DailyNaturalNutrition".Translate(), ref modSettings.setting_DailyNaturalNutrition, "setting_DailyNaturalNutritionDesc".Translate());
            listing.CheckboxLabeled("setting_DailyHarvestedNutrition".Translate(), ref modSettings.setting_DailyHarvestedNutrition, "setting_DailyHarvestedNutritionDesc".Translate());

            listing.Gap(40);

            // Animal Capacity Information
            Text.Font = GameFont.Medium;
            listing.Label("settings_AnimalCapacityLabel".Translate());
            Text.Font = GameFont.Small;
            listing.GapLine(8);

            listing.CheckboxLabeled("setting_GrazingCapacity".Translate(), ref modSettings.setting_GrazingCapacity, "setting_GrazingCapacityDesc".Translate());
            listing.CheckboxLabeled("setting_HarvestedFeedCapacity".Translate(), ref modSettings.setting_HarvestedFeedCapacity, "setting_HarvestedFeedCapacityDesc".Translate());
            listing.Gap(16);
            listing.CheckboxLabeled("setting_ExpandedAnimalHyperlinks".Translate(), ref modSettings.setting_ExpandedAnimalHyperlinks, "setting_ExpandedAnimalHyperlinksDesc".Translate());

            listing.Gap(40);

            // Human Capacity Information
            Text.Font = GameFont.Medium;
            listing.Label("settings_HumanCapacityLabel".Translate());
            Text.Font = GameFont.Small;
            listing.GapLine(8);

            listing.CheckboxLabeled("setting_PawnCapacity".Translate(), ref modSettings.setting_PawnCapacity, "setting_PawnCapacityDesc".Translate());

            listing.End();

            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "CropStatsSettings".Translate();
        }
    }


    public class CropStatsSettings : ModSettings
    {
        // base values
        public bool setting_TrueGrowingTime = true;
        public bool setting_DailyHarvest = true;
        public bool setting_DailyCash = true;
        public bool setting_DailyNaturalNutrition = true;
        public bool setting_DailyHarvestedNutrition = true;

        // Animal specific
        public bool setting_GrazingCapacity = true;
        public bool setting_HarvestedFeedCapacity = true;

        public bool setting_ExpandedAnimalHyperlinks = false;

        // Pawn specific
        public bool setting_PawnCapacity = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref setting_TrueGrowingTime, "setting_TrueGrowingTime");
            Scribe_Values.Look(ref setting_DailyHarvest, "setting_DailyHarvest");
            Scribe_Values.Look(ref setting_DailyCash, "setting_DailyCash");
            Scribe_Values.Look(ref setting_DailyNaturalNutrition, "setting_DailyNaturalNutrition");
            Scribe_Values.Look(ref setting_DailyHarvestedNutrition, "setting_DailyHarvestedNutrition");

            Scribe_Values.Look(ref setting_GrazingCapacity, "setting_GrazingCapacity");
            Scribe_Values.Look(ref setting_HarvestedFeedCapacity, "setting_HarvestedFeedCapacity");

            Scribe_Values.Look(ref setting_ExpandedAnimalHyperlinks, "setting_ExpandedAnimalHyperlinks");

            Scribe_Values.Look(ref setting_PawnCapacity, "setting_PawnCapacity");

            base.ExposeData();
        }
    }


    [DefOf]
    public static class CropStatsDefOf
    {
        public static StatCategoryDef AdditionalPlantStats;
        public static StatCategoryDef AdditionalCropStats;

        public static StatCategoryDef AnimalGrazeStats;
        public static StatCategoryDef AnimalFeedStats;

        public static StatCategoryDef HumanFeedStats;
    }
}