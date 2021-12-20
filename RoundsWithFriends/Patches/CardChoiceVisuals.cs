using HarmonyLib;
using UnityEngine;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(CardChoiceVisuals), "Show")]
    class CardChoiceVisuals_Patch_Show
    {
        static void Postfix(CardChoiceVisuals __instance, ref GameObject ___currentSkin, int pickerID) {
            if (___currentSkin) {
                GameObject.Destroy(___currentSkin);
            }

            // Show team color instead of individual player color
            var child = __instance.transform.GetChild(0);
            var player = PlayerManager.instance.players[pickerID];
            ___currentSkin = Object.Instantiate(PlayerSkinBank.GetPlayerSkinColors(player.colorID()).gameObject, child.position, Quaternion.identity, child);
            ___currentSkin.GetComponentInChildren<ParticleSystem>().Play();
        }
    }
}
