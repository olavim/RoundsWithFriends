using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnboundLib.GameModes;
using InControl;
using RWF.Patches;
using System.Linq;

namespace RWF.UI
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
    public class PrivateRoomCharacterSelectionInstance : MonoBehaviour
    {
        private void Start()
        {
            foreach (Transform child in this.transform)
            {
                child.localPosition = Vector2.zero;
            }
            this.selectors = base.transform.parent.GetComponentsInChildren<PrivateRoomCharacterSelectionInstance>(true);
        }

        public void ResetMenu()
        {
            base.transform.GetChild(0).gameObject.SetActive(false);
            this.currentPlayer = null;
            //this.getReadyObj.gameObject.SetActive(false);
            PlayerManager.instance.RemovePlayers();
        }

        private void OnEnable()
        {
            if (!base.transform.GetChild(0).gameObject.activeSelf)
            {
                base.GetComponentInChildren<GeneralParticleSystem>(true).gameObject.SetActive(true);
                base.GetComponentInChildren<GeneralParticleSystem>(true).Play();
            }
        }

        public void StartPicking(LobbyCharacter pickingCharacter, InputDevice device, bool inControl)
        {
            this.currentPlayer = pickingCharacter;
            this.device = device;
            this.currentlySelectedFace = 0;
            try
            {
                this.GetComponentInChildren<GeneralParticleSystem>(true).gameObject.SetActive(false);
                this.GetComponentInChildren<GeneralParticleSystem>(true).Stop();
            }
            catch { }


            this.transform.GetChild(0).gameObject.SetActive(true);
            //this.getReadyObj.gameObject.SetActive(true);
            //this.getReadyObj.GetComponent<TextMeshProUGUI>().text = "";


            this.transform.GetChild(1).gameObject.SetActive(false);
            this.transform.GetChild(2).gameObject.SetActive(false);


            this.buttons = this.transform.GetComponentsInChildren<HoverEvent>(true);
            for (int i = 0; i < this.buttons.Length; i++)
            {
                this.buttons[i].enabled = false;
                this.buttons[i].GetComponent<Button>().interactable = false;
                this.buttons[i].GetComponent<CharacterCreatorPortrait>().controlType = MenuControllerHandler.MenuControl.Controller;


                this.buttons[i].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = pickingCharacter.NickName;
                this.buttons[i].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                this.buttons[i].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().fontSize = 25f;
                this.buttons[i].transform.GetChild(3).GetChild(0).localPosition -= new Vector3(this.buttons[i].transform.GetChild(3).GetChild(0).localPosition.x, -25f, 0f);
                this.buttons[i].transform.GetChild(3).GetChild(1).gameObject.SetActive(false);
                
                // enabled the "LOCKED" component to reuse as info text
                this.buttons[i].transform.GetChild(4).gameObject.SetActive(true);
                this.buttons[i].transform.GetChild(4).GetChild(0).gameObject.SetActive(false);
                this.buttons[i].transform.GetChild(4).GetChild(1).gameObject.SetActive(false);
                this.buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().text = "";
                this.buttons[i].transform.GetChild(4).GetChild(2).GetComponent<RectTransform>().sizeDelta = new Vector2(150f, this.buttons[i].transform.GetChild(4).GetChild(2).GetComponent<RectTransform>().sizeDelta.y);
                this.buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().color = Colors.joinedcolor;

                // update colors
                this.buttons[i].transform.GetChild(2).GetChild(0).GetComponent<SpriteRenderer>().color = ExtraPlayerSkins.GetPlayerSkinColors(this.currentPlayer.colorID).color;

                // disable the background and frame, since I can't get it to be behind the fucking face
                this.buttons[i].transform.GetChild(0).gameObject.SetActive(false);
                this.buttons[i].transform.GetChild(1).gameObject.SetActive(false);

            }

            if (this.transform.GetChild(0).Find("CharacterSelectButtons") != null)
            {
                GameObject go1 = this.transform.GetChild(0).Find("CharacterSelectButtons")?.gameObject;

                UnityEngine.GameObject.Destroy(go1);
            }

            GameObject characterSelectButtons = new GameObject("CharacterSelectButtons");
            characterSelectButtons.transform.SetParent(this.transform.GetChild(0));
            GameObject leftarrow = new GameObject("LeftArrow", typeof(CharacterSelectButton));
            leftarrow.transform.SetParent(characterSelectButtons.transform);
            GameObject rightarrow = new GameObject("RightArrow", typeof(CharacterSelectButton));
            rightarrow.transform.SetParent(characterSelectButtons.transform);

            characterSelectButtons.transform.localScale = Vector3.one;
            characterSelectButtons.transform.localPosition = Vector3.zero;

            leftarrow.transform.localScale = new Vector3(1f, 3f, 1f);
            leftarrow.transform.localPosition = new Vector3(-60f, 0f, 0f);
            //leftarrow.GetComponent<CharacterSelectButton>().SetCharacterSelectionInstance(this);
            leftarrow.GetComponent<CharacterSelectButton>().SetDirection(CharacterSelectButton.LeftRight.Left);
            rightarrow.transform.localScale = new Vector3(1f, 3f, 1f);
            rightarrow.transform.localPosition = new Vector3(60f, 0f, 0f);
            //rightarrow.GetComponent<CharacterSelectButton>().SetCharacterSelectionInstance(this);
            rightarrow.GetComponent<CharacterSelectButton>().SetDirection(CharacterSelectButton.LeftRight.Right);

            this.buttons[0].GetComponent<Button>().onClick.Invoke();
        }

        public void ReadyUp()
        {
            //this.getReadyObj.GetComponent<TextMeshProUGUI>().text = "";
            for (int i = 0; i < this.buttons.Length; i++)
            {
                this.buttons[i].transform.GetChild(4).GetChild(0).gameObject.SetActive(this.isReady);
                this.buttons[i].transform.GetChild(4).GetChild(1).gameObject.SetActive(this.isReady);
                foreach (Graphic graphic in this.buttons[i].transform.GetChild(4).GetChild(0).GetComponentsInChildren<Graphic>(true))
                {
                    graphic.color = this.isReady ? Colors.Transparent(Colors.readycolor) : Color.clear;
                }
                foreach (Graphic graphic in this.buttons[i].transform.GetChild(4).GetChild(1).GetComponentsInChildren<Graphic>(true))
                {
                    graphic.color = this.isReady ? Colors.Transparent(Colors.readycolor) : Color.clear;
                }
                this.buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().text = this.isReady ? "READY" : "";
                this.buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().color = this.isReady ? Colors.readycolor : Colors.joinedcolor;
            }
        }

        private void Update()
        {
            if (this.currentPlayer == null)
            {
                return;
            }
            if (this.device == null)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    this.ReadyUp();
                }
            }
            else if ((this.device != null) && (this.device.CommandWasPressed || this.device.Action1.WasPressed) && this.counter > 0f)
            {
                this.ReadyUp();
            }
            HoverEvent component = this.buttons[this.currentlySelectedFace].GetComponent<HoverEvent>();
            if (this.currentButton != component)
            {
                if (this.currentButton)
                {
                    this.currentButton.GetComponent<SimulatedSelection>().Deselect();
                    this.currentButton.gameObject.SetActive(false);
                }
                else
                {
                    for (int i = 0; i < this.buttons.Length; i++)
                    {
                        if (i == this.currentlySelectedFace) { continue; }
                        this.buttons[i].GetComponent<SimulatedSelection>().Deselect();
                        this.buttons[i].gameObject.SetActive(false);
                    }
                }
                this.currentButton = component;
                this.currentButton.transform.GetChild(4).gameObject.SetActive(true);
                this.currentButton.gameObject.SetActive(true);
                this.currentButton.GetComponent<SimulatedSelection>().Select();
                this.currentButton.GetComponent<Button>().onClick.Invoke();

                // disable the background and frame, since I can't get it to be behind the fucking face
                this.currentButton.transform.GetChild(0).gameObject.SetActive(false);
                this.currentButton.transform.GetChild(1).gameObject.SetActive(false);
            }
            this.counter += Time.deltaTime;
            if (((this.device != null && (this.device.DeviceClass == InputDeviceClass.Controller) && (Mathf.Abs(this.device.LeftStickX.Value) > 0.5f || this.device.DPadLeft.WasPressed || this.device.DPadRight.WasPressed)) || (this.device == null && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)))) && this.counter > 0.2f)
            {
                // change face
                if ((this.device != null && (this.device.DeviceClass == InputDeviceClass.Controller) && this.device.LeftStickX.Value > 0.5f) || (this.device == null && (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))))
                {
                    this.currentlySelectedFace++;
                }
                else if ((this.device != null && (this.device.DeviceClass == InputDeviceClass.Controller) && this.device.LeftStickX.Value <= 0.5f) || (this.device == null && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))))
                {
                    this.currentlySelectedFace--;
                }
                bool colorChanged = false;
                int colorIDDelta = 0;
                // change team
                if ((this.device != null && this.device.DPadRight.WasPressed) || ((this.device == null) && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))))
                {
                    //newTeamID = UnityEngine.Mathf.Clamp(this.currentPlayer.teamID + 1, 0, RWFMod.MaxTeamsHardLimit - 1);
                    colorIDDelta = +1;
                    colorChanged = true;
                }
                else if ((this.device != null && this.device.DPadLeft.WasPressed) || ((this.device == null) && (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))))
                {
                    //newTeamID = UnityEngine.Mathf.Clamp(this.currentPlayer.teamID - 1, 0, RWFMod.MaxTeamsHardLimit - 1);
                    colorIDDelta = -1;
                    colorChanged = true;
                }

                if (colorChanged)
                {
                    int newColorID = this.currentPlayer.colorID + colorIDDelta;
                    bool fail = false;

                    // wow this syntax is concerning
                    if (GameModeManager.CurrentHandler.Settings.TryGetValue("allowTeams", out object allowTeamsObj) && !(bool) allowTeamsObj)
                    {
                        // teams not allowed, continue to next colorID() - if the last (or first) colorID() is passed, then just fail to change team
                        while (PlayerManager.instance.players.Select(p => p.colorID()).Contains(newColorID) && newColorID < RWFMod.instance.MaxTeams && newColorID >= 0)
                        {
                            newColorID += colorIDDelta;
                        }

                        fail = newColorID >= RWFMod.instance.MaxTeams || newColorID < 0;
                    }

                    if (!fail && newColorID >= 0)
                    {
                        this.currentPlayer.colorID = newColorID;
                        for (int i = 0; i < this.buttons.Length; i++)
                        {
                            this.buttons[i].transform.GetChild(2).GetChild(0).GetComponent<SpriteRenderer>().color = ExtraPlayerSkins.GetPlayerSkinColors(this.currentPlayer.colorID).color;
                        }
                    }
                }

                this.counter = 0f;
            }
            this.currentlySelectedFace = Mathf.Clamp(this.currentlySelectedFace, 0, this.buttons.Length - 1);

        }

        public int currentlySelectedFace;

        public LobbyCharacter currentPlayer = null;

        public InputDevice device;

        public GameObject getReadyObj;

        private HoverEvent currentButton;

        private PrivateRoomCharacterSelectionInstance[] selectors;

        private HoverEvent[] buttons;

        public bool isReady;

        private float counter;
    }

}
