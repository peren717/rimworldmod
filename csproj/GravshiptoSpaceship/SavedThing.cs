using System.Collections.Generic;
using Verse;

namespace GravshiptoSpaceship;

public class SavedThing : IExposable
{
	public string defName;

	public string stuffDef;

	public int posX;

	public int posZ;

	public int rotation;

	public Dictionary<string, string> extraData = new Dictionary<string, string>();

	public string innerContainerXml;

	public void ExposeData()
	{
		Scribe_Values.Look(ref defName, "defName");
		Scribe_Values.Look(ref stuffDef, "stuffDef");
		Scribe_Values.Look(ref posX, "posX", 0);
		Scribe_Values.Look(ref posZ, "posZ", 0);
		Scribe_Values.Look(ref rotation, "rotation", 0);
		Scribe_Collections.Look(ref extraData, "extraData", LookMode.Value, LookMode.Value);
		Scribe_Values.Look(ref innerContainerXml, "innerContainerXml");
	}
}
