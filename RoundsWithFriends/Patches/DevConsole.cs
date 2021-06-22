using HarmonyLib;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(DevConsole), "Send")]
    class DevConsole_Patch_Send
    {
        static bool Prefix(string message) {
            if (RWFMod.DEBUG) {
                if (message.StartsWith("join:")) {
                    string[] parts = message.Split(':');
                    string region = parts[1];
                    string room = parts[2];

                    NetworkConnectionHandler.instance.ForceRegionJoin(region, room);

                    return false;
                }
            }

            return true;
        }
    }
}
