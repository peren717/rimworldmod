using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GravshiptoSpaceship;

public class GravshiptoSpaceshipMod : Mod
{
	public class GameComp_AllowedAreaCleaner : GameComponent
	{
		public GameComp_AllowedAreaCleaner(Game game)
		{
		}

		public override void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				CleanInvalidAllowedAreas();
			}
		}

		private void CleanInvalidAllowedAreas()
		{
			FieldInfo field = typeof(Pawn_PlayerSettings).GetField("allowedAreas", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null)
			{
				return;
			}
			List<Map> maps = Find.Maps;
			foreach (Pawn item in PawnsFinder.AllMapsWorldAndTemporary_Alive)
			{
				Pawn_PlayerSettings playerSettings = item.playerSettings;
				if (playerSettings == null || !(field.GetValue(playerSettings) is Dictionary<Map, Area> dictionary))
				{
					continue;
				}
				Dictionary<Map, Area> dictionary2 = new Dictionary<Map, Area>();
				foreach (KeyValuePair<Map, Area> item2 in dictionary)
				{
					if ((item2.Key == null || item2.Value == null) && GravshipLogger.ShouldLog)
					{
						Log.Warning($"[Gravship DEBUG] セーブ前全体クリーン: Pawn={item.LabelShortCap} ({item.ThingID}) に不正な allowedAreas エントリ �?key={item2.Key}, val={item2.Value}");
					}
					if (item2.Key != null && maps.Contains(item2.Key) && item2.Value != null && item2.Key.areaManager.AllAreas.Contains(item2.Value))
					{
						dictionary2[item2.Key] = item2.Value;
					}
				}
				field.SetValue(playerSettings, dictionary2);
			}
		}
	}

	public static GravshipSettings Settings;

	private Vector2 scrollPos;

	private float cachedRequiredHeight = 0f;

	private int cachedFileCount = -1;

	public GravshiptoSpaceshipMod(ModContentPack content)
		: base(content)
	{
		Harmony harmony = new Harmony("Gravshipto.Spaceship");
		harmony.PatchAll();
		Settings = GetSettings<GravshipSettings>();
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		if (Settings == null)
		{
			Settings = GetSettings<GravshipSettings>();
		}
		List<string> list = new List<string>();
		string path = Path.Combine(GenFilePaths.ConfigFolderPath, "GravshipToSpaceship");
		try
		{
			if (Directory.Exists(path))
			{
				list = Directory.GetFiles(path, "GravshipExport_*.xml").OrderByDescending(File.GetLastWriteTime).ToList();
			}
		}
		catch (Exception arg)
		{
			Log.Warning($"[Gravship] 設定ファイルの読み込みに失敗: {arg}");
			list = new List<string>();
		}
		float num = 25f;
		if (cachedFileCount != list.Count)
		{
			cachedRequiredHeight = 30f + (float)list.Count * num;
			cachedFileCount = list.Count;
		}
		Rect rect = new Rect(inRect.x, inRect.y, inRect.width - 16f, cachedRequiredHeight);
		Rect outRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
		Widgets.BeginScrollView(outRect, ref scrollPos, rect);
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.Begin(rect);
		listing_Standard.Label("GravshipToSpaceship.SelectSaveFileLabel".Translate());
		if (list.Count == 0)
		{
			listing_Standard.Label("GravshipToSpaceship.NoSaveFilesLabel".Translate());
		}
		else
		{
			if (Settings.selectedFileName == null)
			{
				Settings.selectedFileName = Path.GetFileName(list[0]);
				Settings.Write();
			}
			foreach (string item in list)
			{
				string fileName = Path.GetFileName(item);
				bool active = Settings.selectedFileName == fileName;
				if (listing_Standard.RadioButton(fileName, active))
				{
					Settings.selectedFileName = fileName;
					Settings.Write();
				}
			}
		}
		listing_Standard.End();
		Widgets.EndScrollView();
	}

	public override string SettingsCategory()
	{
		return "GravshipToSpaceship";
	}

	public static void CleanInvalidAllowedAreasBeforeSave()
	{
		FieldInfo field = typeof(Pawn_PlayerSettings).GetField("allowedAreas", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field == null)
		{
			return;
		}
		List<Map> maps = Find.Maps;
		HashSet<Pawn> hashSet = new HashSet<Pawn>();
		hashSet.AddRange(PawnsFinder.AllMapsWorldAndTemporary_Alive);
		foreach (Map map in Find.Maps)
		{
			foreach (IThingHolder item2 in map.listerThings.AllThings.OfType<IThingHolder>())
			{
				foreach (Thing item3 in ThingOwnerUtility.GetAllThingsRecursively(item2))
				{
					if (item3 is Pawn item)
					{
						hashSet.Add(item);
					}
				}
			}
		}
		foreach (Map map2 in Find.Maps)
		{
			foreach (Thing allThing in map2.listerThings.AllThings)
			{
				if (allThing is Corpse { InnerPawn: not null } corpse)
				{
					hashSet.Add(corpse.InnerPawn);
				}
			}
		}
		foreach (Pawn item4 in hashSet)
		{
			Pawn_PlayerSettings playerSettings = item4.playerSettings;
			if (playerSettings == null || !(field.GetValue(playerSettings) is Dictionary<Map, Area> dictionary))
			{
				continue;
			}
			Dictionary<Map, Area> dictionary2 = new Dictionary<Map, Area>();
			foreach (KeyValuePair<Map, Area> item5 in dictionary)
			{
				if (item5.Key != null && maps.Contains(item5.Key) && item5.Value != null && item5.Key.areaManager.AllAreas.Contains(item5.Value))
				{
					dictionary2[item5.Key] = item5.Value;
				}
			}
			field.SetValue(playerSettings, dictionary2);
		}
	}

	public static void PreCleanPawnAllowedAreas(List<Thing> things)
	{
		FieldInfo field = typeof(Pawn_PlayerSettings).GetField("allowedAreas", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field == null)
		{
			return;
		}
		List<Map> maps = Find.Maps;
		foreach (Thing thing in things)
		{
			if (thing is Pawn { playerSettings: not null } pawn)
			{
				if (!(field.GetValue(pawn.playerSettings) is Dictionary<Map, Area> dictionary))
				{
					continue;
				}
				foreach (KeyValuePair<Map, Area> item in dictionary)
				{
					if (item.Key == null && GravshipLogger.ShouldLog)
					{
						Log.Warning("[Gravship DEBUG] null Map key in allowedAreas for Pawn=" + pawn.LabelShortCap + " (" + pawn.ThingID + ")");
					}
					if (item.Value == null && GravshipLogger.ShouldLog)
					{
						Log.Warning("[Gravship DEBUG] null Area value in allowedAreas for Pawn=" + pawn.LabelShortCap + " (" + pawn.ThingID + ")");
					}
				}
				Dictionary<Map, Area> dictionary2 = new Dictionary<Map, Area>();
				foreach (KeyValuePair<Map, Area> item2 in dictionary)
				{
					if (item2.Key != null && item2.Value != null && maps.Contains(item2.Key) && item2.Key.areaManager.AllAreas.Contains(item2.Value))
					{
						dictionary2[item2.Key] = item2.Value;
					}
				}
				field.SetValue(pawn.playerSettings, dictionary2);
			}
			if (!(thing is Corpse corpse) || corpse.InnerPawn?.playerSettings == null)
			{
				continue;
			}
			Pawn innerPawn = corpse.InnerPawn;
			if (!(field.GetValue(innerPawn.playerSettings) is Dictionary<Map, Area> dictionary3))
			{
				continue;
			}
			foreach (KeyValuePair<Map, Area> item3 in dictionary3)
			{
				if (item3.Key == null)
				{
					Log.Warning("[Gravship DEBUG] null Map key in allowedAreas for Corpse.InnerPawn=" + innerPawn.LabelShortCap + " (" + innerPawn.ThingID + ")");
				}
				if (item3.Value == null)
				{
					Log.Warning("[Gravship DEBUG] null Area value in allowedAreas for Corpse.InnerPawn=" + innerPawn.LabelShortCap + " (" + innerPawn.ThingID + ")");
				}
			}
			Dictionary<Map, Area> dictionary4 = new Dictionary<Map, Area>();
			foreach (KeyValuePair<Map, Area> item4 in dictionary3)
			{
				if (item4.Key != null && item4.Value != null && maps.Contains(item4.Key) && item4.Key.areaManager.AllAreas.Contains(item4.Value))
				{
					dictionary4[item4.Key] = item4.Value;
				}
			}
			field.SetValue(innerPawn.playerSettings, dictionary4);
		}
	}

	public static void ClearAllowedAreas(List<Thing> things)
	{
		FieldInfo field = typeof(Pawn_PlayerSettings).GetField("allowedAreas", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field == null)
		{
			return;
		}
		HashSet<Pawn> hashSet = new HashSet<Pawn>();
		foreach (Thing thing in things)
		{
			if (thing is Pawn item)
			{
				hashSet.Add(item);
			}
			if (thing is Corpse { InnerPawn: not null } corpse)
			{
				hashSet.Add(corpse.InnerPawn);
			}
			if (!(thing is IThingHolder holder))
			{
				continue;
			}
			foreach (Thing item3 in ThingOwnerUtility.GetAllThingsRecursively(holder))
			{
				if (item3 is Pawn item2)
				{
					hashSet.Add(item2);
				}
				if (item3 is Corpse { InnerPawn: not null } corpse2)
				{
					hashSet.Add(corpse2.InnerPawn);
				}
			}
		}
		foreach (Pawn item4 in hashSet)
		{
			if (item4?.playerSettings != null)
			{
				field.SetValue(item4.playerSettings, new Dictionary<Map, Area>());
			}
		}
	}
}
