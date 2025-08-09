using HarmonyLib;
using RimWorld;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(PlaceWorker_HeadOnShipBeam), "AllowsPlacing")]
public static class Harmony_HeadOnShipBeam_PlaceOverride
{
	private static bool Prefix(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore, Thing thing, ref AcceptanceReport __result)
	{
		IntVec3 c = loc + rot.FacingCell * -1;
		if (c.InBounds(map))
		{
			Building edifice = c.GetEdifice(map);
			if (edifice != null && edifice.def == ThingDefOf.Ship_Beam)
			{
				__result = true;
				return false;
			}
		}
		if (GravshipConnectionUtility.IsSubstructure(loc, map))
		{
			__result = true;
			return false;
		}
		__result = "MustPlaceHeadOnShipBeam".Translate();
		return false;
	}
}
