using HarmonyLib;
using Photon.Pun;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sonigon;
using System.Linq;
using UnboundLib;

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
        private static LayerMask groundMask = (LayerMask) LayerMask.GetMask(new string[] { "Default", "IgnorePlayer" });
        static bool MapHasValidGround(Map map)
        {
            if (!(bool)map.GetFieldValue("hasCalledReady")) { return false; }

            foreach (Transform transform in map.gameObject.transform)
            {
                RaycastHit2D raycastHit2D = Physics2D.Raycast(transform.position + 100f*Vector3.up, Vector2.down, 101f, groundMask);
                if (raycastHit2D.transform && raycastHit2D.distance > 0.1f)
                {
                    Vector2 screenPoint = MainCam.instance.transform.GetComponent<Camera>().FixedWorldToScreenPoint(raycastHit2D.point);
                    screenPoint.x /= FixedScreen.fixedWidth;
                    screenPoint.y /= Screen.height;
                    if (screenPoint.x >= 0f && screenPoint.x <= 1f && screenPoint.y >= 0f && screenPoint.y <= 1f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        static IEnumerator WaitForMapToLoad(PlayerManager __instance, SpawnPoint[] spawnPoints)
        {
            /*
            yield return new WaitUntil(() => MapManager.instance.currentMap.Map.allRigs.Any());
            foreach (Rigidbody2D rig in MapManager.instance.currentMap.Map.allRigs)
            {
                UnityEngine.Debug.Log(rig.gameObject.name);
            }
            yield return new WaitForEndOfFrame();
            yield return new WaitUntil(() => MapManager.instance.currentMap.Map.allRigs.Where(r => r?.gameObject?.activeInHierarchy == true).All(r => r?.gameObject?.GetComponent<Collider2D>()?.isActiveAndEnabled == true)); // wait for map colliders to load
            foreach (Rigidbody2D rig in MapManager.instance.currentMap.Map.allRigs)
            {
                UnityEngine.Debug.Log(rig.gameObject.name + " - ACTIVE: " + ((bool)rig?.gameObject?.activeInHierarchy).ToString() + " - ENABLED COLLIDER: "+ ((bool)rig?.gameObject?.GetComponent<Collider2D>()?.isActiveAndEnabled).ToString());
            }*/
            yield return new WaitUntil(() => MapHasValidGround(MapManager.instance.currentMap?.Map));
            yield return new WaitForSecondsRealtime(0.5f);

            //yield return new WaitForSecondsRealtime(3f);

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
