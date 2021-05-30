using HarmonyLib;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(PlayerManager), "PlayerJoined")]
    class PlayerManager_Patch_PlayerJoined
    {
        static void Postfix(Player player) {
            var skinHandler = player.gameObject.GetComponentInChildren<PlayerSkinHandler>();
            skinHandler.ToggleSimpleSkin(true);
            PrivateRoomHandler.instance.PlayerJoined(player);
        }
    }
}
