using Verse;

namespace GravshiptoSpaceship;

public class SavedTerrain : IExposable
{
	public string baseTerrain;

	public string floorTerrain;

	public string roofDef;

	public int posX;

	public int posZ;

	public void ExposeData()
	{
		Scribe_Values.Look(ref baseTerrain, "baseTerrain");
		Scribe_Values.Look(ref floorTerrain, "floorTerrain");
		Scribe_Values.Look(ref roofDef, "roofDef");
		Scribe_Values.Look(ref posX, "posX", 0);
		Scribe_Values.Look(ref posZ, "posZ", 0);
	}
}
