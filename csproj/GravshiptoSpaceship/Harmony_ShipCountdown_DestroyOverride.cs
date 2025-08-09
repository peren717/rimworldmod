using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(ShipCountdown), "CountdownEnded")]
public static class Harmony_ShipCountdown_DestroyOverride
{
	private static bool Prefix()
	{
		if (GravshipLogger.EnableLogging)
		{
			Log.Warning("[Gravship DEBUG] CountdownEnded triggered");
		}
		if (ShipCountdown.CountingDown)
		{
			Harmony_ShipBuildingsAttachedTo_ForGravship.hasLoggedThisLaunch = false;
		}
		if (!(AccessTools.Field(typeof(ShipCountdown), "shipRoot")?.GetValue(null) is Building { Map: not null } building))
		{
			return true;
		}
		List<string> list = ShipUtility.LaunchFailReasons(building).ToList();
		if (list.Count > 0)
		{
			if (GravshipLogger.EnableLogging)
			{
				Log.Warning("[Gravship] Launch aborted due to fail reasons:\n - " + string.Join("\n - ", list));
			}
			return true;
		}
		Map map = building.Map;
		if (GravshipLogger.EnableLogging)
		{
			Log.Warning($"[Gravship] DestroyOverride called for root={building.LabelCap} at {building.Position}, map={map.Index}");
		}
		if (GravshipLogger.EnableLogging)
		{
			Log.Warning($"[Gravship] isGravshipLaunch={Harmony_ShipBuildingsAttachedTo_ForGravship.isGravshipLaunch}, launchCells.Contains={Harmony_ShipBuildingsAttachedTo_ForGravship.launchCells.Contains(building.Position)}");
		}
		IntVec3? intVec = GravshipConnectionUtility.FindGravshipRootConnectedToThing(building);
		if (!intVec.HasValue)
		{
			return true;
		}
		if (!Harmony_ShipBuildingsAttachedTo_ForGravship.launchCells.Contains(building.Position))
		{
			return true;
		}
		if (!Harmony_ShipBuildingsAttachedTo_ForGravship.isGravshipLaunch)
		{
			return true;
		}
		HashSet<IntVec3> hashSet = GravshipConnectionUtility.FindConnectedSubstructure(intVec.Value, map);
		List<Thing> list2 = (from t in hashSet.SelectMany((IntVec3 c) => c.GetThingList(map))
			where t != null && t.Spawned && t.Map == map
			select t).Distinct().ToList();
		List<Thing> allThings = hashSet.SelectMany((IntVec3 c) => c.GetThingList(map)).Where(delegate(Thing t)
		{
			int result;
			if (t != null)
			{
				if (t.Spawned || t is IThingHolder)
				{
					result = ((t.Map == map) ? 1 : 0);
					goto IL_0026;
				}
			}
			result = 0;
			goto IL_0026;
			IL_0026:
			return (byte)result != 0;
		}).Distinct()
			.ToList();
		
		// Save gravship data before destroying anything
		try
		{
			if (GravshipLogger.EnableLogging)
			{
				Log.Message("[Gravship] Saving gravship data before launch...");
			}
			GravshipNewGameSaver.SaveGravshipData(allThings);
			if (GravshipLogger.EnableLogging)
			{
				Log.Message("[Gravship] Gravship data saved successfully.");
			}
		}
		catch (Exception ex)
		{
			Log.Error($"[Gravship] Failed to save gravship data: {ex}");
		}
		
		HashSet<Pawn> casketPawns = (from c in allThings.OfType<Building_CryptosleepCasket>()
			where c.ContainedThing is Pawn
			select (Pawn)c.ContainedThing).ToHashSet();
		List<Pawn> list3 = (from p in allThings.OfType<Pawn>()
			where !casketPawns.Contains(p)
			select p).ToList();
		List<Pawn> list4 = (from p in allThings.OfType<Pawn>()
			where p.RaceProps.IsFlesh
			where !allThings.OfType<Building_CryptosleepCasket>().Any((Building_CryptosleepCasket c) => c.HasAnyContents && c.ContainedThing == p)
			select p).ToList();
		foreach (Building_CryptosleepCasket item in allThings.OfType<Building_CryptosleepCasket>())
		{
			string arg = ((item.ContainedThing is Pawn pawn) ? pawn.LabelShortCap : "null");
			if (GravshipLogger.EnableLogging)
			{
				Log.Message($"[Gravship DEBUG] Casket at {item.Position}, HasAnyContents={item.HasAnyContents}, ContainedPawn={arg}");
			}
		}
		if (GravshipLogger.EnableLogging)
		{
			Log.Message("[Gravship DEBUG] --- 脱出対象（CryptosleepCasket内）ポーン一覧 ---");
		}
		foreach (Pawn item2 in casketPawns)
		{
			if (GravshipLogger.EnableLogging)
			{
				Log.Message("[Gravship DEBUG]  - " + item2.LabelShortCap + " (" + item2.ThingID + ")");
			}
		}
		if (GravshipLogger.EnableLogging)
		{
			Log.Message("[Gravship DEBUG] --- Kill対象（CryptosleepCasket外）ポーン一覧 ---");
		}
		foreach (Pawn item3 in list3)
		{
			if (GravshipLogger.EnableLogging)
			{
				Log.Message("[Gravship DEBUG]  - " + item3.LabelShortCap + " (" + item3.ThingID + ")");
			}
		}
		foreach (Pawn item4 in list3)
		{
			if (GravshipLogger.EnableLogging)
			{
				Log.Warning("[Gravship] " + item4.LabelShortCap + " was outside cryptosleep casket and will be killed.");
			}
			item4.Kill(null, null);
		}
		List<Pawn> list5 = casketPawns.ToList();
		string text = list5.Select((Pawn p) => "   " + p.LabelCap).Join(null, "\n");
		text += "\n";
		foreach (Pawn item5 in list5)
		{
			Find.StoryWatcher.statsRecord.colonistsLaunched++;
			TaleRecorder.RecordTale(TaleDefOf.LaunchedShip, item5);
			if (GravshipLogger.EnableLogging)
			{
				Log.Message("[Gravship] Launch tale recorded for: " + item5.LabelCap);
			}
		}
		string victoryText = GameVictoryUtility.MakeEndCredits("GameOverShipLaunchedIntro".Translate(), "GameOverShipLaunchedEnding".Translate(), text, "GameOverColonistsEscaped", list5);
		GameVictoryUtility.ShowCredits(victoryText, SongDefOf.EndCreditsSong);
		if (GravshipLogger.EnableLogging)
		{
			Log.Message($"[Gravship] Escaped pawns count = {list5.Count}");
		}
		foreach (Pawn item6 in list5)
		{
			if (GravshipLogger.EnableLogging)
			{
				Log.Message($"[Gravship] Escaped: {item6.NameFullColored} ({item6.ThingID})");
			}
		}
		List<IThingHolder> list6 = allThings.OfType<IThingHolder>().ToList();
		HashSet<Thing> hashSet2 = new HashSet<Thing>();
		foreach (IThingHolder item7 in list6)
		{
			foreach (Thing item8 in ThingOwnerUtility.GetAllThingsRecursively(item7))
			{
				hashSet2.Add(item8);
				if (item8 is Corpse { InnerPawn: not null } corpse)
				{
					hashSet2.Add(corpse.InnerPawn);
				}
			}
		}
		List<Pawn> list7 = (from p in allThings.OfType<Pawn>()
			where !p.RaceProps.IsFlesh
			select p).ToList();
		List<Thing> list8 = new List<Thing>(allThings);
		foreach (Pawn item9 in list7)
		{
			if (GravshipLogger.EnableLogging)
			{
				Log.Warning("[Gravship] " + item9.LabelShortCap + " was mechanoid outside casket and will be destroyed.");
			}
			item9.Destroy();
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Building_CryptosleepCasket item10 in allThings.OfType<Building_CryptosleepCasket>())
		{
			if (item10.HasAnyContents && item10.ContainedThing is Pawn pawn2)
			{
				stringBuilder.AppendLine("   " + pawn2.LabelCap);
				Find.StoryWatcher.statsRecord.colonistsLaunched++;
				TaleRecorder.RecordTale(TaleDefOf.LaunchedShip, pawn2);
				if (GravshipLogger.EnableLogging)
				{
					Log.Message("[Gravship] Launch tale recorded for: " + pawn2.LabelCap);
				}
			}
		}
		foreach (IntVec3 item11 in hashSet)
		{
			map.roofGrid.SetRoof(item11, null);
		}
		foreach (Thing item12 in allThings)
		{
			try
			{
				if (item12 is Pawn)
				{
					continue;
				}
				if (item12 is MinifiedThing minifiedThing)
				{
					if (minifiedThing.InnerThing != null && !minifiedThing.InnerThing.Destroyed)
					{
						minifiedThing.InnerThing.Destroy();
					}
					if (!minifiedThing.Destroyed)
					{
						minifiedThing.Destroy();
					}
				}
				else if ((item12 is Frame || item12 is Blueprint_Build) ? true : false)
				{
					if (!item12.Destroyed)
					{
						item12.Destroy();
					}
				}
				else if (!item12.Destroyed)
				{
					item12.Destroy();
				}
			}
			catch (Exception arg2)
			{
				if (GravshipLogger.EnableLogging)
				{
					Log.Warning($"[Gravship] Failed to destroy thing: {item12.LabelCap} ({item12.GetType()})\n{arg2}");
				}
			}
		}
		foreach (IntVec3 item13 in hashSet)
		{
			try
			{
				TerrainGrid terrainGrid = map.terrainGrid;
				if (terrainGrid.CanRemoveTopLayerAt(item13))
				{
					terrainGrid.RemoveTopLayer(item13);
				}
				if (terrainGrid.CanRemoveFoundationAt(item13))
				{
					terrainGrid.RemoveFoundation(item13);
				}
				FilthMaker.RemoveAllFilth(item13, map);
			}
			catch (Exception arg3)
			{
				if (GravshipLogger.EnableLogging)
				{
					Log.Warning($"[Gravship] Failed to remove terrain at {item13}: {arg3}");
				}
			}
		}
		try
		{
			foreach (IntVec3 item14 in hashSet)
			{
				List<Thing> thingList = item14.GetThingList(map);
				foreach (Thing item15 in thingList.ToList())
				{
					if (!(item15 is Building_VacBarrier { Spawned: not false } building_VacBarrier))
					{
						continue;
					}
					try
					{
						building_VacBarrier.Destroy();
					}
					catch (Exception arg4)
					{
						if (GravshipLogger.EnableLogging)
						{
							Log.Warning($"[Gravship] Failed to destroy VacBarrier: {building_VacBarrier.LabelCap}\n{arg4}");
						}
					}
				}
			}
		}
		catch (Exception arg5)
		{
			if (GravshipLogger.EnableLogging)
			{
				Log.Error($"[Gravship] Unexpected error while destroying VacBarriers\n{arg5}");
			}
		}
		foreach (IntVec3 item16 in hashSet)
		{
			foreach (Thing item17 in item16.GetThingList(map).ToList())
			{
				if (item17 == null || !item17.Spawned || item17.Map != map || item17.Destroyed || (!item17.def.EverHaulable && item17.def.category != ThingCategory.Item && !(item17 is Filth)))
				{
					continue;
				}
				try
				{
					item17.Destroy();
				}
				catch (Exception arg6)
				{
					if (GravshipLogger.EnableLogging)
					{
						Log.Warning($"[Gravship] Failed to destroy item on substructure: {item17.LabelCap}\n{arg6}");
					}
				}
			}
		}
		try
		{
			HashSet<Thing> preLaunchNearbyThings = Harmony_ShipBuildingsAttachedTo_ForGravship.preLaunchNearbyThings;
			HashSet<IntVec3> hashSet3 = new HashSet<IntVec3>();
			foreach (IntVec3 launchCell in Harmony_ShipBuildingsAttachedTo_ForGravship.launchCells)
			{
				hashSet3.Add(launchCell);
				IntVec3[] adjacentCells = GenAdj.AdjacentCells;
				foreach (IntVec3 intVec2 in adjacentCells)
				{
					IntVec3 intVec3 = launchCell + intVec2;
					if (intVec3.InBounds(map))
					{
						hashSet3.Add(intVec3);
					}
				}
			}
			foreach (IntVec3 item18 in hashSet3)
			{
				foreach (Thing item19 in item18.GetThingList(map).ToList())
				{
					if (preLaunchNearbyThings.Contains(item19) || item19 == null || !item19.Spawned || item19.Destroyed || item19.Map != map || (!item19.def.EverHaulable && item19.def.category != ThingCategory.Item && !(item19 is Filth)))
					{
						continue;
					}
					try
					{
						item19.Destroy();
					}
					catch (Exception arg7)
					{
						if (GravshipLogger.EnableLogging)
						{
							Log.Warning($"[Gravship] Failed to destroy scattered item: {item19.LabelCap}\n{arg7}");
						}
					}
				}
			}
			Harmony_ShipBuildingsAttachedTo_ForGravship.preLaunchNearbyThings.Clear();
		}
		catch (Exception arg8)
		{
			if (GravshipLogger.EnableLogging)
			{
				Log.Warning($"[Gravship] Failed to perform scatter cleanup\n{arg8}");
			}
		}
		foreach (Pawn item20 in map.mapPawns.AllPawnsSpawned)
		{
			if (!item20.Dead && item20.Spawned && !item20.Position.Standable(map))
			{
				if (GravshipLogger.EnableLogging)
				{
					Log.Warning("[Gravship] " + item20.LabelShortCap + " is on unstandable tile after launch, ending job.");
				}
				item20.jobs?.EndCurrentJob(JobCondition.InterruptForced);
			}
		}
		foreach (Pawn item21 in map.mapPawns.AllPawns.ToList())
		{
			if (!item21.Destroyed && !item21.Dead && !item21.RaceProps.IsFlesh && hashSet.Contains(item21.Position))
			{
				if (GravshipLogger.EnableLogging)
				{
					Log.Warning("[Gravship] Final cleanup: forcibly destroying " + item21.LabelShortCap + " still on gravship.");
				}
				if (item21.Spawned)
				{
					item21.DeSpawn();
				}
				item21.Destroy();
			}
		}
		foreach (Pawn item22 in map.mapPawns.AllPawns.ToList())
		{
			if (!item22.Destroyed && !item22.Dead && item22.RaceProps.IsFlesh && hashSet.Contains(item22.Position))
			{
				if (GravshipLogger.EnableLogging)
				{
					Log.Warning("[Gravship] Final cleanup: forcibly killing " + item22.LabelShortCap + " still on gravship.");
				}
				if (item22.Spawned)
				{
					item22.DeSpawn();
				}
				item22.Kill(null, null);
			}
		}
		Harmony_ShipBuildingsAttachedTo_ForGravship.hasLoggedThisLaunch = false;
		return false;
	}
}
