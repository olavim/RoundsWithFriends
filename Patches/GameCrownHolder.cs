using HarmonyLib;
using UnityEngine;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(GameCrownHandler), "LateUpdate")]
    class GameCrownHandler_Patch_LateUpdate
    {
        static bool Prefix(int ___currentCrownHolder) {
            return ___currentCrownHolder < PlayerManager.instance.players.Count && PlayerManager.instance.players.Count > 0;
        }
    }
}
