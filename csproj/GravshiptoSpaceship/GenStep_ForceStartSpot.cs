using System;
using RimWorld;
using Verse;

namespace GravshiptoSpaceship;

public class GenStep_ForceStartSpot : GenStep
{
	public override int SeedPart => 196743;

	public override void Generate(Map map, GenStepParams parms)
	{
		try
		{
			Log.Message("[Gravship] ForceStartSpot Generate() 呼び出し開始");
			if (!ModsConfig.OdysseyActive || Find.Scenario?.name != "強くてニューゲーム")
			{
				Log.Message("[Gravship] OdysseyActive ではない、またはシナリオ名が一致しないため Space 塗りをスキップ");
				return;
			}
			MapGenerator.PlayerStartSpot = new IntVec3(map.Size.x / 2, 0, map.Size.z / 2);
			Log.Message("[Gravship] PlayerStartSpot を GenStep で強制設定しました");
			map.regionAndRoomUpdater.Enabled = false;
			TerrainGrid terrainGrid = map.terrainGrid;
			foreach (IntVec3 allCell in map.AllCells)
			{
				terrainGrid.SetTerrain(allCell, TerrainDefOf.Space);
			}
		}
		catch (Exception arg)
		{
			Log.Error($"[Gravship] ForceStartSpot Generate() で例外: {arg}");
		}
	}
}
