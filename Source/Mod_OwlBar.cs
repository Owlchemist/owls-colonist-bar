using Verse;
using HarmonyLib;
using UnityEngine;
using RimWorld;
using static OwlBar.ModSettings_OwlBar;
 
namespace OwlBar
{
    public class Mod_OwlBar : Mod
	{		
		public Mod_OwlBar(ModContentPack content) : base(content)
		{
			new Harmony(this.Content.PackageIdPlayerFacing).PatchAll();
			base.GetSettings<ModSettings_OwlBar>();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			inRect.yMin += 20f;
			inRect.yMax -= 20f;
			Listing_Standard options = new Listing_Standard();
			Rect outRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
			Rect rect = new Rect(0f, 0f, inRect.width - 30f, inRect.height * 1.2f);
			Widgets.BeginScrollView(outRect, ref scrollPos, rect, true);

			options.Begin(rect);
			
			options.Label("OwlBar.EntriesPerRow".Translate(entriesPerRow.ToString()), -1f, null);
			entriesPerRow = (int)options.Slider(entriesPerRow, 10f, 64f);

			options.Label("OwlBar.MaxRows".Translate(maxRows.ToString()), -1f, null);
			maxRows = (int)options.Slider(maxRows, 1f, 5f);

			options.Label("OwlBar.EntryScale".Translate(System.Math.Round(entryScale, 1).ToString()), -1f, null);
			entryScale = (float)System.Math.Round(options.Slider(entryScale, 0.7f, 2.5f), 1);

			options.CheckboxLabeled("OwlBar.MoodBackgrounds".Translate(), ref moodBackgrounds, "OwlBar.MoodBackgrounds.Desc".Translate());
			options.CheckboxLabeled("OwlBar.GoodMoodAltMode".Translate(), ref goodMoodAltMode, "OwlBar.GoodMoodAltMode.Desc".Translate());
			options.CheckboxLabeled("OwlBar.RelationshipAltMode".Translate(), ref relationshipAltMode, "OwlBar.RelationshipAltMode.Desc".Translate());
			options.CheckboxLabeled("OwlBar.DrawWeaponsBelow".Translate(), ref drawWeaponsBelow, "OwlBar.DrawWeaponsBelow.Desc".Translate());
			options.CheckboxLabeled("OwlBar.CompatMode".Translate(), ref compatMode, "OwlBar.CompatMode.Desc".Translate());
			options.Gap();
			options.Label("OwlBar.Header.Icons".Translate());
			options.GapLine(); //======================================
			if (ModLister.IdeologyInstalled) options.CheckboxLabeled("OwlBar.ShowRoles".Translate(), ref showRoles, "OwlBar.ShowRoles.Desc".Translate());
			options.CheckboxLabeled("OwlBar.ShowHunger".Translate(), ref showHunger, "OwlBar.ShowHunger.Desc".Translate());
			if (showHunger) options.CheckboxLabeled("OwlBar.ShowHungerIfDrafted".Translate(), ref showHungerIfDrafted, "OwlBar.ShowHungerIfDrafted.Desc".Translate());
			options.CheckboxLabeled("OwlBar.ShowTired".Translate(), ref showTired, "OwlBar.ShowTired.Desc".Translate());
			if (showTired) options.CheckboxLabeled("OwlBar.ShowTiredIfDrafted".Translate(), ref showTiredIfDrafted, "OwlBar.ShowTiredIfDrafted.Desc".Translate());
			

			options.End();
			Widgets.EndScrollView();

			//Refresh the bar in case the scale is changing
			if (Current.ProgramState == ProgramState.Playing && Find.ColonistBar != null) Find.ColonistBar.entriesDirty = true;
		}

		public override string SettingsCategory()
		{
			return "Owl's Colonist Bar";
		}

		public override void WriteSettings()
		{
			base.WriteSettings();
			if (OwlColonistBar._instance != null) OwlColonistBar._instance.relationshipViewerEnabled = !relationshipAltMode;
		}
	}

	public class ModSettings_OwlBar : ModSettings
	{
		public override void ExposeData()
		{
			Scribe_Values.Look(ref showRoles, "showRoles", true);
			Scribe_Values.Look(ref showHunger, "showHunger", true);
			Scribe_Values.Look(ref showHungerIfDrafted, "showHungerIfDrafted", true);
			Scribe_Values.Look(ref showTired, "showTired", true);
			Scribe_Values.Look(ref showTiredIfDrafted, "showTiredIfDrafted", true);
			Scribe_Values.Look(ref moodBackgrounds, "moodbackgrounds", true);
			Scribe_Values.Look(ref relationshipAltMode, "relationshipAltMode");
			Scribe_Values.Look(ref goodMoodAltMode, "goodMoodAltMode");
			Scribe_Values.Look(ref compatMode, "compactMode");
			Scribe_Values.Look(ref drawWeaponsBelow, "drawWeaponsBelow", true);
			Scribe_Values.Look(ref entriesPerRow, "entriesPerRow", 20);
			Scribe_Values.Look(ref maxRows, "maxRows", 2);
			Scribe_Values.Look(ref entryScale, "entryScale", 1f);
			base.ExposeData();
		}

		public static bool showRoles = true, showHunger = true, showHungerIfDrafted = true, showTired = true, showTiredIfDrafted = true,
		moodBackgrounds = true, relationshipAltMode, goodMoodAltMode, compatMode, drawWeaponsBelow = true;
		public static Vector2 scrollPos = Vector2.zero;
		public static float entryScale = 1f;
		public static int entriesPerRow = 20, maxRows = 2;
	}
}