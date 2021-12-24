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

            this.gameObject.name += " " + name;

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

            this.StartPicking(lobbyCharacter, inputDevice);

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
            this.currentPlayer = null;
            //this.getReadyObj.gameObject.SetActive(false);
            //PlayerManager.instance.RemovePlayers();
        }

        private void OnEnable()
        {
            /*
            if (!base.transform.GetChild(0).gameObject.activeSelf)
            {
                base.GetComponentInChildren<GeneralParticleSystem>(true).gameObject.SetActive(true);
                base.GetComponentInChildren<GeneralParticleSystem>(true).Play();
            }*/
        }

        public void StartPicking(LobbyCharacter pickingCharacter, InputDevice device)
        {
            this.currentPlayer = pickingCharacter;
            this.device = device;
            this.currentlySelectedFace = pickingCharacter.faceID;
            if (this.currentPlayer.IsMine)
            {
                PlayerFace faceToSend = CharacterCreatorHandler.instance.GetFacePreset(this.currentlySelectedFace);
                this.view.RPC(nameof(RPCO_SelectFace), RpcTarget.Others, this.currentlySelectedFace, faceToSend.eyeID, faceToSend.eyeOffset, faceToSend.mouthID, faceToSend.mouthOffset, faceToSend.detailID, faceToSend.detailOffset, faceToSend.detail2ID, faceToSend.detail2Offset);
            }
            else
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
            //this.getReadyObj.gameObject.SetActive(true);
            //this.getReadyObj.GetComponent<TextMeshProUGUI>().text = "";


            //this.transform.GetChild(1).gameObject.SetActive(false);
            //this.transform.GetChild(2).gameObject.SetActive(false);


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

            }

            if (this.transform.GetChild(0).Find("CharacterSelectButtons") != null)
            {
                GameObject go1 = this.transform.GetChild(0).Find("CharacterSelectButtons")?.gameObject;

                UnityEngine.GameObject.Destroy(go1);
            }

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


            // disable all the buttons, except for the currently selected one
            for (int i = 0; i < this.buttons.Length; i++)
            {
                if (i == this.currentlySelectedFace) { continue; }
                this.buttons[i].gameObject.SetActive(false);
            }
            this.buttons[this.currentlySelectedFace].transform.GetChild(4).gameObject.SetActive(true);
            this.buttons[this.currentlySelectedFace].gameObject.SetActive(true);
            this.buttons[this.currentlySelectedFace].GetComponent<PrivateRoomSimulatedSelection>().Select();
            this.buttons[this.currentlySelectedFace].GetComponent<Button>().onClick.Invoke();
            this.buttons[this.currentlySelectedFace].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().Rebuild(CanvasUpdate.Prelayout);

            this.StartCoroutine(this.FinishSetup());
        }
        private IEnumerator FinishSetup()
        {
            yield return new WaitUntil(() => this.buttons[this.currentlySelectedFace].gameObject.activeInHierarchy);
            yield return new WaitForSecondsRealtime(0.1f);
            this.buttons[this.currentlySelectedFace].GetComponent<PrivateRoomSimulatedSelection>().Select();
            this.buttons[this.currentlySelectedFace].GetComponent<Button>().onClick.Invoke();
            this.buttons[this.currentlySelectedFace].transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().Rebuild(CanvasUpdate.Prelayout);

            if (this.currentPlayer.ready)
            {
                this.RPCA_ReadyUp(this.currentPlayer.ready);
            }

            yield break;
        }
        public void ReadyUp(bool ready)
        {
            if (PhotonNetwork.IsMasterClient) { this.view.RPC(nameof(this.RPCA_ReadyUp), RpcTarget.All, ready); }
        }
        [PunRPC]
        public void RPCA_ReadyUp(bool ready)
        {
            this.isReady = ready;
            for (int i = 0; i < this.buttons.Length; i++)
            {
                this.buttons[i].transform.GetChild(4).gameObject.SetActive(true);
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
            if (this.currentPlayer == null || !this.currentPlayer.IsMine || !this.enableInput || this.isReady)
            {
                return;
            }
            /*
            if (this.device == null)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    this.ReadyUp();
                }
            }*/
            /*
            else if ((this.device != null) && (this.device.CommandWasPressed || this.device.Action1.WasPressed) && this.counter > 0f)
            {
                this.ReadyUp();
            }*/
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
                this.currentButton.GetComponent<Button>().onClick.Invoke();
                this.currentButton.transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().Rebuild(CanvasUpdate.Prelayout);
            }
            this.counter += Time.deltaTime;
            int previouslySelectedFace = this.currentlySelectedFace;
            if (((this.device != null && (this.device.DeviceClass == InputDeviceClass.Controller) && (Mathf.Abs(this.device.LeftStickX.Value) > 0.5f || this.device.DPadLeft.WasPressed || this.device.DPadRight.WasPressed)) || (this.device == null && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)))) && this.counter > 0.2f)
            {
                // change face
                if ((this.device != null && (this.device.DeviceClass == InputDeviceClass.Controller) && this.device.LeftStickX.Value > 0.5f) || (this.device == null && (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))))
                {
                    this.currentlySelectedFace++;
                }
                else if ((this.device != null && (this.device.DeviceClass == InputDeviceClass.Controller) && this.device.LeftStickX.Value < -0.5f) || (this.device == null && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))))
                {
                    this.currentlySelectedFace--;
                }
                bool colorChanged = false;
                int colorIDDelta = 0;
                // change team
                if ((this.device != null && this.device.DPadRight.WasPressed) || ((this.device == null) && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))))
                {
                    colorIDDelta = +1;
                    colorChanged = true;
                }
                else if ((this.device != null && this.device.DPadLeft.WasPressed) || ((this.device == null) && (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))))
                {
                    colorIDDelta = -1;
                    colorChanged = true;
                }

                if (colorChanged)
                {
                    // ask the host client for permission to change team
                    this.view.RPC(nameof(this.RPCH_RequestChangeTeam), RpcTarget.MasterClient, colorIDDelta);
                }

                this.counter = 0f;
            }
            this.currentlySelectedFace = Mathf.Clamp(this.currentlySelectedFace, 0, this.buttons.Length - 1);
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
        /*
        private IEnumerator SetFaceCoroutine(HoverEvent button, int eyeID, Vector2 eyeOffset, int mouthID, Vector2 mouthOffset, int detailID, Vector2 detailOffset, int detail2ID, Vector2 detail2Offset)
        {
            yield return new WaitUntil(() => button.gameObject.activeInHierarchy);

            for (int j = 0; j < 3; j++)
            {
                yield return new WaitForEndOfFrame();

                button.GetComponent<CharacterCreatorItemEquipper>().RPCA_SetFace(eyeID, eyeOffset, mouthID, mouthOffset, detailID, detailOffset, detail2ID, detail2Offset);
            }


            yield break;
        }*/

        [PunRPC]
        private void RPCH_RequestChangeTeam(int colorIDDelta)
        {
            // RPCH -> RPC(Host)
            // this is only sent to the host client

            // ask the host if the team can be changed in the direction specified
            int newColorID = this.currentPlayer.colorID + colorIDDelta;

            // wow this syntax is concerning
            if (GameModeManager.CurrentHandler.Settings.TryGetValue("allowTeams", out object allowTeamsObj) && !(bool) allowTeamsObj)
            {
                // teams not allowed, continue to next colorID() - if the last (or first) colorID() is passed, then just fail to change team
                while (PhotonNetwork.CurrentRoom.Players.Select(kv => kv.Value.GetProperty<LobbyCharacter[]>("players")).SelectMany(p => p).Where(p => p != null && p.uniqueID != this.currentPlayer.uniqueID && p.colorID == newColorID).Any() && newColorID < RWFMod.instance.MaxTeams && newColorID >= 0)
                {
                    newColorID += colorIDDelta;
                }
            }

            bool fail = newColorID >= RWFMod.MaxTeamsHardLimit || newColorID < 0;

            if (!fail)
            {
                // approve the request and send it to all clients
                this.view.RPC(nameof(this.RPCA_ChangeTeam), RpcTarget.All, newColorID);
            }
        }
        [PunRPC]
        private void RPCA_ChangeTeam(int newColorID)
        {
            // RPCA -> RPC(All)
            // this is sent to all players

            // host has approved the team change, update across all clients
            this.currentPlayer.colorID = newColorID;
            PrivateRoomHandler.instance.FindLobbyCharacter(this.currentPlayer.actorID, this.currentPlayer.localID).colorID = newColorID;
            this.buttons = this.transform.GetComponentsInChildren<HoverEvent>(true);
            for (int i = 0; i < this.buttons.Length; i++)
            {
                this.buttons[i].transform.GetChild(2).GetChild(0).GetComponent<SpriteRenderer>().color = ExtraPlayerSkins.GetPlayerSkinColors(this.currentPlayer.colorID).color;
            }
            if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.UpdateVersusDisplay));
            }
        }
        
        /*
        [PunRPC]
        private void RPCS_SendFaces(int requestingID)
        {
            UnityEngine.Debug.Log("REQUEST TO SEND FACES FROM: " + requestingID.ToString());

            // RPCS -> RPC(Specific) or RPC(Single)
            // this is only sent to the player who owns this instance, but just to be sure it is checked here
            if (!(this?.currentPlayer?.IsMine ?? false)) { return; }

            // set the face for all portraits for all other players
            this.buttons = this.transform.GetComponentsInChildren<HoverEvent>(true);


            for (int i = 0; i < this.buttons.Count(); i++)
            {
                PlayerFace faceToSend = this.buttons[i].GetComponent<CharacterCreatorPortrait>().myFace;
                this.view.RPC(nameof(RPCO_SetFace), PhotonNetwork.CurrentRoom.GetPlayer(requestingID), i, faceToSend.eyeID, faceToSend.eyeOffset, faceToSend.mouthID, faceToSend.mouthOffset, faceToSend.detailID, faceToSend.detailOffset, faceToSend.detail2ID, faceToSend.detail2Offset);
            }
            

        }
        [PunRPC]
        public void RPCO_SetFace(int index, int eyeID, Vector2 eyeOffset, int mouthID, Vector2 mouthOffset, int detailID, Vector2 detailOffset, int detail2ID, Vector2 detail2Offset)
        {
            UnityEngine.Debug.Log("RECEIVED FACE FOR INDEX: " + index.ToString());

            // RPCS -> RPC(Specific) or RPC(Single)
            // this is sent to the player that requested RPCS_SendFaces

            this.buttons = this.transform.GetComponentsInChildren<HoverEvent>(true);
            //this.buttons[index].GetComponent<CharacterCreatorPortrait>().myFace = face;
            //this.buttons[index].GetComponent<CharacterCreatorItemEquipper>().RPCA_SetFace(eyeID, eyeOffset, mouthID, mouthOffset, detailID, detailOffset, detail2ID, detail2Offset);
            this.StartCoroutine(this.SetFaceCoroutine(this.buttons[index], eyeID, eyeOffset, mouthID, mouthOffset, detailID, detailOffset, detail2ID, detail2Offset));
        }*/


        public int currentlySelectedFace;

        public LobbyCharacter currentPlayer = null;

        public InputDevice device;

        public GameObject getReadyObj;

        private HoverEvent currentButton;

        private HoverEvent[] buttons;

        public bool isReady;

        private float counter;

        private bool enableInput = true;
    }

}
