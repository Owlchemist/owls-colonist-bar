using UnityEngine;
using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;
using static OwlBar.Mod_OwlBar;
using static RimWorld.ColonistBarColonistDrawer;
using Settings = OwlBar.ModSettings_OwlBar;

namespace OwlBar
{
	public class PawnCache
	{
		public PawnCache(Pawn pawn, Vector2 cachedDrawLocs, int groupID, int skipped, int i)
		{
			entryIndex = i;
			ID = pawn.thingIDNumber;

			//Setup BG rect
			container = new Rect(cachedDrawLocs.x - (72f * skipped * vanillaColonistBar.cachedScale), cachedDrawLocs.y, 48f, 48f);;
			portraitRect = vanillaColonistBar.drawer.GetPawnTextureRect(container.position);
			portrait = PortraitsCache.Get(pawn, PawnTextureSize, Rot4.South, PawnTextureCameraOffset, 1.28205f, true, true, true, true, null, null, false);

			//Weapon rect
			weaponRect = container.ContractedBy(5f);
			weaponRect.y += 5f * vanillaColonistBar.cachedScale;
			weaponRect.x += 5f * vanillaColonistBar.cachedScale;
			
			//Label stuff
			label = GenMapUI.GetPawnLabel(pawn, fastColonistBar.labelMaxWidth, vanillaColonistBar.drawer.pawnLabelsCache, 0); 
			labelPos = new Vector2(container.center.x, container.yMax - 4f * vanillaColonistBar.cachedScale);
			labelWidth = GenMapUI.GetPawnLabelNameWidth(pawn, fastColonistBar.labelMaxWidth, vanillaColonistBar.drawer.pawnLabelsCache, 0);
			labelBGRect = new Rect(labelPos.x - labelWidth / 2f - 4f, labelPos.y, labelWidth + 8f, 12f);
			labelRect = new Rect(labelBGRect.center.x - labelWidth / 2f, labelBGRect.y - 2f, labelWidth, 100f);

			lastGroupID = groupID; //Checked each loop to determine if data is dirty
			FetchShortCache(pawn);
		}
		public Texture portrait;
		public string label;
		public float labelWidth;
		public Vector2 labelPos;
		public Rect container, portraitRect, labelBGRect, labelRect, weaponRect;
		public int lastGroupID, entryIndex, ID;

		//Short cache, refreshed every 120 frames (2~ seconds)
		public void FetchShortCache(Pawn pawn)
		{
			dead = pawn?.Dead ?? false;
			if (!dead)
			{
				//Mood border
				float moodPercentage = pawn.needs?.mood?.CurLevelPercentage ?? 1f;
				if (moodPercentage >= pawn.mindState.mentalBreaker.BreakThresholdMinor) {
					moodColor = Color.clear;
					moodBorderWidth = ResourceBank.vector4Zero;
				}
				else if (moodPercentage < pawn.mindState.mentalBreaker.BreakThresholdExtreme) {
					moodColor = ResourceBank.colorWhite;
					moodBorderWidth = ResourceBank.vector4One * 3;
				}
				else if (moodPercentage < pawn.mindState.mentalBreaker.BreakThresholdMajor) {
					moodColor = ResourceBank.colorRed;
					moodBorderWidth = ResourceBank.vector4One * 2;
				}
				else {
					moodColor = ResourceBank.colorYellow;
					moodBorderWidth = ResourceBank.vector4One * 1;
				}

				//Mood grievances
				if (moodPercentage < pawn.mindState.mentalBreaker.BreakThresholdMinor)
				{
					PawnNeedsUIUtility.GetThoughtGroupsInDisplayOrder(pawn.needs.mood, NeedsCardUtility.thoughtGroupsPresent);

					int length = NeedsCardUtility.thoughtGroupsPresent.Count;
					List<string> grievancesList = new List<string>();
					for (int i = 0; i < length; ++i)
					{
						Thought thoughtGroup = NeedsCardUtility.thoughtGroupsPresent[i];
						if (!thoughtGroup.VisibleInNeedsTab) continue;

						float offset = pawn.needs.mood.thoughts.MoodOffsetOfGroup(thoughtGroup);
						if (offset < 0f)
						{
							grievancesList.Add(System.Math.Ceiling(offset) + " " + thoughtGroup.def.LabelCap);
						}
					}
					grievances = string.Join(System.Environment.NewLine, grievancesList);
				}
			}

			drafted = pawn?.Drafted  ?? false;
			health = pawn?.health.summaryHealth.SummaryHealthPercent ?? 0;

			//Icons
			vanillaColonistBar.drawer.DrawIcons(portraitRect, pawn);
			iconCount = ColonistBarColonistDrawer.tmpIconsToDraw?.Count ?? 0;
			for (int i = 0; i < iconCount; ++i)
			{
				var icon = ColonistBarColonistDrawer.tmpIconsToDraw[i];
				//Process the showRoles user setting
				if (!Settings.showRoles && ResourceBank.roles.Contains(icon.texture.name)) 
				{
					ColonistBarColonistDrawer.tmpIconsToDraw.RemoveAt(i);
					--iconCount;
					--i;
					continue;
				}
				//Predetermine the colors because doing null checks isn't free
				if (icon.color == null)
				{
					icon.color = ResourceBank.colorWhite;
					ColonistBarColonistDrawer.tmpIconsToDraw[i] = icon;
				}
			}
			iconCache = ColonistBarColonistDrawer.tmpIconsToDraw?.ToArray();
			iconGap = Mathf.Min(BaseIconAreaWidth / (float)iconCount, BaseIconMaxSize) * vanillaColonistBar.cachedScale;
			iconRect = new Rect(portraitRect.x + 1f, portraitRect.yMax - iconGap - 1f, iconGap, iconGap);

			//Health
			healthBar = labelBGRect;
			healthBar.width *= health;

			//Label color
			labelColor = PawnNameColorUtility.PawnNameColorOf(pawn);
			labelIsColored = labelColor != ResourceBank.colorWhite;

			//Weapon
			weapon = pawn.equipment.Primary;
			if (weapon != null)
			{
				weaponIcon = (weapon.Graphic.ExtractInnerGraphicFor(weapon).MatSingle.mainTexture as Texture2D) ?? weapon.def.uiIcon;
				
				//Weapon rect
				Vector2 vector = GUIClip.Unclip(new Vector2(container.xMin + container.width / 2f, container.yMin + container.height / 2f) * Prefs.UIScale);
				weaponMatrix = Matrix4x4.TRS(vector, Quaternion.Euler(0f, 0f, weapon.def.equippedAngleOffset +50f), Vector3.one) * Matrix4x4.TRS(-vector, Quaternion.identity, Vector3.one) * GUI.matrix;
			}
		}
		public IconDrawCall[] iconCache;
		public Rect iconRect, healthBar;
		public Color labelColor, moodColor;
		public Vector4 moodBorderWidth;
		public Thing weapon;
		public Texture2D weaponIcon;
		public Matrix4x4 weaponMatrix;
		public bool drafted, dead, labelIsColored;
		public int iconCount;
		public float iconGap, health;
		public string grievances;

		public Pawn Pawn
		{
			get
			{
				return PawnsFinder.All_AliveOrDead.FirstOrDefault(y => y.thingIDNumber == ID);
			}
		}
	}
}