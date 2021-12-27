using HarmonyLib;
using UnityEngine;
using RWF.UI;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(ListMenuPage),"Close")]
    class ListMenuPage_Patch_Close
    {
        // patch to clear any keybind hints on screen when exiting a menu
        static void Postfix()
        {
            KeybindHints.ClearHints();
        }
    }
}
