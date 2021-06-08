using HarmonyLib;
using Photon.Pun;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(PlayerManager), "PlayerJoined")]
    class PlayerManager_Patch_PlayerJoined
    {
        static void Postfix(Player player) {
            if (!PhotonNetwork.OfflineMode) {
                PrivateRoomHandler.instance.PlayerJoined(player);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "RemovePlayers")]
    class PlayerManager_Patch_RemovePlayers
    {
        static void Prefix(PlayerManager __instance) {
            if (!PhotonNetwork.OfflineMode) {
                var players = __instance.players;

                for (int i = players.Count - 1; i >= 0; i--) {
                    if (players[i].data.view.AmOwner) {
                        PhotonNetwork.Destroy(players[i].data.view);
                    }
                }
            }
        }
    }
}
