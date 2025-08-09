using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace GravshiptoSpaceship;

public static class GravshipNewGameSaver
{
	private const string FolderName = "GravshipToSpaceship";

	public static string GetExportPath()
	{
		string text = Path.Combine(GenFilePaths.ConfigFolderPath, "GravshipToSpaceship");
		Directory.CreateDirectory(text);
		string text2 = DateTime.Now.ToString("yyyyMMdd_HHmmss");
		string path = "GravshipExport_" + text2 + ".xml";
		return Path.Combine(text, path);
	}

	public static void SaveGravshipData(List<Thing> allThings)
	{
		GravshiptoSpaceshipMod.PreCleanPawnAllowedAreas(allThings);
		GravshiptoSpaceshipMod.CleanInvalidAllowedAreasBeforeSave();
		GravshiptoSpaceshipMod.ClearAllowedAreas(allThings);
		GravshipExportData target = new GravshipExportData();
		Map map = allThings.FirstOrDefault()?.Map;
		if (map == null)
		{
			Log.Warning("[Gravship] マップが取得できませんでした");
			return;
		}
		target.originalMapSizeX = map.Size.x;
		target.originalMapSizeZ = map.Size.z;
		Thing thing = allThings.FirstOrDefault<Thing>((Thing thing2) => thing2.def.defName == "Ship_ComputerCore");
		if (thing == null)
		{
			Log.Warning("[Gravship] Ship_ComputerCore が見つかりませんでした");
			return;
		}
		HashSet<IntVec3> hashSet = GravshipConnectionUtility.FindConnectedSubstructure(GravshipConnectionUtility.FindGravshipRootConnectedToThing(thing).Value, map);
		TerrainGrid terrainGrid = map.terrainGrid;
		HashSet<IntVec3> hashSet2 = new HashSet<IntVec3>();
		foreach (Thing t in allThings)
		{
			if (t.Destroyed)
			{
				continue;
			}
			if (t is Pawn || t is Corpse { InnerPawn: not null })
			{
				Pawn pawn = t as Pawn;
				if (pawn == null && t is Corpse corpse2)
				{
					pawn = corpse2.InnerPawn;
				}
				if (pawn != null && pawn.Dead)
				{
					Log.Warning("[Gravship] 死亡済ポーンを除外: " + pawn.LabelShortCap + " (" + pawn.ThingID + ")");
					continue;
				}
			}
			if (!(t is Pawn) && !(t is Building) && !t.def.EverHaulable)
			{
				continue;
			}
			SavedThing savedThing = new SavedThing
			{
				defName = t.def.defName,
				stuffDef = t.Stuff?.defName,
				posX = t.Position.x,
				posZ = t.Position.z,
				rotation = t.Rotation.AsInt
			};
			ThingOwner thingOwner = null;
			if (t is IThingHolder thingHolder)
			{
				thingOwner = thingHolder.GetDirectlyHeldThings();
			}
			else
			{
				CompTransporter compTransporter = t.TryGetComp<CompTransporter>();
				if (compTransporter != null)
				{
					thingOwner = compTransporter.innerContainer;
				}
				else
				{
					CompThingContainer compThingContainer = t.TryGetComp<CompThingContainer>();
					if (compThingContainer != null)
					{
						thingOwner = compThingContainer.innerContainer;
					}
				}
			}
			if (thingOwner != null && thingOwner.Any)
			{
				bool flag = t.def.defName != "Ship_CryptosleepCasket" && thingOwner.Any((Thing th) => th is Pawn pawn9 && pawn9.RaceProps.IsFlesh && !pawn9.Dead);
				List<Thing> list = thingOwner.Where((Thing th) => t.def.defName == "Ship_CryptosleepCasket" || !(th is Pawn pawn9) || !pawn9.RaceProps.IsFlesh || pawn9.Dead).ToList();
				if (flag)
				{
					Log.Warning($"[Gravship] innerContainerXml from {t.LabelCap}: skipping live flesh pawns, keeping {list.Count} items.");
				}
				if (list.Any())
				{
					FieldInfo field = typeof(Pawn_PlayerSettings).GetField("allowedAreas", BindingFlags.Instance | BindingFlags.NonPublic);
					List<Map> maps = Find.Maps;
					foreach (Thing item in list)
					{
						Pawn pawn2 = item as Pawn;
						if (pawn2 == null && item is Corpse corpse3)
						{
							pawn2 = corpse3.InnerPawn;
						}
						if (pawn2?.playerSettings == null)
						{
							continue;
						}
						field.SetValue(pawn2.playerSettings, new Dictionary<Map, Area>());
						if (!(field.GetValue(pawn2.playerSettings) is Dictionary<Map, Area> dictionary))
						{
							continue;
						}
						Dictionary<Map, Area> dictionary2 = new Dictionary<Map, Area>();
						foreach (KeyValuePair<Map, Area> item2 in dictionary)
						{
							if (item2.Key == null || item2.Value == null)
							{
								Log.Warning($"[Gravship DEBUG] 保存時 innerContainer: Pawn={pawn2.LabelShortCap} ({pawn2.ThingID}) に不正な allowedAreas エントリあり → key={item2.Key}, val={item2.Value}");
							}
							if (item2.Key != null && item2.Value != null)
							{
								AreaManager areaManager = item2.Key.areaManager;
								if (areaManager != null && areaManager.AllAreas?.Contains(item2.Value) == true)
								{
									dictionary2[item2.Key] = item2.Value;
								}
							}
						}
						field.SetValue(pawn2.playerSettings, dictionary2);
					}
					try
					{
						ThingOwner<Thing> thingOwner2 = new ThingOwner<Thing>();
						thingOwner2.TryAddRangeOrTransfer(list);
						string text = Path.Combine(GenFilePaths.TempFolderPath, "temp_inner.xml");
						Scribe.saver.InitSaving(text, "ThingContainer");
						thingOwner2.ExposeData();
						Scribe.saver.FinalizeSaving();
						savedThing.innerContainerXml = File.ReadAllText(text);
					}
					catch (Exception arg)
					{
						Log.Warning($"[Gravship] Thing中身保存エラー: {arg}");
					}
				}
			}
			CompRefuelable compRefuelable = t.TryGetComp<CompRefuelable>();
			if (compRefuelable != null)
			{
				savedThing.extraData["fuel"] = compRefuelable.Fuel.ToString();
			}
			CompHibernatable compHibernatable = t.TryGetComp<CompHibernatable>();
			if (compHibernatable != null)
			{
				savedThing.extraData["hibernatableState"] = compHibernatable.State?.defName;
			}
			CompPowerTrader compPowerTrader = t.TryGetComp<CompPowerTrader>();
			if (compPowerTrader != null)
			{
				savedThing.extraData["powerOn"] = compPowerTrader.PowerOn.ToString();
			}
			if (t is Building_GravEngine building_GravEngine)
			{
				savedThing.extraData["gravshipName"] = building_GravEngine.RenamableLabel;
				savedThing.extraData["nameHidden"] = building_GravEngine.nameHidden.ToString();
			}
			target.Things.Add(savedThing);
			HashSet<string> savedPawnIDs = new HashSet<string>(from p in allThings.OfType<Pawn>()
				where !p.Destroyed && !p.Dead
				select p.ThingID);
			List<Pawn> list2 = PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead.Where((Pawn p) => !savedPawnIDs.Contains(p.ThingID)).ToList();
			TaleManager taleManager = Find.TaleManager;
			if (taleManager != null)
			{
				List<Tale> allTalesListForReading = taleManager.AllTalesListForReading;
				List<Tale> list3 = new List<Tale>();
				foreach (Tale item3 in allTalesListForReading)
				{
					foreach (Pawn item4 in list2)
					{
						if (item3.Concerns(item4))
						{
							list3.Add(item3);
							Log.Message("[Gravship] Tale " + item3.def.defName + " を削除対象に追加（" + item4.LabelShortCap + "）");
							break;
						}
					}
				}
				foreach (Tale item5 in list3.Distinct())
				{
					if (item5.Unused)
					{
						allTalesListForReading.Remove(item5);
						Log.Message("[Gravship] Tale " + item5.def.defName + " を削除（Unused）");
						continue;
					}
					Log.Warning("[Gravship] Tale " + item5.def.defName + " は使用中のため削除不可（" + item5.ShortSummary + "）");
				}
			}
			HashSet<Pawn> hashSet3 = new HashSet<Pawn>();
			foreach (Thing allThing in allThings)
			{
				if (allThing is Pawn pawn3)
				{
					if (hashSet3.Add(pawn3))
					{
						Log.Message("[Gravship] Pawn検出: " + pawn3.LabelShortCap + " (" + pawn3.ThingID + ") from " + allThing.LabelCap + " (" + allThing.def.defName + ")");
					}
					continue;
				}
				if (allThing is Corpse { InnerPawn: not null } corpse4)
				{
					if (hashSet3.Add(corpse4.InnerPawn))
					{
						Log.Message("[Gravship] Corpse内のポーン検出: " + corpse4.InnerPawn.LabelShortCap + " (" + corpse4.InnerPawn.ThingID + ") from " + allThing.LabelCap + " (" + allThing.def.defName + ")");
					}
					continue;
				}
				if (allThing is IThingHolder thingHolder2)
				{
					foreach (Thing item6 in (IEnumerable<Thing>)thingHolder2.GetDirectlyHeldThings())
					{
						if (item6 is Pawn pawn4 && hashSet3.Add(pawn4))
						{
							Log.Message("[Gravship] IThingHolder内のポーン検出: " + pawn4.LabelShortCap + " (" + pawn4.ThingID + ") from " + allThing.LabelCap + " (" + allThing.def.defName + ")");
						}
						else if (item6 is Corpse { InnerPawn: not null } corpse5 && hashSet3.Add(corpse5.InnerPawn))
						{
							Log.Message("[Gravship] IThingHolder内の死体からポーン検出: " + corpse5.InnerPawn.LabelShortCap + " (" + corpse5.InnerPawn.ThingID + ") from " + allThing.LabelCap + " (" + allThing.def.defName + ")");
						}
					}
					continue;
				}
				CompThingContainer compThingContainer2 = allThing.TryGetComp<CompThingContainer>();
				if (compThingContainer2 == null)
				{
					continue;
				}
				foreach (Thing item7 in (IEnumerable<Thing>)compThingContainer2.innerContainer)
				{
					if (item7 is Pawn pawn5 && hashSet3.Add(pawn5))
					{
						Log.Message("[Gravship] innerContainer 内のポーン検出: " + pawn5.LabelShortCap + " (" + pawn5.ThingID + ") from " + allThing.LabelCap + " (" + allThing.def.defName + ")");
					}
					else if (item7 is Corpse { InnerPawn: not null } corpse6 && hashSet3.Add(corpse6.InnerPawn))
					{
						Log.Message("[Gravship] innerContainer 内の死体からポーン検出: " + corpse6.InnerPawn.LabelShortCap + " (" + corpse6.InnerPawn.ThingID + ") from " + allThing.LabelCap + " (" + allThing.def.defName + ")");
					}
				}
			}
			Log.Message("[Gravship] --- 保存対象ポーンの Thought_MemorySocial ログ出力 ---");
			foreach (Pawn item8 in hashSet3)
			{
				List<Thought_MemorySocial> list4 = item8?.needs?.mood?.thoughts?.memories?.Memories?.OfType<Thought_MemorySocial>()?.ToList();
				if (list4 == null || list4.Count == 0)
				{
					continue;
				}
				Log.Message("[Gravship] " + item8.LabelShortCap + " (" + item8.ThingID + ") の Thought_MemorySocial 一覧:");
				foreach (Thought_MemorySocial item9 in list4)
				{
					Pawn pawn6 = item9.OtherPawn();
					string text2 = ((pawn6 != null) ? (pawn6.LabelShortCap + " (" + pawn6.ThingID + ")") : "null");
					Log.Message("[Gravship]  - " + item9.def.defName + " 対象: " + text2);
				}
			}
			HashSet<string> allSavedPawnIDs = new HashSet<string>(hashSet3.Select((Pawn p) => p.ThingID));
			foreach (Pawn item10 in hashSet3)
			{
				List<Thought_Memory> list5 = item10?.needs?.mood?.thoughts?.memories?.Memories?.ToList();
				if (list5 == null)
				{
					continue;
				}
				foreach (Thought_MemorySocial item11 in list5.OfType<Thought_MemorySocial>())
				{
					Pawn pawn7 = item11.OtherPawn();
					if (pawn7 != null && !allSavedPawnIDs.Contains(pawn7.ThingID))
					{
						item10.needs.mood.thoughts.memories.RemoveMemory(item11);
						Log.Message("[Gravship] " + item10.LabelShortCap + " から " + item11.def.defName + " (対: " + pawn7.LabelShortCap + ") を削除");
					}
				}
			}
			foreach (Pawn item12 in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead)
			{
				List<Thought_Memory> list6 = item12?.needs?.mood?.thoughts?.memories?.Memories?.ToList();
				if (list6 == null)
				{
					continue;
				}
				foreach (Thought_MemorySocial item13 in list6.OfType<Thought_MemorySocial>())
				{
					Pawn pawn8 = item13.OtherPawn();
					if (pawn8 != null && !savedPawnIDs.Contains(pawn8.ThingID))
					{
						item12.needs.mood.thoughts.memories.RemoveMemory(item13);
						Log.Message("[Gravship] 全体削除: " + item12.LabelShortCap + " から " + item13.def.defName + " (対: " + pawn8.LabelShortCap + ") を削除");
					}
				}
			}
			foreach (Pawn item14 in hashSet3)
			{
				if (item14.outfits != null)
				{
					item14.outfits.CurrentApparelPolicy = null;
				}
				if (item14.drugs != null)
				{
					item14.drugs.CurrentPolicy = null;
				}
				if (item14.foodRestriction != null)
				{
					item14.foodRestriction.CurrentFoodPolicy = null;
				}
				// 意识形态相关功能已移除
			}
			if (Find.BattleLog == null)
			{
				continue;
			}
			List<LogEntry> list7 = Find.BattleLog.Battles.SelectMany((Battle b) => b.Entries).ToList();
			foreach (LogEntry item15 in list7)
			{
				if (item15 != null)
				{
					IEnumerable<Thing> concerns = item15.GetConcerns();
					if (concerns != null && concerns.Any((Thing thing2) => thing2 is Pawn pawn9 && !allSavedPawnIDs.Contains(pawn9.ThingID)))
					{
						Find.BattleLog.RemoveEntry(item15);
						Log.Message("[Gravship] BattleLog entry removed due to unknown pawn ref");
					}
				}
			}
		}
		foreach (IntVec3 item16 in hashSet)
		{
			if (item16.InBounds(map) && hashSet2.Add(item16))
			{
				TerrainDef terrainDef = terrainGrid.FoundationAt(item16);
				TerrainDef terrainDef2 = terrainGrid.TopTerrainAt(item16);
				string baseTerrain = terrainDef?.defName;
				string floorTerrain = null;
				if (terrainDef2 != null && terrainDef2.layerable && terrainDef2 != TerrainDefOf.Space)
				{
					floorTerrain = terrainDef2.defName;
				}
				RoofDef roofDef = map.roofGrid.RoofAt(item16);
				target.Terrain.Add(new SavedTerrain
				{
					posX = item16.x,
					posZ = item16.z,
					baseTerrain = baseTerrain,
					floorTerrain = floorTerrain,
					roofDef = roofDef?.defName
				});
			}
		}
		target.completedResearch = (from r in DefDatabase<ResearchProjectDef>.AllDefs
			where r.IsFinished
			select r.defName).ToList();
		target.extraFlags["gravEngineInspected"] = Find.ResearchManager.gravEngineInspected.ToString();
		target.extraFlags["departureYear"] = GenDate.Year(Find.TickManager.TicksAbs, 0f).ToString();
		try
		{
			string exportPath = GetExportPath();
			Scribe.saver.InitSaving(exportPath, "GravshipToNewGame");
			Scribe_Deep.Look(ref target, "GravshipExportData");
			Scribe.saver.FinalizeSaving();
			Log.Message("[Gravship] 発射船データを保存しました: " + exportPath);
		}
		catch (Exception arg2)
		{
			Log.Warning($"[Gravship] データ保存中に例外が発生: {arg2}");
		}
	}
}
