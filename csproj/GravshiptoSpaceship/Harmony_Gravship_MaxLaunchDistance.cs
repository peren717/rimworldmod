using HarmonyLib;
using RimWorld;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(Building_GravEngine), "MaxLaunchDistance", MethodType.Getter)]
public static class Harmony_Gravship_MaxLaunchDistance
{
	private static bool Prefix(Building_GravEngine __instance, ref int __result)
	{
		Map map = __instance.Map;
		if (map == null)
		{
			return true;
		}
		IntVec3? intVec = GravshipConnectionUtility.FindGravshipRootConnectedToThing(__instance);
		if (!intVec.HasValue)
		{
			return true;
		}
		GravshipConnectionUtility.GravshipStatus gravshipStatus = GravshipConnectionUtility.EvaluateGravshipStatusCached(map, intVec.Value);
		if (GravshipLogger.ShouldLog)
		{
			Log.Message($"[Gravship] MaxLaunchDistance check for {__instance.LabelCap} at {__instance.Position}");
			Log.Message($"[Gravship] Status: Reactor={gravshipStatus.HasReactor}, NuclearEngine={gravshipStatus.HasNuclearEngine}, Thruster={gravshipStatus.HasFunctionalThruster}");
		}
		if (gravshipStatus.HasReactor && gravshipStatus.HasNuclearEngine && gravshipStatus.HasFunctionalThruster)
		{
			__result = 500;
			return false;
		}
		return true;
	}
}
