using HarmonyLib;
using SoundImplementation;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(SoundPlayerStatic), "PlayPlayerAdded")]
    class SoundPlayerStatic_Patch_PlayPlayerAdded
    {
        static bool Prefix() {
            return RWFMod.instance.GetSoundEnabled("PlayerAdded");
        }
    }
}
