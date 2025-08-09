using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using Verse;

namespace GravshiptoSpaceship;

[HarmonyPatch(typeof(MapGenerator), "GenerateMap")]
public static class Patch_MapGenerator_GenerateMap
{
	private static void Prefix(ref IntVec3 mapSize)
	{
		// 检查当前场景是否包含重力船恢复组件
		if (Find.Scenario?.AllParts?.Any(part => part is GravshiptoSpaceship.ScenPart_GravshipRestore) != true)
		{
			return;
		}
		string text = Path.Combine(GenFilePaths.ConfigFolderPath, "GravshipToSpaceship", GravshiptoSpaceshipMod.Settings?.selectedFileName ?? "");
		if (File.Exists(text))
		{
			GravshipExportData target = null;
			try
			{
				Scribe.loader.InitLoading(text);
				Scribe_Deep.Look(ref target, "GravshipExportData");
				Scribe.loader.FinalizeLoading();
			}
			catch (Exception arg)
			{
				Log.Error($"[Gravship] サイズ読み込み失敗: {arg}");
			}
			if (target != null && target.originalMapSizeX > 0 && target.originalMapSizeZ > 0)
			{
				mapSize = new IntVec3(target.originalMapSizeX, 1, target.originalMapSizeZ);
				Log.Message($"[Gravship] マップサイズを保存時と同じに強制変更: {mapSize}");
			}
		}
	}
}
