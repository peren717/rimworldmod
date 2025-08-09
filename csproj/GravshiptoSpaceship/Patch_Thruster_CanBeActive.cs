using HarmonyLib;
using RimWorld;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(CompGravshipThruster), "CanBeActive", MethodType.Getter)]
public static class Patch_Thruster_CanBeActive
{
	private static void Postfix(CompGravshipThruster __instance, ref bool __result)
	{
		if (__instance.parent?.def?.defName != "Ship_Engine")
		{
			return;
		}
		CompBreakdownable breakdownable = __instance.Breakdownable;
		if ((breakdownable != null && breakdownable.BrokenDown) || __instance.Blocked)
		{
			__result = false;
			return;
		}
		Map map = __instance.parent.Map;
		if (map == null)
		{
			__result = false;
			return;
		}
		IntVec3? intVec = GravshipConnectionUtility.FindGravshipRootConnectedToThing(__instance.parent);
		if (!intVec.HasValue)
		{
			__result = false;
		}
		else
		{
			__result = GravshipConnectionUtility.HasRunningReactor(map, intVec.Value);
		}
	}
}
