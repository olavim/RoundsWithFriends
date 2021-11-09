using HarmonyLib;
using Photon.Pun;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sonigon;

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

    [HarmonyPatch(typeof(PlayerManager), "GetOtherPlayer")]
    class PlayerManager_Patch_GetOtherPlayer
    {
        static bool Prefix(PlayerManager __instance, Player asker, ref Player __result)
        {
            __result = __instance.GetClosestPlayerInOtherTeam(asker.transform.position, asker.teamID, false);
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "MovePlayers")]
    class PlayerManager_Patch_MovePlayers
    {
        static bool Prefix(PlayerManager __instance, SpawnPoint[] spawnPoints)
        {
            __instance.StartCoroutine(WaitForMapToLoad(__instance, spawnPoints));
            return false;
        }
        static IEnumerator WaitForMapToLoad(PlayerManager __instance, SpawnPoint[] spawnPoints)
        {
            yield return new WaitForSecondsRealtime(1f); // wait for map colliders to load

            Dictionary<Player, Vector2> spawnDictionary = GeneralizedSpawnPositions.GetSpawnDictionary(__instance.players, spawnPoints);

            for (int i = 0; i < __instance.players.Count; i++)
            {

                __instance.StartCoroutine((IEnumerator) typeof(PlayerManager).InvokeMember("Move",
                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                    BindingFlags.NonPublic, null, __instance, new object[] { __instance.players[i].data.playerVel, (Vector3) spawnDictionary[__instance.players[i]] }));
                /*
                yield return (IEnumerator) typeof(PlayerManager).InvokeMember("Move",
    BindingFlags.Instance | BindingFlags.InvokeMethod |
    BindingFlags.NonPublic, null, __instance, new object[] { __instance.players[i].data.playerVel, (Vector3) spawnPositions[i] });
                */
                // I have no idea why this is in the original method like this but I'm afraid to change it
                int j;
                for (j = i; j >= __instance.soundCharacterSpawn.Length; j -= __instance.soundCharacterSpawn.Length)
                {
                }

                SoundManager.Instance.Play(__instance.soundCharacterSpawn[j], __instance.players[i].transform);
            }
        }


    }
}
