using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(Gravship), "AddThing")]
public static class Harmony_Gravship_AddThing_Patch
{
	private static void Postfix(Gravship __instance, Thing thing, IntVec3 offset)
	{
		if (!(AccessTools.Field(__instance.GetType(), "thrusters")?.GetValue(__instance) is Dictionary<Thing, PositionData> dictionary) || thing.def.defName != "Ship_Engine" || dictionary.ContainsKey(thing))
		{
			return;
		}
		CompGravshipThruster compGravshipThruster = thing.TryGetComp<CompGravshipThruster>();
		if (compGravshipThruster != null && compGravshipThruster.CanBeActive)
		{
			IntVec3 position = offset;
			switch (thing.Rotation.AsInt)
			{
			case 0:
				position += new IntVec3(0, 0, 0);
				break;
			case 1:
				position += new IntVec3(0, 0, 0);
				break;
			case 2:
				position += new IntVec3(0, 0, 0);
				break;
			case 3:
				position += new IntVec3(0, 0, 0);
				break;
			}
			dictionary[thing] = new PositionData(position, thing.Rotation);
		}
	}
}
