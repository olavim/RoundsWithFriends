using HarmonyLib;
using UnityEngine;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(HealthHandler), "Revive")]
    class HealthHandler_Patch_Revive
    {
        static void Postfix(SpriteRenderer ___hpSprite, Player ___player) {
            // For reasons unknown, "Health" is actually player color
            ___hpSprite.color = PlayerSkinBank.GetPlayerSkinColors(___player.teamID).color;
        }
    }
}
