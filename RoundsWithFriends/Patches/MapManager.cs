using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnboundLib;
using UnityEngine;
using Photon.Pun;
using RWF.Algorithms;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(MapManager), "CallInNewMapAndMovePlayers")]
    class MapManager_Patch_CallInNewMapAndMovePlayers
    {
        static void Prefix(MapManager __instance, PhotonView ___view, int mapID)
        {
            if (!PhotonNetwork.IsMasterClient && !PhotonNetwork.OfflineMode)
            {
                return;
            }
            NetworkingManager.RPC(typeof(GeneralizedSpawnPositions), nameof(GeneralizedSpawnPositions.RPCA_SetSeed), new object[] { UnityEngine.Random.Range(int.MinValue, int.MaxValue) });
        }
    }
}
