using Verse;
using HarmonyLib;
using UnityEngine;
using RimWorld;
using static OwlBar.ModSettings_OwlBar;
 
namespace OwlBar
{
    public class Mod_OwlBar : Mod
	{
		public static OwlColonistBar fastColonistBar;
		public static ColonistBar vanillaColonistBar;
		public static PawnGroups pawnGroups;
		static public bool shortDataDirty = false;
		//static public int frameID;

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

			//Listing_Standard options = new Listing_Standard();
			options.Begin(rect);
			if (Prefs.DevMode) options.CheckboxLabeled("DevMode: Mod enabled", ref modEnabled, null);
			options.CheckboxLabeled("OwlBar.MoodBackgrounds".Translate(), ref moodBackgrounds, "OwlBar.MoodBackgrounds.Desc".Translate());
			options.CheckboxLabeled("OwlBar.GoodMoodAltMode".Translate(), ref goodMoodAltMode, "OwlBar.GoodMoodAltMode.Desc".Translate());
			options.CheckboxLabeled("OwlBar.RelationshipAltMode".Translate(), ref relationshipAltMode, "OwlBar.RelationshipAltMode.Desc".Translate());
			options.Gap();
			options.Label("OwlBar.Header.Icons".Translate());
			options.GapLine(); //======================================
			options.CheckboxLabeled("OwlBar.ShowRoles".Translate(), ref showRoles, "OwlBar.ShowRoles.Desc".Translate());
			options.CheckboxLabeled("OwlBar.ShowHunger".Translate(), ref showHunger, "OwlBar.ShowHunger.Desc".Translate());
			if (showHunger) options.CheckboxLabeled("OwlBar.ShowHungerIfDrafted".Translate(), ref showHungerIfDrafted, "OwlBar.ShowHungerIfDrafted.Desc".Translate());
			options.CheckboxLabeled("OwlBar.ShowTired".Translate(), ref showTired, "OwlBar.ShowTired.Desc".Translate());
			if (showTired) options.CheckboxLabeled("OwlBar.ShowTiredIfDrafted".Translate(), ref showTiredIfDrafted, "OwlBar.ShowTiredIfDrafted.Desc".Translate());
			
			options.Gap();
			options.Label("OwlBar.Header.Weapons".Translate());
			options.GapLine(); //======================================
			options.CheckboxLabeled("OwlBar.ShowWeapons".Translate(), ref showWeapons, "OwlBar.ShowWeapons.Desc".Translate());
			if (showWeapons) options.CheckboxLabeled("OwlBar.ShowWeaponsIfDrafted".Translate(), ref showWeaponsIfDrafted, "OwlBar.ShowWeaponsIfDrafted.Desc".Translate());
			

			options.End();
			Widgets.EndScrollView();
			base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "Owl's Colonist Bar";
		}

		public override void WriteSettings()
		{
			base.WriteSettings();
			if (fastColonistBar != null) fastColonistBar.relationshipViewerEnabled = !relationshipAltMode;
		}
	}

	public class ModSettings_OwlBar : ModSettings
	{
		public override void ExposeData()
		{
			Scribe_Values.Look<bool>(ref showRoles, "showRoles", true, false);
			Scribe_Values.Look<bool>(ref showHunger, "showHunger", true, false);
			Scribe_Values.Look<bool>(ref showHungerIfDrafted, "showHungerIfDrafted", true, false);
			Scribe_Values.Look<bool>(ref showTired, "showTired", true, false);
			Scribe_Values.Look<bool>(ref showTiredIfDrafted, "showTiredIfDrafted", true, false);
			Scribe_Values.Look<bool>(ref showWeapons, "showWeapons", true, false);
			Scribe_Values.Look<bool>(ref showWeaponsIfDrafted, "showWeaponsIfDrafted", true, false);
			Scribe_Values.Look<bool>(ref moodBackgrounds, "moodbackgrounds", true, false);
			Scribe_Values.Look<bool>(ref relationshipAltMode, "relationshipAltMode", false, false);
			Scribe_Values.Look<bool>(ref goodMoodAltMode, "goodMoodAltMode", false, false);
			Scribe_Values.Look<bool>(ref modEnabled, "modEnabled", true, false);
			base.ExposeData();
		}

		public static bool showRoles = true, showHunger = true, showHungerIfDrafted = true, showTired = true, showTiredIfDrafted = true,
		showWeapons = true, showWeaponsIfDrafted = true, moodBackgrounds = true, modEnabled = true, relationshipAltMode, goodMoodAltMode;
		public static Vector2 scrollPos = Vector2.zero;
	}
}