using UnityEngine;
using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;
using static RimWorld.ColonistBarColonistDrawer;
using Settings = OwlBar.ModSettings_OwlBar;

namespace OwlBar
{
	public class PawnCache
	{
		public PawnCache(Pawn pawn, Vector2 cachedDrawLocs, float labelMaxWidth, int worldGroupID, int skipped, int i)
		{
			entryIndex = i;
			ID = pawn.thingIDNumber;
			var scale = Find.ColonistBar.Scale;

			//Setup BG rect
			container = new Rect(cachedDrawLocs.x - (72f * skipped * 1f), cachedDrawLocs.y, 48f, 48f);
			portraitRect = new Rect(container.x + 1f, container.y - 26f, container.m_Width - 2f, container.m_Height + 26f).ContractedBy(2f);
			portrait = PortraitsCache.Get(pawn, PawnTextureSize, Rot4.South, PawnTextureCameraOffset, 1.28205f, true, true, true, true, null, null, false);

			//Weapon rect
			if (Settings.drawWeaponsBelow)
			{
				weaponRect = new Rect(container.x, container.y + container.height * 1.05f, container.width, container.height).ScaledBy(0.75f);
			}
			else 
			{
				weaponRect = container.ContractedBy(5f);
				weaponRect.y += 5f;
				weaponRect.x += 5f;
			}
			
			//Label stuff
			label = GenMapUI.GetPawnLabel(pawn, labelMaxWidth, Find.ColonistBar.drawer.pawnLabelsCache, 0); 
			labelPos = new Vector2(container.center.x, container.yMax - 4f * 1f);
			labelWidth = GenMapUI.GetPawnLabelNameWidth(pawn, labelMaxWidth, Find.ColonistBar.drawer.pawnLabelsCache, 0);
			labelBGRect = new Rect(labelPos.x - labelWidth / 2f - 4f, labelPos.y, labelWidth + 8f, 12f);
			labelRect = new Rect(labelBGRect.center.x - labelWidth / 2f, labelBGRect.y - 2f, labelWidth, 100f);

			//Group leader check
			if (OwlColonistBar._instance.pawnGroups.groupLeaders.ContainsKey(ID))
			{
				groupRect = container;
				groupRect.x += container.width;
				groupRect.width = container.width / 5f;
			}

			lastWorldGroupID = worldGroupID; //Checked each loop to determine if data is dirty
			FetchShortCache(pawn, labelMaxWidth, false);
		}
		public Texture portrait;
		public string label;
		public float labelWidth;
		public Vector2 labelPos;
		public Rect container, portraitRect, labelBGRect, labelRect, weaponRect, groupRect;
		public int lastWorldGroupID, entryIndex, ID, cacheReorderableGroup;
		
		//Short cache, refreshed every 120 frames (2~ seconds)
		public void FetchShortCache(Pawn pawn, float labelMaxWidth, bool shortOnly = true)
		{
			dead = pawn.Dead;
			drafted = pawn.Drafted;
			CacheMoodData(pawn);

			//Icons
			Find.ColonistBar.drawer.DrawIcons(portraitRect, pawn);
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
			iconGap = Mathf.Min(BaseIconAreaWidth / (float)iconCount, BaseIconMaxSize) * 1f;
			iconRect = new Rect(portraitRect.m_XMin + 1f, portraitRect.yMax - iconGap - 1f, iconGap, iconGap);

			//Label color
			labelColor = PawnNameColorUtility.PawnNameColorOf(pawn);
			labelIsColored = labelColor != ResourceBank.colorWhite;

			//Weapon
			weapon = pawn.equipment.Primary;
			if (weapon != null)
			{
				weaponIcon = (weapon.Graphic.ExtractInnerGraphicFor(weapon).MatSingle.mainTexture as Texture2D) ?? weapon.def.uiIcon;
				
				//Weapon rect
				Vector2 vector = GUIClip.Unclip(new Vector2(container.m_XMin + container.width / 2f, container.m_YMin + container.height / 2f) * Prefs.UIScale);
				var iconAngle = Settings.drawWeaponsBelow ? weapon.def.uiIconPath.NullOrEmpty() ? weapon.def.uiIconAngle : 0f : weapon.def.equippedAngleOffset + 50f;
				weaponMatrix = Matrix4x4.TRS(vector, Quaternion.Euler(0f, 0f, iconAngle), Vector3.one) * Matrix4x4.TRS(-vector, Quaternion.identity, Vector3.one) * GUI.matrix;
			}

			//Bleeding
			if (pawn.health.hediffSet.cachedBleedRate > 1f)
			{
				//These need to be reset to their original values
				if (shortOnly)
				{
					label = GenMapUI.GetPawnLabel(pawn, labelMaxWidth, Find.ColonistBar.drawer.pawnLabelsCache, 0); 
					labelBGRect = new Rect(labelPos.x - labelWidth / 2f - 4f, labelPos.y, labelWidth + 8f, 12f);
				}
				labelColor = ResourceBank.colorRed;
				labelBGRect.height *= 2.2f;
				labelBGRect.ExpandedBy(5f);
				int num = HealthUtility.TicksUntilDeathDueToBloodLoss(pawn);
				if (num < 60000)
				{
					emergency = true;
					label += "\n" + num.ToStringTicksToPeriod(true, true, false, false);
				}
			}
		}
		public IconDrawCall[] iconCache;
		public Rect iconRect, healthBar;
		public Color labelColor, moodColor;
		public Vector4 moodBorderWidth;
		public Thing weapon;
		public Texture2D weaponIcon;
		public Matrix4x4 weaponMatrix;
		public bool drafted, dead, labelIsColored, emergency;
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

		void CacheMoodData(Pawn pawn)
		{
			if (!dead)
			{
				//Mood border
				float moodPercentage = pawn.needs?.mood?.CurLevelPercentage ?? 1f;
				if (moodPercentage >= pawn.mindState.mentalBreaker.BreakThresholdMinor) {
					if (!Settings.goodMoodAltMode)
					{
						moodColor = ResourceBank.colorClear;
						moodBorderWidth = ResourceBank.vector4Zero;
					}
					else
					{
						moodColor = ResourceBank.colorGreen;
						moodBorderWidth = ResourceBank.vector4One * 1;
					}
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
					pawn.needs.mood.thoughts.GetDistinctMoodThoughtGroups(NeedsCardUtility.thoughtGroupsPresent);
					for (int i = NeedsCardUtility.thoughtGroupsPresent.Count - 1; i >= 0; --i)
					{
						if (!NeedsCardUtility.thoughtGroupsPresent[i].VisibleInNeedsTab) NeedsCardUtility.thoughtGroupsPresent.RemoveAt(i);
					}

					int length = NeedsCardUtility.thoughtGroupsPresent.Count;
					List<string> grievancesList = new List<string>();
					for (int i = 0; i < length; ++i)
					{
						Thought thoughtGroup = NeedsCardUtility.thoughtGroupsPresent[i];
						if (!thoughtGroup.VisibleInNeedsTab) continue;

						float offset = pawn.needs.mood.thoughts.MoodOffsetOfGroup(thoughtGroup);
						if (offset < 0f)
						{
							grievancesList.Add(System.Math.Ceiling(offset) + " " + thoughtGroup.LabelCap);
						}
					}
					grievances = string.Join(System.Environment.NewLine, grievancesList);
				}
			}
		}
	}
}