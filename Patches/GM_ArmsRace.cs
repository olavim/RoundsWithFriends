using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnboundLib;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_artInstance = AccessTools.Field(typeof(ArtHandler), "instance");

            for (int i = 0; i < list.Count; i++) {
                if (list[i].LoadsField(f_artInstance)) {
                    i += 1;
                } else {
                    newInstructions.Add(list[i]);
                }
            }

            return newInstructions;
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "DoStartGame")]
    class GM_ArmsRace_Patch_DoStartGame
    {
        static void Prefix(GM_ArmsRace __instance) {
            // Rebuild the top right player card visual to match the number of players
            CardBarHandler.instance.Rebuild();
            UIHandler.instance.InvokeMethod("SetNumberOfRounds", __instance.roundsToWinGame);
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

        static void ResetPoints()
        {
            GM_ArmsRace.instance.p1Points = 0;
            GM_ArmsRace.instance.p2Points = 0;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_cardChoiceInstance = AccessTools.Field(typeof(CardChoice), "instance");
            var f_cardChoiceVisualsInstance = AccessTools.Field(typeof(CardChoiceVisuals), "instance");
            var m_cardChoiceVisualsShow = ExtensionMethods.GetMethodInfo(typeof(CardChoiceVisuals), "Show");
            var m_getPlayerIndex = ExtensionMethods.GetMethodInfo(typeof(GM_ArmsRace_Patch_RoundTransition), "GetPlayerIndex");
            var m_winSequence = ExtensionMethods.GetMethodInfo(typeof(PointVisualizer), "DoWinSequence");
            var m_startCoroutine = ExtensionMethods.GetMethodInfo(typeof(MonoBehaviour), "StartCoroutine", new Type[] { typeof(IEnumerator) });

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
                } else if (
                    list[i].Calls(m_winSequence) &&
                    list[i + 1].Calls(m_startCoroutine) &&
                    list[i + 2].opcode == OpCodes.Pop
                ) {
                    newInstructions.AddRange(list.GetRange(i, 3));
                    newInstructions.Add(CodeInstruction.Call(typeof(GM_ArmsRace_Patch_RoundTransition), "ResetPoints"));
                    i += 2;
                } else {
                    newInstructions.Add(list[i]);
                }
            }

            return newInstructions;
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "RoundOver")]
    class GM_ArmsRace_Patch_RoundOver
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
        {
            // Do not set p1Points and p2Points to zero in RoundOver. We want to do it only after we've displayed them in RoundTransition.
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_p1Points = ExtensionMethods.GetFieldInfo(typeof(GM_ArmsRace), "p1Points");
            var f_p2Points = ExtensionMethods.GetFieldInfo(typeof(GM_ArmsRace), "p2Points");

            for (int i = 0; i < list.Count; i++)
            {
                if (i < list.Count - 2 && (list[i + 2].StoresField(f_p1Points) || list[i + 2].StoresField(f_p2Points)))
                {
                    i += 2;
                    continue;
                }

                newInstructions.Add(list[i]);
            }

            return newInstructions;
        }
    }
}
