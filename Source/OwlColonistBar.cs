using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
using static RimWorld.ColonistBar;
using static OwlBar.Mod_OwlBar;
using static OwlBar.FastGUI;
using Settings = OwlBar.ModSettings_OwlBar;

namespace OwlBar
{
	#if DEBUG
	[HotSwap.HotSwappable]
	#endif
	public class OwlColonistBar
	{
		public PawnCache[] colonistBarCache;
		Event eventCurrent;
		int previousNumOfEntries;
		public OwlColonistBarDrawer fastDrawer;
		
		//The below are used by FastColonistBarDrawer. We'd pass it along directly but we need to pass through the vanilla method first.
		public Map currentMap;
		public PawnCache pawnCache;
		public GUIStyle guiStyle;
		public Pawn selectedPawn;
		public HashSet<int> selectedPawnsLovers;
		public float labelMaxWidth = 70f;
		public List<object> selectorBuffer;
		public bool worldRender, showWeapon;
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
		
		public void ColonistBarOnGUI()
		{
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
				guiStyle = Text.CurFontStyle;
				selectorBuffer = Find.Selector.SelectedObjects;
				currentMap = Find.CurrentMap;
				int groupIDTracker = -1;
				int reorderableGroup = -1;
				int skipped = 0;

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
					if (entry.pawn == null) continue; //Failsafe
					int group = entry.group;

					//Check if they're in a group and if the group is expanded or not
					int leaderID = -1;
					if (pawnGroups.groupMembers.TryGetValue(entry.pawn.thingIDNumber, out leaderID) && !pawnGroups.groupLeaders[leaderID])
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
					else if (group != pawnCache.lastGroupID) //This would happen if there has been a change in the grouping, like a pawn switching maps
					{
						ResetCache();
						goto Redo;
					}
					else if (shortDataDirty)
					{
						pawnCache.FetchShortCache(entry.pawn);
						labelMaxWidth = pawnCache.container.width + vanillaColonistBar.SpaceBetweenColonistsHorizontal - 2f;
						worldRender = WorldRendererUtility.WorldRenderedNow;
					}					

					//Determine which group
					if (groupIDTracker != group) reorderableGroup = ReorderableWidget.NewGroup_NewTemp(entry.reorderAction, ReorderableDirection.Horizontal, vanillaColonistBar.SpaceBetweenColonistsHorizontal, entry.extraDraggedItemOnGUI, true);
					groupIDTracker = group;

					//Ordering
					bool reordering;
					HandleClicks(entry.pawn, reorderableGroup, out reordering);
					
					//Finally draw
					showWeapon = Settings.showWeapons && pawnCache.weapon != null && (!Settings.showWeaponsIfDrafted || pawnCache.drafted);
					if (eventCurrent.type == EventType.Repaint) vanillaColonistBar.drawer.DrawColonist(pawnCache.container, entry.pawn, entry.map, vanillaColonistBar.colonistsToHighlight.Contains(entry.pawn), reordering);

					
					//Manage the weapon draw queue
					if (showWeapon) weaponDrawQueue.Add(i);

					//Check if a group icon should be generated
					bool groupExpanded;
					if (pawnGroups.groupLeaders.TryGetValue(entry.pawn.thingIDNumber, out groupExpanded))
					{
						Rect groupRect = pawnCache.container;
						groupRect.x += pawnCache.container.width;
						groupRect.width = pawnCache.container.width / 5f;
						DrawTextureFast(groupRect, groupExpanded ? ResourceBank.Group_Collapse : ResourceBank.Group_Expand, ResourceBank.vector4Zero, ResourceBank.colorWhite);
						if (eventCurrent.type == EventType.MouseDown && eventCurrent.button == 0 && Mouse.IsOver(groupRect))
						{
							pawnGroups.groupLeaders[entry.pawn.thingIDNumber] ^= true;
							SoundDefOf.Click.PlayOneShotOnCamera(null);
							ResetCache();
							return;
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
					GUIClip.SetMatrix_Injected(ref pawnCache.weaponMatrix);
					DrawTextureFast(pawnCache.weaponRect, pawnCache.weaponIcon, ResourceBank.vector4Zero, pawnCache.weapon.DrawColor);
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
					DrawTextureFast(iconEntry.Item1, icon.texture, ResourceBank.vector4Zero, (Color)icon.color);
				}
				iconDrawQueue.Clear();
				
				//Reset the GUI controller to defaults when done here
				Text.Font = GameFont.Small;

				/*
				if (vanillaColonistBar.ShowGroupFrames)
				{
					groupIDTracker = -1;
					for (int j = 0; j < length; j++)
					{
						Entry entry2 = vanillaColonistBar.cachedEntries[j];
						bool flag2 = groupIDTracker != entry2.group;
						groupIDTracker = entry2.group;
						if (flag2) vanillaColonistBar.drawer.HandleGroupFrameClicks(entry2.group);
					}
				}
				*/
			}
			if (eventCurrent.type == EventType.Repaint) vanillaColonistBar.colonistsToHighlight.Clear();
		}

		void HandleSelectedPawns()
		{
			selectedPawn = null; //reset
			int selectedPawnsCount = 0;
			for (int i = 0; i < selectorBuffer.Count; ++i)
			{
				if (selectorBuffer[i].GetType() == typeof(Pawn)) ++selectedPawnsCount;
			}

			if (selectedPawnsCount == 1)
			{
				selectedPawn = selectorBuffer.FirstOrDefault(x => x.GetType() == typeof(Pawn)) as Pawn;
				if (selectedPawn.relations != null) selectedPawnsLovers = selectedPawn.GetLoveRelations(false).Select(x => x.otherPawn.thingIDNumber).ToHashSet();
				else selectedPawn = null;
			}
		}

		public void AppendIcons(Pawn pawn)
		{
			//Hungry?
			if (Settings.showHunger && !pawn.Dead && (!Settings.showHungerIfDrafted || pawn.Drafted))
			{
				float value = pawn.needs.food?.curLevelInt ?? 1;
				if (value < pawn.needs.food.PercentageThreshHungry)
				{
					Color color = ResourceBank.colorYellow;
					if (value < pawn.needs.food.PercentageThreshUrgentlyHungry) color = ResourceBank.colorRed;
					ColonistBarColonistDrawer.tmpIconsToDraw.Add(new ColonistBarColonistDrawer.IconDrawCall(ResourceBank.Icon_Hungry, "OCB_IconHungry".Translate(), color));
				}
			}
			//Tired?
			if (Settings.showTired && !pawn.Dead && (!Settings.showTiredIfDrafted || pawn.Drafted))
			{
				float value = pawn.needs.rest?.curLevelInt ?? 1;
				if (value < 0.28f)
				{
					Color color = ResourceBank.colorYellow;
					if (value < 0.14f) color = ResourceBank.colorRed;
					ColonistBarColonistDrawer.tmpIconsToDraw.Add(new ColonistBarColonistDrawer.IconDrawCall(ResourceBank.Icon_Tired, "OCB_IconTired".Translate(), color));
				}
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
			reordering = ReorderableWidget.Reorderable(reorderableGroup, pawnCache.container, true);
			if (mouseButton == 1 && Mouse.IsOver(pawnCache.container))
			{
				if (eventCurrent.type == EventType.MouseDown) eventCurrent.Use();
				else if (eventCurrent.type == EventType.MouseUp)
				{
					List<FloatMenuOption> righClickMenu = HandleRightClick(pawn).ToList<FloatMenuOption>();
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
						pawnGroups.JoinGroup(pawn.thingIDNumber, leader.Key, pawnCache.lastGroupID);
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