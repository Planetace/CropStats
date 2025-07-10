
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CropStats;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;


namespace CropStats
{
    [HarmonyPatch(typeof(PlantProperties), "SpecialDisplayStats")]
    public class CropStats_SpecialDisplayStatsPatch
    {

        // Duplicate of the Hyperlink Infocard method from the PlantProperties class because it's private.
        // This is probably bad code practise but it works so idgaf.
        public static IEnumerable<Dialog_InfoCard.Hyperlink> Hyperlink(ThingDef def)
        {
            yield return new Dialog_InfoCard.Hyperlink(def);
        }

        // Thanks to Halicade for the hyperlink example (and also the concept of `foo: IEnumerable`)
        public IEnumerable<DefHyperlink> GetListOfDefHyperlinks(List<ThingDef> defs)
        {
            foreach (ThingDef def in defs)
            {
                yield return new DefHyperlink(def);
            }
        }

        // Alright I need to do pseudocode
        [HarmonyPostfix]
        public static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> entries, PlantProperties __instance) {

            // Recyable list of Hyperlinks that'll be used for the animal stats.
            // We need this so it can be overwritten with either the shortened list or the extended list, depending in setting.
            IEnumerable<Dialog_InfoCard.Hyperlink> links;

            // Check the plant cache for what the parent plant is so we can calculate its nutritional growth.
            ThingDef parent = null;
            foreach (ThingDef plantDef in CropStatsCache.cachedPlantList)
            {
                if (plantDef.plant == __instance) {
                    parent = plantDef;
                    break;
                }
            }

            // If a plant has no growing-time then everything after will collapse in on itself.
            if (__instance.growDays <= 0)
            {
                Log.Warning(CropStatsMod.logID + parent.defName + " has no growth time!");
                yield break;
            }

            float trueGrowthTime = (__instance.growDays * 60000) / 32500;
            if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_TrueGrowingTime) {
                yield return new StatDrawEntry(
                    CropStatsDefOf.AdditionalPlantStats,
                    "TrueGrowingTime".Translate(),
                    trueGrowthTime.ToString("0.##") + " " + "Days".Translate(),
                    "TrueGrowingTimeDesc".Translate(),
                    4152
                );
            }

            // Pre-loade daily Nutrition because we will use it for both the plant nutrition growth and produce nutrition entries.
            float dailyNutrition;

            // Cannot skip this because a plant could theoretically have no nutrition but have produce that does.
            if (parent.GetStatValueAbstract(StatDefOf.Nutrition) > 0)
            {
                // Plant specific nutrition growth per day
                dailyNutrition = parent.GetStatValueAbstract(StatDefOf.Nutrition) / trueGrowthTime;

                if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_DailyNaturalNutrition)
                {
                    yield return new StatDrawEntry(
                        CropStatsDefOf.AdditionalPlantStats,
                        "PlantNutritionPerDay".Translate(),
                        dailyNutrition.ToString("0.###") + " " + "NutritionPerDayEquivalence".Translate(),
                        "PlantNutritionPerDayDesc".Translate(),
                        4151
                    );
                }

                if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_GrazingCapacity)
                {
                    // Thrumbo Grazing
                    if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_ExpandedAnimalHyperlinks)
                        links = Dialog_InfoCard.DefsToHyperlinks(CropStatsCache.cachedThrumbolike);
                    else
                        links = Hyperlink(ThingDefOf.Thrumbo);

                    yield return new StatDrawEntry(
                        CropStatsDefOf.AnimalGrazeStats,
                        "GrazePerThrumbo".Translate(),
                        (2.8f / dailyNutrition).ToString("0.##") + " " + "GrazePerAnimalEquivalence".Translate(),
                        "PlantsPerAnimalDesc".Translate(),
                        4150,
                        null,
                        hyperlinks: links
                    );


                    // Cow Grazing
                    if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_ExpandedAnimalHyperlinks)
                        links = Dialog_InfoCard.DefsToHyperlinks(CropStatsCache.cachedCowlike);
                    else
                        links = Hyperlink(ThingDefOf.Cow);

                    yield return new StatDrawEntry(
                        CropStatsDefOf.AnimalGrazeStats,
                        "GrazePerCow".Translate(),
                        (0.86f / dailyNutrition).ToString("0.##") + " " + "GrazePerAnimalEquivalence".Translate(),
                        "PlantsPerAnimalDesc".Translate(),
                        4149,
                        null,
                        hyperlinks: links
                    );


                    // Goat Grazing
                    if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_ExpandedAnimalHyperlinks)
                        links = Dialog_InfoCard.DefsToHyperlinks(CropStatsCache.cachedGoatlike);
                    else
                        links = Hyperlink(ThingDefOf.Goat);

                    yield return new StatDrawEntry(
                        CropStatsDefOf.AnimalGrazeStats,
                        "GrazePerGoat".Translate(),
                        (0.36f / dailyNutrition).ToString("0.##") + " " + "GrazePerAnimalEquivalence".Translate(),
                        "PlantsPerAnimalDesc".Translate(),
                        4148,
                        null,
                        hyperlinks: links
                    );


                    // Chicken Grazing
                    if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_ExpandedAnimalHyperlinks)
                        links = Dialog_InfoCard.DefsToHyperlinks(CropStatsCache.cachedChickenlike);
                    else
                        links = Hyperlink(ThingDefOf.Chicken);

                    yield return new StatDrawEntry(
                        CropStatsDefOf.AnimalGrazeStats,
                        "GrazePerChicken".Translate(),
                        (0.22f / dailyNutrition).ToString("0.##") + " " + "GrazePerAnimalEquivalence".Translate(),
                        "PlantsPerAnimalDesc".Translate(),
                        4147,
                        null,
                        hyperlinks: links
                    );
                }
            }

            // No longer need to continue if the plant produces no harvestable produce.
            if (__instance.harvestedThingDef == null || __instance.harvestYield <= 0)
            {
                yield break;
            }


            // Calculating Daily Yield and Silver.
            float dailyYield = __instance.harvestYield / trueGrowthTime;
            float dailySilver = dailyYield * __instance.harvestedThingDef.GetStatValueAbstract(StatDefOf.MarketValue);

            if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_DailyHarvest)
            {
                yield return new StatDrawEntry(
                    CropStatsDefOf.AdditionalCropStats,
                    "HarvestPerDay".Translate(),
                    dailyYield.ToString("0.##") + " " + "HarvestPerDayEquivalence".Translate(),
                    "HarvestPerDayDesc".Translate(),
                    4146,
                    null,
                    Hyperlink(__instance.harvestedThingDef)
                );
            }


            if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_DailyCash)
            {
                yield return new StatDrawEntry(
                    CropStatsDefOf.AdditionalCropStats,
                    "CashPerDay".Translate(),
                    dailySilver.ToString("0.##") + " " + "CashPerDayEquivalence".Translate(),
                    "CashPerDayDesc".Translate(),
                    4145,
                    null,
                    hyperlinks: Dialog_InfoCard.DefsToHyperlinks(new List<ThingDef> { __instance.harvestedThingDef, ThingDefOf.Silver })
                );
            }


            if (!__instance.harvestedThingDef.IsIngestible)
            {
                yield break;
            }

            // Calculate Daily Nutrition.
            dailyNutrition = dailyYield * __instance.harvestedThingDef.GetStatValueAbstract(StatDefOf.Nutrition);

            if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_DailyHarvestedNutrition)
            {
                yield return new StatDrawEntry(
                   CropStatsDefOf.AdditionalCropStats,
                   "NutritionPerDay".Translate(),
                   dailyNutrition.ToString("0.###") + " " + "NutritionPerDayEquivalence".Translate(),
                   "NutritionPerDayDesc".Translate(),
                   4144,
                   null,
                   Hyperlink(__instance.harvestedThingDef)
               );
            }


            if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_HarvestedFeedCapacity)
            {
                // Thrumbo Feed
                if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_ExpandedAnimalHyperlinks)
                    links = Dialog_InfoCard.DefsToHyperlinks(CropStatsCache.cachedThrumbolike);
                else
                    links = Dialog_InfoCard.DefsToHyperlinks(new List<ThingDef>{ ThingDefOf.Thrumbo, __instance.harvestedThingDef });

                yield return new StatDrawEntry(
                    CropStatsDefOf.AnimalFeedStats,
                    "CropsPerThrumbo".Translate(),
                    (2.8f / dailyNutrition).ToString("0.##") + " " + "CropsPerPawnEquivalence".Translate(),
                    "PlantsPerAnimalDesc".Translate(),
                    4143,
                    null,
                    hyperlinks: links
                );


                // Cow Feed
                if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_ExpandedAnimalHyperlinks)
                    links = Dialog_InfoCard.DefsToHyperlinks(CropStatsCache.cachedCowlike);
                else
                    links = Dialog_InfoCard.DefsToHyperlinks(new List<ThingDef>{ ThingDefOf.Cow, __instance.harvestedThingDef });                  

                yield return new StatDrawEntry(
                    CropStatsDefOf.AnimalFeedStats,
                    "CropsPerCow".Translate(),
                    (0.86f / dailyNutrition).ToString("0.##") + " " + "CropsPerPawnEquivalence".Translate(),
                    "PlantsPerAnimalDesc".Translate(),
                    4142,
                    null,
                    hyperlinks: links
                );


                // Goat Feed
                if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_ExpandedAnimalHyperlinks)
                    links = Dialog_InfoCard.DefsToHyperlinks(CropStatsCache.cachedGoatlike);
                else
                    links = Dialog_InfoCard.DefsToHyperlinks(new List<ThingDef>{ ThingDefOf.Goat, __instance.harvestedThingDef });

                yield return new StatDrawEntry(
                    CropStatsDefOf.AnimalFeedStats,
                    "CropsPerGoat".Translate(),
                    (0.36f / dailyNutrition).ToString("0.##") + " " + "CropsPerPawnEquivalence".Translate(),
                    "PlantsPerAnimalDesc".Translate(),
                    4141,
                    null,
                    hyperlinks: links
                );


                // Chicken Feed
                if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_ExpandedAnimalHyperlinks)
                    links = Dialog_InfoCard.DefsToHyperlinks(CropStatsCache.cachedChickenlike);
                else
                    links = Dialog_InfoCard.DefsToHyperlinks(new List<ThingDef>{ ThingDefOf.Chicken, __instance.harvestedThingDef });

                yield return new StatDrawEntry(
                    CropStatsDefOf.AnimalFeedStats,
                    "CropsPerChicken".Translate(),
                    (0.22f / dailyNutrition).ToString("0.##") + " " + "CropsPerPawnEquivalence".Translate(),
                    "PlantsPerAnimalDesc".Translate(),
                    4140,
                    null,
                    hyperlinks: links
                );
            }

            // Break if the food is not consumable by human standards.
            if (!__instance.harvestedThingDef.IsRawHumanFood())
            {
                yield break;
            }


            if (LoadedModManager.GetMod<CropStatsMod>().GetSettings<CropStatsSettings>().setting_PawnCapacity)
            {
                // Raw Food for Humans
                yield return new StatDrawEntry(
                    CropStatsDefOf.HumanFeedStats,
                    "CropsPerPawn".Translate(),
                    (1.6f / dailyNutrition).ToString("0.##") + " " + "CropsPerPawnEquivalence".Translate(),
                    "CropsPerPawnDesc".Translate(),
                    4139,
                    null,
                    Hyperlink(__instance.harvestedThingDef)
                );

                // Simple Meals for Humans
                yield return new StatDrawEntry(
                    CropStatsDefOf.HumanFeedStats,
                    "CropsPerPawnCooked".Translate(),
                    (1.6f / 1.8f / dailyNutrition).ToString("0.##") + " " + "CropsPerPawnEquivalence".Translate(),
                    "CropsPerPawnDesc".Translate(),
                    4138,
                    null,
                    Hyperlink(ThingDefOf.MealSimple)
                );

                // Paste for Humans
                yield return new StatDrawEntry(
                    CropStatsDefOf.HumanFeedStats,
                    "CropsPerPawnNutrient".Translate(),
                    (1.6f / 3.0f / dailyNutrition).ToString("0.##") + " " + "CropsPerPawnEquivalence".Translate(),
                    "CropsPerPawnDesc".Translate(),
                    4137,
                    null,
                    Hyperlink(ThingDefOf.MealNutrientPaste)
                );
            }
        }
    }
}
