using HarmonyLib;
using System.Reflection;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(ControllerImageToggler), "Start")]
    class ControllerImageToggler_Patch_Start
    {
        static void Prefix(ControllerImageToggler __instance, ref CharacterSelectionInstance ___selector, ref CharacterCreatorPortrait ___portrait) {
            if (!___selector) {
                ___selector = __instance.GetComponentInParent<CharacterSelectionInstance>();
            }

            if (!___portrait) {
                ___portrait = __instance.GetComponentInParent<CharacterCreatorPortrait>();
            }

            if (___portrait && ___portrait.controlType != MenuControllerHandler.MenuControl.Unassigned) {
                var m_Switch = typeof(ControllerImageToggler).GetMethod("Switch", BindingFlags.Instance | BindingFlags.NonPublic);
                m_Switch.Invoke(__instance, new object[] { ___portrait.controlType });
            }
        }
    }
}
