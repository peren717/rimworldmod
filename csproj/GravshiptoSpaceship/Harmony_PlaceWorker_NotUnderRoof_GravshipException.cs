using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(PlaceWorker_NotUnderRoof), "AllowsPlacing")]
public static class Harmony_PlaceWorker_NotUnderRoof_GravshipException
{
	private static readonly HashSet<string> AllowedUnderRoofDefs = new HashSet<string> { "Ship_Reactor", "Ship_Beam", "Ship_ComputerCore", "Ship_SensorCluster", "Ship_CryptosleepCasket", "Ship_Engine" };

	private static bool Prefix(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore, Thing thing, ref AcceptanceReport __result)
	{
		if (loc.GetRoof(map) == null)
		{
			__result = true;
			return false;
		}
		if (checkingDef is ThingDef thingDef && AllowedUnderRoofDefs.Contains(thingDef.defName) && GravshipConnectionUtility.IsSubstructure(loc, map))
		{
			__result = true;
			return false;
		}
		__result = "MustPlaceUnroofed".Translate();
		return false;
	}
}
