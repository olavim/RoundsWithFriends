﻿using HarmonyLib;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(MainMenuHandler), "Awake")]
    class MainMenuHandler_Patch_Awake
    {
        static void Postfix() {
            RWFMod.instance.InjectGameModes();
            RWFMod.instance.InjectUIElements();
        }
    }
}
