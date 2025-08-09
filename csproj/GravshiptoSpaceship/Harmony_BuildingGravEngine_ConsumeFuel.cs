using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(Building_GravEngine), "ConsumeFuel")]
public static class Harmony_BuildingGravEngine_ConsumeFuel
{
	private static void Prefix(Building_GravEngine __instance)
	{
		if (__instance.launchInfo == null)
		{
			return;
		}
		Map map = __instance.Map;
		if (map == null)
		{
			return;
		}
		IntVec3? intVec = GravshipConnectionUtility.FindGravshipRootConnectedToThing(__instance);
		if (!intVec.HasValue)
		{
			return;
		}
		List<Building> gravshipStructure = GravshipConnectionUtility.GetGravshipStructure(intVec.Value, map);
		bool flag = false;
		bool flag2 = false;
		HashSet<string> hashSet = new HashSet<string>();
		foreach (Building item in gravshipStructure)
		{
			if (hashSet.Add(item.ThingID))
			{
				if (item.def.defName == "Ship_ComputerCore")
				{
					flag = true;
				}
				if (item.def.defName == "Ship_SensorCluster")
				{
					flag2 = true;
				}
				if (flag && flag2)
				{
					break;
				}
			}
		}
		float num = 0f;
		if (flag)
		{
			num += 0.4f;
		}
		if (flag2)
		{
			num += 0.1f;
		}
		float quality = __instance.launchInfo.quality;
		float num2 = Mathf.Clamp01(quality + num);
		__instance.launchInfo.quality = num2;
		if (GravshipLogger.EnableLogging)
		{
			Log.Message($"[Gravship] Quality calculation for engine {__instance.LabelCap} at {__instance.Position}");
		}
		if (GravshipLogger.EnableLogging)
		{
			Log.Message($"[Gravship] Connected parts (deduplicated): {hashSet.Count}, Core={flag}, Sensor={flag2}, Bonus={num}, ThingID={__instance.ThingID}");
		}
		if (GravshipLogger.EnableLogging)
		{
			Log.Message($"[Gravship] Previous quality = {quality}");
		}
		if (GravshipLogger.EnableLogging)
		{
			Log.Message($"[Gravship] Final quality for {__instance.LabelCap} = {num2}");
		}
	}
}
