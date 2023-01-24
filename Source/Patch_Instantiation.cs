using HarmonyLib;
using RimWorld;
 
namespace OwlBar
{
    [HarmonyPatch(typeof(MapInterface), MethodType.Constructor)]
    static class Patch_MapInterface
    {
        static void Postfix()
        {
            new OwlColonistBar();
        }
    }
}