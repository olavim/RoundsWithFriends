using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(PlayerManager), "Update")]
    class PlayerManager_Patch_Update
    {
        static void Postfix() {
            try {
                bool isHost = PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom;

                // Team size can only be changed in main menu
                if (MainMenuHandler.instance.isOpen && isHost) {
                    if (Input.GetKeyDown(KeyCode.F1)) {
                        RWFMod.instance.SetTeamSize(1);
                    }
                    if (Input.GetKeyDown(KeyCode.F2)) {
                        RWFMod.instance.SetTeamSize(2);
                    }
                }
            } catch (System.Exception e) {
                PatchLogger.Get("PlayerManager::Update").LogError(e.ToString());
            }
        }
    }

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
