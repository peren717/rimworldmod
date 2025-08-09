using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace GravshiptoSpaceship;

public static class GravshipConnectionUtility
{
	public class GravshipStatus
	{
		public bool HasReactor;

		public bool HasNuclearEngine;

		public bool HasFunctionalThruster;

		public GravshipStatus(bool hasReactor, bool hasNuclearEngine, bool hasFunctionalThruster)
		{
			HasReactor = hasReactor;
			HasNuclearEngine = hasNuclearEngine;
			HasFunctionalThruster = hasFunctionalThruster;
		}
	}

	public static class GravshipFuelOptimizerCache
	{
		private static readonly Dictionary<(Map map, IntVec3 root), (int count, int tick)> cache = new Dictionary<(Map, IntVec3), (int, int)>();

		private const int CacheLifeTicks = 300;

		public static int GetOptimizerCount(Map map, IntVec3 root)
		{
			(Map, IntVec3) key = (map, root);
			int ticksGame = Find.TickManager.TicksGame;
			if (cache.TryGetValue(key, out (int, int) value) && ticksGame - value.Item2 < 300)
			{
				return value.Item1;
			}
			int num = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).Count((Thing t) => t.def.defName == "FuelOptimizer" && FindGravshipRootConnectedToThing(t) == root);
			cache[key] = (num, ticksGame);
			return num;
		}

		public static void Clear(Map map)
		{
			cache.Keys.Where<(Map, IntVec3)>(((Map map, IntVec3 root) k) => k.map == map).ToList().ForEach(delegate((Map map, IntVec3 root) k)
			{
				cache.Remove(k);
			});
		}

		public static void ClearAll()
		{
			cache.Clear();
		}
	}

	private static readonly Dictionary<(Map, IntVec3), (HashSet<IntVec3> cells, int lastUsedTick)> SubstructureCacheSmart = new Dictionary<(Map, IntVec3), (HashSet<IntVec3>, int)>();

	private const int SubstructureCacheKeepAliveTicks = 60;

	private static readonly Dictionary<Thing, (IntVec3 root, int tick)> GravshipRootCache = new Dictionary<Thing, (IntVec3, int)>();

	private static readonly int GravshipRootCacheValidTicks = 300;

	private static readonly Dictionary<Thing, (IntVec3 root, int lastUsedTick)> GravshipRootCacheSmart = new Dictionary<Thing, (IntVec3, int)>();

	private const int GravshipRootCacheKeepAliveTicks = 60;

	private static readonly Dictionary<(Map map, IntVec3 root), (bool result, int lastUsedTick)> ReactorCheckCacheSmart = new Dictionary<(Map, IntVec3), (bool, int)>();

	private const int ReactorCheckCacheKeepAliveTicks = 60;

	private static readonly Dictionary<(Map map, IntVec3 root), (GravshipStatus status, int lastUsedTick)> GravshipStatusCacheSmart = new Dictionary<(Map, IntVec3), (GravshipStatus, int)>();

	private const int GravshipStatusCacheKeepAliveTicks = 60;

	private static readonly Dictionary<(Map, IntVec3), (List<Building> structure, int lastUsedTick)> VanillaStructureCacheSmart = new Dictionary<(Map, IntVec3), (List<Building>, int)>();

	private const int VanillaStructureCacheKeepAliveTicks = 60;

	private static readonly Dictionary<int, (List<List<Building>> structures, int lastUsedTick)> VanillaStructuresCacheSmart = new Dictionary<int, (List<List<Building>>, int)>();

	private const int VanillaStructuresCacheKeepAliveTicks = 60;

	private static readonly Dictionary<int, (List<List<Building>> structures, int lastUsedTick)> GravshipStructureCacheSmart = new Dictionary<int, (List<List<Building>>, int)>();

	private const int GravshipStructureCacheKeepAliveTicks = 60;

	public static bool IsSubstructure(IntVec3 cell, Map map)
	{
		if (!cell.InBounds(map))
		{
			return false;
		}
		TerrainGrid terrainGrid = map.terrainGrid;
		return terrainGrid.TopTerrainAt(cell)?.defName == "Substructure" || terrainGrid.UnderTerrainAt(cell)?.defName == "Substructure" || terrainGrid.FoundationAt(cell)?.defName == "Substructure";
	}

	public static HashSet<IntVec3> FindConnectedSubstructure(IntVec3 start, Map map)
	{
		(Map, IntVec3) key = (map, start);
		int ticksGame = Find.TickManager.TicksGame;
		if (SubstructureCacheSmart.TryGetValue(key, out (HashSet<IntVec3>, int) value))
		{
			if (ticksGame - value.Item2 <= 60)
			{
				SubstructureCacheSmart[key] = (value.Item1, ticksGame);
				return value.Item1;
			}
			SubstructureCacheSmart.Remove(key);
		}
		HashSet<IntVec3> hashSet = new HashSet<IntVec3>();
		Queue<IntVec3> queue = new Queue<IntVec3>();
		queue.Enqueue(start);
		hashSet.Add(start);
		while (queue.Count > 0)
		{
			IntVec3 intVec = queue.Dequeue();
			IntVec3[] cardinalDirections = GenAdj.CardinalDirections;
			foreach (IntVec3 intVec2 in cardinalDirections)
			{
				IntVec3 intVec3 = intVec + intVec2;
				if (!hashSet.Contains(intVec3) && IsSubstructure(intVec3, map))
				{
					hashSet.Add(intVec3);
					queue.Enqueue(intVec3);
				}
			}
		}
		SubstructureCacheSmart[key] = (hashSet, ticksGame);
		return hashSet;
	}

	public static IntVec3? FindGravshipRootConnectedToThing(Thing thing)
	{
		Map map = thing.Map;
		if (map == null || !thing.Position.InBounds(map))
		{
			return null;
		}
		if (!IsSubstructure(thing.Position, map))
		{
			return null;
		}
		int ticksGame = Find.TickManager.TicksGame;
		if (GravshipRootCacheSmart.TryGetValue(thing, out (IntVec3, int) value))
		{
			if (ticksGame - value.Item2 <= 60)
			{
				GravshipRootCacheSmart[thing] = (value.Item1, ticksGame);
				return value.Item1;
			}
			GravshipRootCacheSmart.Remove(thing);
		}
		HashSet<IntVec3> hashSet = FindConnectedSubstructure(thing.Position, map);
		if (hashSet == null || hashSet.Count == 0)
		{
			return null;
		}
		IntVec3 intVec = hashSet.MinBy((IntVec3 c) => c.x + c.z * 1000);
		GravshipRootCacheSmart[thing] = (intVec, ticksGame);
		return intVec;
	}

	public static IntVec3? FindGravshipRootConnectedToConsole(Thing console)
	{
		return FindGravshipRootConnectedToThing(console);
	}

	public static bool ArePartsOnSameSubstructure(Thing a, Thing b)
	{
		if (a.Map != b.Map || a.Map == null)
		{
			return false;
		}
		HashSet<IntVec3> hashSet = FindConnectedSubstructure(a.Position, a.Map);
		return hashSet.Contains(b.Position);
	}

	public static bool IsPartOnGravshipFoundation(Thing thing)
	{
		return IsSubstructure(thing.Position, thing.Map);
	}

	public static bool HasConnectedReactorAndEngine(Thing console, out bool reactorOn, out bool enginePresent)
	{
		reactorOn = false;
		enginePresent = false;
		if (console?.Map == null)
		{
			return false;
		}
		IntVec3? intVec = FindGravshipRootConnectedToThing(console);
		if (!intVec.HasValue)
		{
			return false;
		}
		foreach (Thing item in console.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
		{
			if (FindGravshipRootConnectedToThing(item) != intVec)
			{
				continue;
			}
			if (item.def.defName == "Ship_Reactor")
			{
				CompPowerTrader compPowerTrader = item.TryGetComp<CompPowerTrader>();
				if (compPowerTrader != null && compPowerTrader.PowerOn)
				{
					reactorOn = true;
				}
			}
			if (item.def.defName == "Ship_Engine")
			{
				enginePresent = true;
			}
		}
		return reactorOn & enginePresent;
	}

	public static float GetLaunchQuality(Building_GravEngine engine)
	{
		float num = 1f;
		if (engine.GravshipComponents.Any((CompGravshipFacility c) => c.parent.def.defName == "Ship_ComputerCore"))
		{
			num += 0.4f;
		}
		if (engine.GravshipComponents.Any((CompGravshipFacility c) => c.parent.def.defName == "Ship_SensorCluster"))
		{
			num += 0.1f;
		}
		return Mathf.Clamp01(num);
	}

	public static List<Building> GetGravshipStructure(IntVec3 gravRoot, Map map)
	{
		HashSet<IntVec3> source = FindConnectedSubstructure(gravRoot, map);
		return (from b in source.SelectMany((IntVec3 c) => c.GetThingList(map)).OfType<Building>()
			where b.def.building?.shipPart ?? false
			select b).ToList();
	}

	public static List<Building> GetGravshipStructureWithThrusters(IntVec3 gravRoot, Map map)
	{
		HashSet<IntVec3> source = FindConnectedSubstructure(gravRoot, map);
		return source.SelectMany((IntVec3 c) => c.GetThingList(map)).OfType<Building>().Where(delegate(Building b)
		{
			BuildingProperties building = b.def.building;
			return (building != null && building.shipPart) || b.TryGetComp<CompGravshipThruster>() != null;
		})
			.ToList();
	}

	public static void UpdateAllGravshipConnections(Map map)
	{
		if (map == null)
		{
			return;
		}
		GravshipRootCache.Clear();
		foreach (Thing item in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
		{
			if (item.Spawned)
			{
				BuildingProperties building = item.def.building;
				if (building != null && building.shipPart)
				{
					FindGravshipRootConnectedToThing(item);
				}
			}
		}
	}

	public static bool HasRunningReactor(Map map, IntVec3 root)
	{
		int ticksGame = Find.TickManager.TicksGame;
		(Map, IntVec3) key = (map, root);
		if (ReactorCheckCacheSmart.TryGetValue(key, out (bool, int) value))
		{
			if (ticksGame - value.Item2 <= 60)
			{
				ReactorCheckCacheSmart[key] = (value.Item1, ticksGame);
				return value.Item1;
			}
			ReactorCheckCacheSmart.Remove(key);
		}
		List<Thing> list = (from t in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
			where t.def.defName == "Ship_Reactor"
			select t).OfType<Thing>().ToList();
		bool flag = false;
		foreach (Thing item in list)
		{
			CompHibernatable compHibernatable = item.TryGetComp<CompHibernatable>();
			if (compHibernatable == null || !compHibernatable.Running || !(FindGravshipRootConnectedToThing(item) == root))
			{
				continue;
			}
			flag = true;
			break;
		}
		ReactorCheckCacheSmart[key] = (flag, ticksGame);
		return flag;
	}

	public static GravshipStatus EvaluateGravshipStatus(Map map, IntVec3 root, List<Building> structure)
	{
		bool flag = HasRunningReactor(map, root);
		bool flag2 = false;
		bool flag3 = false;
		foreach (Building item in structure)
		{
			CompGravshipThruster compGravshipThruster = item.TryGetComp<CompGravshipThruster>();
			if (compGravshipThruster != null)
			{
				bool flag4 = compGravshipThruster.Breakdownable?.BrokenDown ?? false;
				bool blocked = compGravshipThruster.Blocked;
				if (!flag4 && !blocked)
				{
					if (item.def.defName == "Ship_Engine")
					{
						flag2 = true;
					}
					else
					{
						flag3 = true;
					}
				}
			}
			if (flag && flag2 && flag3)
			{
				break;
			}
		}
		return new GravshipStatus(flag, flag2, flag3);
	}

	public static GravshipStatus EvaluateGravshipStatusCached(Map map, IntVec3 root)
	{
		int ticksGame = Find.TickManager.TicksGame;
		(Map, IntVec3) key = (map, root);
		if (GravshipStatusCacheSmart.TryGetValue(key, out (GravshipStatus, int) value))
		{
			if (ticksGame - value.Item2 <= 60)
			{
				GravshipStatusCacheSmart[key] = (value.Item1, ticksGame);
				return value.Item1;
			}
			GravshipStatusCacheSmart.Remove(key);
		}
		List<List<Building>> allGravshipStructuresCached = GetAllGravshipStructuresCached(map);
		Dictionary<Thing, IntVec3?> rootCache = new Dictionary<Thing, IntVec3?>();
		List<Building> list = allGravshipStructuresCached.FirstOrDefault((List<Building> group) => group.Any(delegate(Building b)
		{
			if (!rootCache.TryGetValue(b, out var value2))
			{
				value2 = FindGravshipRootConnectedToThing(b);
				rootCache[b] = value2;
			}
			return value2.HasValue && value2.Value == root;
		}));
		if (list == null)
		{
			GravshipStatus gravshipStatus = new GravshipStatus(hasReactor: false, hasNuclearEngine: false, hasFunctionalThruster: false);
			GravshipStatusCacheSmart[key] = (gravshipStatus, ticksGame);
			return gravshipStatus;
		}
		List<Building> gravshipStructureWithThrusters = GetGravshipStructureWithThrusters(root, map);
		GravshipStatus gravshipStatus2 = EvaluateGravshipStatus(map, root, gravshipStructureWithThrusters);
		GravshipStatusCacheSmart[key] = (gravshipStatus2, ticksGame);
		return gravshipStatus2;
	}

	public static List<Building> GetVanillaStructureFromBeam(Building root)
	{
		Map map = root.Map;
		if (map == null || root.Destroyed || !root.Spawned)
		{
			return new List<Building>();
		}
		(Map, IntVec3) key = (map, root.Position);
		int ticksGame = Find.TickManager.TicksGame;
		if (VanillaStructureCacheSmart.TryGetValue(key, out (List<Building>, int) value))
		{
			if (ticksGame - value.Item2 <= 60)
			{
				VanillaStructureCacheSmart[key] = (value.Item1, ticksGame);
				if (GravshipLogger.ShouldLog)
				{
					Log.Warning($"[Gravship DEBUG] VanillaStructure cache hit for {root.def.defName} at {root.Position}, count={value.Item1.Count}");
				}
				return value.Item1;
			}
			VanillaStructureCacheSmart.Remove(key);
		}
		List<List<Building>> allVanillaStructuresCached = GetAllVanillaStructuresCached(map);
		List<Building> list = allVanillaStructuresCached.FirstOrDefault((List<Building> group) => group.Contains(root)) ?? new List<Building>();
		VanillaStructureCacheSmart[key] = (list, ticksGame);
		if (GravshipLogger.ShouldLog)
		{
			Log.Warning($"[Gravship DEBUG] VanillaStructure resolved for {root.def.defName} at {root.Position}, count={list.Count}");
			foreach (Building item in list)
			{
				Log.Warning($"  [VanillaPart] {item.def.defName} at {item.Position}");
			}
		}
		return list;
	}

	public static List<List<Building>> GetAllVanillaStructuresCached(Map map)
	{
		int ticksGame = Find.TickManager.TicksGame;
		int uniqueID = map.uniqueID;
		if (VanillaStructuresCacheSmart.TryGetValue(uniqueID, out (List<List<Building>>, int) value))
		{
			if (ticksGame - value.Item2 <= 60)
			{
				VanillaStructuresCacheSmart[uniqueID] = (value.Item1, ticksGame);
				return value.Item1;
			}
			VanillaStructuresCacheSmart.Remove(uniqueID);
		}
		List<Building> list = (from b in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).OfType<Building>()
			where b.def == ThingDefOf.Ship_Beam && b.Spawned && !b.Destroyed
			select b).ToList();
		HashSet<Building> hashSet = new HashSet<Building>();
		List<List<Building>> list2 = new List<List<Building>>();
		foreach (Building item in list)
		{
			if (hashSet.Contains(item))
			{
				continue;
			}
			List<Building> list3 = new List<Building>();
			Queue<Building> queue = new Queue<Building>();
			queue.Enqueue(item);
			hashSet.Add(item);
			while (queue.Count > 0)
			{
				Building building = queue.Dequeue();
				list3.Add(building);
				foreach (IntVec3 item2 in GenAdj.CellsAdjacentCardinal(building))
				{
					Building edifice = item2.GetEdifice(map);
					if (edifice != null)
					{
						Building building2 = edifice;
						BuildingProperties building3 = building2.def.building;
						if (building3 != null && building3.shipPart && !hashSet.Contains(building2))
						{
							hashSet.Add(building2);
							queue.Enqueue(building2);
						}
					}
				}
			}
			if (list3.Count > 0)
			{
				list2.Add(list3);
			}
		}
		VanillaStructuresCacheSmart[uniqueID] = (list2, ticksGame);
		return list2;
	}

	public static List<List<Building>> GetAllGravshipStructures(Map map)
	{
		List<List<Building>> list = new List<List<Building>>();
		List<Building> list2 = (from b in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).OfType<Building>()
			where b.Spawned && !b.Destroyed && (b.def.building?.shipPart ?? false)
			select b).ToList();
		HashSet<Building> vanillaParts = new HashSet<Building>();
		foreach (Building item in list2.Where((Building b) => b.def == ThingDefOf.Ship_Beam))
		{
			foreach (Building item2 in GetVanillaStructureFromBeam(item))
			{
				vanillaParts.Add(item2);
			}
		}
		HashSet<IntVec3> hashSet = new HashSet<IntVec3>();
		foreach (Building item3 in list2)
		{
			if (vanillaParts.Contains(item3) || !IsSubstructure(item3.Position, map))
			{
				continue;
			}
			IntVec3? intVec = FindGravshipRootConnectedToThing(item3);
			if (!intVec.HasValue || hashSet.Contains(intVec.Value))
			{
				continue;
			}
			hashSet.Add(intVec.Value);
			HashSet<IntVec3> source = FindConnectedSubstructure(intVec.Value, map);
			List<Building> list3 = source.SelectMany((IntVec3 cell) => cell.GetThingList(map)).OfType<Building>().Where(delegate(Building b)
			{
				int result;
				if (b.Spawned && !b.Destroyed)
				{
					BuildingProperties building = b.def.building;
					if (building != null && building.shipPart)
					{
						result = ((!vanillaParts.Contains(b)) ? 1 : 0);
						goto IL_003b;
					}
				}
				result = 0;
				goto IL_003b;
				IL_003b:
				return (byte)result != 0;
			})
				.Distinct()
				.ToList();
			if (list3.Count > 0)
			{
				list.Add(list3);
			}
		}
		return list;
	}

	public static List<List<Building>> GetAllGravshipStructuresCached(Map map)
	{
		int ticksGame = Find.TickManager.TicksGame;
		int uniqueID = map.uniqueID;
		if (GravshipStructureCacheSmart.TryGetValue(uniqueID, out (List<List<Building>>, int) value))
		{
			if (ticksGame - value.Item2 <= 60)
			{
				GravshipStructureCacheSmart[uniqueID] = (value.Item1, ticksGame);
				if (GravshipLogger.ShouldLog)
				{
					Log.Warning($"[Gravship DEBUG] GravshipStructure cache hit for map {map.Index}, structure count = {value.Item1.Count}");
				}
				return value.Item1;
			}
			GravshipStructureCacheSmart.Remove(uniqueID);
		}
		List<List<Building>> allGravshipStructures = GetAllGravshipStructures(map);
		GravshipStructureCacheSmart[uniqueID] = (allGravshipStructures, ticksGame);
		if (GravshipLogger.ShouldLog)
		{
			Log.Warning($"[Gravship DEBUG] GravshipStructure recalculated for map {map.Index}, structure count = {allGravshipStructures.Count}");
			int num = 0;
			foreach (List<Building> item in allGravshipStructures)
			{
				Log.Warning($"  [GravshipGroup #{num++}] size={item.Count}");
				foreach (Building item2 in item)
				{
					Log.Warning($"    [GravPart] {item2.def.defName} at {item2.Position}");
				}
			}
		}
		return allGravshipStructures;
	}

	public static void ClearAllShipCaches(Map map)
	{
		if (map != null)
		{
			int uniqueID = map.uniqueID;
			SubstructureCacheSmart.Keys.Where<(Map, IntVec3)>(((Map, IntVec3) k) => k.Item1 == map).ToList().ForEach(delegate((Map, IntVec3) k)
			{
				SubstructureCacheSmart.Remove(k);
			});
			GravshipRootCacheSmart.Keys.Where((Thing k) => k.Map == map).ToList().ForEach(delegate(Thing k)
			{
				GravshipRootCacheSmart.Remove(k);
			});
			ReactorCheckCacheSmart.Keys.Where<(Map, IntVec3)>(((Map map, IntVec3 root) k) => k.map == map).ToList().ForEach(delegate((Map map, IntVec3 root) k)
			{
				ReactorCheckCacheSmart.Remove(k);
			});
			GravshipStatusCacheSmart.Keys.Where<(Map, IntVec3)>(((Map map, IntVec3 root) k) => k.map == map).ToList().ForEach(delegate((Map map, IntVec3 root) k)
			{
				GravshipStatusCacheSmart.Remove(k);
			});
			VanillaStructureCacheSmart.Keys.Where<(Map, IntVec3)>(((Map, IntVec3) k) => k.Item1 == map).ToList().ForEach(delegate((Map, IntVec3) k)
			{
				VanillaStructureCacheSmart.Remove(k);
			});
			VanillaStructuresCacheSmart.Remove(uniqueID);
			GravshipStructureCacheSmart.Remove(uniqueID);
			if (GravshipLogger.ShouldLog)
			{
				Log.Warning($"[Gravship DEBUG] All ship-related caches cleared for map {map.Index}");
			}
		}
	}
}
