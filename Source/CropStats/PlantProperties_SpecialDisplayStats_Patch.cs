
using System.Collections.Generic;
using CropStats;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;


namespace PlantProperties_SpecialDisplayStats_Patch
{
    [HarmonyPatch(typeof(PlantProperties),"SpecialDisplayStats")]
    class PlantProperties_SpecialDisplayStats_Patch
    {
        [HarmonyPostfix]
        static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> entries, PlantProperties __instance) {
            foreach (var entry in entries)
            {
                yield return entry;
            }

            float trueGrowth = (60000f * __instance.growDays) / 32500f;
            yield return new StatDrawEntry(CropStatsDefOf.AdditionalPlantStats, "TrueGrowingTime".Translate(), trueGrowth.ToString("0.##") + " " + "Days".Translate(), "TrueGrowingTimeDesc".Translate(), 3005);

            if (!__instance.Harvestable)
            {
                yield break;
            }

            float efficiencyStat = __instance.harvestYield / trueGrowth;
            yield return new StatDrawEntry(CropStatsDefOf.AdditionalPlantStats, "HarvestPerDay".Translate(), efficiencyStat.ToString("0.##") + " " + "HarvestPerDayEquivilence".Translate(), "HarvestPerDayDesc".Translate(), 3004);

            if (!__instance.harvestedThingDef.IsIngestible)
            {
                yield break;
            }

            float nutritionalEfficiency = efficiencyStat * (float)__instance.harvestedThingDef.GetStatValueAbstract(StatDefOf.Nutrition);
            yield return new StatDrawEntry(CropStatsDefOf.AdditionalPlantStats, "NutritionPerDay".Translate(), nutritionalEfficiency.ToString("0.###") + " " + "NutritionPerDayEquivilence".Translate(), "NutritionPerDayDesc".Translate(), 3003);

            float plantsPerPawn = 1.6f / nutritionalEfficiency;
            yield return new StatDrawEntry(CropStatsDefOf.AdditionalPlantStats, "CropsPerPawn".Translate(), Mathf.RoundToInt(plantsPerPawn).ToString() + " " + "CropsPerPawnEquivilence".Translate(), "CropsPerPawnDesc".Translate(), 3002);
            yield return new StatDrawEntry(CropStatsDefOf.AdditionalPlantStats, "CropsPerPawnCooked".Translate(), Mathf.RoundToInt(plantsPerPawn / 2).ToString() + " " + "CropsPerPawnEquivilence".Translate(), "CropsPerPawnDesc".Translate(), 3001);
            yield return new StatDrawEntry(CropStatsDefOf.AdditionalPlantStats, "CropsPerPawnNutrient".Translate(), Mathf.RoundToInt(plantsPerPawn / 3).ToString() + " " + "CropsPerPawnEquivilence".Translate(), "CropsPerPawnDesc".Translate(), 3000);
        }
    }
}
