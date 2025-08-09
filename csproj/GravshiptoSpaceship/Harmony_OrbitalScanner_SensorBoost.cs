using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(CompOrbitalScanner), "ReceiveSignal")]
public static class Harmony_OrbitalScanner_SensorBoost
{
	public static int ModifyOrbitalScanDurationIfSensorExists(int original, CompOrbitalScanner scanner)
	{
		Map map = scanner.parent?.Map;
		if (map == null)
		{
			return original;
		}
		IntVec3? root = GravshipConnectionUtility.FindGravshipRootConnectedToThing(scanner.parent);
		if (!root.HasValue)
		{
			return original;
		}
		if (!map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).Any((Thing t) => t.def.defName == "Ship_SensorCluster" && GravshipConnectionUtility.FindGravshipRootConnectedToThing(t) == root))
		{
			return original;
		}
		int num = original / 2;
		if (GravshipLogger.EnableLogging)
		{
			Log.Message("[Gravship] Sensor cluster detected: scan time reduced from " + original.ToStringTicksToPeriod() + " to " + num.ToStringTicksToPeriod() + ".");
		}
		return num;
	}

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> codes = instructions.ToList();
		MethodInfo durationMethod = typeof(IntRange).GetMethod("get_RandomInRange");
		for (int i = 0; i < codes.Count; i++)
		{
			yield return codes[i];
			if (codes[i].Calls(durationMethod))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return CodeInstruction.Call(typeof(Harmony_OrbitalScanner_SensorBoost), "ModifyOrbitalScanDurationIfSensorExists");
			}
		}
	}
}
