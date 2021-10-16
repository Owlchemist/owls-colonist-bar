using Verse;
using HarmonyLib;
using UnityEngine;
using RimWorld;
using static OwlBar.ModSettings_OwlBar;
 
namespace OwlBar
{
	#if DEBUG
	[HotSwap.HotSwappable]
	#endif
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
			options.Gap();
			options.Label("OwlBar_Header_Icons".Translate());
			options.GapLine(); //======================================
			options.CheckboxLabeled("OwlBar_ShowRoles".Translate(), ref showRoles, "OwlBar_ShowRolesDesc".Translate());
			options.CheckboxLabeled("OwlBar_ShowHunger".Translate(), ref showHunger, "OwlBar_ShowHungerDesc".Translate());
			if (showHunger) options.CheckboxLabeled("OwlBar_ShowHungerIfDrafted".Translate(), ref showHungerIfDrafted, "OwlBar_ShowHungerIfDraftedDesc".Translate());
			options.CheckboxLabeled("OwlBar_ShowTired".Translate(), ref showTired, "OwlBar_ShowTiredDesc".Translate());
			if (showTired) options.CheckboxLabeled("OwlBar_ShowTiredIfDrafted".Translate(), ref showTiredIfDrafted, "OwlBar_ShowTiredIfDraftedDesc".Translate());
			
			options.Gap();
			options.Label("OwlBar_Header_Weapons".Translate());
			options.GapLine(); //======================================
			options.CheckboxLabeled("OwlBar_ShowWeapons".Translate(), ref showWeapons, "OwlBar_ShowWeaponsDesc".Translate());
			if (showWeapons) options.CheckboxLabeled("OwlBar_ShowWeaponsIfDrafted".Translate(), ref showWeaponsIfDrafted, "OwlBar_ShowWeaponsIfDraftedDesc".Translate());
			

			options.End();
			Widgets.EndScrollView();
			base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "Unnamed Colonist Bar";
		}

		public override void WriteSettings()
		{
			base.WriteSettings();
		}
	}

	#if DEBUG
	[HotSwap.HotSwappable]
	#endif
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
			base.ExposeData();
		}

		public static bool showRoles = true;
		public static bool showHunger = true;
		public static bool showHungerIfDrafted = true;
		public static bool showTired = true;
		public static bool showTiredIfDrafted = true;
		public static bool showWeapons = true;
		public static bool showWeaponsIfDrafted = true;
		public static Vector2 scrollPos = Vector2.zero;
	}
}