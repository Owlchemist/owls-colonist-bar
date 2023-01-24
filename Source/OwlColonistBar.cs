using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using System.Runtime.CompilerServices;
using static RimWorld.ColonistBar;
using static OwlBar.FastGUI;
using static OwlBar.ResourceBank;
using Settings = OwlBar.ModSettings_OwlBar;

namespace OwlBar
{
	public class OwlColonistBar
	{
		public PawnCache[] colonistBarCache;
		int previousNumOfEntries;
		
		//The below are used by FastColonistBarDrawer. We'd pass it along directly but we need to pass through the vanilla method first.
		public bool shortDataDirty, selectedPawnAlt, relationshipViewerEnabled = true;
		static GUIContent guiContent = GUIContent.Temp(""); //Dirty hack to avoid reinstantiating this every label
		static int frames = 120, frameLoops;

		public static OwlColonistBar _instance;
		public Pawn selectedPawn;
		public PawnGroups pawnGroups;

		public OwlColonistBar()
		{
			_instance = this;
		}
		public void ResetCache(ColonistBar cb)
		{
			cb.CheckRecacheEntries();
			colonistBarCache = new PawnCache[cb.cachedDrawLocs.Count];
		}
		public void ColonistBarOnGUI(ColonistBar _vanillaInstance)
		{
			PawnCache pawnCache;
			Entry entry;
			Rect pawnCacheContainer;
			Internal_DrawTextureArguments drawArguments = FastGUI.drawArguments;
				
			//Prepare
            if (shortDataDirty = ++frames == 121)
            {
                frames = 0;
                _vanillaInstance.CheckRecacheEntries();
                if (++frameLoops == 20)
                {
                    frameLoops = 0;
                    ResetCache(_vanillaInstance);
                }
            }

			//Begin
			var eventCurrent = Event.current;
			var eventType = eventCurrent.type;
			if (_vanillaInstance.Visible && eventType != EventType.Layout)
			{
				var length = _vanillaInstance.cachedDrawLocs.Count;
				//Quick way to check if the data has gone stale
				if (length != previousNumOfEntries)
				{
					ResetCache(_vanillaInstance);
					previousNumOfEntries = length;
				}
				
				//Variables cached out of the loop
				Text.fontInt = 0;
				Text.anchorInt = TextAnchor.UpperCenter;
				GUIStyle guiStyle = Text.CurFontStyle;
				var currentMap = Current.gameInt.CurrentMap;
				int groupIDTracker = -1, reorderableGroup = -1, skipped = 0;
				bool showGroupFrames = false;
				float currentTransparency = 1f, labelMaxWidth = 70f;
				List<int> weaponDrawQueue = new List<int>();
				List<(Rect, RimWorld.ColonistBarColonistDrawer.IconDrawCall, bool)> iconDrawQueue = new List<(Rect, RimWorld.ColonistBarColonistDrawer.IconDrawCall, bool)>();

				//Handle pawns that are currently clicked
				HashSet<int> selectedPawnsLovers;
				Pawn selectedPawn;
				HandleSelectedPawns(((UIRoot_Play)Current.rootInt.uiRoot).mapUI.selector.selected, out selectedPawnsLovers, out selectedPawn);
				
				//Iterate through the portrait entries
				for (int i = 0; i < length; i++)
				{
					Vector2 cachedDrawLocs;
					
					//This would happen if a pawn is removed before the cache is regenerated
					try { cachedDrawLocs = _vanillaInstance.cachedDrawLocs[i]; }
					catch (System.ArgumentOutOfRangeException) { continue;  }

					entry = _vanillaInstance.cachedEntries[i];
					int group = entry.group;
					Pawn pawn = entry.pawn;

					//Invoke the group frame. We don't use this but this is for mod support like SoS2
					_vanillaInstance.drawer.DrawGroupFrame(group);

					if (pawn == null) continue; //Failsafe					

					//Check if they're in a group and if the group is expanded or not
					if (pawnGroups.groupMembers.TryGetValue(pawn.thingIDNumber, out int leaderID) && !pawnGroups.groupLeaders[leaderID])
					{
						++skipped;
						continue;
					}

					//Fetch or rebuild the cache
					Redo:
					pawnCache = colonistBarCache[i];
					if (pawnCache == null)
					{
						pawnCache = new PawnCache(pawn, cachedDrawLocs, group, skipped, i, labelMaxWidth);
						colonistBarCache[i] = pawnCache;
					}
					else if (group != pawnCache.lastWorldGroupID) //This would happen if there has been a change in the grouping, like a pawn switching maps
					{
						ResetCache(_vanillaInstance);
						goto Redo;
					}
					else if (shortDataDirty)
					{
						pawnCache.FetchShortCache(pawn, labelMaxWidth, true);
						labelMaxWidth = pawnCache.container.width + _vanillaInstance.SpaceBetweenColonistsHorizontal - 2f;
					}
					pawnCacheContainer = pawnCache.container;

					bool flag = groupIDTracker != group;
					groupIDTracker = group;
					if (eventType == EventType.Repaint)
					{
						if (flag)
						{
							reorderableGroup = ReorderableWidget.NewGroup(entry.reorderAction, ReorderableDirection.Horizontal, new Rect(0f, 0f, (float)UI.screenWidth,
								(float)UI.screenHeight), _vanillaInstance.SpaceBetweenColonistsHorizontal, entry.extraDraggedItemOnGUI, true);
						}
						pawnCache.cacheReorderableGroup = reorderableGroup;
					}

					//Ordering
					bool reordering;
					HandleClicks(pawn, pawnCache, pawnCacheContainer, pawnCache.cacheReorderableGroup, eventCurrent, eventType, out reordering);
					
					//Finally draw
					bool showWeapon = Settings.showWeapons && pawnCache.weapon != null && (!Settings.showWeaponsIfDrafted || pawnCache.drafted);
					if (eventType == EventType.Repaint)
					{
						bool highlight = _vanillaInstance.colonistsToHighlight.Contains(pawn);
						//First invoke the vanilla drawer. Not actually used, just prompting other mod's harmony patches to pre/postfix.
						_vanillaInstance.drawer.DrawColonist(pawnCacheContainer, pawn, entry.map, highlight, reordering);
						//Use our replacement method
						//Determine transparency
						if (entry.map != currentMap || reordering) currentTransparency = 0.5f;
						else currentTransparency = 1f;
						DrawColonistFast(entry, pawnCache, pawnCacheContainer, drawArguments, selectedPawn, guiStyle, _vanillaInstance, highlight, showWeapon, currentTransparency, selectedPawnsLovers, iconDrawQueue);
					}

					//Manage the weapon draw queue
					if (showWeapon) weaponDrawQueue.Add(i);

					//Check if a group icon should be generated
					if (pawnGroups.groupLeaders.TryGetValue(pawn.thingIDNumber, out bool groupExpanded))
					{
						//Draw lines
						if (groupExpanded)
						{
							int memberCount = pawnGroups.groupCounts[entry.pawn.thingIDNumber];
							float lineLength = (memberCount * 72f * 1f) - 23f;
							DrawTextureFast(drawArguments, new Rect(pawnCacheContainer.m_XMin - 1f, pawnCacheContainer.m_YMin - 10f, lineLength, 1f), BaseContent.WhiteTex, vector4Zero, Color.grey, currentTransparency);
							DrawTextureFast(drawArguments,new Rect(pawnCacheContainer.m_XMin - 1f, pawnCacheContainer.yMax, 1f, -58f), BaseContent.WhiteTex, vector4Zero, Color.grey, currentTransparency);
							DrawTextureFast(drawArguments,new Rect(pawnCacheContainer.m_XMin - 1f + lineLength, pawnCacheContainer.yMax, 1f, -58f), BaseContent.WhiteTex, vector4Zero, Color.grey, currentTransparency);

							//Draw button
							DrawTextureFast(drawArguments,pawnCache.groupRect,groupCollapse, vector4Zero, colorWhite, currentTransparency);
						}
						//Just draw the button
						else DrawTextureFast(drawArguments,pawnCache.groupRect, groupExpand, vector4Zero, colorWhite, currentTransparency);
						
						//Click the expand/collapse button?
						if (eventType == EventType.MouseDown && eventCurrent.button == 0 && Mouse.IsOver(pawnCache.groupRect))
						{
							pawnGroups.groupLeaders[entry.pawn.thingIDNumber] ^= true;
							SoundDefOf.Click.PlayOneShotOnCamera(null);
							ResetCache(_vanillaInstance);
							return;
						}
					}
				}

				//Handle being able to click group frames
				int num = -1;
				if (showGroupFrames)
				{
					for (int j = 0; j < length; j++)
					{
						ColonistBar.Entry entry2 = _vanillaInstance.cachedEntries[j];
						bool flag2 = num != entry2.group;
						num = entry2.group;
						if (flag2)
						{
							_vanillaInstance.drawer.HandleGroupFrameClicks(entry2.group);
						}
					}
				}
				
				//Instead of drawing the weapons in the main loop, we batch them all at once, because changing the matrix each loop is quite expensive
				Matrix4x4 matrix = GUI.matrix;
				currentTransparency = 1f;
				foreach (var index in weaponDrawQueue)
				{
					PawnCache pawnCache2 = colonistBarCache[index];
					if (pawnCache2?.weaponIcon == null) continue; //Bad texture?
					GUIClip.SetMatrix_Injected(ref pawnCache2.weaponMatrix);
					DrawTextureFast(drawArguments, pawnCache2.weaponRect, pawnCache2.weaponIcon, vector4Zero, pawnCache2.weapon.DrawColor, currentTransparency);
				}
				GUI.matrix = matrix;

				//This needs to happen here due to draw layering. The icons need to be ontop of the weapons.
				length = iconDrawQueue.Count;
				for (int j = 0; j < length; j++)
				{
					var iconEntry = iconDrawQueue[j];
					//Transparency
					if (iconEntry.Item3) currentTransparency = 0.7f;
					else currentTransparency = 1f;
					
					var icon = iconEntry.Item2;
					DrawTextureFast(drawArguments, iconEntry.Item1, icon.texture, vector4Zero, (Color)icon.color, currentTransparency);
				}
				
				//Reset the GUI controller to defaults when done here
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.UpperLeft;
			}
			if (eventType == EventType.Repaint) _vanillaInstance.colonistsToHighlight.Clear();
		}
		void DrawColonistFast(
			Entry entry, 
			PawnCache pawnCache, 
			Rect pawnCacheContainer, 
			Internal_DrawTextureArguments drawArguments, 
			Pawn selectedPawn, GUIStyle guiStyle, 
			ColonistBar _vanillaInstance, 
			bool highlight, 
			bool showWeapon, 
			float currentTransparency, 
			HashSet<int> selectedPawnsLovers,
			List<(Rect, RimWorld.ColonistBarColonistDrawer.IconDrawCall, bool)> iconDrawQueue)
		{
			//Determine this pawn's relationships with whomever is highlighted
			Color portraitColor;
			Texture2D portraitBGTexture;
			GetRelations(entry, out portraitColor, out portraitBGTexture, selectedPawnsLovers, selectedPawn);

			//Prepare mood color
			Color moodColor = pawnCache.moodColor;
			if (pawnCache.moodColor.b == 1f || pawnCache.emergency)
			{
				float pulseNum = Pulser.PulseBrightness(0.5f, Pulser.PulseBrightness(0.5f, 0.6f));
				moodColor = new Color(pulseNum, pulseNum, pulseNum) * ResourceBank.colorRed;
			}

			//Draw BG box
			if ((!Settings.moodBackgrounds || pawnCache.moodColor.a != 1f || (relationshipViewerEnabled && selectedPawn != null)) && !pawnCache.emergency) DrawTextureFast(drawArguments, pawnCacheContainer, portraitBGTexture, vector4Zero, portraitColor, currentTransparency);
			else DrawTextureFast(drawArguments, pawnCacheContainer, portraitBackgroundWhite, vector4Zero, moodColor, currentTransparency);

			//Show relationship tooltip
			if (relationshipViewerEnabled && selectedPawn != null)
			{
				if (!selectedPawnAlt) TooltipHandler.TipRegion(pawnCacheContainer, "OwlBar.RelationshipView.Tooltip".Translate());
				else TooltipHandler.TipRegion(pawnCacheContainer, "OwlBar.RelationshipViewAlt.Tooltip".Translate());
			}

			//Draw mood border
			if (pawnCache.moodColor.a == 1f) DrawTextureFast(drawArguments, pawnCacheContainer, BaseContent.WhiteTex, pawnCache.moodBorderWidth, moodColor, currentTransparency);

			//Check for tooltips
			if (pawnCache.grievances != null) TooltipHandler.TipRegion(pawnCacheContainer, pawnCache.grievances);

			//White outline when you mouseover related UI elements, like sidebar alerts
			if (highlight) DrawTextureFast(drawArguments, pawnCacheContainer, BaseContent.WhiteTex, ResourceBank.vector4One * 2f, ResourceBank.colorWhite, currentTransparency);
			
			//The white target retacle when you click on a pawn
			
			if (Current.gameInt.worldInt.renderer.wantedMode == WorldRenderMode.None && 
				pawnCache.dead ? 
				((UIRoot_Play)Current.rootInt.uiRoot).mapUI.selector.selected.Contains(entry.pawn.Corpse) : 
				((UIRoot_Play)Current.rootInt.uiRoot).mapUI.selector.selected.Contains(entry.pawn)
				)
					_vanillaInstance.drawer.DrawSelectionOverlayOnGUI(entry.pawn, pawnCacheContainer);
			
			else if (Current.gameInt.worldInt.renderer.wantedMode != WorldRenderMode.None && 
				entry.pawn.IsCaravanMember() && 
				Find.WorldSelector.IsSelected(entry.pawn.GetCaravan())
				)
					_vanillaInstance.drawer.DrawCaravanSelectionOverlayOnGUI(entry.pawn.GetCaravan(), pawnCacheContainer);

			//Draw portrait
			DrawTextureFast(drawArguments, pawnCache.portraitRect, pawnCache.portrait, vector4Zero, colorWhite, currentTransparency);

			//Prepare icons for drawing
			if (pawnCache.iconCount != 0)
			{
				Rect rect = pawnCache.iconRect;
				for (int i = 0; i < pawnCache.iconCount; ++i, rect.m_XMin += pawnCache.iconGap)
				{
					RimWorld.ColonistBarColonistDrawer.IconDrawCall icon = pawnCache.iconCache[i];
					iconDrawQueue.Add((rect, icon, showWeapon));
					if (icon.tooltip != null) TooltipHandler.TipRegion(rect, icon.tooltip);
				}
			}

			//Draw dead pawn X
			if (pawnCache.dead) DrawTextureFast(drawArguments, pawnCacheContainer, RimWorld.ColonistBarColonistDrawer.DeadColonistTex, vector4Zero, colorWhite, currentTransparency);

			//Draw shadow box behind the label
			DrawTextureFast(drawArguments, pawnCache.labelBGRect, TexUI.GrayTextBG, vector4Zero, colorWhite, currentTransparency);

			//Draw label, use the faster method if possible
			guiContent.m_Text = pawnCache.label;
			if (pawnCache.labelIsColored)
			{
				GUI.color = pawnCache.labelColor;
				guiStyle.Internal_Draw_Injected(ref pawnCache.labelRect, guiContent, false, false, false, false);
				GUI.color = ResourceBank.colorWhite;
			}
			else guiStyle.Internal_Draw_Injected(ref pawnCache.labelRect, guiContent, false, false, false, false);
			
			//Draw draft underline
			if (pawnCache.drafted) DrawTextureFast(drawArguments, new Rect(pawnCache.labelRect.m_XMin, pawnCache.labelBGRect.yMax, pawnCache.labelWidth, 1f), BaseContent.WhiteTex, vector4Zero, colorWhite, currentTransparency);
		}
		void GetRelations(Entry entry, out Color portraitColor, out Texture2D portraitBGTexture, HashSet<int> selectedPawnsLovers, Pawn selectedPawn)
		{
			if (relationshipViewerEnabled && selectedPawn != null && entry.pawn.relations != null && selectedPawn != entry.pawn)
			{
				Pawn from = selectedPawnAlt ? entry.pawn : selectedPawn;
				Pawn to = selectedPawnAlt ? selectedPawn : entry.pawn;

				if (selectedPawnAlt)
				{
					foreach (var lover in entry.pawn.GetLoveRelations(false))
					{
						selectedPawnsLovers.Add(lover.otherPawn.thingIDNumber);
					}
				}
				if (selectedPawnsLovers.Contains(to.thingIDNumber))
				{
					portraitColor = new Color(1f, 0.75f, 0.80f, 1f);
					portraitBGTexture = BaseContent.WhiteTex;
				}
				else
				{
					portraitBGTexture = RimWorld.ColonistBar.BGTex;
					int opinion = from.relations?.OpinionOf(to) ?? 0;
					if (opinion > 20) {
						portraitColor = Color.green;
						portraitBGTexture = BaseContent.GreyTex;
					}
					else if (opinion < -20) {
						float pulseNum = Pulser.PulseBrightness(0.5f, Pulser.PulseBrightness(0.5f, 0.6f));
						portraitColor = new Color(pulseNum, pulseNum, pulseNum) * ResourceBank.colorRed;
						portraitBGTexture = BaseContent.GreyTex;
					}
					else portraitColor = colorWhite;
				}
			}
			else
			{
				portraitColor = colorWhite;
				portraitBGTexture = RimWorld.ColonistBar.BGTex;
			}
		}
		void HandleSelectedPawns(List<object> selectorBuffer, out HashSet<int> selectedPawnsLovers, out Pawn selectedPawn)
		{
			selectedPawn = null; //reset
			selectedPawnsLovers = new HashSet<int>();
			int selectedPawnsCount = 0;
			int length = selectorBuffer.Count;
			for (int i = 0; i < length; ++i)
			{
				if (selectorBuffer[i] is Pawn isPawn)
				{
					if (selectedPawn == null) selectedPawn = isPawn;
					if (++selectedPawnsCount == 2)
					{
						selectedPawn = null;
						break; //Don't care if there's more than 2
					}
				} 
			}
			this.selectedPawn = selectedPawn;

			if (selectedPawnsCount == 1 && selectedPawn.relations != null)
			{
				
				foreach (var lover in selectedPawn.GetLoveRelations(false))
				{
					selectedPawnsLovers.Add(lover.otherPawn.thingIDNumber);
				}
			}
			else if (Settings.relationshipAltMode) relationshipViewerEnabled = false;
		}
		public void AppendIcons(Pawn pawn)
		{
			//Hungry?
			if (Settings.showHunger && !pawn.Dead && (!Settings.showHungerIfDrafted || pawn.Drafted))
			{
				float value = pawn.needs.food?.curLevelInt ?? 1;
				if (value < pawn.needs.food?.PercentageThreshHungry)
				{
					Color color = colorYellow;
					if (value < pawn.needs.food.PercentageThreshUrgentlyHungry) color = colorRed;
					ColonistBarColonistDrawer.tmpIconsToDraw.Add(new ColonistBarColonistDrawer.IconDrawCall(iconHungry, "OwlBar.Icon.Hungry".Translate(), color));
				}
			}
			//Tired?
			if (Settings.showTired && !pawn.Dead && (!Settings.showTiredIfDrafted || pawn.Drafted))
			{
				float value = pawn.needs.rest?.curLevelInt ?? 1;
				if (value < 0.28f)
				{
					Color color = colorYellow;
					if (value < 0.14f) color = colorRed;
					ColonistBarColonistDrawer.tmpIconsToDraw.Add(new ColonistBarColonistDrawer.IconDrawCall(iconTired, "OwlBar.Icon.Tired".Translate(), color));
				}
			}
			//Bleeding?
			if (Settings.showHunger && !pawn.Dead && pawn.health.hediffSet.BleedRateTotal > 0f) 
			{
				ColonistBarColonistDrawer.tmpIconsToDraw.Add(new ColonistBarColonistDrawer.IconDrawCall(iconBleeding, "OwlBar.Icon.Bleeding".Translate(), colorWhite));
			}
		}
		void HandleClicks(Pawn pawn, PawnCache pawnCache, Rect pawnCacheContainer, int reorderableGroup, Event eventCurrent, EventType eventType, out bool reordering)
		{
			int mouseButton = eventCurrent.button;
			if (eventType == EventType.MouseDown && mouseButton == 0 && eventCurrent.clickCount == 2 && Mouse.IsOver(pawnCacheContainer))
			{
				eventCurrent.Use();
				CameraJumper.TryJump(pawn);
			}
			reordering = ReorderableWidget.Reorderable(reorderableGroup, pawnCacheContainer, true, true);
			if (mouseButton == 1 && Mouse.IsOver(pawnCacheContainer))
			{
				if (eventType == EventType.MouseDown) eventCurrent.Use();
				else if (eventType == EventType.MouseUp)
				{
					List<FloatMenuOption> righClickMenu = new List<FloatMenuOption>(HandleRightClick(pawn, pawnCache));
					if (righClickMenu.Count != 0)
					{
						Find.WindowStack.Add(new FloatMenu(righClickMenu));
						eventCurrent.Use();
					}
				}
			}
		}
		IEnumerable<FloatMenuOption> HandleRightClick(Pawn pawn, PawnCache pawnCache)
		{
			if (pawn == null) yield break;
			
			//Add option to make this pawn a group leader
			if (!pawnGroups.groupLeaders.ContainsKey(pawn.thingIDNumber))
			{
				yield return new FloatMenuOption("Make " + pawn.Name + " a group leader", delegate()
				{
					pawnGroups.MakeLeader(pawn.thingIDNumber);
				}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
			}
			//Add option to remove as leader if they are one
			else
			{
				yield return new FloatMenuOption("Remove " + pawn.Name + " as group leader", delegate()
				{
					pawnGroups.RemoveLeader(pawn.thingIDNumber);
				}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
			}

			//Add option to join a group if they are not part of one already
			if (!pawnGroups.groupMembers.ContainsKey(pawn.thingIDNumber))
			{
				foreach (var leader in pawnGroups.groupLeaders)
				{
					if (leader.Key == pawn.thingIDNumber) continue;
					yield return new FloatMenuOption("Join " + PawnsFinder.All_AliveOrDead.FirstOrDefault(x => x.thingIDNumber == leader.Key).Name + "'s group", delegate()
					{
						pawnGroups.JoinGroup(pawn.thingIDNumber, leader.Key, pawnCache.lastWorldGroupID);
					}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
				}
			}
			//Add option to leave a group otherwise
			else
			{
				yield return new FloatMenuOption("Leave " + PawnsFinder.All_AliveOrDead.FirstOrDefault(x => x.thingIDNumber == pawnGroups.groupMembers[pawn.thingIDNumber]).Name + "'s group", delegate()
				{
					pawnGroups.LeaveGroup(pawn.thingIDNumber);
				}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
			}

			yield break;
		}
	}
}