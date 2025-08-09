using Verse;

namespace GravshiptoSpaceship;

public class GravshipSettings : ModSettings
{
	public string selectedFileName = null;

	public override void ExposeData()
	{
		Scribe_Values.Look(ref selectedFileName, "selectedFileName");
	}
}
