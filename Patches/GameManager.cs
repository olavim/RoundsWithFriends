using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(GameManager), "Start")]
    class GameManager_Patch_Await
    {
        static void Postfix() {
            RWFMod.instance.InjectUIElements();
        }
    }
}
