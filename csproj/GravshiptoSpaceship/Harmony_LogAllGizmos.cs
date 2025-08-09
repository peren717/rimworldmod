using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(ThingWithComps), "GetGizmos")]
public static class Harmony_LogAllGizmos
{
	public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, ThingWithComps __instance)
	{
		foreach (Gizmo gizmo in __result)
		{
			Command_Action cmd = gizmo as Command_Action;
			if (cmd != null && cmd.defaultLabel != null && cmd.defaultLabel.Contains("グラヴシップ発射"))
			{
				GravshipLaunchContext.LastUsedConsolePos = __instance.Position;
				GravshipLaunchContext.LastUsedConsoleMap = __instance.Map;
			}
			yield return gizmo;
		}
	}
}
