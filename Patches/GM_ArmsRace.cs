using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using System.Reflection.Emit;
using UnityEngine.SceneManagement;
using Photon.Pun;
using UnboundLib;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(GM_ArmsRace), "Start")]
    class GM_ArmsRace_Patch_Start
    {
        static void Postfix(ref int ___playersNeededToStart) {
            ___playersNeededToStart = RWFMod.instance.MinPlayers;
            PlayerAssigner.instance.maxPlayers = RWFMod.instance.MaxPlayers;
            UIHandler.instance.HideJoinGameText();
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "StartGame")]
    class GM_ArmsRace_Patch_StartGame
    {
        static void Postfix() {
            // Rebuild the top right player card visual to match the number of players
            CardBarHandler.instance.Rebuild();
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "PlayerJoined")]
    class GM_ArmsRace_Patch_PlayerJoined
    {
        static bool Prefix(Player player) {
            // When playing in a private match, we want to pretty much ignore this function since we handle player joins in PrivateRoomHandler
            return NetworkConnectionHandler.instance.IsSearchingQuickMatch() || NetworkConnectionHandler.instance.IsSearchingTwitch();
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "GameOverRematch")]
    class GM_ArmsRace_Patch_GameOverRematch
    {
        // Fixing rematch for >2 players is possible, but going back to lobby is enough for now
        static bool Prefix(GM_ArmsRace __instance) {
            if (PhotonNetwork.OfflineMode) {
                return true;
            }

            // Enable rematch if playing against a single unmodded player
            var allModded = PhotonNetwork.CurrentRoom.Players.Values.ToList().All(p => p.IsModded());
            if (!allModded && PlayerManager.instance.players.Count == 2) {
                return true;
            }

            /* The master client destroys all networked player objects after each game. Otherwise, if someone
             * joins a lobby after a game has been played, all the previously created player objects will be
             * created for the new client as well, which causes a host of problems.
             */
            if (PhotonNetwork.IsMasterClient) {
                foreach (var player in PhotonNetwork.CurrentRoom.Players.Values.ToList()) {
                    PhotonNetwork.DestroyPlayerObjects(player);
                }
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return false;
        }
    }

    [HarmonyPatch]
    class GM_ArmsRace_Patch_RoundTransition
    {
        public static int GetPlayerIndex(Player player) {
            return PlayerManager.instance.players.FindIndex(p => p.playerID == player.playerID);
        }

        static Type GetNestedRoundTransitionType() {
            var nestedTypes = typeof(GM_ArmsRace).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic);
            Type nestedType = null;

            foreach (var type in nestedTypes) {
                if (type.Name.Contains("RoundTransition")) {
                    nestedType = type;
                    break;
                }
            }

            return nestedType;
        }

        static MethodBase TargetMethod() {
            return AccessTools.Method(GetNestedRoundTransitionType(), "MoveNext");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_cardChoiceInstance = AccessTools.Field(typeof(CardChoice), "instance");
            var f_cardChoiceVisualsInstance = AccessTools.Field(typeof(CardChoiceVisuals), "instance");
            var m_cardChoiceVisualsShow = ExtensionMethods.GetMethodInfo(typeof(CardChoiceVisuals), "Show");
            var m_getPlayerIndex = ExtensionMethods.GetMethodInfo(typeof(GM_ArmsRace_Patch_RoundTransition), "GetPlayerIndex");

            var f_iteratorIndex = ExtensionMethods.GetFieldInfo(GetNestedRoundTransitionType(), "<i>5__3");
            var f_players = ExtensionMethods.GetFieldInfo(GetNestedRoundTransitionType(), "<players>5__2");

            for (int i = 0; i < list.Count; i++) {
                if (
                    i < list.Count - 1 &&
                    list[i].opcode == OpCodes.Ldarg_0 &&
                    list[i + 1].LoadsField(f_cardChoiceInstance)
                ) {
                    // Adds `CardChoiceVisuals.instance.Show(GetPlayerIndex(players[i]), true)` before the DoPick call
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldsfld, f_cardChoiceVisualsInstance));
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldfld, f_players));
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldfld, f_iteratorIndex));
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldelem_Ref));
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_getPlayerIndex));
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
                    newInstructions.Add(new CodeInstruction(OpCodes.Callvirt, m_cardChoiceVisualsShow));
                    newInstructions.Add(list[i]);
                } else {
                    newInstructions.Add(list[i]);
                }
            }

            return newInstructions;
        }
    }
}
