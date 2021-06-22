using HarmonyLib;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(HoldingObject), "Update")]
    class HoldingObject_Patch_Update
    {
        static bool Prefix(HoldingObject __instance) {
            // HoldingObject throws some errors when a player is removed because it doesn't check if the holder still exists
            return __instance.holder != null;
        }
    }
}
