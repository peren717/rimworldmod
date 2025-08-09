using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace GravshiptoSpaceship
{
    /// <summary>
    /// 自定义scenario部分，用于创建没有初始小人的游戏
    /// 这个类绕过了游戏对至少一个小人的要求
    /// </summary>
    public class ScenPart_NoStartingPawns : ScenPart
    {
        public override void PostWorldGenerate()
        {
            // 设置初始小人数为0
            Find.GameInitData.startingPawnCount = 0;
            
            // 清空所有小人列表
            Find.GameInitData.startingAndOptionalPawns.Clear();
            Find.GameInitData.startingPossessions.Clear();
            
            // 清空所有小人要求
            Find.GameInitData.startingPawnsRequired?.Clear();
            Find.GameInitData.startingXenotypesRequired?.Clear();
            Find.GameInitData.startingMutantsRequired?.Clear();
            
            Log.Message("ScenPart_NoStartingPawns: 已设置零小人开局");
        }



        public override void PostGameStart()
        {
            // 只在游戏初始化阶段清理GameInitData中的小人数据
            // 不要删除地图上已经存在的小人（比如从重力船恢复的小人）
            if (Find.GameInitData.startingAndOptionalPawns.Count > 0)
            {
                Log.Message("ScenPart_NoStartingPawns: 清理GameInitData中的小人数据");
                Find.GameInitData.startingAndOptionalPawns.Clear();
            }
            
            // 为重力船区域提供视野支持
            UnfogGravshipArea();
            
            Log.Message("ScenPart_NoStartingPawns: 零小人开局设置完成，保留重力船恢复的小人");
        }

        /// <summary>
        /// 重写GetConfigPages方法，返回空列表以避免显示小人选择界面
        /// </summary>
        public override IEnumerable<Page> GetConfigPages()
        {
            return System.Linq.Enumerable.Empty<Page>();
        }

        /// <summary>
        /// 为重力船区域清除战争迷雾，提供视野支持
        /// </summary>
        private void UnfogGravshipArea()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Log.Warning("ScenPart_NoStartingPawns: 无法获取当前地图，跳过视野设置");
                return;
            }

            // 查找地图上的重力船相关建筑
            List<Thing> gravshipBuildings = new List<Thing>();
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing.def.defName.Contains("Grav") || 
                    thing.def.defName.Contains("Ship") ||
                    thing.def.building?.shipPart == true)
                {
                    gravshipBuildings.Add(thing);
                }
            }

            if (gravshipBuildings.Count == 0)
            {
                Log.Message("ScenPart_NoStartingPawns: 未找到重力船建筑，跳过视野设置");
                return;
            }

            // 收集所有重力船相关的格子
            HashSet<IntVec3> gravshipCells = new HashSet<IntVec3>();
            foreach (Thing building in gravshipBuildings)
            {
                // 添加建筑本身占用的格子
                foreach (IntVec3 cell in building.OccupiedRect())
                {
                    gravshipCells.Add(cell);
                }
                
                // 添加建筑周围的格子以提供更好的视野
                foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(building))
                {
                    if (cell.InBounds(map))
                    {
                        gravshipCells.Add(cell);
                    }
                }
            }

            // 为所有重力船格子清除战争迷雾
            int unfoggedCount = 0;
            foreach (IntVec3 cell in gravshipCells)
            {
                if (cell.InBounds(map) && map.fogGrid.IsFogged(cell))
                {
                    map.fogGrid.Unfog(cell);
                    unfoggedCount++;
                }
            }

            // 使用FloodUnfog为重力船核心区域提供更大范围的视野
            if (gravshipBuildings.Count > 0)
            {
                Thing centerBuilding = gravshipBuildings.OrderBy(b => b.Position.DistanceTo(map.Center)).First();
                Verse.FloodFillerFog.FloodUnfog(centerBuilding.Position, map);
            }

            Log.Message($"ScenPart_NoStartingPawns: 为重力船区域清除了 {unfoggedCount} 个格子的战争迷雾，找到 {gravshipBuildings.Count} 个重力船建筑");
        }

        public override string Summary(Scenario scen)
        {
            return "开始游戏时不生成任何小人，完全依靠重力船恢复系统";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // 这个ScenPart不需要保存任何数据
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ "NoStartingPawns".GetHashCode();
        }
    }
}