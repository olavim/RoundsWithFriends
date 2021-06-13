using HarmonyLib;
using UnboundLib.GameModes;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(CharacterSelectionInstance), "ReadyUp")]
    class CharacterSelectionInstance_Patch_ReadyUp
    {
        static bool Prefix(CharacterSelectionInstance[] ___selectors, ref bool ___isReady) {
            ___isReady = !___isReady;

            int numReady = 0;
            int numPlayers = 0;

            for (int i = 0; i < ___selectors.Length; i++) {
                if (___selectors[i].isReady) {
                    numReady++;
                }
                if (___selectors[i].currentPlayer) {
                    numPlayers++;
                }
            }

            if (numReady == numPlayers && numReady >= RWFMod.instance.MinPlayers) {
                MainMenuHandler.instance.Close();
                GameModeManager.CurrentHandler.StartGame();
                return false;
            }

            ___isReady = !___isReady;
            return true;
        }
    }
}
