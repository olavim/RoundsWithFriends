using HarmonyLib;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(Holding), "Start")]
    class Holding_Patch_Start
    {
        static void Postfix(Holdable ___holdable, Player ___player) {
            if (___holdable) {
                ___holdable.SetTeamColors(PlayerSkinBank.GetPlayerSkinColors(___player.teamID), ___player);
            }
        }
    }
}
