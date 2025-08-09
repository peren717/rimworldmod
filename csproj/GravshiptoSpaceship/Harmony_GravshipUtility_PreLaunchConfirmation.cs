using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(GravshipUtility), "PreLaunchConfirmation")]
public static class Harmony_GravshipUtility_PreLaunchConfirmation
{
	private static void Prefix(Building_GravEngine engine, ref Action launchAction)
	{
		Map map = engine.Map;
		IntVec3? root = GravshipConnectionUtility.FindGravshipRootConnectedToThing(engine);
		if (!root.HasValue || map == null)
		{
			return;
		}
		CompPilotConsole console = (from t in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
			select t.TryGetComp<CompPilotConsole>()).FirstOrDefault((CompPilotConsole c) => c != null && GravshipConnectionUtility.FindGravshipRootConnectedToThing(c.parent) == root);
		if (console == null)
		{
			return;
		}
		Action originalAction = launchAction;
		launchAction = delegate
		{
			GravshipLaunchContext.LastUsedConsoleMap = console.parent.Map;
			GravshipLaunchContext.LastUsedConsolePos = console.parent.Position;
			if (GravshipLogger.EnableLogging)
			{
				Log.Message($"[Gravship] GravshipLaunchContext 更新: {console.parent.Position}");
			}
			originalAction?.Invoke();
		};
	}
}
