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
            __instance.transform.GetChild(0).GetComponent<CharacterSelectionMenuLayoutGroup>().PlayerJoined(joinedPlayer);
            __instance.transform.GetChild(0).GetChild(joinedPlayer.playerID).GetComponent<CharacterSelectionInstance>().StartPicking(joinedPlayer);
            return false;
        }
    }
}
