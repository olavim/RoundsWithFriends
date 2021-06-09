using HarmonyLib;
using UnboundLib;

namespace RWF.Patches.Cards
{
    [HarmonyPatch(typeof(Chase), "Update")]
    class Chase_Patch_Update
    {
        static bool Prefix(Player ___player) {
            return (bool) ___player.data.playerVel.GetFieldValue("simulated") && !RWFMod.instance.gameSettings.GameMode.IsCeaseFire;
        }
    }
}
