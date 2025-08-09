using System;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(CompGravshipThruster), "CompInspectStringExtra")]
public static class Patch_Thruster_InspectString
{
	private static void Postfix(CompGravshipThruster __instance, ref string __result)
	{
		if (__instance.parent?.def?.defName != "Ship_Engine")
		{
			return;
		}
		if (!string.IsNullOrEmpty(__result))
		{
			string input = __result;
			string str = "ThrusterNotFunctional".Translate();
			string str2 = "ThrusterNotConnected".Translate();
			string pattern = "(<color=.+?>)?" + Regex.Escape(str) + ":.*" + Regex.Escape(str2) + ".*?(</color>)?";
			input = Regex.Replace(input, pattern, "", RegexOptions.IgnoreCase);
			string remove2 = "GravFacilityMaxSimultaneousConnections".Translate();
			input = (from line in input.Split(new char[1] { '\n' })
				where !line.Contains(remove2)
				where !string.IsNullOrWhiteSpace(line)
				select line).ToLineList();
			__result = input;
		}
		if (__instance.CanBeActive)
		{
			return;
		}
		Map map = __instance.parent.Map;
		if (map == null)
		{
			return;
		}
		IntVec3? intVec = GravshipConnectionUtility.FindGravshipRootConnectedToThing(__instance.parent);
		if (intVec.HasValue && !GravshipConnectionUtility.HasRunningReactor(map, intVec.Value))
		{
			string text = "GravshipThrusterNoReactor".Translate().Colorize(ColorLibrary.RedReadable);
			if (string.IsNullOrEmpty(__result))
			{
				__result = text;
				return;
			}
			__result = __result.TrimEnd(Array.Empty<char>());
			__result = __result + "\n" + text;
		}
	}
}
