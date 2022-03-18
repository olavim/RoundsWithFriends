using HarmonyLib;

namespace RWF.Patches
{
    // patch to extend this to significantly more than 2 players
    [HarmonyPatch(typeof(LowerScreenshakePerPlayer), "Update")]
    class LowerScreenshakePerPlayer_Patch_Update
    {
        private const float defaultForce = 10f;
        static bool Prefix(LowerScreenshakePerPlayer __instance)
        {
            if (PlayerManager.instance.players.Count > 2)
            {
                __instance.shake.shakeforce = LowerScreenshakePerPlayer_Patch_Update.defaultForce / (UnityEngine.Mathf.Pow(2f, UnityEngine.Mathf.Floor(PlayerManager.instance.players.Count/2f)));
            }
            return false;
        }
    }
}
