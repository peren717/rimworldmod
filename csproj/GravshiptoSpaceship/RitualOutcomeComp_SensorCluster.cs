using RimWorld;
using Verse;

namespace GravshiptoSpaceship;

public class RitualOutcomeComp_SensorCluster : RitualOutcomeComp
{
	public override bool Applies(LordJob_Ritual ritual)
	{
		return true;
	}

	public override QualityFactor GetQualityFactor(Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation, RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
	{
		if (ritualTarget.Thing == null || ritualTarget.Map == null)
		{
			return null;
		}
		CompPilotConsole compPilotConsole = ritualTarget.Thing.TryGetComp<CompPilotConsole>();
		if (compPilotConsole?.engine == null)
		{
			return null;
		}
		IntVec3? root = GravshipConnectionUtility.FindGravshipRootConnectedToConsole(compPilotConsole.parent);
		if (!root.HasValue)
		{
			return null;
		}
		Map map = ritualTarget.Map;
		if (!map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).Any((Thing t) => t.def.defName == "Ship_SensorCluster" && GravshipConnectionUtility.FindGravshipRootConnectedToThing(t) == root))
		{
			return null;
		}
		ThingDef named = DefDatabase<ThingDef>.GetNamed("Ship_SensorCluster");
		return new QualityFactor
		{
			label = named.label,
			qualityChange = "OutcomeBonusDesc_QualitySingleOffset".Translate(0.1f.ToStringWithSign("0.#%")).Resolve(),
			quality = 0.1f,
			count = "1 / 1",
			positive = true,
			priority = 5f,
			toolTip = " - " + named.label + ": +" + 0.1f.ToStringPercent()
		};
	}
}
