using HarmonyLib;
using RimWorld;
 
namespace OwlBar
{
    [HarmonyPatch(typeof(MapInterface), MethodType.Constructor)]
    static class Patch_MapInterface
    {
        static void Postfix(MapInterface __instance)
        {
            Mod_OwlBar.fastColonistBar = new OwlColonistBar(__instance.colonistBar);
        }
    }
}