using HarmonyLib;
using UnboundLib.GameModes;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [HarmonyPatch(typeof(CharacterSelectionInstance), "StartPicking")]
    class CharacterSelectionInstance_Patch_StartPicking
    {
        static bool Prefix(CharacterSelectionInstance __instance, Player pickingPlayer, ref HoverEvent[] ___buttons, ref HoverEvent ___currentButton, ref float ___counter)
        {
            __instance.currentPlayer = pickingPlayer;
            __instance.currentlySelectedFace = 0;
            __instance.GetComponentInChildren<GeneralParticleSystem>(true).gameObject.SetActive(false);
            __instance.GetComponentInChildren<GeneralParticleSystem>(true).Stop();
            __instance.transform.GetChild(0).gameObject.SetActive(true);
            __instance.getReadyObj.gameObject.SetActive(true);
            if (__instance.currentPlayer.data.input.inputType == GeneralInput.InputType.Keyboard)
            {
                __instance.getReadyObj.GetComponent<TextMeshProUGUI>().text = "[SPACE]";
            }
            else
            {
                __instance.getReadyObj.GetComponent<TextMeshProUGUI>().text = "[START]";
            }
            ___buttons = __instance.transform.GetComponentsInChildren<HoverEvent>();
            for (int i = 0; i < ___buttons.Length; i++)
            {
                if (true)//pickingPlayer.data.input.inputType == GeneralInput.InputType.Controller)
                {
                    ___buttons[i].enabled = false;
                    ___buttons[i].GetComponent<Button>().interactable = false;
                    ___buttons[i].GetComponent<CharacterCreatorPortrait>().controlType = MenuControllerHandler.MenuControl.Controller;
                }
                else
                {
                    /*
                    __instance.buttons[i].enabled = true;
                    __instance.buttons[i].GetComponent<Button>().interactable = true;
                    __instance.buttons[i].GetComponent<CharacterCreatorPortrait>().controlType = MenuControllerHandler.MenuControl.Mouse;
                    Navigation navigation = __instance.buttons[i].GetComponent<Button>().navigation;
                    navigation.mode = Navigation.Mode.None;
                    __instance.buttons[i].GetComponent<Button>().navigation = navigation;
                    */
                }

                if (pickingPlayer.data.input.inputType != GeneralInput.InputType.Controller)
                {
                    ___buttons[i].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = "[E] TO EDIT";
                    ___buttons[i].transform.GetChild(3).GetChild(1).gameObject.SetActive(false);
                }

            }
            return false;
        }
    }

    [HarmonyPatch(typeof(CharacterSelectionInstance), "Update")]
    class CharacterSelectionInstance_Patch_Update
    {
        static bool Prefix(CharacterSelectionInstance __instance, ref HoverEvent[] ___buttons, ref HoverEvent ___currentButton, ref float ___counter)
        {
            if (!__instance.currentPlayer)
            {
                return false;
            }
            if (__instance.currentPlayer.data.input.inputType != GeneralInput.InputType.Controller)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    __instance.ReadyUp();
                }
                //return false;
            }
            else if (__instance.currentPlayer.data.playerActions.Device.CommandWasPressed)
            {
                __instance.ReadyUp();
            }
            HoverEvent component = ___buttons[__instance.currentlySelectedFace].GetComponent<HoverEvent>();
            if (___currentButton != component)
            {
                if (___currentButton)
                {
                    ___currentButton.GetComponent<SimulatedSelection>().Deselect();
                    ___currentButton.gameObject.SetActive(false);
                }
                else
                {
                    for (int i = 0; i < ___buttons.Length; i++)
                    {
                        if (i == __instance.currentlySelectedFace) { continue; }
                        ___buttons[i].GetComponent<SimulatedSelection>().Deselect();
                        ___buttons[i].gameObject.SetActive(false);
                    }
                }
                ___currentButton = component;
                ___currentButton.gameObject.SetActive(true);
                ___currentButton.GetComponent<SimulatedSelection>().Select();
                ___currentButton.GetComponent<Button>().onClick.Invoke();
            }
            ___counter += Time.deltaTime;
            if ((((__instance.currentPlayer.data.input.inputType == GeneralInput.InputType.Controller) && Mathf.Abs(__instance.currentPlayer.data.playerActions.Move.X) > 0.5f) || ((__instance.currentPlayer.data.input.inputType != GeneralInput.InputType.Controller) && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)))) && ___counter > 0.2f)
            {
                if (((__instance.currentPlayer.data.input.inputType == GeneralInput.InputType.Controller) && __instance.currentPlayer.data.playerActions.Move.X > 0.5f) || ((__instance.currentPlayer.data.input.inputType != GeneralInput.InputType.Controller) && (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))))
                {
                    __instance.currentlySelectedFace++;
                }
                else
                {
                    __instance.currentlySelectedFace--;
                }
                ___counter = 0f;
            }
            //if (__instance.currentPlayer.data.playerActions.Jump.WasPressed)
            //{
            //    ___currentButton.GetComponent<Button>().onClick.Invoke();
            //}
            if (((__instance.currentPlayer.data.input.inputType == GeneralInput.InputType.Controller) && __instance.currentPlayer.data.playerActions.Device.Action4.WasPressed) || ((__instance.currentPlayer.data.input.inputType != GeneralInput.InputType.Controller) && Input.GetKeyDown(KeyCode.E)))
            {
                ___currentButton.GetComponent<CharacterCreatorPortrait>().EditCharacter();
            }
            __instance.currentlySelectedFace = Mathf.Clamp(__instance.currentlySelectedFace, 0, ___buttons.Length - 1);

            return false;
        }
    }

}
