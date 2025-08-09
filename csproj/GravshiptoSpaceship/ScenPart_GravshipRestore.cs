using RimWorld;
using Verse;
using System;
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
        public override void PostMapGenerate(Map map)
        {
            Log.Message("ScenPart_GravshipRestore: 在地图生成完成后执行重力船恢复");
            
            // 在地图生成完成后立即执行重力船恢复，此时小人已经空投完成
            try
            {
                var gravshipRestorer = Current.Game.GetComponent<GravshipRestorer>();
                if (gravshipRestorer != null)
                {
                    Log.Message("[Gravship] 开始在PostMapGenerate阶段恢复重力船...");
                    gravshipRestorer.TriggerRestore();
                    Log.Message("[Gravship] 重力船恢复完成");
                }
                else
                {
                    Log.Warning("[Gravship] 未找到GravshipRestorer组件");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Gravship] PostMapGenerate阶段恢复失败: {ex}");
            }
        }

        public override void PostGameStart()
        {
            Log.Message("ScenPart_GravshipRestore: 游戏开始后的后续处理");
            
            // 确保GameComponent_OnetimeTicker存在（用于其他可能的延迟操作）
            var onetimeTicker = Current.Game.GetComponent<GravshipRestorer.GameComponent_OnetimeTicker>();
            if (onetimeTicker == null)
            {
                Current.Game.components.Add(new GravshipRestorer.GameComponent_OnetimeTicker(Current.Game));
                Log.Message("[Gravship] GameComponent_OnetimeTicker已添加");
            }
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
            return base.GetHashCode() ^ "GravshipRestore".GetHashCode();
        }
    }
}