using HarmonyLib;
using UnboundLib;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(PlayerInRangeTrigger), "Update")]
    class PlayerInRangeTrigger_Patch_Update
    {
        static bool Prefix(ref bool ___inRange, Player ___ownPlayer) {
            if (!(bool)___ownPlayer.data.playerVel.GetFieldValue("simulated") || RWFMod.instance.GameMode.IsRoundStartCeaseFire) {
                ___inRange = false;
                return false;
            }

            return true;
        }
    }
}
