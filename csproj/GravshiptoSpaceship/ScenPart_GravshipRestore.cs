using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace GravshiptoSpaceship
{
    /// <summary>
    /// 自定义scenario部分，用于恢复重力船并提供视野支持
    /// 这个类专注于重力船恢复功能，不干预正常的小人开局流程
    /// </summary>
    public class ScenPart_GravshipRestore : ScenPart
    {
        public override void PostGameStart()
        {
            // 为重力船区域提供视野支持
            UnfogGravshipArea();
            
            Log.Message("ScenPart_GravshipRestore: 重力船恢复系统已激活，视野支持已启用");
        }

        /// <summary>
        /// 为重力船区域清除战争迷雾，提供视野支持
        /// </summary>
        private void UnfogGravshipArea()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Log.Warning("ScenPart_GravshipRestore: 无法获取当前地图，跳过视野设置");
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
                Log.Message("ScenPart_GravshipRestore: 未找到重力船建筑，跳过视野设置");
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

            Log.Message($"ScenPart_GravshipRestore: 为重力船区域清除了 {unfoggedCount} 个格子的战争迷雾，找到 {gravshipBuildings.Count} 个重力船建筑");
        }

        public override string Summary(Scenario scen)
        {
            return "激活重力船恢复系统，为重力船区域提供视野支持";
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