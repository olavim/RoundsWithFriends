using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnboundLib.GameModes;
using InControl;
using RWF.Patches;
using System.Linq;
using Photon.Pun;
using UnboundLib;
using System.Collections;
using UnityEngine.UI.ProceduralImage;
using System.Reflection;
using UnboundLib.Utils;

namespace RWF.UI
{
    static class Colors
    {
        public static Color Transparent(Color color, float a = 0.5f)
        {
            return new Color(color.r, color.g, color.b, a);
        }
        public static Color readycolor = new Color(0.2f, 0.8f, 0.1f, 1f);
        public static Color createdColor = new Color(0.9f, 0f, 0.1f, 1f);
        public static Color joinedcolor = new Color(0.566f, 0.566f, 0.566f, 1f);
        public static Color myBorderColor = Color.white;
        public static Color othersBorderColor = Color.clear;
    }
    [RequireComponent(typeof(PhotonView))]
    public class PrivateRoomCharacterSelectionInstance : MonoBehaviour, IPunInstantiateMagicCallback
    {
        private PhotonView view => this.gameObject.GetComponent<PhotonView>();

        public void OnPhotonInstantiate(Photon.Pun.PhotonMessageInfo info)
        {
            RWFMod.instance.StartCoroutine(this.Instantiate(info));
        }
        private IEnumerator Instantiate(Photon.Pun.PhotonMessageInfo info)
        {
            // info[0] will be the actorID of the player and info[1] will be the localID of the player
            // info[2] will be the name of the player picking, purely to assign this gameobject's new name
            object[] instantiationData = info.photonView.InstantiationData;

            int actorID = (int) instantiationData[0];
            int localID = (int) instantiationData[1];
            string name = (string) instantiationData[2];

            yield return new WaitUntil(() =>
            {
                return PhotonNetwork.CurrentRoom != null
                    && PhotonNetwork.CurrentRoom.GetPlayer(actorID).GetProperty<LobbyCharacter[]>("players") != null
                    && PhotonNetwork.CurrentRoom.GetPlayer(actorID).GetProperty<LobbyCharacter[]>("players").Count() > localID
                    && PhotonNetwork.CurrentRoom.GetPlayer(actorID).GetProperty<LobbyCharacter[]>("players")[localID] != null
                    && (PhotonNetwork.LocalPlayer.ActorNumber != actorID || PrivateRoomHandler.instance.devicesToUse.Count() > localID);
            });

            LobbyCharacter lobbyCharacter = PhotonNetwork.CurrentRoom.GetPlayer(actorID).GetProperty<LobbyCharacter[]>("players")[localID];

            if (lobbyCharacter == null)
            {
                yield break;
            }

            this.gameObject.name += " " + name;

            if (lobbyCharacter.IsMine && !PrivateRoomHandler.instance.devicesToUse.ContainsKey(localID))
            {
                PhotonNetwork.Destroy(this.gameObject);
                yield break;
            }
            InputDevice inputDevice = lobbyCharacter.IsMine ? PrivateRoomHandler.instance.devicesToUse[localID] : null;

            VersusDisplay.instance.SetPlayerSelectorGO(lobbyCharacter.uniqueID, this.gameObject);

            VersusDisplay.instance.TeamGroupGO(lobbyCharacter.teamID, lobbyCharacter.colorID).SetActive(true);
            VersusDisplay.instance.PlayerGO(lobbyCharacter.uniqueID).SetActive(true);
            this.transform.SetParent(VersusDisplay.instance.PlayerGO(lobbyCharacter.uniqueID).transform);

            this.transform.localScale = Vector3.one;
            this.transform.localPosition = Vector3.zero;

            this.buttons = this.transform.GetComponentsInChildren<HoverEvent>(true);
            for (int i = 0; i < this.buttons.Length; i++)
            {
                if (this.buttons[i].GetComponent<SimulatedSelection>() != null)
                {
                    UnityEngine.GameObject.DestroyImmediate(this.buttons[i].GetComponent<SimulatedSelection>());
                }
                this.buttons[i].gameObject.GetOrAddComponent<PrivateRoomSimulatedSelection>().InvokeMethod("Start");
            }

            this.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = name;

            this.StartPicking(lobbyCharacter.uniqueID, inputDevice);

            yield break;
        }
        private void Start()
        {

            this.transform.GetChild(0).localPosition = Vector2.zero;

            this.buttons = this.transform.GetComponentsInChildren<HoverEvent>(true);
            for (int i = 0; i < this.buttons.Length; i++)
            {
                if (this.buttons[i].GetComponent<SimulatedSelection>() != null)
                {
                    UnityEngine.GameObject.DestroyImmediate(this.buttons[i].GetComponent<SimulatedSelection>());
                }
                this.buttons[i].gameObject.GetOrAddComponent<PrivateRoomSimulatedSelection>().InvokeMethod("Start");
            }
        }

        public void ResetMenu()
        {
            base.transform.GetChild(0).gameObject.SetActive(false);
            this.uniqueID = 1;

        }

        private void OnEnable()
        {

        }

        public void StartPicking(int uniqueID, InputDevice device)
        {
            this.uniqueID = uniqueID;
            this.colorID = this.currentPlayer.colorID;
            this.device = device;
            this.currentlySelectedFace = this.currentPlayer.faceID;
            if (!this.currentPlayer.IsMine)
            {
                this.view.RPC(nameof(RPCS_RequestSelectedFace), this.currentPlayer.networkPlayer, PhotonNetwork.LocalPlayer.ActorNumber);
            }
            try
            {
                this.GetComponentInChildren<GeneralParticleSystem>(true).gameObject.SetActive(false);
                this.GetComponentInChildren<GeneralParticleSystem>(true).Stop();
            }
            catch { }

            this.transform.GetChild(0).gameObject.SetActive(true);

            this.buttons = this.transform.GetComponentsInChildren<HoverEvent>(true);
            for (int i = 0; i < this.buttons.Length; i++)
            {

                this.buttons[i].gameObject.GetOrAddComponent<PrivateRoomSimulatedSelection>().InvokeMethod("Start");

                this.buttons[i].enabled = false;
                this.buttons[i].GetComponent<Button>().interactable = false;
                this.buttons[i].GetComponent<CharacterCreatorPortrait>().controlType = MenuControllerHandler.MenuControl.Controller;

                this.buttons[i].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
                this.buttons[i].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                this.buttons[i].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().fontSize = 25f;
                this.buttons[i].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().autoSizeTextContainer = true;
                this.buttons[i].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().overflowMode = TextOverflowModes.Ellipsis;
                this.buttons[i].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().enableWordWrapping = false;
                this.buttons[i].transform.GetChild(3).GetChild(0).localPosition -= new Vector3(this.buttons[i].transform.GetChild(3).GetChild(0).localPosition.x, -25f, 0f);
                this.buttons[i].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().Rebuild(CanvasUpdate.Prelayout);
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

                // update the border color and width
                this.buttons[i].transform.GetChild(1).GetComponent<ProceduralImage>().color = this.currentPlayer.IsMine ? Colors.myBorderColor : Colors.othersBorderColor;
                this.buttons[i].transform.GetChild(1).GetComponent<ProceduralImage>().BorderWidth = 3f;
                this.buttons[i].transform.GetChild(1).GetComponent<ProceduralImage>().FalloffDistance = 3f;

                if (this.currentPlayer.IsMine)
                {
                    // set "playerID" so that preferences will be updated when changed
                    this.buttons[i].GetComponentInChildren<CharacterCreatorPortrait>().playerId = this.currentPlayer.localID;
                }


            }

            if (this.transform.GetChild(0).Find("CharacterSelectButtons") != null)
            {
                GameObject go1 = this.transform.GetChild(0).Find("CharacterSelectButtons")?.gameObject;

                UnityEngine.GameObject.Destroy(go1);
            }

            /*
            GameObject characterSelectButtons = new GameObject("CharacterSelectButtons");
            characterSelectButtons.transform.SetParent(this.transform.GetChild(0));
            GameObject leftarrow = new GameObject("LeftArrow", typeof(PrivateRoomCharacterSelectButton));
            leftarrow.transform.SetParent(characterSelectButtons.transform);
            GameObject rightarrow = new GameObject("RightArrow", typeof(PrivateRoomCharacterSelectButton));
            rightarrow.transform.SetParent(characterSelectButtons.transform);

            characterSelectButtons.transform.localScale = Vector3.one;
            characterSelectButtons.transform.localPosition = Vector3.zero;

            leftarrow.transform.localScale = new Vector3(1f, 3f, 1f);
            leftarrow.transform.localPosition = new Vector3(-60f, 0f, 0f);
            leftarrow.GetComponent<PrivateRoomCharacterSelectButton>().SetCharacterSelectionInstance(this);
            leftarrow.GetComponent<PrivateRoomCharacterSelectButton>().SetDirection(PrivateRoomCharacterSelectButton.LeftRight.Left);
            rightarrow.transform.localScale = new Vector3(1f, 3f, 1f);
            rightarrow.transform.localPosition = new Vector3(60f, 0f, 0f);
            rightarrow.GetComponent<PrivateRoomCharacterSelectButton>().SetCharacterSelectionInstance(this);
            rightarrow.GetComponent<PrivateRoomCharacterSelectButton>().SetDirection(PrivateRoomCharacterSelectButton.LeftRight.Right);
            */


            // disable all the buttons, except for the currently selected one
            for (int i = 0; i < this.buttons.Length; i++)
            {
                if (i == this.currentlySelectedFace) { continue; }
                this.buttons[i].gameObject.SetActive(false);
            }
            this.buttons[this.currentlySelectedFace].transform.GetChild(4).gameObject.SetActive(true);
            this.buttons[this.currentlySelectedFace].gameObject.SetActive(true);
            this.buttons[this.currentlySelectedFace].GetComponent<PrivateRoomSimulatedSelection>().Select();
            if (this.currentPlayer.IsMine) { this.buttons[this.currentlySelectedFace].GetComponent<Button>().onClick.Invoke(); }
            this.buttons[this.currentlySelectedFace].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().Rebuild(CanvasUpdate.Prelayout);

            this.StartCoroutine(this.FinishSetup());
        }
        private IEnumerator FinishSetup()
        {
            yield return new WaitUntil(() => this?.buttons == null || this.buttons[this.currentlySelectedFace]?.gameObject == null || this.buttons[this.currentlySelectedFace].gameObject.activeInHierarchy);
            yield return new WaitForSecondsRealtime(0.1f);
            if (this?.buttons == null || this.buttons[this.currentlySelectedFace]?.gameObject == null || this.currentPlayer == null)
            {
                yield break;
            }
            this.buttons[this.currentlySelectedFace].GetComponent<PrivateRoomSimulatedSelection>().Select();
            if (this.currentPlayer.IsMine) { this.buttons[this.currentlySelectedFace].GetComponent<Button>().onClick.Invoke(); }
            this.buttons[this.currentlySelectedFace].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().Rebuild(CanvasUpdate.Prelayout);

            yield break;
        }
        public void UpdateReadyVisuals()
        {
            for (int i = 0; i < this.buttons.Length; i++)
            {
                this.buttons[i].transform.GetChild(4).gameObject.SetActive(true);
                this.buttons[i].transform.GetChild(4).GetChild(0).gameObject.SetActive(this.isReady || this.created);
                this.buttons[i].transform.GetChild(4).GetChild(1).gameObject.SetActive(this.isReady || this.created);
                foreach (Graphic graphic in this.buttons[i].transform.GetChild(4).GetChild(0).GetComponentsInChildren<Graphic>(true))
                {
                    graphic.color = this.created ? Colors.Transparent(Colors.createdColor) : this.isReady ? Colors.Transparent(Colors.readycolor) : Color.clear;
                }
                foreach (Graphic graphic in this.buttons[i].transform.GetChild(4).GetChild(1).GetComponentsInChildren<Graphic>(true))
                {
                    graphic.color = this.created ? Colors.Transparent(Colors.createdColor) : this.isReady ? Colors.Transparent(Colors.readycolor) : Color.clear;
                }
                this.buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().text = this.created ? "IN GAME" : this.isReady ? "READY" : "";
                this.buttons[i].transform.GetChild(4).GetChild(2).GetComponent<TextMeshProUGUI>().color = this.created ? Colors.createdColor : this.isReady ? Colors.readycolor : Colors.joinedcolor;
            }
        }
        public void Created()
        {
            this.created = true;
            if (this.currentPlayer.IsMine) { this.view.RPC(nameof(this.RPCA_Created), RpcTarget.All); }
        }

        [PunRPC]
        public void RPCA_Created()
        {
            this.created = true;
        }
        private void Update()
        {
            if (PrivateRoomHandler.instance == null || PhotonNetwork.CurrentRoom == null || this.currentPlayer == null)
            {
                return;
            }

            this.UpdateReadyVisuals();

            if (!this.currentPlayer.IsMine)
            {
                this.colorID = this.currentPlayer.colorID;
                this.UpdateFaceColors();
                return;
            }
            else if (this.lastChangedTeams > 0f && Time.realtimeSinceStartup - this.lastChangedTeams >= 2f * PhotonNetwork.GetPing()*0.001f)
            {
                if (this.colorID != this.currentPlayer.colorID)
                {
                    this.ChangeToTeam(this.colorID);
                }
            }
            if (!this.currentPlayer.IsMine || !this.enableInput || this.isReady) { return; }

            HoverEvent component = this.buttons[this.currentlySelectedFace].GetComponent<HoverEvent>();
            if (this.currentButton != component)
            {
                if (this.currentButton)
                {
                    this.currentButton.GetComponent<PrivateRoomSimulatedSelection>().Deselect();
                    this.currentButton.gameObject.SetActive(false);
                }
                else
                {
                    for (int i = 0; i < this.buttons.Length; i++)
                    {
                        if (i == this.currentlySelectedFace) { continue; }
                        this.buttons[i].GetComponent<PrivateRoomSimulatedSelection>().Deselect();
                        this.buttons[i].gameObject.SetActive(false);
                    }
                }
                this.currentButton = component;
                this.currentButton.transform.GetChild(4).gameObject.SetActive(true);
                this.currentButton.gameObject.SetActive(true);
                this.currentButton.GetComponent<PrivateRoomSimulatedSelection>().Select();
                if (this.currentPlayer.IsMine) { this.currentButton.GetComponent<Button>().onClick.Invoke(); }
                this.currentButton.transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().Rebuild(CanvasUpdate.Prelayout);
            }
            int previouslySelectedFace = this.currentlySelectedFace;
            if (((this.device != null && (this.device.DeviceClass == InputDeviceClass.Controller) && (this.device.Direction.Left.WasPressed || this.device.Direction.Right.WasPressed || this.device.DPadLeft.WasPressed || this.device.DPadRight.WasPressed|| this.device.RightBumper.WasPressed || this.device.LeftBumper.WasPressed)) || (this.device == null && (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow)))))
            {
                // change face
                if ((this.device != null && (this.device.DeviceClass == InputDeviceClass.Controller) && this.device.RightBumper.WasPressed) || (this.device == null && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.UpArrow))))
                {
                    this.currentlySelectedFace++;
                }
                else if ((this.device != null && (this.device.DeviceClass == InputDeviceClass.Controller) && this.device.LeftBumper.WasPressed) || (this.device == null && (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.DownArrow))))
                {
                    this.currentlySelectedFace--;
                }
                bool colorChanged = false;
                int colorIDDelta = 0;
                // change team
                if (this.device != null && ((this.device.DeviceClass == InputDeviceClass.Controller) && (this.device.Direction.Right.WasPressed || this.device.DPadRight.WasPressed)) || ((this.device == null) && (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))))
                {
                    colorIDDelta = +1;
                    colorChanged = true;
                }
                else if (this.device != null && ((this.device.DeviceClass == InputDeviceClass.Controller) && (this.device.Direction.Left.WasPressed || this.device.DPadLeft.WasPressed)) || ((this.device == null) && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))))
                {
                    colorIDDelta = -1;
                    colorChanged = true;
                }

                if (colorChanged)
                {
                    // ask the host client for permission to change team
                    //this.view.RPC(nameof(this.RPCH_RequestChangeTeam), RpcTarget.MasterClient, colorIDDelta);
                    this.ChangeTeam(colorIDDelta);
                }

            }
            this.currentlySelectedFace = Math.mod(this.currentlySelectedFace, this.buttons.Length);
            if (this.currentlySelectedFace != previouslySelectedFace)
            {
                this.currentPlayer.faceID = this.currentlySelectedFace;
                LobbyCharacter[] characters = PhotonNetwork.LocalPlayer.GetProperty<LobbyCharacter[]>("players");
                characters[this.currentPlayer.localID] = this.currentPlayer;
                PhotonNetwork.LocalPlayer.SetProperty("players", characters);
                PlayerFace faceToSend = CharacterCreatorHandler.instance.GetFacePreset(this.currentlySelectedFace);
                this.view.RPC(nameof(RPCO_SelectFace), RpcTarget.Others, this.currentlySelectedFace, faceToSend.eyeID, faceToSend.eyeOffset, faceToSend.mouthID, faceToSend.mouthOffset, faceToSend.detailID, faceToSend.detailOffset, faceToSend.detail2ID, faceToSend.detail2Offset);
            }
        }
        public void SetInputEnabled(bool enabled)
        {
            this.enableInput = enabled;
        }
        
        [PunRPC]
        private void RPCS_RequestSelectedFace(int askerID)
        {
            PlayerFace faceToSend = CharacterCreatorHandler.instance.GetFacePreset(this.currentlySelectedFace);
            this.view.RPC(nameof(RPCO_SelectFace), PhotonNetwork.CurrentRoom.GetPlayer(askerID), this.currentlySelectedFace, faceToSend.eyeID, faceToSend.eyeOffset, faceToSend.mouthID, faceToSend.mouthOffset, faceToSend.detailID, faceToSend.detailOffset, faceToSend.detail2ID, faceToSend.detail2Offset);
        }
        [PunRPC]
        private void RPCO_SelectFace(int faceID, int eyeID, Vector2 eyeOffset, int mouthID, Vector2 mouthOffset, int detailID, Vector2 detailOffset, int detail2ID, Vector2 detail2Offset)
        {
            this.currentPlayer.faceID = faceID;
            this.buttons = this.transform.GetComponentsInChildren<HoverEvent>(true);
            for (int i = 0; i < this.buttons.Length; i++)
            {
                if (i == faceID)
                {
                    this.buttons[i].gameObject.SetActive(true);
                    this.StartCoroutine(this.SelectFaceCoroutine(this.buttons[i], eyeID, eyeOffset, mouthID, mouthOffset, detailID, detailOffset, detail2ID, detail2Offset));
                }
                else
                {
                    this.buttons[i].GetComponent<PrivateRoomSimulatedSelection>().Deselect();
                    this.buttons[i].gameObject.SetActive(false);
                }
            }
        }
        private IEnumerator SelectFaceCoroutine(HoverEvent button, int eyeID, Vector2 eyeOffset, int mouthID, Vector2 mouthOffset, int detailID, Vector2 detailOffset, int detail2ID, Vector2 detail2Offset)
        {
            yield return new WaitUntil(() => button.gameObject.activeInHierarchy);

            button.GetComponent<PrivateRoomSimulatedSelection>().Select();
            button.transform.GetChild(1).gameObject.SetActive(true);
            button.transform.GetChild(4).gameObject.SetActive(true);
            button.transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().Rebuild(CanvasUpdate.Prelayout);
            button.GetComponent<CharacterCreatorItemEquipper>().RPCA_SetFace(eyeID, eyeOffset, mouthID, mouthOffset, detailID, detailOffset, detail2ID, detail2Offset);
            

            yield break;
        }

        private void ChangeTeam(int colorIDDelta)
        {
            int newColorID = this.colorID + colorIDDelta;
            int orig = this.colorID;

            if (!GameModeManager.CurrentHandler.AllowTeams)
            {
                // teams not allowed, continue to next colorID
                while (PrivateRoomHandler.instance.PrivateRoomCharacters.Where(p => p != null && p.uniqueID != this.currentPlayer.uniqueID && p.colorID == newColorID).Any() && newColorID < RWFMod.MaxColorsHardLimit && newColorID >= 0)
                {
                    newColorID = newColorID + colorIDDelta;
                    if (newColorID == orig || newColorID >= RWFMod.MaxColorsHardLimit || newColorID < 0)
                    {
                        // make sure it's impossible to get stuck in an infinite loop here,
                        // even though prior logic limiting the number of players should prevent this
                        break;
                    }
                }
            }

            bool fail = newColorID == orig || newColorID >= RWFMod.MaxColorsHardLimit || newColorID < 0;

            if (!fail)
            {
                this.ChangeToTeam(newColorID);
            }
        }

        private void ChangeToTeam(int newColorID)
        {
            // send the team change to all clients
            if (!this.currentPlayer.IsMine) { return; }

            this.lastChangedTeams = Time.realtimeSinceStartup;

            this.colorID = newColorID;

            LobbyCharacter character = PrivateRoomHandler.instance.FindLobbyCharacter(this.currentPlayer.uniqueID);
            character.colorID = newColorID;
            LobbyCharacter[] characters = PhotonNetwork.LocalPlayer.GetProperty<LobbyCharacter[]>("players");
            characters[character.localID] = character;
            PhotonNetwork.LocalPlayer.SetProperty("players", characters);

            this.UpdateFaceColors();

        }

        public void UpdateFaceColors()
        {
            // set player color
            if (this.transform.GetComponentsInChildren<HoverEvent>(true).Any())
            {
                this.buttons = this.transform.GetComponentsInChildren<HoverEvent>(true);
                for (int i = 0; i < this.buttons.Length; i++)
                {
                    this.buttons[i].transform.GetChild(2).GetChild(0).GetComponent<SpriteRenderer>().color = ExtraPlayerSkins.GetPlayerSkinColors(this.colorID).color;
                }
            }
        }

        [PunRPC]
        internal void RPCA_ChangeTeam(int newColorID)
        {
            if (this.currentPlayer.IsMine) { this.ChangeToTeam(newColorID); }

            return;

        }

        public int currentlySelectedFace;

        public LobbyCharacter currentPlayer => PrivateRoomHandler.instance.FindLobbyCharacter(this.uniqueID);

        public int uniqueID = 1;
        private int _colorID = -1;
        public int colorID
        {
            get
            {
                return this._colorID;
            }
            private set
            {
                this._colorID = value;
            }
        }

        public InputDevice device;

        public GameObject getReadyObj;

        private HoverEvent currentButton;

        private HoverEvent[] buttons;

        public bool isReady
        {
            get
            {
                if (this.currentPlayer != null)
                {
                    return this.currentPlayer.ready;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool created = false;

        private float lastChangedTeams = -1f;

        private bool enableInput = true;
    }

}
