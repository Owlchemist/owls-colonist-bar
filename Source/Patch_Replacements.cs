using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using Settings = OwlBar.ModSettings_OwlBar;
 
namespace OwlBar
{
    //Do not go to the vanilla bar, use ours instead. This may be destructive and something to revisit later if it's too much of a compatibiltiy problem
    [HarmonyPatch(typeof(ColonistBar), nameof(ColonistBar.ColonistBarOnGUI))]
    static class Replace_ColonistBarOnGUI
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(OwlColonistBar), nameof(OwlColonistBar._instance)));
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Callvirt, typeof(OwlColonistBar).GetMethod(nameof(OwlColonistBar.ColonistBarOnGUI)));
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }

    //Instead of going directly to our own drawer, we pass through the vanilla drawer first to allow postfix patches from other mods to do their thing, like Pawn Badges
    [HarmonyPatch(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawColonist))]
    [HarmonyPriority(Priority.Last)]
    static class Patch_DrawColonist
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }

    //We don't draw the frame but we still need to invoke it for mod's that use it like SoS2
    [HarmonyPatch(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawGroupFrame))]
    [HarmonyPriority(Priority.Last)]
    static class Patch_DrawGroupFrame
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }

    //This is responsible for handling pawn scale so the portraits don't get smushed together
    [HarmonyPatch]
	static class Patch_FindBestScale
	{
		public static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(ColonistBarDrawLocsFinder), nameof(ColonistBarDrawLocsFinder.FindBestScale), new Type[] { typeof(bool).MakeByRefType(), typeof(int).MakeByRefType(), typeof(int) });
		}
		static bool Prefix(ColonistBarDrawLocsFinder __instance, ref int maxPerGlobalRow, ref bool onlyOneRow, int groupsCount, ref float __result)
		{
			float scaleOverride = Settings.entryScale;
			maxPerGlobalRow = Settings.entriesPerRow;
			List<ColonistBar.Entry> entries = __instance.ColonistBar.Entries;
			int failsafe = 100;
			while (--failsafe != 00)
			{
				onlyOneRow = true;
				if (__instance.TryDistributeHorizontalSlotsBetweenGroups(maxPerGlobalRow, groupsCount))
				{
					bool finished = true;
					int group = -1;
					var length = entries.Count;
					for (int i = 0; i < length; i++)
					{
						var entry = entries[i];
						if (group != entry.group)
						{
							group = entry.group;
							int groupRowCount = (int)System.Math.Ceiling((float)__instance.entriesInGroup[entry.group] / (float)__instance.horizontalSlotsPerGroup[entry.group]);
							onlyOneRow = groupRowCount == 1;
							if (groupRowCount > Settings.maxRows)
							{
								finished = false;
								break;
							}
						}
					}
					if (finished) break;
				}
				scaleOverride *= 0.95f;
				float X = scaleOverride * (ColonistBar.BaseSize.x + 24f);
				float totalX = ColonistBarDrawLocsFinder.MaxColonistBarWidth - (float)(groupsCount - 1) * scaleOverride * 25f;
				maxPerGlobalRow = (int)System.Math.Floor(totalX / X);
			}
			if (failsafe == 0) Log.Error("[Owl's Colonist Bar] Failed to calculate scale.");
			__result = scaleOverride;
			return false;
		}
	}

    //This changes the vanilla portait icon code to only fetch the icons into its list, but removes the actual drawing portion.
    [HarmonyPatch(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawIcons))]
    static class Patch_DrawIcons
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            bool ran = false;
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; ++i)
            {
                if (codes[i].opcode == OpCodes.Call && codes[i].operand as MethodInfo == AccessTools.Method(typeof(GUI), "set_color"))
                {
                    codes.RemoveRange(i - 9, 20);
                    ran = true;
                    break;
                }
            }
            
            if (!ran) Log.Warning("[Owl's Colonist Bar] Transpiler could not find target. There may be a mod conflict, or RimWorld updated?");
            return codes;
        }

        //Now we add our own icons to this list
        static void Postfix(Pawn colonist)
        {
            OwlColonistBar._instance.AppendIcons(colonist);
        }
    }

    //This is how click selects target a pawn in the bar. Vanilla code relies on position -> entry but since we fudge the positioning we need to take it over.
    [HarmonyPatch(typeof(ColonistBar), nameof(ColonistBar.ColonistOrCorpseAt))]
    static class Replace_ColonistOrCorpseAt
    {
        static bool Prefix(Vector2 pos, ref Thing __result) //Needs to be a ref due to changing the pointer
        {
            var mousePos = Event.current.mousePosition;
            if (!Mouse.IsInputBlockedNow)
            {
                foreach (var pawnCache in OwlColonistBar._instance.colonistBarCache)
                {
                    if (pawnCache == null) continue;
                    if (pawnCache.container.Contains(mousePos))
                    {
                        Pawn pawn = pawnCache.Pawn;

                        if (OwlColonistBar._instance._selectedPawn == pawn) 
                        {
                            if (!Settings.relationshipAltMode || OwlColonistBar._instance.relationshipViewerEnabled) OwlColonistBar._instance.selectedPawnAlt ^= true;
                            OwlColonistBar._instance.relationshipViewerEnabled = true;
                        }

                        if (pawn != null && pawn.Dead && pawn.Corpse != null && pawn.Corpse.SpawnedOrAnyParentSpawned) __result = pawn.Corpse;
                        else __result = pawn;
                        break;
                    }
                }
            }
            return false;
        }
    }
}