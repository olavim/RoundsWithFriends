using System;
using System.Collections.Generic;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

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
            if (PlayerManager.instance.players.Count == 2) {
                return true;
            }

            var m_ResetMatch = typeof(GM_ArmsRace).GetMethod("ResetMatch", BindingFlags.NonPublic | BindingFlags.Instance);
            m_ResetMatch.Invoke(__instance, null);

            // Reset crown
            var crown = __instance.gameObject.GetComponentInChildren<GameCrownHandler>();
            crown.transform.position = new Vector3(-1000, -1000, 0);
            var f_currentCrownHolder = typeof(GameCrownHandler).GetField("currentCrownHolder", BindingFlags.NonPublic | BindingFlags.Instance);
            f_currentCrownHolder.SetValue(crown, -1);

            // Reset players and map
            PlayerManager.instance.RemovePlayers();
            GameManager.instance.isPlaying = false;
            UIHandler.instance.HideRoundCounterSmall();
            MapManager.instance.UnloadScene(MapManager.instance.currentMap.Scene);
            MapManager.instance.currentMap = null;

            // Open lobby
            MainMenuHandler.instance.Open();
            PrivateRoomHandler.instance.Open();
            return false;
        }
    }

    [HarmonyPatch]
    class GM_ArmsRace_Patch_RoundTransition
    {
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
            var m_cardChoiceVisualsShow = typeof(CardChoiceVisuals).GetMethod("Show", BindingFlags.Public | BindingFlags.Instance);
            var f_iteratorIndex = GetNestedRoundTransitionType().GetField("<i>5__3", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = 0; i < list.Count; i++) {
                if (
                    i < list.Count - 1 &&
                    list[i].opcode == OpCodes.Ldarg_0 &&
                    list[i + 1].LoadsField(f_cardChoiceInstance)
                ) {
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldsfld, f_cardChoiceVisualsInstance));
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldfld, f_iteratorIndex));
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
