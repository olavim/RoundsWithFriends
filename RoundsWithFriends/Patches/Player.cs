using System.Collections.Generic;
using HarmonyLib;
using UnboundLib;
using UnityEngine;
using System.Reflection.Emit;
using RWF.UI;
using Photon.Pun;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(Player), "Start")]
    class Player_Patch_Start
    {
        static void CallPlayerJoinedOffline(PlayerManager playerManager, Player player)
        {
            // we only want to call PlayerJoined when offline here to prevent double-registering of players online
            // when online, PlayerJoined will be called in Player.AssignCharacter
            if (PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom == null)
            {
                playerManager.PlayerJoined(player);
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m_CallPlayerJoinedOffline = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(Player_Patch_Start), nameof(Player_Patch_Start.CallPlayerJoinedOffline));
            var m_PlayerJoined = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(PlayerManager), nameof(PlayerManager.PlayerJoined));

            foreach (var ins in instructions)
            {
                if (ins.Calls(m_PlayerJoined))
                {
                    yield return new CodeInstruction(OpCodes.Call, m_CallPlayerJoinedOffline);
                }
                else
                {
                    yield return ins;
                }
            }

        }


        static void Postfix(Player __instance)
        {
            if (__instance.data.view.IsMine)
            {
                PlayerSpotlight.AddSpotToPlayer(__instance);
            }
        }
    }
    [HarmonyPatch(typeof(Player), "AssignPlayerID")]
    class Player_Patch_AssignPlayerID
    {
        // postfix to ensure sprite layer is set correctly on remote clients
        static void Postfix(Player __instance) 
        {
            if (__instance?.gameObject?.GetComponentInChildren<SetPlayerSpriteLayer>(true) != null)
            {
                __instance.gameObject.GetComponentInChildren<SetPlayerSpriteLayer>(true).InvokeMethod("Start");
            }

        }
    }
    [HarmonyPatch(typeof(Player), "ReadTeamID")]
    class Player_Patch_ReadTeamID
    {
        static bool Prefix()
        {
            return false;
        }
    }
    [HarmonyPatch(typeof(Player), "ReadPlayerID")]
    class Player_Patch_ReadPlayerID
    {
        static bool Prefix()
        {
            return false;
        }
    }
    [HarmonyPatch(typeof(Player), "AssignTeamID")]
    class Player_Patch_AssignTeamID
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            // Somewhy the AssignTeamID method assigns playerID to teamID when player joins a room the second time
            var f_playerID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var f_teamID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "teamID");

            foreach (var ins in instructions) {
                if (ins.LoadsField(f_playerID)) {
                    // Instead of `this.teamID = playerID`, we obviously want `this.teamID = teamID`
                    ins.operand = f_teamID;
                }

                yield return ins;
            }
        }
    }
}
