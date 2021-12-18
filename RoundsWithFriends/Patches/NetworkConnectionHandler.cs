using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using System.Linq;
using Photon.Realtime;
using System.Reflection.Emit;
using Landfall.Network;
using SoundImplementation;
using UnboundLib;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(NetworkConnectionHandler), "Start")]
    class NetworkConnectionHandler_Patch_Start
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Do not leave lobby when NetworkConnectionHandler is reloaded
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_lobby = AccessTools.Field(typeof(NetworkConnectionHandler), "m_SteamLobby");
            var m_leaveLobby = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(ClientSteamLobby), "LeaveLobby");

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].LoadsField(f_lobby) && list[i + 1].Calls(m_leaveLobby))
                {
                    // Copy labels to a no-op from the previous instruction
                    newInstructions.Add(new CodeInstruction(OpCodes.Nop).WithLabels(list[i].labels));
                    i++;
                }
                else
                {
                    newInstructions.Add(list[i]);
                }
            }

            return newInstructions;
        }
    }

    [HarmonyPatch(typeof(NetworkConnectionHandler), "OnJoinedRoom")]
    class NetworkConnectionHandler_Patch_OnJoinedRoom
    {
        static Exception Finalizer(Exception __exception) {
            if (__exception != null) {
                PatchLogger.Get("NetworkConnectionHandler::OnJoinedRoom").LogDebug($"Suppressed error:\n{__exception}");
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(NetworkConnectionHandler), "RPCA_FoundGame")]
    class NetworkConnectionHandler_Patch_RPCA_FoundGame
    {
        static bool Prefix(NetworkConnectionHandler __instance) {
            // Disable loading screen in private matches
            return __instance.IsSearchingQuickMatch() || __instance.IsSearchingTwitch();
        }
    }

    [HarmonyPatch(typeof(NetworkConnectionHandler), "HostPrivateAndInviteFriend")]
    class NetworkConnectionHandler_Patch_HostPrivateAndInviteFriend
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var list = instructions.ToList();
            var m_getMaxPlayers = typeof(RWFMod).GetProperty("MaxPlayers").GetGetMethod();
            var f_teamManagerInstance = AccessTools.Field(typeof(RWFMod), "instance");
            var f_roomOptionsMaxPlayers = AccessTools.Field(typeof(RoomOptions), "MaxPlayers");

            for (int i = 0; i < list.Count; i++) {
                if (i < list.Count - 1 && list[i + 1].StoresField(f_roomOptionsMaxPlayers)) {
                    // Replace `options.MaxPlayers = 2` with `options.MaxPlayers = (byte)TeamManager.instance.MaxPlayers`
                    yield return new CodeInstruction(OpCodes.Ldsfld, f_teamManagerInstance);
                    yield return new CodeInstruction(OpCodes.Callvirt, m_getMaxPlayers);
                    yield return new CodeInstruction(OpCodes.Conv_U1);
                    yield return list[i + 1];
                    i++;
                } else {
                    yield return list[i];
                }
            }
        }
    }

    // NetworkConnectionHandler::ForceRegionJoin is called when joining a private steam lobby
    [HarmonyPatch(typeof(NetworkConnectionHandler), "ForceRegionJoin")]
    class NetworkConnectionHandler_Patch_ForceRegionJoin
    {
        static bool Prefix(NetworkConnectionHandler __instance, string region, string room) {
            if (PhotonNetwork.InRoom) {
                PhotonNetwork.Disconnect();
            }

            CharacterCreatorHandler.instance.CloseMenus();
            PrivateRoomHandler.instance.Open();

            RegionSelector.region = region;
            TimeHandler.instance.gameStartTime = 1f;

            __instance.SetForceRegion(true);

            Action joinSpecificRoomDelegate = () => __instance.InvokeMethod("JoinSpecificRoom", room);
            __instance.StartCoroutine((IEnumerator) __instance.InvokeMethod("DoActionWhenConnected", joinSpecificRoomDelegate));

            // We'll replace the whole method
            return false;
        }
    }

    [HarmonyPatch(typeof(NetworkConnectionHandler), "OnPlayerLeftRoom")]
    class NetworkConnectionHandler_Patch_OnPlayerLeftRoom
    {
        static bool Prefix(NetworkConnectionHandler __instance) {
            if (__instance.IsSearchingQuickMatch() || __instance.IsSearchingTwitch()) {
                return true;
            }

            // We can handle the player left -event more gracefully in PrivateRoomHandler
            return false;
        }
    }

    [HarmonyPatch(typeof(NetworkConnectionHandler), "OnPlayerEnteredRoom")]
    class NetworkConnectionHandler_Patch_OnPlayerEnteredRoom
    {
        static bool Prefix(NetworkConnectionHandler __instance) {
            // When playing in a private match, we want to pretty much ignore this function since we handle player joins in PrivateRoomHandler
            if (!__instance.IsSearchingQuickMatch() && !__instance.IsSearchingTwitch()) {
                SoundPlayerStatic.Instance.PlayPlayerAdded();

                if (PhotonNetwork.IsMasterClient) {
                    // Compatibility for unmodded clients
                    __instance.GetComponent<PhotonView>().RPC("RPCA_FoundGame", RpcTarget.All, Array.Empty<object>());
                }

                var field = AccessTools.Field(typeof(NetworkConnectionHandler), "m_SteamLobby");
                var lobby = (ClientSteamLobby) field.GetValue(null);

                if (PhotonNetwork.PlayerList.Length == RWFMod.instance.MaxPlayers && lobby != null) {
                    lobby.HideLobby();
                }

                return false;
            }

            return true;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var list = instructions.ToList();
            var m_getMaxPlayers = typeof(RWFMod).GetProperty("MaxPlayers").GetGetMethod();
            var m_getPlayerList = typeof(PhotonNetwork).GetProperty("PlayerList").GetGetMethod();
            var f_teamManagerInstance = AccessTools.Field(typeof(RWFMod), "instance");

            for (int i = 0; i < list.Count; i++) {
                if (list[i].Calls(m_getPlayerList)) {
                    // Replace `if (PhotonNetwork.PlayerList.Length == 2)` with `if (PhotonNetwork.PlayerList.Length == TeamManager.instance.MaxPlayers)`
                    yield return list[i];
                    yield return list[i + 1];
                    yield return list[i + 2];
                    yield return new CodeInstruction(OpCodes.Ldsfld, f_teamManagerInstance);
                    yield return new CodeInstruction(OpCodes.Callvirt, m_getMaxPlayers);
                    i += 3;
                } else {
                    yield return list[i];
                }
            }
        }
    }
}
