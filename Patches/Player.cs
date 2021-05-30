using System.Collections.Generic;
using HarmonyLib;
using System.Reflection;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(Player), "AssignTeamID")]
    class Player_Patch_AssignTeamID
    {
        static void Postfix(Player __instance) {
            SetTeamColor.TeamColorThis(__instance.gameObject, PlayerSkinBank.GetPlayerSkinColors(__instance.teamID));
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            // Somewhy the AssignTeamID method assigns playerID to teamID when player joins a room the second time
            var f_playerID = typeof(Player).GetField("playerID", BindingFlags.Instance | BindingFlags.Public);
            var f_teamID = typeof(Player).GetField("teamID", BindingFlags.Instance | BindingFlags.Public);

            foreach (var ins in instructions) {
                if (ins.LoadsField(f_playerID)) {
                    // Instead of `this.teamID = playerID`, we obviously want `this.teamID = teamID`
                    ins.operand = f_teamID;
                }

                yield return ins;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "ReadTeamID")]
    class Player_Patch_ReadTeamID
    {
        static void Postfix(Player __instance) {
            SetTeamColor.TeamColorThis(__instance.gameObject, PlayerSkinBank.GetPlayerSkinColors(__instance.teamID));
        }
    }
}
