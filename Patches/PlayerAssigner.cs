using System;
using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using InControl;
using UnboundLib;

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
            // Replace `this.playerIDToSet = PlayerManager.instance.players.Count;` with `this.playerIDToSet = PatchUtils.FindAvailablePlayerID();`
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_playerManagerInstance = AccessTools.Field(typeof(PlayerManager), "instance");
            var f_playerManagerPlayers = ExtensionMethods.GetFieldInfo(typeof(PlayerManager), "players");
            var m_playerManagerPlayersCountGet = ExtensionMethods.GetPropertyInfo(typeof(List<Player>), "Count").GetGetMethod();
            var m_FindAvailablePlayerID = ExtensionMethods.GetMethodInfo(typeof(PatchUtils), "FindAvailablePlayerID");

            for (int i = 0; i < list.Count; i++) {
                if (
                    i < list.Count - 2 &&
                    list[i].LoadsField(f_playerManagerInstance) &&
                    list[i + 1].LoadsField(f_playerManagerPlayers) &&
                    list[i + 2].Calls(m_playerManagerPlayersCountGet)
                ) {
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_FindAvailablePlayerID));
                    i += 2;
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
            // Replace `int count = PlayerManager.instance.players.Count;` with `int count = PatchUtils.FindAvailablePlayerID();`
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_playerManagerInstance = AccessTools.Field(typeof(PlayerManager), "instance");
            var f_playerManagerPlayers = ExtensionMethods.GetFieldInfo(typeof(PlayerManager), "players");
            var m_playerManagerPlayersCountGet = ExtensionMethods.GetPropertyInfo(typeof(List<Player>), "Count").GetGetMethod();
            var m_FindAvailablePlayerID = ExtensionMethods.GetMethodInfo(typeof(PatchUtils), "FindAvailablePlayerID");

            for (int i = 0; i < list.Count; i++) {
                if (
                    i < list.Count - 2 &&
                    list[i].LoadsField(f_playerManagerInstance) &&
                    list[i + 1].LoadsField(f_playerManagerPlayers) &&
                    list[i + 2].Calls(m_playerManagerPlayersCountGet)
                ) {
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_FindAvailablePlayerID));
                    i += 2;
                } else {
                    newInstructions.Add(list[i]);
                }
            }

            return newInstructions;
        }
    }
}
