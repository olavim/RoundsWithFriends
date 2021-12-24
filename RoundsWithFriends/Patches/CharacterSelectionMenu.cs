using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnboundLib;
using System.Collections;
using TMPro;
using RWF.UI;
using UnboundLib.GameModes;

namespace RWF.Patches
{

    [HarmonyPatch(typeof(CharacterSelectionMenu), "Start")]
    class CharacterSelectionMenu_Patch_Start
    {
        static void Postfix(CharacterSelectionMenu __instance)
        {
            GameObject group = __instance?.gameObject?.transform?.GetChild(0)?.gameObject;
            if (group == null) { return; }

            if (group?.GetComponent<VerticalLayoutGroup>() != null)
            {
                UnityEngine.GameObject.DestroyImmediate(group.GetComponent<VerticalLayoutGroup>());
            }

            CharacterSelectionMenuLayoutGroup grid = group.GetOrAddComponent<CharacterSelectionMenuLayoutGroup>();
            grid.Init();
        }
    }
    [HarmonyPatch(typeof(CharacterSelectionMenu), "PlayerJoined")]
    class CharacterSelectionMenu_Patch_PlayerJoined
    {
        static bool Prefix(CharacterSelectionMenu __instance, Player joinedPlayer)
        {
            if (!__instance.gameObject.activeInHierarchy)
            {
                return false;
            }

            // if the current gamemode doesn't allow duplicate colors, assign this player the first available colorID
            if (GameModeManager.CurrentHandler.Settings.TryGetValue("allowTeams", out object allowTeamsObj) && !(bool) allowTeamsObj)
            {
                joinedPlayer.AssignColorID(Enumerable.Range(0, RWFMod.MaxTeamsHardLimit).Except(PlayerManager.instance.players.Where(p => p.playerID != joinedPlayer.playerID).Select(p => p.colorID())).Distinct().First());
            }

            __instance.transform.GetChild(0).GetComponent<CharacterSelectionMenuLayoutGroup>().PlayerJoined(joinedPlayer);
            __instance.transform.GetChild(0).GetChild(joinedPlayer.playerID).GetComponent<CharacterSelectionInstance>().StartPicking(joinedPlayer);
            return false;
        }
    }
}
