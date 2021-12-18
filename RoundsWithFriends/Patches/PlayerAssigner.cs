using System;
using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using InControl;
using UnboundLib;
using UnityEngine;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(PlayerAssigner), "LateUpdate")]
    class PlayerAssigner_Patch_LateUpdate
    {
        static bool Prefix() {
            // When playing in a private match, we want to pretty much ignore this function since we handle player joins in PrivateRoomHandler
            return PhotonNetwork.OfflineMode ||
                NetworkConnectionHandler.instance.IsSearchingQuickMatch() ||
                NetworkConnectionHandler.instance.IsSearchingTwitch();
        }

        static void Postfix(PlayerAssigner __instance, bool ___playersCanJoin)
        {
            if (RWFMod.DEBUG)
            {
                if (!___playersCanJoin)
                {
                    return;
                }
                if (__instance.players.Count >= __instance.maxPlayers)
                {
                    return;
                }
                if (DevConsole.isTyping)
                {
                    return;
                }
                if (Input.GetKey(KeyCode.LeftBracket))
                {

                    __instance.StartCoroutine(__instance.CreatePlayer(null, false));
                    
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerAssigner), "JoinButtonWasPressedOnDevice")]
    class PlayerAssigner_Patch_JoinButtonWasPressedOnDevice
    {
        static bool Prefix(ref bool __result, InputDevice inputDevice) {
            if (inputDevice.Action2.WasPressed) {
                __result = false;
                return false;
            }
            return true;
        }
    }

    // The RemovePlayer method is declared but unimplemented, so we'll implement it here
    [HarmonyPatch(typeof(PlayerAssigner), "RemovePlayer")]
    class PlayerAssigner_Patch_RemovePlayer
    {
        static void Prefix(PlayerAssigner __instance, CharacterData player) {
            var playingOnline = !PhotonNetwork.OfflineMode && player.isPlaying;

            if (PhotonNetwork.OfflineMode || playingOnline) {
                /* Things get pretty complicated when playing online. The game seems to end forcefully on disconnect so we'll ignore this case for now.
                 * We also skip this when playing offline so that the controller can just be reconnected, though I'm not sure if that works.
                 */
                return;
            }

            __instance.players.Remove(player);
            PlayerManager.instance.RemovePlayer(player.player);

            if (player.view.OwnerActorNr == PhotonNetwork.LocalPlayer.ActorNumber) {
                PhotonNetwork.Destroy(player.view);
                __instance.SetFieldValue("hasCreatedLocalPlayer", false);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerAssigner), "ClearPlayers")]
    class PlayerAssigner_Patch_RemovePlayers
    {
        static void Postfix(PlayerAssigner __instance) {
            __instance.SetFieldValue("hasCreatedLocalPlayer", false);
        }
    }

    // The RemovePlayer method is declared but unimplemented, so we'll implement it here
    [HarmonyPatch(typeof(PlayerAssigner), "RPC_ReturnPlayerAndTeamID")]
    class PlayerAssigner_Patch_RPC_ReturnPlayerAndTeamID
    {
        static void Prefix(ref int teamId, ref int playerID) {
            // This method is called wrong with (playerID, teamId) instead of (teamId, playerID) like the method signature...
            int temp = teamId;
            teamId = playerID;
            playerID = temp;
        }
    }

    [HarmonyPatch]
    class PlayerAssigner_Patch_CreatePlayer
    {
        static MethodBase TargetMethod() {
            var nestedTypes = typeof(PlayerAssigner).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic);
            Type nestedCreatePlayerType = null;

            foreach (var type in nestedTypes) {
                if (type.Name.Contains("CreatePlayer")) {
                    nestedCreatePlayerType = type;
                }
            }

            return AccessTools.Method(nestedCreatePlayerType, "MoveNext");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            // Replace `this.playerIDToSet = PlayerManager.instance.players.Count;` with `this.playerIDToSet = PatchUtils.NextPlayerID();`
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_playerManagerInstance = AccessTools.Field(typeof(PlayerManager), "instance");
            var f_playerIDToSet = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(PlayerAssigner), "playerIDToSet");
            var f_teamIDToSet = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(PlayerAssigner), "teamIDToSet");

            var m_NextPlayerID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(PatchUtils), "NextPlayerID");
            var m_NextTeamID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(PatchUtils), "NextTeamID");

            for (int i = 0; i < list.Count; i++) {
                if (list[i].LoadsField(f_playerManagerInstance) && list[i + 3].StoresField(f_playerIDToSet)) {
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_NextPlayerID));
                    newInstructions.Add(new CodeInstruction(OpCodes.Stfld, f_playerIDToSet));
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_1));
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_NextTeamID));
                    newInstructions.Add(new CodeInstruction(OpCodes.Stfld, f_teamIDToSet));

                    while (!list[i].StoresField(f_teamIDToSet)) {
                        i++;
                    }
                } else {
                    newInstructions.Add(list[i]);
                }
            }

            return newInstructions;
        }
    }

    [HarmonyPatch(typeof(PlayerAssigner), "RPCM_RequestTeamAndPlayerID")]
    class PlayerAssigner_Patch_RPCM_RequestTeamAndPlayerID
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            /* Replace
             *   int count = PlayerManager.instance.players.Count;
             *   int num = (count % 2 == 0) ? 0 : 1;
             * with
             *   int count = PatchUtils.NextPlayerID();
             *   int count = PatchUtils.NextTeamID();
             */
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var m_NextPlayerID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(PatchUtils), "NextPlayerID");
            var m_NextTeamID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(PatchUtils), "NextTeamID");

            int rangeStart;
            for (rangeStart = 0; rangeStart < list.Count; rangeStart++) {
                if (list[rangeStart].opcode == OpCodes.Stloc_1) {
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_NextPlayerID));
                    newInstructions.Add(new CodeInstruction(OpCodes.Stloc_0));
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_NextTeamID));
                    newInstructions.Add(new CodeInstruction(OpCodes.Stloc_1));
                    rangeStart++;
                    break;
                }
            }


            for (int i = rangeStart; i < list.Count; i++) {
                newInstructions.Add(list[i]);
            }

            return newInstructions;
        }
    }
}
