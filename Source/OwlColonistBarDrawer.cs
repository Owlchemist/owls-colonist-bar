using RimWorld.Planet;
using UnityEngine;
using Verse;
using System.Linq;
using RimWorld;
using static OwlBar.Mod_OwlBar;
using static OwlBar.FastGUI;
using static OwlBar.ResourceBank;
using Settings = OwlBar.ModSettings_OwlBar;

namespace OwlBar
{
	public class OwlColonistBarDrawer
	{
		GUIContent guiContent = GUIContent.Temp(""); //Dirty hack to avoid reinstantiating this every label

		public void DrawColonistFast(PawnCache pawnCache, Rect containerRect, Pawn pawn, Map map, bool highlight, bool reordering)
		{
			//Determine transparency
			if (map != fastColonistBar.currentMap || reordering) FastGUI.currentTransparency = 0.5f;
			else FastGUI.currentTransparency = 1f;

			//Determine this pawn's relationships with whomever is highlighted
			Color portraitColor;
			Texture2D portraitBGTexture;
			GetRelations(pawn, out portraitBGTexture, out portraitColor);

			//Prepare mood color
			Color moodColor = pawnCache.moodColor;
			if (pawnCache.moodColor.b == 1f || pawnCache.emergency)
			{
				float pulseNum = Pulser.PulseBrightness(0.5f, Pulser.PulseBrightness(0.5f, 0.6f));
				moodColor = new Color(pulseNum, pulseNum, pulseNum) * ResourceBank.colorRed;
			}

			//Draw BG box
			if ((!Settings.moodBackgrounds || pawnCache.moodColor.a != 1f || (fastColonistBar.relationshipViewerEnabled && fastColonistBar.selectedPawn != null)) && !pawnCache.emergency) DrawTextureFast(containerRect, portraitBGTexture, vector4Zero, portraitColor);
			else DrawTextureFast(containerRect, portraitBackgroundWhite, vector4Zero, moodColor);

			//Show relationship tooltip
			if (fastColonistBar.relationshipViewerEnabled && fastColonistBar.selectedPawn != null)
			{
				if (!fastColonistBar.selectedPawnAlt) TooltipHandler.TipRegion(containerRect, "OwlBar.RelationshipView.Tooltip".Translate());
				else TooltipHandler.TipRegion(containerRect, "OwlBar.RelationshipViewAlt.Tooltip".Translate());
			}

			//Draw mood border
			if (pawnCache.moodColor.a == 1f) DrawTextureFast(containerRect, BaseContent.WhiteTex, pawnCache.moodBorderWidth, moodColor);

			//Check for tooltips
			if (pawnCache.grievances != null) TooltipHandler.TipRegion(containerRect, pawnCache.grievances);

			//White outline when you mouseover related UI elements, like sidebar alerts
			if (highlight) DrawTextureFast(containerRect, BaseContent.WhiteTex, ResourceBank.vector4One * 2f, ResourceBank.colorWhite);
			
			//The white target retacle when you click on a pawn
			if (!fastColonistBar.worldRender && pawnCache.dead ? fastColonistBar.selectorBuffer.Contains(pawn.Corpse) : fastColonistBar.selectorBuffer.Contains(pawn)){
				vanillaColonistBar.drawer.DrawSelectionOverlayOnGUI(pawn, pawnCache.container);
			}
			else if (fastColonistBar.worldRender && pawn.IsCaravanMember() && Find.WorldSelector.IsSelected(pawn.GetCaravan())){
				vanillaColonistBar.drawer.DrawCaravanSelectionOverlayOnGUI(pawn.GetCaravan(), pawnCache.container);
			}

			//Draw portrait
			DrawTextureFast(pawnCache.portraitRect, pawnCache.portrait, vector4Zero, colorWhite);

			//Prepare icons for drawing
			if (pawnCache.iconCount != 0)
			{
				Rect rect = pawnCache.iconRect;
				for (int i = 0; i < pawnCache.iconCount; ++i, rect.x += pawnCache.iconGap)
				{
					RimWorld.ColonistBarColonistDrawer.IconDrawCall icon = pawnCache.iconCache[i];
					fastColonistBar.iconDrawQueue.Add((rect, icon, fastColonistBar.showWeapon));
					if (icon.tooltip != null) TooltipHandler.TipRegion(rect, icon.tooltip);
				}
			}

			//Draw dead pawn X
			if (pawnCache.dead) DrawTextureFast(containerRect, RimWorld.ColonistBarColonistDrawer.DeadColonistTex, vector4Zero, colorWhite);

			//Draw shadow box behind the label
			DrawTextureFast(pawnCache.labelBGRect, TexUI.GrayTextBG, vector4Zero, colorWhite);

			//Draw label, use the faster method if possible
			guiContent.m_Text = pawnCache.label;
			if (pawnCache.labelIsColored)
			{
				GUI.color = pawnCache.labelColor;
				fastColonistBar.guiStyle.Internal_Draw_Injected(ref pawnCache.labelRect, guiContent, false, false, false, false);
				GUI.color = ResourceBank.colorWhite;
			}
			else fastColonistBar.guiStyle.Internal_Draw_Injected(ref pawnCache.labelRect, guiContent, false, false, false, false);
			
			//Draw draft underline
			if (pawnCache.drafted) DrawTextureFast(new Rect(pawnCache.labelRect.x, pawnCache.labelBGRect.yMax, pawnCache.labelWidth, 1f), BaseContent.WhiteTex, vector4Zero, colorWhite);
		}

		public void GetRelations(Pawn pawn, out Texture2D portraitTexture, out Color portraitColor)
		{
			if (fastColonistBar.relationshipViewerEnabled && fastColonistBar.selectedPawn != null && fastColonistBar.selectedPawn != pawn)
			{
				
				Pawn from = fastColonistBar.selectedPawnAlt ? pawn : fastColonistBar.selectedPawn;
				Pawn to = fastColonistBar.selectedPawnAlt ? fastColonistBar.selectedPawn : pawn;

				if (fastColonistBar.selectedPawnAlt)
				{
					if (pawn.relations != null) fastColonistBar.selectedPawnsLovers = pawn.GetLoveRelations(false).Select(x => x.otherPawn.thingIDNumber).ToHashSet();
				}
				if (fastColonistBar.selectedPawnsLovers.Contains(to.thingIDNumber))
				{
					portraitColor = new Color(1f, 0.75f, 0.80f, 1f);
					portraitTexture = BaseContent.WhiteTex;
				}
				else
				{
					portraitTexture = RimWorld.ColonistBar.BGTex;
					var opinion = from.relations.OpinionOf(to);
					if (opinion > 20) {
						portraitColor = Color.green;
						portraitTexture = BaseContent.GreyTex;
					}
					else if (opinion < -20) {
						float pulseNum = Pulser.PulseBrightness(0.5f, Pulser.PulseBrightness(0.5f, 0.6f));
						portraitColor = new Color(pulseNum, pulseNum, pulseNum) * ResourceBank.colorRed;
						portraitTexture = BaseContent.GreyTex;
					}
					else portraitColor = colorWhite;
				}
			}
			else
			{
				portraitColor = colorWhite;
				portraitTexture = RimWorld.ColonistBar.BGTex;
			}
		}
	}
}