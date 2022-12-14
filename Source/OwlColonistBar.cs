using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using static RimWorld.ColonistBar;
using static OwlBar.Mod_OwlBar;
using static OwlBar.FastGUI;
using static OwlBar.ResourceBank;
using Settings = OwlBar.ModSettings_OwlBar;

namespace OwlBar
{
	public class OwlColonistBar
	{
		public PawnCache[] colonistBarCache;
		Event eventCurrent;
		int previousNumOfEntries;
		private List<int> cachedReorderableGroups = new List<int>();
		public OwlColonistBarDrawer fastDrawer;
		
		//The below are used by FastColonistBarDrawer. We'd pass it along directly but we need to pass through the vanilla method first.
		public Map currentMap;
		public PawnCache pawnCache;
		public GUIStyle guiStyle;
		public Pawn selectedPawn;
		public bool selectedPawnAlt;
		public bool relationshipViewerEnabled = true;
		public HashSet<int> selectedPawnsLovers = new HashSet<int>();
		public float labelMaxWidth = 70f;
		public List<object> selectorBuffer;
		public bool worldRender, showWeapon, showGroupFrames;
		public List<int> weaponDrawQueue = new List<int>();
		public List<(Rect, RimWorld.ColonistBarColonistDrawer.IconDrawCall, bool)> iconDrawQueue = new List<(Rect, RimWorld.ColonistBarColonistDrawer.IconDrawCall, bool)>();


		public OwlColonistBar(ColonistBar cb)
		{
			fastDrawer = new OwlColonistBarDrawer();
			vanillaColonistBar = cb; //Static
		}

		public void ResetCache()
		{
			vanillaColonistBar.CheckRecacheEntries();
			colonistBarCache = new PawnCache[vanillaColonistBar.cachedDrawLocs.Count];
		}
		
		static int frames = 120; //May need 1 frame for things to finish initializing
        static int frameLoops = 0;
		public void ColonistBarOnGUI()
		{
			//Prepare
            if (shortDataDirty = ++frames == 121)
            {
                frames = 0;
                vanillaColonistBar.CheckRecacheEntries();
                if (++frameLoops == 20)
                {
                    frameLoops = 0;
                    fastColonistBar.ResetCache();
                }
            }
            if (vanillaColonistBar.Visible) 

			//Begin
			eventCurrent = Event.current;
			if (eventCurrent.type != EventType.Layout)
			{
				var length = vanillaColonistBar.cachedDrawLocs.Count;
				//Quick way to check if the data has gone stale
				if (length != previousNumOfEntries)
				{
					ResetCache();
					previousNumOfEntries = length;
				}
				
				//Variables cached out of the loop
				Text.Font = 0;
				Text.Anchor = TextAnchor.UpperCenter;
				guiStyle = Text.CurFontStyle;
				selectorBuffer = Find.Selector.SelectedObjects;
				currentMap = Find.CurrentMap;
				int groupIDTracker = -1, reorderableGroup = -1, skipped = 0;
				showGroupFrames = false;

				//Handle pawns that are currently clicked
				HandleSelectedPawns();
				
				//Iterate through the portrait entries
				for (int i = 0; i < length; ++i)
				{
					Vector2 cachedDrawLocs;

					//This would happen if a pawn is removed before the cache is regenerated
					try { cachedDrawLocs = vanillaColonistBar.cachedDrawLocs[i]; }
					catch (System.ArgumentOutOfRangeException) { continue;  }

					Entry entry = vanillaColonistBar.cachedEntries[i];
					int group = entry.group;

					//Invoke the group frame. We don't use this but this is for mod support like SoS2
					vanillaColonistBar.drawer.DrawGroupFrame(group);

					if (entry.pawn == null) continue; //Failsafe					

					//Check if they're in a group and if the group is expanded or not
					if (pawnGroups.groupMembers.TryGetValue(entry.pawn.thingIDNumber, out int leaderID) && !pawnGroups.groupLeaders[leaderID])
					{
						++skipped;
						continue;
					}

					//Fetch or rebuild the cache
					Redo:
					pawnCache = colonistBarCache[i];
					if (pawnCache == null)
					{
						colonistBarCache[i] = new PawnCache(entry.pawn, cachedDrawLocs, group, skipped, i);
						pawnCache = colonistBarCache[i];
					}
					else if (group != pawnCache.lastWorldGroupID) //This would happen if there has been a change in the grouping, like a pawn switching maps
					{
						ResetCache();
						goto Redo;
					}
					else if (shortDataDirty)
					{
						pawnCache.FetchShortCache(entry.pawn, true);
						labelMaxWidth = pawnCache.container.width + vanillaColonistBar.SpaceBetweenColonistsHorizontal - 2f;
						worldRender = WorldRendererUtility.WorldRenderedNow;
					}					

					bool flag = groupIDTracker != group;
					groupIDTracker = group;
					if (eventCurrent.type == EventType.Repaint)
					{
						if (flag)
						{
							reorderableGroup = ReorderableWidget.NewGroup(entry.reorderAction, ReorderableDirection.Horizontal, new Rect(0f, 0f, (float)UI.screenWidth,
								(float)UI.screenHeight), vanillaColonistBar.SpaceBetweenColonistsHorizontal, entry.extraDraggedItemOnGUI, true);
						}
						pawnCache.cacheReorderableGroup = reorderableGroup;
					}

					//Ordering
					bool reordering;
					HandleClicks(entry.pawn, pawnCache.cacheReorderableGroup, out reordering);
					
					//Finally draw
					showWeapon = Settings.showWeapons && pawnCache.weapon != null && (!Settings.showWeaponsIfDrafted || pawnCache.drafted);
					if (eventCurrent.type == EventType.Repaint) {
						//First invoke the vanilla drawer. Not actually used, just prompting other mod's harmony patches to pre/postfix.
						vanillaColonistBar.drawer.DrawColonist(pawnCache.container, entry.pawn, entry.map, vanillaColonistBar.colonistsToHighlight.Contains(entry.pawn), reordering);
						//Use our replacement method
						fastColonistBar.fastDrawer.DrawColonistFast(fastColonistBar.pawnCache, pawnCache.container, entry.pawn, entry.map, vanillaColonistBar.colonistsToHighlight.Contains(entry.pawn), reordering);
					}

					//Manage the weapon draw queue
					if (showWeapon) weaponDrawQueue.Add(i);

					//Check if a group icon should be generated
					if (pawnGroups.groupLeaders.TryGetValue(entry.pawn.thingIDNumber, out bool groupExpanded))
					{
						//Draw lines
						if (groupExpanded)
						{
							int memberCount = pawnGroups.groupCounts[entry.pawn.thingIDNumber];
							float lineLength = (memberCount * 72f * 1f) - 23f;
							DrawTextureFast(new Rect(pawnCache.container.x - 1f, pawnCache.container.y - 10f, lineLength, 1f), BaseContent.WhiteTex, vector4Zero, Color.grey);
							DrawTextureFast(new Rect(pawnCache.container.x - 1f, pawnCache.container.yMax, 1f, -58f), BaseContent.WhiteTex, vector4Zero, Color.grey);
							DrawTextureFast(new Rect(pawnCache.container.x - 1f + lineLength, pawnCache.container.yMax, 1f, -58f), BaseContent.WhiteTex, vector4Zero, Color.grey);

							//Draw button
							DrawTextureFast(pawnCache.groupRect,groupCollapse, vector4Zero, colorWhite);
						}
						//Just draw the button
						else DrawTextureFast(pawnCache.groupRect, groupExpand, vector4Zero, colorWhite);
						
						//Click the expand/collapse button?
						if (eventCurrent.type == EventType.MouseDown && eventCurrent.button == 0 && Mouse.IsOver(pawnCache.groupRect))
						{
							pawnGroups.groupLeaders[entry.pawn.thingIDNumber] ^= true;
							SoundDefOf.Click.PlayOneShotOnCamera(null);
							ResetCache();
							return;
						}
					}
				}

				//Handle being able to click group frames
				int num = -1;
				if (showGroupFrames)
				{
					for (int j = 0; j < vanillaColonistBar.cachedDrawLocs.Count; j++)
					{
						ColonistBar.Entry entry2 = vanillaColonistBar.cachedEntries[j];
						bool flag2 = num != entry2.group;
						num = entry2.group;
						if (flag2)
						{
							vanillaColonistBar.drawer.HandleGroupFrameClicks(entry2.group);
						}
					}
				}
				
				//Instead of drawing the weapons in the main loop, we batch them all at once, because changing the matrix each loop is quite expensive
				Matrix4x4 matrix = GUI.matrix;
				length = weaponDrawQueue.Count;
				currentTransparency = 1f;
				for (int i = 0; i < length; ++i)
				{
					PawnCache pawnCache = colonistBarCache[weaponDrawQueue[i]];
					if (pawnCache?.weaponIcon == null) continue; //Bad texture?
					GUIClip.SetMatrix_Injected(ref pawnCache.weaponMatrix);
					DrawTextureFast(pawnCache.weaponRect, pawnCache.weaponIcon, vector4Zero, pawnCache.weapon.DrawColor);
				}
				weaponDrawQueue.Clear();
				GUI.matrix = matrix;

				//This needs to happen here due to draw layering. The icons need to be ontop of the weapons.
				length = iconDrawQueue.Count;
				for (int i = 0; i < length; ++i)
				{
					var iconEntry = iconDrawQueue[i];
					//Transparency
					if (iconEntry.Item3) currentTransparency = 0.7f;
					else currentTransparency = 1f;
					
					var icon = iconEntry.Item2;
					DrawTextureFast(iconEntry.Item1, icon.texture, vector4Zero, (Color)icon.color);
				}
				iconDrawQueue.Clear();
				
				//Reset the GUI controller to defaults when done here
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.UpperLeft;
			}
			if (eventCurrent.type == EventType.Repaint) vanillaColonistBar.colonistsToHighlight.Clear();
		}

		void HandleSelectedPawns()
		{
			selectedPawn = null; //reset
			selectedPawnsLovers.Clear();
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

			if (selectedPawnsCount == 1)
			{
				foreach (var lover in selectedPawn.GetLoveRelations(false))
				{
					selectedPawnsLovers.Add(lover.otherPawn.thingIDNumber);
				}
			}
			else if (Settings.relationshipAltMode) fastColonistBar.relationshipViewerEnabled = false;
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
		
		void HandleClicks(Pawn pawn, int reorderableGroup, out bool reordering)
		{
			int mouseButton = eventCurrent.button;
			if (eventCurrent.type == EventType.MouseDown && mouseButton == 0 && eventCurrent.clickCount == 2 && Mouse.IsOver(pawnCache.container))
			{
				eventCurrent.Use();
				CameraJumper.TryJump(pawn);
			}
			reordering = ReorderableWidget.Reorderable(reorderableGroup, pawnCache.container, true, true);
			if (mouseButton == 1 && Mouse.IsOver(pawnCache.container))
			{
				if (eventCurrent.type == EventType.MouseDown) eventCurrent.Use();
				else if (eventCurrent.type == EventType.MouseUp)
				{
					List<FloatMenuOption> righClickMenu = new List<FloatMenuOption>(HandleRightClick(pawn));
					if (righClickMenu.Count != 0)
					{
						Find.WindowStack.Add(new FloatMenu(righClickMenu));
						eventCurrent.Use();
					}
				}
			}
		}

		IEnumerable<FloatMenuOption> HandleRightClick(Pawn pawn)
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