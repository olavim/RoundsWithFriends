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
            PlayerManager.instance.SetPlayersPlaying(false);
        }
    }
}
