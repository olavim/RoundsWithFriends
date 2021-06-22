using HarmonyLib;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(CharacterCreator), "Finish")]
    class CharacterCreator_Patch_Finish
    {
        static void Postfix(CharacterCreator __instance) {
            foreach (var go in __instance.GetObjectsToEnable()) {
                go.SetActive(true);
            }
        }
    }

    [HarmonyPatch(typeof(CharacterCreator), "Close")]
    class CharacterCreator_Patch_Close
    {
        static void Postfix(CharacterCreator __instance) {
            foreach (var go in __instance.GetObjectsToEnable()) {
                go.SetActive(true);
            }
        }
    }
}
