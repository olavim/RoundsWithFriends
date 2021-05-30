using HarmonyLib;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(CharacterSelectionInstance), "ReadyUp")]
    class CharacterSelectionInstance_Patch_ReadyUp
    {
        static void Postfix(CharacterSelectionInstance[] ___selectors) {
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
            if (numReady == numPlayers && numReady >= GM_ArmsRace.instance.GetPlayersNeededToStart()) {
                MainMenuHandler.instance.Close();
                GM_ArmsRace.instance.StartGame();
            }
        }
    }
}
