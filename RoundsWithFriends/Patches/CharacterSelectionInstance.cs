using HarmonyLib;
using UnboundLib.GameModes;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RWF.UI;
using UnboundLib;
using System.Collections.Generic;
using System.Linq;
using UnboundLib.Extensions;
using UnboundLib.Utils;

namespace RWF.Patches
{
    static class Colors
    {
        public static Color Transparent(Color color, float a = 0.5f)
        {
            return new Color(color.r, color.g, color.b, a);
        }
        public static Color readycolor = new Color(0.2f, 0.8f, 0.1f, 1f);
        public static Color editcolor = new Color(0.9f, 0f, 0.1f, 1f);
        public static Color joinedcolor = new Color(0.566f, 0.566f, 0.566f, 1f);
    }

    [HarmonyPatch(typeof(CharacterSelectionInstance), "Start")]
    class CharacterSelectionInstance_Patch_Start
    {
        static void Prefix(CharacterSelectionInstance __instance, ref float ___counter)
        {
            foreach (Transform child in __instance.transform)
            {
                child.localPosition = Vector2.zero;
            }
            //__instance.currentPlayer.data.playerActions.Jump.ClearInputState();
            ___counter = -0.1f;
        }

        static void Postfix(CharacterSelectionInstance __instance, ref CharacterSelectionInstance[] ___selectors)
        {
            ___selectors = __instance.transform.parent.GetComponentsInChildren<CharacterSelectionInstance>(true);
        }
    }

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

                // assign teamIDs according to colorIDs
                int nextTeamID = 0;
                Dictionary<int, int> colorToTeam = new Dictionary<int, int>() { };
                foreach (Player player in PlayerManager.instance.players)
                {
                    if (colorToTeam.TryGetValue(player.colorID(), out int teamID))
                    {
                        player.AssignTeamID(teamID);
                    }
                    else
                    {
                        player.AssignTeamID(nextTeamID);
                        colorToTeam[player.colorID()] = nextTeamID;
                        nextTeamID++;
                    }
                }

                GameModeManager.CurrentHandler.StartGame();
                return false;
            }

            ___isReady = !___isReady;
            return true;
        }

        static void Postfix(CharacterSelectionInstance __instance, HoverEvent[] ___buttons)
        {
            __instance.getReadyObj.GetComponent<TextMeshProUGUI>().text = "";
            for (int i = 0; i< ___buttons.Length; i++)
            {
                ___buttons[i].transform.GetChild(4).GetChild(0).gameObject.SetActive(__instance.isReady);
                ___buttons[i].transform.GetChild(4).GetChild(1).gameObject.SetActive(__instance.isReady);
                foreach (Graphic graphic in ___buttons[i].transform.GetChild(4).GetChild(0).GetComponentsInChildren<Graphic>(true))
                {
                    graphic.color = __instance.isReady ? Colors.Transparent(Colors.readycolor) : Color.clear;
                }
                foreach (Graphic graphic in ___buttons[i].transform.GetChild(4).GetChild(1).GetComponentsInChildren<Graphic>(true))
                {
                    graphic.color = __instance.isReady ? Colors.Transparent(Colors.readycolor) : Color.clear;
                }
                ___buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().text = __instance.isReady ? "READY" : $"{((GameModeManager.CurrentHandler.Settings.TryGetValue("allowTeams", out object allowTeamsObj) && !(bool) allowTeamsObj) ? "" : "TEAM ")}{ExtraPlayerSkins.GetTeamColorName(__instance.currentPlayer.colorID()).ToUpper()}";
                ___buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().color = __instance.isReady ? Colors.readycolor : Colors.joinedcolor;
            }
        }
    }

    [HarmonyPatch(typeof(CharacterSelectionInstance), "StartPicking")]
    class CharacterSelectionInstance_Patch_StartPicking
    {

        static bool Prefix(CharacterSelectionInstance __instance, Player pickingPlayer, ref HoverEvent[] ___buttons, ref HoverEvent ___currentButton, ref float ___counter)
        {
            __instance.currentPlayer = pickingPlayer;
            __instance.currentlySelectedFace = PlayerPrefs.GetInt("SelectedFace" + pickingPlayer.playerID);
            try
            {
                __instance.GetComponentInChildren<GeneralParticleSystem>(true).gameObject.SetActive(false);
                __instance.GetComponentInChildren<GeneralParticleSystem>(true).Stop();
            }
            catch { }

            __instance.transform.GetChild(0).gameObject.SetActive(true);
            __instance.getReadyObj.gameObject.SetActive(true);
            __instance.getReadyObj.GetComponent<TextMeshProUGUI>().text = "";

            __instance.transform.GetChild(1).gameObject.SetActive(false);
            __instance.transform.GetChild(2).gameObject.SetActive(false);
            
            ___buttons = __instance.transform.GetComponentsInChildren<HoverEvent>(true);
            for (int i = 0; i < ___buttons.Length; i++)
            {
                ___buttons[i].enabled = false;
                ___buttons[i].GetComponent<Button>().interactable = false;
                ___buttons[i].GetComponent<CharacterCreatorPortrait>().controlType = MenuControllerHandler.MenuControl.Controller;

                if (pickingPlayer.data.input.inputType != GeneralInput.InputType.Controller)
                {
                    ___buttons[i].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = "[E] TO EDIT";
                    ___buttons[i].transform.GetChild(3).GetChild(0).localPosition -= new Vector3(___buttons[i].transform.GetChild(3).GetChild(0).localPosition.x, 0f, 0f);
                    ___buttons[i].transform.GetChild(3).GetChild(1).gameObject.SetActive(false);
                }
                else
                {
                    ___buttons[i].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = "TO EDIT";
                    ___buttons[i].transform.GetChild(3).GetChild(0).localPosition = new Vector3(23.4f, 15.7f, 0f);
                    ___buttons[i].transform.GetChild(3).GetChild(1).gameObject.SetActive(true);
                }

                // enabled the "LOCKED" component to reuse as info text
                ___buttons[i].transform.GetChild(4).gameObject.SetActive(true);
                ___buttons[i].transform.GetChild(4).GetChild(0).gameObject.SetActive(false);
                ___buttons[i].transform.GetChild(4).GetChild(1).gameObject.SetActive(false);
                ___buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().text = $"{((GameModeManager.CurrentHandler.Settings.TryGetValue("allowTeams", out object allowTeamsObj) && !(bool) allowTeamsObj) ? "" : "TEAM ")}{ExtraPlayerSkins.GetTeamColorName(__instance.currentPlayer.colorID()).ToUpper()}";
                ___buttons[i].transform.GetChild(4).GetChild(2).GetComponent<RectTransform>().sizeDelta = new Vector2(150f, ___buttons[i].transform.GetChild(4).GetChild(2).GetComponent<RectTransform>().sizeDelta.y);
                ___buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().color = Colors.joinedcolor;

                // update colors and team names

                ___buttons[i].transform.GetChild(2).GetChild(0).GetComponent<SpriteRenderer>().color = __instance.currentPlayer.GetTeamColors().color;
                ___buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().text = $"{((GameModeManager.CurrentHandler.Settings.TryGetValue("allowTeams", out object allowTeamsObj2) && !(bool) allowTeamsObj2) ? "" : "TEAM ")}{ExtraPlayerSkins.GetTeamColorName(__instance.currentPlayer.colorID()).ToUpper()}";
                

            }

            if (__instance.transform.GetChild(0).Find("CharacterSelectButtons") != null)
            {
                GameObject go1 = __instance.transform.GetChild(0).Find("CharacterSelectButtons")?.gameObject;

                UnityEngine.GameObject.Destroy(go1);
            }

            GameObject characterSelectButtons = new GameObject("CharacterSelectButtons");
            characterSelectButtons.transform.SetParent(__instance.transform.GetChild(0));
            GameObject leftarrow = new GameObject("LeftArrow", typeof(CharacterSelectButton));
            leftarrow.transform.SetParent(characterSelectButtons.transform);
            GameObject rightarrow = new GameObject("RightArrow", typeof(CharacterSelectButton));
            rightarrow.transform.SetParent(characterSelectButtons.transform);

            characterSelectButtons.transform.localScale = Vector3.one;
            characterSelectButtons.transform.localPosition = Vector3.zero;

            leftarrow.transform.localScale = new Vector3(1f, 3f, 1f);
            leftarrow.transform.localPosition = new Vector3(-60f, 0f, 0f);
            leftarrow.GetComponent<CharacterSelectButton>().SetCharacterSelectionInstance(__instance);
            leftarrow.GetComponent<CharacterSelectButton>().SetDirection(CharacterSelectButton.LeftRight.Left);
            rightarrow.transform.localScale = new Vector3(1f, 3f, 1f);
            rightarrow.transform.localPosition = new Vector3(60f, 0f, 0f);
            rightarrow.GetComponent<CharacterSelectButton>().SetCharacterSelectionInstance(__instance);
            rightarrow.GetComponent<CharacterSelectButton>().SetDirection(CharacterSelectButton.LeftRight.Right);

            ___buttons[0].GetComponent<Button>().onClick.Invoke();

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
            }
            else if (__instance.currentPlayer.data.playerActions.Device.CommandWasPressed || (__instance.currentPlayer.data.playerActions.Jump.WasPressed && ___counter > 0f))
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
                ___currentButton.transform.GetChild(4).gameObject.SetActive(true);
                ___currentButton.gameObject.SetActive(true);
                ___currentButton.GetComponent<SimulatedSelection>().Select();
                ___currentButton.GetComponent<Button>().onClick.Invoke();
            }
            ___counter += Time.deltaTime;
            if ((((__instance.currentPlayer.data.input.inputType == GeneralInput.InputType.Controller) && Mathf.Abs(__instance.currentPlayer.data.playerActions.Move.X) > 0.5f) || ((__instance.currentPlayer.data.input.inputType != GeneralInput.InputType.Controller) && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))) || __instance.currentPlayer.data.playerActions.GetAdditionalData().increaseColorID.WasPressed || __instance.currentPlayer.data.playerActions.GetAdditionalData().decreaseColorID.WasPressed) && ___counter > 0.2f)
            {
                // change face
                if (((__instance.currentPlayer.data.input.inputType == GeneralInput.InputType.Controller) && __instance.currentPlayer.data.playerActions.Move.X > 0.5f) || ((__instance.currentPlayer.data.input.inputType != GeneralInput.InputType.Controller) && (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))))
                {
                    __instance.currentlySelectedFace++;
                }
                else if (((__instance.currentPlayer.data.input.inputType == GeneralInput.InputType.Controller) && __instance.currentPlayer.data.playerActions.Move.X <= 0.5f) || ((__instance.currentPlayer.data.input.inputType != GeneralInput.InputType.Controller) && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))))
                {
                    __instance.currentlySelectedFace--;
                }
                bool colorChanged = false;
                int colorIDDelta = 0;
                // change team
                if (__instance.currentPlayer.data.playerActions.GetAdditionalData().increaseColorID.WasPressed)
                {
                    //newTeamID = UnityEngine.Mathf.Clamp(__instance.currentPlayer.teamID + 1, 0, RWFMod.MaxTeamsHardLimit - 1);
                    colorIDDelta = +1;
                    colorChanged = true;
                }
                else if (__instance.currentPlayer.data.playerActions.GetAdditionalData().decreaseColorID.WasPressed)
                {
                    //newTeamID = UnityEngine.Mathf.Clamp(__instance.currentPlayer.teamID - 1, 0, RWFMod.MaxTeamsHardLimit - 1);
                    colorIDDelta = -1;
                    colorChanged = true;
                }

                if (colorChanged)
                {
                    int newColorID = Math.mod((__instance.currentPlayer.colorID() + colorIDDelta), RWFMod.MaxColorsHardLimit);
                    int orig = __instance.currentPlayer.colorID();

                    // wow this syntax is concerning
                    if (GameModeManager.CurrentHandler.Settings.TryGetValue("allowTeams", out object allowTeamsObj) && !(bool) allowTeamsObj)
                    {
                        // teams not allowed, continue to next colorID()
                        while (PlayerManager.instance.players.Select(p => p.colorID()).Contains(newColorID) && newColorID != orig)
                        {
                            newColorID = Math.mod((newColorID + colorIDDelta), RWFMod.MaxColorsHardLimit);
                        }

                    }
                    
                    bool fail = newColorID == orig || newColorID >= RWFMod.MaxColorsHardLimit || newColorID < 0 || (!(bool)allowTeamsObj && PlayerManager.instance.players.Select(p => p.colorID()).Contains(newColorID));

                    if (!fail)
                    {
                        // update player preferences
                        PlayerPrefs.SetInt(RWFMod.GetCustomPropertyKey("PreferredColor" + __instance.currentPlayer.playerID.ToString()), newColorID);

                        __instance.currentPlayer.AssignColorID(newColorID);
                        __instance.currentPlayer.SetColors();
                        for (int i = 0; i < ___buttons.Length; i++)
                        {
                            ___buttons[i].transform.GetChild(2).GetChild(0).GetComponent<SpriteRenderer>().color = __instance.currentPlayer.GetTeamColors().color;
                            ___buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().text = $"{((GameModeManager.CurrentHandler.Settings.TryGetValue("allowTeams", out object allowTeamsObj3) && !(bool) allowTeamsObj3) ? "" : "TEAM ")}{ExtraPlayerSkins.GetTeamColorName(__instance.currentPlayer.colorID()).ToUpper()}";
                        }
                    }
                }

                ___counter = 0f;
            }
            if (((__instance.currentPlayer.data.input.inputType == GeneralInput.InputType.Controller) && __instance.currentPlayer.data.playerActions.Device.Action4.WasPressed) || ((__instance.currentPlayer.data.input.inputType != GeneralInput.InputType.Controller) && Input.GetKeyDown(KeyCode.E)))
            {
                for (int i = 0; i < ___buttons.Length; i++)
                {
                    ___buttons[i].transform.GetChild(4).GetChild(0).gameObject.SetActive(true);
                    ___buttons[i].transform.GetChild(4).GetChild(1).gameObject.SetActive(true);
                    foreach (Graphic graphic in ___buttons[i].transform.GetChild(4).GetChild(0).GetComponentsInChildren<Graphic>(true))
                    {
                        graphic.color = Colors.Transparent(Colors.editcolor);
                    }
                    ___buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().text = "EDITING";
                    ___buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().color = Colors.editcolor;
                }

                ___currentButton.GetComponent<CharacterCreatorPortrait>().EditCharacter();

                __instance.getReadyObj.GetComponent<TextMeshProUGUI>().text = "";
                ___currentButton.GetComponent<SimulatedSelection>().Select();


                __instance.transform.GetChild(1).gameObject.SetActive(false);
                __instance.transform.GetChild(2).gameObject.SetActive(false);

                for (int i = 0; i < ___buttons.Length; i++)
                {
                    ___buttons[i].transform.GetChild(4).GetChild(0).gameObject.SetActive(false);
                    ___buttons[i].transform.GetChild(4).GetChild(1).gameObject.SetActive(false);
                    foreach (Graphic graphic in ___buttons[i].transform.GetChild(4).GetChild(0).GetComponentsInChildren<Graphic>(true))
                    {
                        graphic.color = Color.clear;
                    }
                    ___buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().text = $"{((GameModeManager.CurrentHandler.Settings.TryGetValue("allowTeams", out object allowTeamsObj) && !(bool) allowTeamsObj) ? "" : "TEAM ")}{ExtraPlayerSkins.GetTeamColorName(__instance.currentPlayer.colorID()).ToUpper()}";
                    ___buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().color = Colors.joinedcolor;
                }

            }
            __instance.currentlySelectedFace = Math.mod(__instance.currentlySelectedFace, ___buttons.Length);

            return false;
        }
    }

}
