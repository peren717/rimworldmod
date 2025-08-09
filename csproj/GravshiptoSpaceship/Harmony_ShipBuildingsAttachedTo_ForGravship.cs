using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(ShipUtility), "ShipBuildingsAttachedTo")]
public static class Harmony_ShipBuildingsAttachedTo_ForGravship
{
	public static bool isGravshipLaunch = false;

	public static bool hasLoggedThisLaunch = false;

	public static HashSet<IntVec3> launchCells = new HashSet<IntVec3>();

	public static HashSet<Thing> preLaunchNearbyThings = new HashSet<Thing>();

	private static bool Prefix(Building root, ref IEnumerable<Building> __result)
	{
		if (root == null || root.Map == null)
		{
			return true;
		}
		Map map = root.Map;
		List<List<Building>> allVanillaStructuresCached = GravshipConnectionUtility.GetAllVanillaStructuresCached(map);
		IntVec3? intVec = GravshipConnectionUtility.FindGravshipRootConnectedToThing(root);
		bool hasValue = intVec.HasValue;
		List<Building> list = GravshipConnectionUtility.GetAllVanillaStructuresCached(map).FirstOrDefault<List<Building>>((List<Building> g) => g.Contains(root));
		if (list != null)
		{
			isGravshipLaunch = false;
			launchCells.Clear();
			preLaunchNearbyThings.Clear();
			__result = list;
			return false;
		}
		if (intVec.HasValue)
		{
			GravshipConnectionUtility.GravshipStatus gravshipStatus = GravshipConnectionUtility.EvaluateGravshipStatusCached(map, intVec.Value);
			if (gravshipStatus.HasReactor)
			{
				HashSet<IntVec3> hashSet = GravshipConnectionUtility.FindConnectedSubstructure(intVec.Value, map);
				List<Building> list2 = hashSet.SelectMany((IntVec3 c2) => c2.GetThingList(map)).OfType<Building>().ToList();
				launchCells = new HashSet<IntVec3>(hashSet);
				preLaunchNearbyThings = new HashSet<Thing>();
				foreach (IntVec3 launchCell in launchCells)
				{
					IntVec3[] adjacentCellsAndInside = GenAdj.AdjacentCellsAndInside;
					foreach (IntVec3 intVec2 in adjacentCellsAndInside)
					{
						IntVec3 c = launchCell + intVec2;
						if (!c.InBounds(map))
						{
							continue;
						}
						foreach (Thing thing in c.GetThingList(map))
						{
							preLaunchNearbyThings.Add(thing);
						}
					}
				}
				isGravshipLaunch = true;
				__result = list2;
				return false;
			}
		}
		isGravshipLaunch = true;
		launchCells = new HashSet<IntVec3> { root.Position };
		preLaunchNearbyThings.Clear();
		__result = new List<Building> { root };
		return false;
	}
}
