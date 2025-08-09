using System.Text.RegularExpressions;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(CompPilotConsole), "CompInspectStringExtra")]
public static class Harmony_PilotConsole_InspectString_Postfix
{
	private static void Postfix(CompPilotConsole __instance, ref string __result)
	{
		if (__instance?.engine == null)
		{
			return;
		}
		int maxLaunchDistance = __instance.engine.MaxLaunchDistance;
		if (maxLaunchDistance < 500)
		{
			return;
		}
		string pattern = string.Format("({0}: )\\d+", "GravshipRange".Translate().CapitalizeFirst());
		__result = Regex.Replace(__result, pattern, string.Format("$1{0}", "GravshipRange_Inf".Translate()));
		int ticksGame = Find.TickManager.TicksGame;
		float num;
		if (GravshipLaunchContext.LastFuelCostCalcTick.HasValue && ticksGame - GravshipLaunchContext.LastFuelCostCalcTick < 60 && GravshipLaunchContext.LastComputedFuelCost.HasValue)
		{
			num = GravshipLaunchContext.LastComputedFuelCost.Value;
		}
		else
		{
			num = 50f;
			Map map = __instance.parent.Map;
			IntVec3? root = GravshipConnectionUtility.FindGravshipRootConnectedToThing(__instance.parent);
			if (map != null && root.HasValue)
			{
				GravshipConnectionUtility.GravshipStatus gravshipStatus = GravshipConnectionUtility.EvaluateGravshipStatusCached(map, root.Value);
				if (gravshipStatus.HasReactor && gravshipStatus.HasNuclearEngine && gravshipStatus.HasFunctionalThruster)
				{
					int num2 = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).Count((Thing t) => t.def.defName == "FuelOptimizer" && GravshipConnectionUtility.FindGravshipRootConnectedToThing(t) == root);
					if (num2 >= 2)
					{
						num = 20f;
					}
					else if (num2 == 1)
					{
						num = 35f;
					}
				}
			}
			GravshipLaunchContext.LastComputedFuelCost = num;
			GravshipLaunchContext.LastFuelCostCalcTick = ticksGame;
		}
		string text = "FuelConsumption".Translate().CapitalizeFirst();
		string text2 = "FuelFixed50".Translate();
		string text3 = Prefs.LangFolderName?.ToLowerInvariant() ?? "unknown";
		string text4 = ((!text3.StartsWith("ja")) ? $"{num} {text2}" : $"{text2}{num}");
		string pattern2 = text + ": .*";
		string replacement = text + ": " + text4;
		__result = Regex.Replace(__result, pattern2, replacement);
	}
}
