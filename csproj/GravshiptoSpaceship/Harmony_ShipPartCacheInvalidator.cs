using HarmonyLib;
using RimWorld;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch]
public static class Harmony_ShipPartCacheInvalidator
{
	private static bool IsShipOrGravshipRelevant(Thing t)
	{
		BuildingProperties building = t.def.building;
		return (building != null && building.shipPart) || t.TryGetComp<CompGravshipThruster>() != null;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(Thing), "DeSpawn")]
	public static bool Pre_DeSpawn(Thing __instance, ref Map __state)
	{
		if (IsShipOrGravshipRelevant(__instance))
		{
			__state = __instance.MapHeld;
			return true;
		}
		__state = null;
		return true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Thing), "DeSpawn")]
	public static void Post_DeSpawn(Thing __instance, Map __state)
	{
		if (IsShipOrGravshipRelevant(__instance) && __state != null)
		{
			InvalidateCaches(__state);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Thing), "SpawnSetup")]
	public static void OnShipPartSpawned(Thing __instance)
	{
		if (IsShipOrGravshipRelevant(__instance) && __instance.Map != null)
		{
			InvalidateCaches(__instance.Map);
		}
	}

	private static void InvalidateCaches(Map map)
	{
		if (GravshipLogger.ShouldLog)
		{
			Log.Warning($"[Gravship DEBUG] Invalidating all ship-related caches for map {map.Index}");
		}
		GravshipConnectionUtility.ClearAllShipCaches(map);
	}
}
