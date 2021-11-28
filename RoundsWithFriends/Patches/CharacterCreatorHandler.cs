using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(CharacterCreatorHandler), "Awake")]
    class CharacterCreatorHandler_Patch_SelectFace
    {
        static void Prefix(CharacterCreatorHandler __instance)
        {
            __instance.selectedFaceID = new int[RWFMod.instance.MaxPlayers];
            __instance.selectedPlayerFaces = new PlayerFace[RWFMod.instance.MaxPlayers];
        }
    }
}
