using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using static OwlBar.Mod_OwlBar;
 
namespace OwlBar
{
    //Do not go to the vanilla bar, use ours instead. This may be destructive and something to revisit later if it's too much of a compatibiltiy problem
    [HarmonyPatch(typeof(ColonistBar), nameof(ColonistBar.ColonistBarOnGUI))]
    static class Patch_ColonistBarOnGUI
    {
        static int frames = 119; //May need 1 frame for things to finish initializing
        static int frameLoops = 0;
        static bool Prefix(ColonistBar __instance)
        {
            //Clunky, but technically faster than modulos
            if (shortDataDirty = ++frames == 120)
            {
                frames = 0;
                __instance.CheckRecacheEntries();
                if (++frameLoops == 20)
                {
                    frameLoops = 0;
                    fastColonistBar.ResetCache();
                }
            }
            if (__instance.Visible) fastColonistBar.ColonistBarOnGUI();
            return false;
        }
    }

    //Instead of going directly to our own drawer, we pass through the vanilla drawer first to allow postfix patches from other mods to do their thing, like Pawn Badges
    [HarmonyPatch(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawColonist))]
    static class Patch_DrawColonist
    {
        static bool Prefix(Rect rect, Pawn colonist, Map pawnMap, bool highlight, bool reordering)
        {
            fastColonistBar.fastDrawer.DrawColonistFast(fastColonistBar.pawnCache, rect, colonist, pawnMap, highlight, reordering, vanillaColonistBar.cachedScale);
            return false;
        }
    }

    //This changes the vanilla portait icon code to only fetch the icons into its list, but removes the actual drawing portion.
    [HarmonyPatch(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawIcons))]
    static class Patch_PathFinder
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
            
            if (!ran) Log.Warning("[Smart Colonist Bar] Transpiler could not find target. There may be a mod conflict, or RimWorld updated?");
            return codes.AsEnumerable();
        }

        //Now we add our own icons to this list
        static void Postfix(Pawn colonist)
        {
            fastColonistBar.AppendIcons(colonist);
        }
    }

    //This is how click selects target a pawn in the bar. Vanilla code relies on position -> entry but since we fudge the positioning we need to take it over.
    [HarmonyPatch(typeof(ColonistBar), nameof(ColonistBar.ColonistOrCorpseAt))]
    static class Patch_ColonistOrCorpseAt
    {
        static bool Prefix(Vector2 pos, ref Thing __result)
        {
            var mousePos = Event.current.mousePosition;
            if (!Mouse.IsInputBlockedNow)
            {
                foreach (var pawnCache in fastColonistBar.colonistBarCache)
                {
                    if (pawnCache == null) continue;
                    if (pawnCache.container.Contains(mousePos))
                    {
                        Pawn pawn = pawnCache.Pawn;

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