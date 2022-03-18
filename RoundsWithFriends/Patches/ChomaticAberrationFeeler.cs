using HarmonyLib;

namespace RWF.Patches
{
    // patch to add functionality similar to LowerScreenshakePerPlayer to ChomaticAberrationFeeler (sic)
    [HarmonyPatch(typeof(ChomaticAberrationFeeler), "Update")]
    class ChomaticAberrationFeeler_Patch_Update
    {
        private const float defaultForce = 10f;
        static void Postfix(ChomaticAberrationFeeler __instance)
        {
            if (PlayerManager.instance.players.Count > 2)
            {
                __instance.force = ChomaticAberrationFeeler_Patch_Update.defaultForce / (UnityEngine.Mathf.Pow(2f, UnityEngine.Mathf.Floor(PlayerManager.instance.players.Count / 2f)));
            }
        }
    }
}
