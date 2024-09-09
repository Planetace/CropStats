
using System;
using Verse;
using HarmonyLib;
using System.Reflection;


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
            }
            catch(Exception e) {
                Log.Error("Crop Stats has failed to initialise due to: " + e);
            }
        }
    }
}
