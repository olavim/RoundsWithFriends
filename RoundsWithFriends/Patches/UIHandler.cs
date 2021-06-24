using HarmonyLib;
using System;
using UnityEngine;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(UIHandler), "DisplayScreenTextLoop", new Type[] { typeof(Color), typeof(string) })]
    class UIHandler_Patch_DisplayScreenTextLoop1
    {
        static bool Prefix(UIHandler __instance) {
            return !__instance.GetData().disableTexts;
        }
    }

    [HarmonyPatch(typeof(UIHandler), "DisplayScreenTextLoop", new Type[] { typeof(string) })]
    class UIHandler_Patch_DisplayScreenTextLoop2
    {
        static bool Prefix(UIHandler __instance)
        {
            return !__instance.GetData().disableTexts;
        }
    }

    [HarmonyPatch(typeof(UIHandler), "DisplayScreenText")]
    class UIHandler_Patch_DisplayScreenText
    {
        static bool Prefix(UIHandler __instance)
        {
            return !__instance.GetData().disableTexts;
        }
    }

    [HarmonyPatch(typeof(UIHandler), "ShowJoinGameText")]
    class UIHandler_Patch_ShowJoinGameText
    {
        static bool Prefix(UIHandler __instance)
        {
            return !__instance.GetData().disableTexts;
        }
    }
}
