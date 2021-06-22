using System.Collections.Generic;
using HarmonyLib;
using UnboundLib;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(Player), "Start")]
    class Player_Patch_Start
    {
        static void Postfix(Player __instance) {
            if (__instance.data.view.IsMine) {

            }
        }
    }

    [HarmonyPatch(typeof(Player), "AssignTeamID")]
    class Player_Patch_AssignTeamID
    {
        static void Postfix(Player __instance) {
            SetTeamColor.TeamColorThis(__instance.gameObject, PlayerSkinBank.GetPlayerSkinColors(__instance.teamID));
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            // Somewhy the AssignTeamID method assigns playerID to teamID when player joins a room the second time
            var f_playerID = ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var f_teamID = ExtensionMethods.GetFieldInfo(typeof(Player), "teamID");

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
