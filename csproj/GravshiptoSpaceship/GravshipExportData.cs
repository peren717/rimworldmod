using System.Collections.Generic;
using Verse;

namespace GravshiptoSpaceship;

public class GravshipExportData : IExposable
{
	public List<SavedThing> Things = new List<SavedThing>();

	public List<SavedTerrain> Terrain = new List<SavedTerrain>();

	public List<string> completedResearch = new List<string>();

	public Dictionary<string, string> extraFlags = new Dictionary<string, string>();

	public int originalMapSizeX;

	public int originalMapSizeZ;

	public void ExposeData()
	{
		Scribe_Collections.Look(ref Things, "Things", LookMode.Deep);
		Scribe_Collections.Look(ref Terrain, "Terrain", LookMode.Deep);
		Scribe_Collections.Look(ref completedResearch, "completedResearch", LookMode.Value);
		Scribe_Collections.Look(ref extraFlags, "extraFlags", LookMode.Value, LookMode.Value);
		Scribe_Values.Look(ref originalMapSizeX, "originalMapSizeX", 0);
		Scribe_Values.Look(ref originalMapSizeZ, "originalMapSizeZ", 0);
	}
}
