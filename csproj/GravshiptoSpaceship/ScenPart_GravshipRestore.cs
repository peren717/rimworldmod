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
            Log.Message("ScenPart_GravshipRestore: 重力船恢复场景已激活");
        }

        public override string Summary(Scenario scen)
        {
            return "";
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ "NoStartingPawns".GetHashCode();
        }
    }
}