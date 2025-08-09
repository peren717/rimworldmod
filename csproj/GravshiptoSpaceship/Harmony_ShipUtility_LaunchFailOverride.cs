using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(ShipUtility), "LaunchFailReasons")]
public static class Harmony_ShipUtility_LaunchFailOverride
{
	private static void Postfix(Building rootBuilding, ref IEnumerable<string> __result)
	{
		if (rootBuilding == null || rootBuilding.Map == null)
		{
			return;
		}
		Map map = rootBuilding.Map;
		List<string> list = __result.ToList();
		string beamLabel = ThingDefOf.Ship_Beam.label;
		list.RemoveAll((string reason) => reason.StartsWith("ShipReportMissingPart".Translate()) && reason.Contains(beamLabel));
		List<List<Building>> allVanillaStructuresCached = GravshipConnectionUtility.GetAllVanillaStructuresCached(map);
		List<Building> list2 = allVanillaStructuresCached.FirstOrDefault((List<Building> group) => group.Contains(rootBuilding));
		bool flag = list2 != null;
		if (flag)
		{
			__result = list;
			if (GravshipLogger.ShouldLog)
			{
				Log.Warning($"[Gravship DEBUG] root={rootBuilding.def.defName} at {rootBuilding.Position} isVanilla={flag}, count={list2.Count}");
			}
		}
		else
		{
			if (!GravshipConnectionUtility.FindGravshipRootConnectedToThing(rootBuilding).HasValue)
			{
				return;
			}
			List<List<Building>> allGravshipStructuresCached = GravshipConnectionUtility.GetAllGravshipStructuresCached(map);
			List<Building> list3 = allGravshipStructuresCached.FirstOrDefault((List<Building> s) => s.Contains(rootBuilding));
			if (list3 == null)
			{
				return;
			}
			if (GravshipLogger.ShouldLog)
			{
				Log.Warning($"[Gravship DEBUG] isVanilla={flag} for root={rootBuilding.def.defName} at {rootBuilding.Position}");
			}
			if (!flag)
			{
				if (GravshipLogger.ShouldLog)
				{
					Log.Warning("[Gravship DEBUG] Structure treated as Gravship");
				}
				if (list3 != null)
				{
					foreach (Building item in list3)
					{
						if (GravshipLogger.ShouldLog)
						{
							Log.Warning($"  [GravPart] {item.def.defName} at {item.Position}");
						}
					}
				}
			}
			bool flag2 = __result.Any((string reason) => reason.StartsWith("ShipReportMissingPart".Translate()) && reason.Contains(ThingDefOf.Ship_Engine.label));
			bool flag3 = __result.Any((string reason) => reason.StartsWith("ShipReportMissingPart".Translate()) && reason.Contains(ThingDefOf.Ship_Reactor.label));
			bool flag4 = __result.Any((string reason) => reason.StartsWith("ShipReportHibernating".Translate()) && reason.Contains(ThingDefOf.Ship_Reactor.label));
			bool flag5 = __result.Any((string reason) => reason.StartsWith("ShipReportNotReady".Translate()) && reason.Contains(ThingDefOf.Ship_Reactor.label));
			int num = list3.Count((Building b) => b.def.defName == "Ship_Engine" && (b.TryGetComp<CompGravshipThruster>()?.CanBeActive ?? false));
			int num2 = 3 - num;
			if (!flag2 && !flag3 && !flag4 && !flag5 && num2 > 0)
			{
				string label = ThingDefOf.Ship_Engine.label;
				list.Add("ShipReportMissingPart".Translate() + ": " + $"{num2}x {label} " + string.Format("({0} {1})", "ShipReportMissingPartRequires".Translate(), 3));
			}
			if (GravshipLogger.ShouldLog)
			{
				Log.Warning($"[Gravship DEBUG] Active nuclear engines (CanBeActive==true) = {num}");
			}
			__result = list;
		}
	}
}
