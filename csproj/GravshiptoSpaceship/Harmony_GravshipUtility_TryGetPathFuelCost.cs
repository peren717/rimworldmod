using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(GravshipUtility), "TryGetPathFuelCost")]
public static class Harmony_GravshipUtility_TryGetPathFuelCost
{
	private static bool Prefix(PlanetTile from, PlanetTile to, out float cost, out int distance, float fuelPerTile, float fuelFactor, ref bool __result)
	{
		cost = 0f;
		distance = 0;
		if (GravshipLaunchContext.LastUsedConsoleMap == null || !GravshipLaunchContext.LastUsedConsolePos.HasValue)
		{
			if (GravshipLogger.EnableLogging)
			{
				Log.Warning("[Gravship] GravshipLaunchContext 情報が不明（map/pos null）");
			}
			return true;
		}
		Map lastUsedConsoleMap = GravshipLaunchContext.LastUsedConsoleMap;
		IntVec3 value = GravshipLaunchContext.LastUsedConsolePos.Value;
		CompPilotConsole compPilotConsole = (from t in value.GetThingList(lastUsedConsoleMap)
			select t.TryGetComp<CompPilotConsole>()).FirstOrDefault((CompPilotConsole c) => c?.CanUseNow().Accepted ?? false);
		if (compPilotConsole == null)
		{
			return true;
		}
		IntVec3? intVec = GravshipConnectionUtility.FindGravshipRootConnectedToThing(compPilotConsole.parent);
		if (!intVec.HasValue)
		{
			return true;
		}
		GravshipConnectionUtility.GravshipStatus gravshipStatus = GravshipConnectionUtility.EvaluateGravshipStatusCached(lastUsedConsoleMap, intVec.Value);
		if (GravshipLogger.EnableLogging)
		{
			Log.Message($"[Gravship] TryGetPathFuelCost: Reactor={gravshipStatus.HasReactor}, NuclearEngine={gravshipStatus.HasNuclearEngine}, Thruster={gravshipStatus.HasFunctionalThruster}");
		}
		if (gravshipStatus.HasReactor && gravshipStatus.HasNuclearEngine && gravshipStatus.HasFunctionalThruster)
		{
			float num = GravshipLaunchContext.LastComputedFuelCost ?? 50f;
			if (!GravshipLaunchContext.LastComputedFuelCost.HasValue)
			{
				int optimizerCount = GravshipConnectionUtility.GravshipFuelOptimizerCache.GetOptimizerCount(lastUsedConsoleMap, intVec.Value);
				if (optimizerCount >= 2)
				{
					num = 20f;
				}
				else if (optimizerCount == 1)
				{
					num = 35f;
				}
				GravshipLaunchContext.LastComputedFuelCost = num;
			}
			cost = num;
			distance = 1;
			__result = true;
			return false;
		}
		return true;
	}
}
