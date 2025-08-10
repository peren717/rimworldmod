using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GravshiptoSpaceship;

public class GravshipRestorer : GameComponent
{
	public class GameComponent_OnetimeTicker : GameComponent
	{
		private static List<(int tick, Action action)> pending = new List<(int, Action)>();

		public GameComponent_OnetimeTicker(Game game)
		{
		}

		public override void GameComponentTick()
		{
			int ticksGame = Find.TickManager.TicksGame;
			for (int num = pending.Count - 1; num >= 0; num--)
			{
				if (pending[num].tick <= ticksGame)
				{
					try
					{
						pending[num].action();
					}
					catch (Exception arg)
					{
						Log.Error($"[Gravship] OnetimeTicker エラー: {arg}");
					}
					pending.RemoveAt(num);
				}
			}
		}

		public static void Schedule(int tick, Action action)
		{
			pending.Add((tick, action));
		}
	}

	private bool initialized = false;

	private GravshipExportData latestData;

	private bool hasRestored = false;

	public GravshipRestorer(Game game)
	{
	}

	public override void ExposeData()
	{
		Scribe_Values.Look(ref hasRestored, "Gravship_HasRestored", defaultValue: false);
	}

	public override void FinalizeInit()
	{
		// FinalizeInit中只做基本初始化，不执行恢复逻辑
		if (!initialized)
		{
			initialized = true;
		}
	}

	public void TriggerRestore()
	{
		if (hasRestored)
		{
			Log.Message("[Gravship] 重力船已经恢复过，跳过");
			return;
		}
		
		// 检查当前场景是否包含重力船恢复组件
		if (Find.Scenario?.AllParts?.Any(part => part is GravshiptoSpaceship.ScenPart_GravshipRestore) != true)
		{
			Log.Message("[Gravship] 当前场景不包含重力船恢复组件，跳过");
			return;
		}
		
		string path = Path.Combine(GenFilePaths.ConfigFolderPath, "GravshipToSpaceship");
		string text = GravshiptoSpaceshipMod.Settings?.selectedFileName;
		string text2 = Path.Combine(path, text ?? "");
		if (!File.Exists(text2))
		{
			Log.Warning("[Gravship] 復元ファイルが存在しません: " + text2);
			return;
		}
		
		try
		{
			Scribe.loader.InitLoading(text2);
			Scribe_Deep.Look(ref latestData, "GravshipExportData");
			Scribe.loader.FinalizeLoading();
			Log.Message("[Gravship] 復元ファイルの読み込み成功: " + text2);
			RestoreFromData(latestData);
			hasRestored = true;
		}
		catch (Exception arg)
		{
			Log.Error($"[Gravship] 復元中にエラーが発生しました: {arg}");
		}
		new GravshiptoSpaceshipMod.GameComp_AllowedAreaCleaner(Current.Game).ExposeData();
	}

	private void RestoreFromData(GravshipExportData data)
	{
		if (data == null)
		{
			Log.Error("[Gravship] GravshipExportData が null です。XML の読み込みに失敗している可能性があります。");
			return;
		}
		Map map = Find.CurrentMap ?? Find.Maps.FirstOrDefault();
		if (map == null)
		{
			Log.Error("[Gravship] Mapが取得できませんでした。");
			return;
		}
		Current.Game.CurrentMap = map;
		if (map != null)
		{
			MapGenerator.PlayerStartSpot = new IntVec3(map.Size.x / 2, 0, map.Size.z / 2);
			Log.Message("プレイヤー開始地点を強制的に中央に設定");
		}

		if (data.completedResearch != null)
		{
			foreach (string item2 in data.completedResearch)
			{
				ResearchProjectDef namedSilentFail = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(item2);
				if (namedSilentFail != null)
				{
					Find.ResearchManager.FinishProject(namedSilentFail, doCompletionDialog: false, null, doCompletionLetter: false);
				}
			}
		}
		if (data.Things != null)
		{
			// 计算飞船占用的所有位置
			HashSet<IntVec3> shipCells = new HashSet<IntVec3>();
			
			// 添加所有地形位置
			foreach (SavedTerrain terrain in data.Terrain)
			{
				IntVec3 pos = new IntVec3(terrain.posX, 0, terrain.posZ);
				if (pos.InBounds(map))
				{
					shipCells.Add(pos);
				}
			}
			
			// 添加所有物体位置
			foreach (SavedThing thing in data.Things)
			{
				IntVec3 pos = new IntVec3(thing.posX, 0, thing.posZ);
				if (pos.InBounds(map))
				{
					shipCells.Add(pos);
					
					// 对于大型建筑，添加其占用的所有格子
					ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(thing.defName);
					if (thingDef != null && (thingDef.size.x > 1 || thingDef.size.z > 1))
					{
						Rot4 rotation = new Rot4(thing.rotation);
						IntVec2 size = thingDef.size;
						
						// 根据旋转调整尺寸
						IntVec2 rotatedSize = size;
						if (rotation == Rot4.East || rotation == Rot4.West)
						{
							rotatedSize = new IntVec2(size.z, size.x);
						}
						
						for (int x = 0; x < rotatedSize.x; x++)
						{
							for (int z = 0; z < rotatedSize.z; z++)
							{
								IntVec3 cellPos = pos + new IntVec3(x, 0, z);
								if (cellPos.InBounds(map))
								{
									shipCells.Add(cellPos);
								}
							}
						}
					}
				}
			}
			
			// 计算飞船周围3格范围内的所有格子
			HashSet<IntVec3> clearanceCells = new HashSet<IntVec3>();
			foreach (IntVec3 shipCell in shipCells)
			{
				// 为每个飞船格子添加周围3格范围
				for (int x = -3; x <= 3; x++)
				{
					for (int z = -3; z <= 3; z++)
					{
						IntVec3 clearanceCell = shipCell + new IntVec3(x, 0, z);
						if (clearanceCell.InBounds(map))
						{
							clearanceCells.Add(clearanceCell);
						}
					}
				}
			}
			
			// 清理飞船周围3格范围 - 强制清除所有非Pawn物体
			Log.Message($"[Gravship] 开始清理飞船周围3格范围，飞船占用 {shipCells.Count} 个格子，清理范围 {clearanceCells.Count} 个格子");
			int clearedThings = 0;
			try
			{
				foreach (IntVec3 cell in clearanceCells)
				{
					if (cell.InBounds(map))
					{
						// 清理该位置的所有物体（但保留小人）
						List<Thing> thingsAtCell = map.thingGrid.ThingsListAt(cell).ToList();
						for (int i = thingsAtCell.Count - 1; i >= 0; i--)
						{
							Thing thing = thingsAtCell[i];
							
							// 只保留小人，其他所有东西都强制清除
							if (thing is Pawn)
							{
								continue;
							}
							
							try
							{
								// 使用最强力的清除方法 - 不管是什么物体都要清除
								bool destroyed = false;
								
								// 方法1：尝试Destroy
								if (!destroyed)
								{
									try
									{
										thing.Destroy(DestroyMode.Vanish);
										destroyed = true;
									}
									catch (Exception ex1)
									{
										Log.Warning($"[Gravship] Destroy失败: {thing.def.defName} - {ex1.Message}");
									}
								}
								
								// 方法2：尝试DeSpawn
								if (!destroyed)
								{
									try
									{
										thing.DeSpawn(DestroyMode.Vanish);
										destroyed = true;
									}
									catch (Exception ex2)
									{
										Log.Warning($"[Gravship] DeSpawn失败: {thing.def.defName} - {ex2.Message}");
									}
								}
								
								// 方法3：强制从thingGrid移除
								if (!destroyed)
								{
									try
									{
										map.thingGrid.Deregister(thing);
										destroyed = true;
										Log.Message($"[Gravship] 通过thingGrid.Deregister强制清除: {thing.def.defName}");
									}
									catch (Exception ex3)
									{
										Log.Error($"[Gravship] 所有清除方法都失败: {thing.def.defName} - {ex3.Message}");
									}
								}
								
								if (destroyed)
								{
									clearedThings++;
								}
							}
							catch (Exception destroyEx)
							{
								Log.Error($"[Gravship] 清除物体时发生未知错误: {thing.def.defName} - {destroyEx.Message}");
							}
						}
						
						// 清理屋顶
						if (map.roofGrid.RoofAt(cell) != null)
						{
							map.roofGrid.SetRoof(cell, null);
						}
						
						// 清理区域设置
						Zone zoneAt = map.zoneManager.ZoneAt(cell);
						if (zoneAt != null)
						{
							zoneAt.RemoveCell(cell);
						}
						
						// 清理建造区域设置
						if (map.areaManager?.BuildRoof != null)
						{
							map.areaManager.BuildRoof[cell] = false;
						}
						if (map.areaManager?.NoRoof != null)
						{
							map.areaManager.NoRoof[cell] = false;
						}
						
						// 强制替换所有特殊地形为普通地形
						TerrainDef currentTerrain = map.terrainGrid.TerrainAt(cell);
						if (currentTerrain != null)
						{
							bool needsReplacement = false;
							
							// 检查是否需要替换地形
							if (currentTerrain.passability == Traversability.Impassable ||
								currentTerrain.dangerous ||
								currentTerrain.defName.Contains("Rock") ||
								currentTerrain.defName.Contains("Wall") ||
								currentTerrain.defName.Contains("Mountain") ||
								currentTerrain.defName.Contains("Geyser") ||
								currentTerrain.defName.Contains("geyser") ||
								currentTerrain.defName.Contains("Steam") ||
								currentTerrain.defName.Contains("Lava") ||
								currentTerrain.defName.Contains("Water") ||
								currentTerrain.defName.Contains("Marsh") ||
								currentTerrain.defName.Contains("Mud") ||
								currentTerrain.affordances.Contains(TerrainAffordanceDefOf.Bridgeable))
							{
								needsReplacement = true;
							}
							
							if (needsReplacement)
							{
								// 尝试使用合适的地面地形
								TerrainDef replacementTerrain = null;
								
								// 优先使用生物群系的中等地形
								if (map.Biome != null)
								{
									replacementTerrain = map.Biome.TerrainForAffordance(TerrainAffordanceDefOf.Medium);
								}
								
								// 如果没有找到合适的地形，使用土地
								if (replacementTerrain == null)
								{
									replacementTerrain = TerrainDefOf.Soil;
								}
								
								if (replacementTerrain != null)
								{
									map.terrainGrid.SetTerrain(cell, replacementTerrain);
									Log.Message($"[Gravship] 替换特殊地形: {currentTerrain.defName} -> {replacementTerrain.defName} at {cell}");
								}
							}
						}
						
						// 最后检查：确保该格子完全清空（除了小人）
						List<Thing> remainingThings = map.thingGrid.ThingsListAt(cell).ToList();
						foreach (Thing remaining in remainingThings)
						{
							if (!(remaining is Pawn))
							{
								Log.Warning($"[Gravship] 发现残留物体，使用终极清除方法: {remaining.def.defName} at {cell}");
								
								try
								{
									// 终极清除方法：强制从所有系统中移除
									if (remaining.Spawned)
									{
										remaining.DeSpawn(DestroyMode.Vanish);
									}
									map.thingGrid.Deregister(remaining);
									
									// 强制设置该位置为可通行地形
									map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
									
									Log.Message($"[Gravship] 终极清除成功: {remaining.def.defName}");
								}
								catch (Exception finalEx)
								{
									Log.Error($"[Gravship] 终极清除也失败: {remaining.def.defName} - {finalEx.Message}");
								}
							}
						}
					}
				}
				
				// 立即清除飞船区域的战争迷雾（飞船周围1格范围）
				try
				{
					// 创建飞船周围1格范围的格子集合
					HashSet<IntVec3> fogClearanceCells = new HashSet<IntVec3>();
					foreach (IntVec3 shipCell in shipCells)
					{
						// 添加飞船格子本身
						if (shipCell.InBounds(map))
						{
							fogClearanceCells.Add(shipCell);
						}
						
						// 添加周围1格范围的格子
						foreach (IntVec3 adjacentCell in GenRadial.RadialCellsAround(shipCell, 1f, true))
						{
							if (adjacentCell.InBounds(map))
							{
								fogClearanceCells.Add(adjacentCell);
							}
						}
					}
					
					// 对每个格子都使用FloodUnfog来清除战争迷雾
					int floodUnfogCount = 0;
					foreach (IntVec3 cell in fogClearanceCells)
					{
						try
						{
							Verse.FloodFillerFog.FloodUnfog(cell, map);
							floodUnfogCount++;
						}
						catch (Exception floodEx)
						{
							Log.Warning($"[Gravship] FloodUnfog失败于格子 {cell}: {floodEx.Message}");
						}
					}
					
					Log.Message($"[Gravship] 对飞船周围1格范围内的 {floodUnfogCount} 个格子执行了FloodUnfog清除战争迷雾");
				}
				catch (Exception fogEx)
				{
					Log.Error($"[Gravship] 清除战争迷雾时发生错误: {fogEx.Message}");
				}
			}
			catch (Exception ex)
			{
				Log.Error($"[Gravship] 清理飞船周围区域时发生错误: {ex.Message}\n{ex.StackTrace}");
			}
			
			HashSet<IntVec3> hashSet = new HashSet<IntVec3>();
			foreach (SavedTerrain item3 in data.Terrain)
			{
				IntVec3 c = new IntVec3(item3.posX, 0, item3.posZ);
				if (c.InBounds(map))
				{
					map.terrainGrid.SetTerrain(c, TerrainDefOf.Space);
				}
			}
			foreach (SavedTerrain item4 in data.Terrain)
			{
				IntVec3 c2 = new IntVec3(item4.posX, 0, item4.posZ);
				if (c2.InBounds(map) && !string.IsNullOrEmpty(item4.baseTerrain))
				{
					TerrainDef named = DefDatabase<TerrainDef>.GetNamed(item4.baseTerrain, errorOnFail: false);
					if (named != null && named.isFoundation)
					{
						map.terrainGrid.SetFoundation(c2, named);
					}
				}
			}
			int num = 0;
			foreach (SavedTerrain item5 in data.Terrain)
			{
				if (string.IsNullOrEmpty(item5.floorTerrain))
				{
					num++;
				}
			}
			TerrainDef named2 = DefDatabase<TerrainDef>.GetNamed("WoodPlankFloor", errorOnFail: false);
			if (named2 == null)
			{
				Log.Error("[Gravship] dummyFloor の定義が見つかりません: WoodPlankFloor");
			}
			else
			{
				foreach (SavedTerrain item6 in data.Terrain)
				{
					IntVec3 intVec = new IntVec3(item6.posX, 0, item6.posZ);
					if (intVec.InBounds(map) && string.IsNullOrEmpty(item6.floorTerrain))
					{
						TerrainDef terrainDef = map.terrainGrid.TerrainAt(intVec);
						map.terrainGrid.SetTerrain(intVec, named2);
						TerrainDef terrainDef2 = map.terrainGrid.TerrainAt(intVec);
						if (terrainDef2 != named2)
						{
							Log.Warning($"[Gravship] dummyFloor ({named2.defName}) を {intVec} に設置できませんでした。現在の地形: {terrainDef.defName} → {terrainDef2.defName}");
						}
					}
				}
			}
			if (named2 == null)
			{
				Log.Error("[Gravship] dummyFloor が null のまま強制設置しようとしています！");
			}
			else
			{
				foreach (SavedTerrain item7 in data.Terrain)
				{
					IntVec3 c3 = new IntVec3(item7.posX, 0, item7.posZ);
					if (c3.InBounds(map))
					{
						map.terrainGrid.SetTerrain(c3, named2);
					}
				}
			}
			foreach (SavedTerrain item8 in data.Terrain)
			{
				IntVec3 c4 = new IntVec3(item8.posX, 0, item8.posZ);
				if (c4.InBounds(map) && !string.IsNullOrEmpty(item8.floorTerrain))
				{
					TerrainDef named3 = DefDatabase<TerrainDef>.GetNamed(item8.floorTerrain, errorOnFail: false);
					if (named3 != null)
					{
						map.terrainGrid.SetTerrain(c4, named3);
					}
				}
			}
			foreach (SavedTerrain item9 in data.Terrain)
			{
				IntVec3 c5 = new IntVec3(item9.posX, 0, item9.posZ);
				if (c5.InBounds(map) && string.IsNullOrEmpty(item9.floorTerrain) && map.terrainGrid.TerrainAt(c5)?.defName == named2.defName)
				{
					map.terrainGrid.SetTerrain(c5, TerrainDefOf.Space);
				}
			}
			foreach (SavedTerrain item10 in data.Terrain)
			{
				IntVec3 c6 = new IntVec3(item10.posX, 0, item10.posZ);
				if (c6.InBounds(map) && !string.IsNullOrEmpty(item10.roofDef))
				{
					RoofDef named4 = DefDatabase<RoofDef>.GetNamed(item10.roofDef, errorOnFail: false);
					if (named4 != null)
					{
						map.roofGrid.SetRoof(c6, named4);
					}
				}
			}
			foreach (SavedThing thing2 in data.Things)
			{
				try
				{
					// 安全地获取ThingDef
					ThingDef named5 = DefDatabase<ThingDef>.GetNamedSilentFail(thing2.defName);
					if (named5 == null)
					{
						Log.Warning($"[Gravship] ThingDef が見つかりません: {thing2.defName}");
						continue;
					}
					
					IntVec3 loc = new IntVec3(thing2.posX, 0, thing2.posZ);
					if (!loc.InBounds(map))
					{
						Log.Warning($"[Gravship] 位置が地図外です: {thing2.defName} at {loc}");
						continue;
					}
					
					Rot4 rot = new Rot4(thing2.rotation);
					
					// 安全地获取stuffDef
					ThingDef stuffDef = null;
					if (!string.IsNullOrEmpty(thing2.stuffDef))
					{
						stuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(thing2.stuffDef);
						if (stuffDef == null)
						{
							Log.Warning($"[Gravship] StuffDef が見つかりません: {thing2.stuffDef} for {thing2.defName}");
						}
					}
					
					Thing thing = ThingMaker.MakeThing(named5, stuffDef);
					if (thing == null)
					{
						Log.Error($"[Gravship] Thing作成失敗: {thing2.defName}");
						continue;
					}
					
					// 安全地设置派系和动物属性
					if (thing is Pawn pawn)
					{
						try
						{
							// 确保动物有基本的生命值和需求
							if (pawn.health?.hediffSet == null)
							{
								Log.Warning($"[Gravship] 动物 {pawn.def.defName} 缺少健康系统，跳过生成");
								continue;
							}
							
							// 设置派系
							if (Faction.OfPlayer != null)
							{
								pawn.SetFactionDirect(Faction.OfPlayer);
							}
							
							// 确保动物有正确的年龄
							if (pawn.ageTracker != null && pawn.ageTracker.AgeBiologicalYears <= 0)
							{
								pawn.ageTracker.AgeBiologicalTicks = (long)(pawn.RaceProps.lifeExpectancy * 0.1f * 3600000f);
							}
							
							// 确保动物有基本需求
							if (pawn.needs != null)
							{
								pawn.needs.SetInitialLevels();
							}
							
							// 意识形态相关功能已移除
						}
						catch (Exception pawnEx)
						{
							Log.Error($"[Gravship] 动物设置失败: {pawn.def.defName} - {pawnEx.Message}");
							continue;
						}
					}
					else if (thing.def.CanHaveFaction && Faction.OfPlayer != null)
					{
						thing.SetFactionDirect(Faction.OfPlayer);
					}
					
					// 检查是否为飞船反应堆，如果是则替换为假的损坏版本
					if (thing2.defName == "Ship_Reactor")
					{
						Log.Message($"[Gravship] 检测到飞船反应堆，将替换为损坏版本: {loc}");
						
						// 销毁原来的真正反应堆
						thing.Destroy();
						
						// 创建假的损坏飞船反应堆
						Thing damagedReactor = CreateDamagedShipReactor();
						if (damagedReactor != null)
						{
							try
							{
								GenSpawn.Spawn(damagedReactor, loc, map, rot);
								Log.Message($"[Gravship] 成功生成损坏的飞船反应堆: {loc}");
								
								// 设置为玩家阵营
								if (damagedReactor.def.CanHaveFaction && Faction.OfPlayer != null)
								{
									damagedReactor.SetFactionDirect(Faction.OfPlayer);
								}
								
								// 跳过原来的thing，使用新的damagedReactor
								thing = damagedReactor;
							}
							catch (Exception spawnEx)
							{
								Log.Error($"[Gravship] 损坏飞船反应堆生成失败: {loc} - {spawnEx.Message}");
								continue;
							}
						}
						else
						{
							Log.Error("[Gravship] 无法创建损坏的飞船反应堆，跳过此建筑");
							continue;
						}
					}
					else
					{
						// 安全にスポーン
						try
						{
							GenSpawn.Spawn(thing, loc, map, rot);
						}
						catch (Exception spawnEx)
						{
							Log.Error($"[Gravship] スポーン失敗: {thing2.defName} at {loc} - {spawnEx.Message}");
							continue;
						}
					}
					if (thing2.extraData != null)
					{
						if (thing is Building_GravEngine building_GravEngine)
						{
							if (thing2.extraData.TryGetValue("gravshipName", out string value))
							{
								building_GravEngine.RenamableLabel = value;
							}
							if (thing2.extraData.TryGetValue("nameHidden", out string value2) && bool.TryParse(value2, out var result))
							{
								building_GravEngine.nameHidden = result;
							}
						}
						CompPowerTrader compPowerTrader = thing.TryGetComp<CompPowerTrader>();
						if (compPowerTrader != null && thing2.extraData.TryGetValue("powerOn", out string value3) && bool.TryParse(value3, out var result2))
						{
							compPowerTrader.PowerOn = result2;
						}
						CompHibernatable compHibernatable = thing.TryGetComp<CompHibernatable>();
						if (compHibernatable != null && thing2.extraData.TryGetValue("hibernatableState", out string value4))
						{
							compHibernatable.State = DefDatabase<HibernatableStateDef>.GetNamedSilentFail(value4) ?? HibernatableStateDefOf.Hibernating;
						}
						CompRefuelable compRefuelable = thing.TryGetComp<CompRefuelable>();
						if (compRefuelable != null && thing2.extraData.TryGetValue("fuel", out string value5) && float.TryParse(value5, out var result3))
						{
							compRefuelable.Refuel(result3 - compRefuelable.Fuel);
						}
					}
					if (string.IsNullOrEmpty(thing2.innerContainerXml))
					{
						continue;
					}
					try
					{
						// 特别检查低温休眠舱
						bool isCryptosleepCasket = thing.def.defName.Contains("CryptosleepCasket") || 
													thing.def.defName.Contains("Cryptosleep") ||
													thing.def.defName.Contains("cryptosleep");
						
						if (isCryptosleepCasket)
						{
							Log.Message($"[Gravship] 正在处理低温休眠舱容器内容: {thing.def.defName} at {thing.Position}");
						}
						
						// 获取容器
						ThingOwner thingOwner = null;
						if (thing is IThingHolder thingHolder)
						{
							thingOwner = thingHolder.GetDirectlyHeldThings();
						}
						else
						{
							CompTransporter compTransporter = thing.TryGetComp<CompTransporter>();
							if (compTransporter != null)
							{
								thingOwner = compTransporter.innerContainer;
							}
							else
							{
								CompThingContainer compThingContainer = thing.TryGetComp<CompThingContainer>();
								if (compThingContainer != null)
								{
									thingOwner = compThingContainer.innerContainer;
								}
							}
						}
						
						if (thingOwner == null)
						{
							if (isCryptosleepCasket)
							{
								Log.Error($"[Gravship] 低温休眠舱 {thing.LabelCap} 的 innerContainer 未找到！这可能导致小人丢失！");
							}
							else
							{
								Log.Warning($"[Gravship] {thing.LabelCap} の innerContainer が見つかりません");
							}
							continue;
						}
						
						if (isCryptosleepCasket)
						{
							Log.Message($"[Gravship] 低温休眠舱 {thing.def.defName} 容器内有 {thingOwner.Count} 个物体");
							foreach (Thing containerThing in thingOwner)
							{
								if (containerThing is Pawn containerPawn)
								{
									Log.Message($"[Gravship] 休眠舱内发现小人: {containerPawn.Name?.ToStringFull ?? containerPawn.def.defName}");
								}
								else
								{
									Log.Message($"[Gravship] 休眠舱内发现物品: {containerThing.def.defName}");
								}
							}
						}
						
						// 安全にファイル操作
						string tempFilePath = null;
						try
						{
							tempFilePath = Path.Combine(GenFilePaths.TempFolderPath, $"temp_inner_load_{thing.thingIDNumber}.xml");
							File.WriteAllText(tempFilePath, thing2.innerContainerXml);
							
							// Scribe初期化
							if (Scribe.mode != LoadSaveMode.Inactive)
							{
								Log.Warning($"[Gravship] Scribe が既にアクティブです: {Scribe.mode}");
								continue;
							}
							
							Scribe.loader.InitLoading(tempFilePath);
							ScribeMetaHeaderUtility.LoadGameDataHeader(ScribeMetaHeaderUtility.ScribeHeaderMode.Map, logVersionConflictWarning: false);
							
							Log.Message($"[Gravship DEBUG] 復元前: {thing.LabelCap} に innerContainer があり、ExposeData() を実行します");
							
							// 安全にExposeData実行
							try
							{
								thingOwner.ExposeData();
								
								// 在FinalizeLoading之前完全重置皇室地位数据
								if (isCryptosleepCasket && thingOwner.Count > 0)
								{
									foreach (Thing innerThing in thingOwner)
									{
										if (innerThing is Pawn innerPawn && innerPawn.royalty != null)
										{
											try
											{
												Log.Message($"[Gravship] 序列化前重置小人 {innerPawn.Name?.ToStringFull ?? innerPawn.def.defName} 的皇室地位");
												
												// 完全重置皇室追踪器，避免序列化问题
												innerPawn.royalty = new Pawn_RoyaltyTracker(innerPawn);
												
												Log.Message($"[Gravship] 已为小人创建全新的皇室追踪器");
											}
											catch (Exception cleanupEx)
											{
												Log.Warning($"[Gravship] 重置皇室地位数据时出现警告: {innerPawn.Name?.ToStringFull ?? innerPawn.def.defName} - {cleanupEx.Message}");
											}
										}
									}
								}
								
								Scribe.loader.FinalizeLoading();
								
								if (isCryptosleepCasket)
								{
									Log.Message($"[Gravship] 低温休眠舱容器内容恢复完成: {thing.LabelCap}，现在容器内有 {thingOwner.Count} 个物体");
									foreach (Thing restoredThing in thingOwner)
									{
										if (restoredThing is Pawn restoredPawn)
										{
											Log.Message($"[Gravship] 恢复后休眠舱内小人: {restoredPawn.Name?.ToStringFull ?? restoredPawn.def.defName}");
											
											try
											{
												// 确保小人的基本组件都已正确初始化
												try
												{
													// 调用PostMake确保基本组件初始化
													if (restoredPawn.def != null && restoredPawn.kindDef != null)
													{
														// 确保所有必要的组件都存在
														PawnComponentsUtility.CreateInitialComponents(restoredPawn);
														
														// 确保工作设置正确初始化
														if (restoredPawn.workSettings != null)
														{
															restoredPawn.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
														}
														
														Log.Message($"[Gravship] 小人组件初始化完成: {restoredPawn.Name?.ToStringFull ?? restoredPawn.def.defName}");
													}
												}
												catch (Exception initEx)
												{
													Log.Warning($"[Gravship] 小人组件初始化警告: {restoredPawn.Name?.ToStringFull ?? restoredPawn.def.defName} - {initEx.Message}");
												}
												
												if (restoredPawn.Faction != Faction.OfPlayer)
												{
													Log.Message($"[Gravship] 将休眠舱内小人 {restoredPawn.Name?.ToStringFull ?? restoredPawn.def.defName} 设置为玩家阵营");
													
													// 完全重置小人的皇室地位，移除所有头衔和好感度
													if (restoredPawn.royalty != null)
													{
														try
														{
															Log.Message($"[Gravship] 开始完全重置小人 {restoredPawn.Name?.ToStringFull ?? restoredPawn.def.defName} 的皇室地位");
															
															var royaltyTracker = restoredPawn.royalty;
															
															// 1. 移除所有皇室头衔
															if (royaltyTracker.AllTitlesForReading != null)
															{
																var allTitles = royaltyTracker.AllTitlesForReading.ToList();
																foreach (var title in allTitles)
																{
																	if (title.faction != null)
																	{
																		royaltyTracker.SetTitle(title.faction, null, false, false, false);
																		Log.Message($"[Gravship] 移除头衔: {title.def?.defName ?? "Unknown"} from {title.faction.Name}");
																	}
																}
															}
															
															// 2. 完全清空好感度字典
															var favorField = typeof(Pawn_RoyaltyTracker).GetField("favor", 
																System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
															
															if (favorField != null)
															{
																var favorDict = favorField.GetValue(royaltyTracker) as Dictionary<Faction, int>;
																if (favorDict != null)
																{
																	favorDict.Clear();
																	Log.Message($"[Gravship] 已清空所有好感度数据");
																}
															}
															
															// 3. 清空许可证
															var permitsField = typeof(Pawn_RoyaltyTracker).GetField("permits", 
																System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
															
															if (permitsField != null)
															{
																var permitsList = permitsField.GetValue(royaltyTracker) as List<FactionPermit>;
																if (permitsList != null)
																{
																	permitsList.Clear();
																	Log.Message($"[Gravship] 已清空所有许可证");
																}
															}
															
															// 4. 清空债务
															var heirField = typeof(Pawn_RoyaltyTracker).GetField("heir", 
																System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
															
															if (heirField != null)
															{
																heirField.SetValue(royaltyTracker, null);
															}
															
															// 5. 重新初始化皇室追踪器
															try
															{
																// 调用ExposeData来确保内部状态一致
																var tempMode = Scribe.mode;
																Scribe.mode = LoadSaveMode.Inactive;
																// 不调用ExposeData，因为可能导致问题
																Scribe.mode = tempMode;
															}
															catch (Exception exposeEx)
															{
																Log.Warning($"[Gravship] 重新初始化皇室追踪器时出现警告: {exposeEx.Message}");
															}
															
															Log.Message($"[Gravship] 已完全重置小人 {restoredPawn.Name?.ToStringFull ?? restoredPawn.def.defName} 的皇室地位");
														}
														catch (Exception royaltyEx)
														{
															Log.Warning($"[Gravship] 重置皇室地位时出现警告: {restoredPawn.Name?.ToStringFull ?? restoredPawn.def.defName} - {royaltyEx.Message}");
															
															// 如果重置失败，尝试创建新的皇室追踪器
															try
															{
																restoredPawn.royalty = new Pawn_RoyaltyTracker(restoredPawn);
																Log.Message($"[Gravship] 已为小人创建新的皇室追踪器");
															}
															catch (Exception newTrackerEx)
															{
																Log.Error($"[Gravship] 创建新皇室追踪器失败: {newTrackerEx.Message}");
															}
														}
													}
													
													restoredPawn.SetFactionDirect(Faction.OfPlayer);
													
													// 确保小人有正确的基本设置
													if (restoredPawn.needs != null)
													{
														restoredPawn.needs.SetInitialLevels();
													}
													
													// 确保小人有正确的年龄（如果需要）
													if (restoredPawn.ageTracker != null && restoredPawn.ageTracker.AgeBiologicalYears <= 0)
													{
														restoredPawn.ageTracker.AgeBiologicalTicks = (long)(restoredPawn.RaceProps.lifeExpectancy * 0.1f * 3600000f);
													}
													
													Log.Message($"[Gravship] 成功将休眠舱内小人设置为玩家阵营: {restoredPawn.Name?.ToStringFull ?? restoredPawn.def.defName}");
												}
												else
												{
													Log.Message($"[Gravship] 休眠舱内小人 {restoredPawn.Name?.ToStringFull ?? restoredPawn.def.defName} 已经是玩家阵营");
												}
											}
											catch (Exception pawnFactionEx)
											{
												Log.Error($"[Gravship] 设置休眠舱内小人阵营失败: {restoredPawn.Name?.ToStringFull ?? restoredPawn.def.defName} - {pawnFactionEx.Message}");
											}
										}
										else
										{
											Log.Message($"[Gravship] 恢复后休眠舱内物品: {restoredThing.def.defName}");
										}
									}
								}
								else
								{
									Log.Message($"[Gravship] 中身復元成功: {thing.LabelCap}");
								}
							}
							catch (Exception exposeEx)
							{
								Log.Error($"[Gravship] 中身復元失敗（ExposeData エラー）: {thing.LabelCap}\n{exposeEx}");
								// Scribeを安全にクリーンアップ
								try
								{
									if (Scribe.mode != LoadSaveMode.Inactive)
									{
										Scribe.loader.FinalizeLoading();
									}
								}
								catch { }
							}
						}
						finally
						{
							// 一時ファイルをクリーンアップ
							if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
							{
								try
								{
									File.Delete(tempFilePath);
								}
								catch (Exception deleteEx)
								{
									Log.Warning($"[Gravship] 一時ファイル削除失敗: {tempFilePath} - {deleteEx.Message}");
								}
							}
						}
					}
					catch (Exception arg2)
					{
						Log.Error($"[Gravship] 中身復元失敗: {thing.LabelCap} - {arg2}");
					}
				}
				catch (Exception arg3)
				{
					Log.Error($"[Gravship] Thing復元失敗: {thing2.defName}: {arg3}");
				}
			}
			map.powerNetManager.UpdatePowerNetsAndConnections_First();
			Log.Message("[Gravship] 電力網（PowerNet）を再構築しました。");
			GravshipConnectionUtility.UpdateAllGravshipConnections(map);
			Log.Message("[Gravship] グラヴシップ接続情報を再構築しました");
			
			// 延迟清除重力船区域的迷雾，确保所有建筑都已完全恢复
			LongEventHandler.ExecuteWhenFinished(delegate
			{
				// 再次延迟一点时间，确保地图完全初始化
				GameComponent_OnetimeTicker.Schedule(Find.TickManager.TicksGame + 30, delegate
				{
					UnfogGravshipArea(map);
					Log.Message("[Gravship] 重力船区域迷雾清除完成（延迟执行）");
				});
			});
		}
		if (data.Things == null || data.Things.Count <= 0)
		{
			return;
		}
		int newX = (int)data.Things.Average((SavedThing t) => t.posX);
		int newZ = (int)data.Things.Average((SavedThing t) => t.posZ);
		IntVec3 center = new IntVec3(newX, 0, newZ);
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			Find.CameraDriver.SetRootPosAndSize(center.ToVector3Shifted(), 24f);
			Log.Message($"[Gravship] カメラを宇宙船中央 {center} に移動（ExecuteWhenFinished）しました。");
			if (data.extraFlags != null && data.extraFlags.TryGetValue("gravEngineInspected", out string value6) && bool.TryParse(value6, out var inspected))
			{
				int num2 = 10;
				int applyAtTick = Find.TickManager.TicksGame + num2;
				GameComponent_OnetimeTicker.Schedule(applyAtTick, delegate
				{
					Find.ResearchManager.gravEngineInspected = inspected;
					Log.Message($"[Gravship] gravEngineInspected を遅延復元（Tick {applyAtTick}）: {inspected}");
				});
			}
			if (data.extraFlags != null && data.extraFlags.TryGetValue("departureYear", out string value7) && int.TryParse(value7, out var _))
			{
				int num3 = 3600000;
				int num4 = 60000;
				int num5 = 2500;
				int num6 = Rand.RangeInclusive(1, 100);
				int num7 = ((num6 <= 10) ? Rand.RangeInclusive(1, 10) : ((num6 <= 60) ? Rand.RangeInclusive(11, 50) : ((num6 <= 85) ? Rand.RangeInclusive(51, 100) : ((num6 <= 94) ? Rand.RangeInclusive(101, 300) : ((num6 <= 97) ? Rand.RangeInclusive(301, 500) : ((num6 > 99) ? Rand.RangeInclusive(1001, 5000) : Rand.RangeInclusive(501, 1000)))))));
				int num8 = Rand.RangeInclusive(0, 59);
				int num9 = Rand.RangeInclusive(0, 23);
				int num10 = num3 * num7 + num4 * num8 + num5 * num9;
				int val = 500 * num3;
				int num11 = num10;
				TickManager tickManager = Find.TickManager;
				int num12 = tickManager.TicksGame;
				int num13 = tickManager.TicksAbs;
				while (num11 > 0)
				{
					int num14 = Math.Min(num11, val);
					num12 += num14;
					num13 += num14;
					num11 -= num14;
				}
				Traverse.Create(tickManager).Field("ticksGameInt").SetValue(num12);
				Traverse.Create(tickManager).Field("ticksAbsInt").SetValue(num13);
				Log.Message($"[Gravship] 宇宙旅行により {num7}年 {num8}日 {num9}時間（{num10:N0} tick）経過させました");
			}
		});
	}

	/// <summary>
	/// 为重力船区域清除战争迷雾，提供视野支持
	/// </summary>
	private void UnfogGravshipArea(Map map)
	{
		if (map == null)
		{
			Log.Warning("[Gravship] 无法获取地图，跳过迷雾清除");
			return;
		}

		// 查找地图上的重力船相关建筑
		List<Thing> gravshipBuildings = new List<Thing>();
		foreach (Thing thing in map.listerThings.AllThings)
		{
			if (thing.def.defName.Contains("Grav") || 
				thing.def.defName.Contains("Ship") ||
				(thing.def.building?.shipPart == true))
			{
				gravshipBuildings.Add(thing);
			}
		}

		if (gravshipBuildings.Count == 0)
		{
			Log.Message("[Gravship] 未找到重力船建筑，跳过迷雾清除");
			return;
		}

		// 收集所有重力船相关的格子，包括更大的范围
		HashSet<IntVec3> gravshipCells = new HashSet<IntVec3>();
		foreach (Thing building in gravshipBuildings)
		{
			// 添加建筑本身占用的格子
			foreach (IntVec3 cell in building.OccupiedRect())
			{
				gravshipCells.Add(cell);
			}
			
			// 添加建筑周围更大范围的格子（3格半径）
			foreach (IntVec3 cell in GenRadial.RadialCellsAround(building.Position, 3f, true))
			{
				if (cell.InBounds(map))
				{
					gravshipCells.Add(cell);
				}
			}
		}

		// 强制清除所有重力船格子的战争迷雾
		int unfoggedCount = 0;
		foreach (IntVec3 cell in gravshipCells)
		{
			if (cell.InBounds(map))
			{
				// 强制清除迷雾，不管当前状态
				map.fogGrid.Unfog(cell);
				unfoggedCount++;
			}
		}

		// 对每个重力船建筑都使用FloodUnfog，确保完全清除
		foreach (Thing building in gravshipBuildings)
		{
			try
			{
				Verse.FloodFillerFog.FloodUnfog(building.Position, map);
			}
			catch (System.Exception ex)
			{
				Log.Warning($"[Gravship] FloodUnfog失败于位置 {building.Position}: {ex.Message}");
			}
		}

		Log.Message($"[Gravship] 强力清除了 {unfoggedCount} 个格子的战争迷雾，处理了 {gravshipBuildings.Count} 个重力船建筑");
	}

	/// <summary>
	/// 创建一个损坏的飞船反应堆，使用XML定义的ThingDef
	/// </summary>
	private Thing CreateDamagedShipReactor()
	{
		try
		{
			// 获取XML定义的损坏飞船反应堆
			ThingDef damagedReactorDef = DefDatabase<ThingDef>.GetNamedSilentFail("Ship_Reactor_Damaged_Gravship");
			if (damagedReactorDef == null)
			{
				Log.Error("[Gravship] 无法找到损坏飞船反应堆定义: Ship_Reactor_Damaged_Gravship");
				return null;
			}
			
			// 创建Thing实例
			Thing damagedReactor = ThingMaker.MakeThing(damagedReactorDef, null);
			if (damagedReactor == null)
			{
				Log.Error("[Gravship] 无法创建损坏的飞船反应堆实例");
				return null;
			}
			
			// 设置损坏状态（降低生命值表示严重损坏）
			if (damagedReactor.HitPoints > 10)
			{
				damagedReactor.HitPoints = Math.Max(10, damagedReactor.MaxHitPoints / 5); // 设置为最大生命值的1/5
			}
			
			Log.Message($"[Gravship] 成功创建损坏的飞船反应堆，生命值: {damagedReactor.HitPoints}/{damagedReactor.MaxHitPoints}");
			return damagedReactor;
		}
		catch (Exception ex)
		{
			Log.Error($"[Gravship] 创建损坏飞船反应堆时发生错误: {ex.Message}\n{ex.StackTrace}");
			return null;
		}
	}
}
