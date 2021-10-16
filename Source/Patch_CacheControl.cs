using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Reflection;
using static OwlBar.Mod_OwlBar;
 
namespace OwlBar
{
    [HarmonyPatch]
    class ResetCacheTriggers
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            //If options are changed..
            yield return AccessTools.Method(typeof(Dialog_Options), nameof(Dialog_Options.DoWindowContents));
            //If colonist is drafted...
            yield return AccessTools.PropertySetter(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted));
            //If colonist portrait is being dragged n' dropped...
            yield return AccessTools.Method(typeof(ColonistBar), nameof(ColonistBar.DrawColonistMouseAttachment));
            //If colonist portrait was just re-ordered...
            yield return AccessTools.Method(typeof(ColonistBar), nameof(ColonistBar.Reorder));
        }

        static void Postfix()
        {
            fastColonistBar?.ResetCache();
        }
    }
}