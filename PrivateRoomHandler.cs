using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
using TMPro;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Landfall.Network;
using InControl;
using Steamworks;

namespace RWF
{
    class PrivateRoomHandler : MonoBehaviourPunCallbacks
    {

        private Button readyButton;
        private ListMenuButton readyListButton;
        private GameObject grid;
        private GameObject readyCheckbox;
        private GameObject waiting;
        private GameObject gameMode;
        private VersusDisplay versusDisplay;
        private bool ready = false;
        private Dictionary<int, bool> waitingForResponse = new Dictionary<int, bool>();

        public static PrivateRoomHandler instance;

        public ListMenuPage MainPage { get; private set; }

        private void Awake() {
            PrivateRoomHandler.instance = this;
            this.gameObject.AddComponent<PhotonView>();

            if (RWFMod.DEBUG) {
                this.gameObject.AddComponent<PhotonLagSimulationGui>();
            }
        }

        private void Start() {
            this.gameMode = GameObject.Find("/Game/Code/Game Modes").transform.Find("[GameMode] Arms race").gameObject;
            this.BuildUI();
        }

        private void BuildUI() {
            var rect = this.gameObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            var mainPageGo = new GameObject("Main");
            mainPageGo.transform.SetParent(this.transform);
            mainPageGo.transform.localScale = Vector3.one;

            this.grid = new GameObject("Group");
            this.grid.transform.SetParent(mainPageGo.transform);
            this.grid.transform.localScale = Vector3.one;

            var playersGo = new GameObject("Players");
            playersGo.transform.SetParent(this.grid.transform);
            playersGo.transform.localScale = Vector3.one;
            playersGo.transform.localPosition += new Vector3(0, 300, 0);

            this.waiting = new GameObject("Waiting");
            this.waiting.transform.SetParent(this.grid.transform);
            this.waiting.transform.localScale = Vector3.one;
            this.waiting.transform.localPosition += new Vector3(0, 300, 0);

            var waitingTextGo = GameObject.Instantiate(RoundsResources.FlickeringTextPrefab);
            waitingTextGo.transform.SetParent(this.waiting.transform);
            waitingTextGo.transform.localScale = Vector3.one;
            waitingTextGo.transform.localPosition = Vector3.zero;

            var divGo1 = new GameObject("Divider1");
            divGo1.transform.SetParent(this.grid.transform);
            divGo1.transform.localScale = Vector3.one;

            var readyGo = new GameObject("Ready");
            readyGo.transform.SetParent(this.grid.transform);
            readyGo.transform.localScale = Vector3.one;

            var readyTextGo = GetText("READY");
            readyTextGo.transform.SetParent(readyGo.transform);
            readyTextGo.transform.localScale = Vector3.one;

            this.readyCheckbox = new GameObject("Checkbox");
            this.readyCheckbox.transform.SetParent(readyGo.transform);
            this.readyCheckbox.transform.localScale = Vector3.one;

            var inviteGo = new GameObject("Invite");
            inviteGo.transform.SetParent(this.grid.transform);
            inviteGo.transform.localScale = Vector3.one;

            var inviteTextGo = GetText("INVITE");
            inviteTextGo.transform.SetParent(inviteGo.transform);
            inviteTextGo.transform.localScale = Vector3.one;

            var backGo = new GameObject("Back");
            backGo.transform.SetParent(this.grid.transform);
            backGo.transform.localScale = Vector3.one;

            var backTextGo = GetText("BACK");
            backTextGo.transform.SetParent(backGo.transform);
            backTextGo.transform.localScale = Vector3.one;

            var playersGoRect = playersGo.AddComponent<RectTransform>();
            var playersGoLayout = playersGo.AddComponent<LayoutElement>();
            this.versusDisplay = playersGo.AddComponent<VersusDisplay>();
            playersGoLayout.ignoreLayout = true;
            playersGoRect.sizeDelta = new Vector2(900, 300);

            var waitingGoRect = this.waiting.AddComponent<RectTransform>();
            var waitingGoLayout = this.waiting.AddComponent<LayoutElement>();
            var waitingText = waitingTextGo.GetComponent<TextMeshProUGUI>();
            waitingText.text = "WAITING FOR PLAYERS...";
            waitingText.fontSize = 80;
            waitingGoLayout.ignoreLayout = true;
            waitingGoRect.sizeDelta = new Vector2(900, 300);

            readyGo.AddComponent<RectTransform>();
            readyGo.AddComponent<CanvasRenderer>();
            var readyLayout = readyGo.AddComponent<LayoutElement>();
            readyLayout.minHeight = 92;
            this.readyButton = readyGo.AddComponent<Button>();
            this.readyListButton = readyGo.AddComponent<ListMenuButton>();
            this.readyListButton.setBarHeight = 92f;

            this.readyButton.onClick.AddListener(() => this.StartCoroutine(this.ToggleReady()));

            var readyBoxRect = this.readyCheckbox.AddComponent<RectTransform>();
            var readyBoxImage = this.readyCheckbox.AddComponent<ProceduralImage>();
            var readyBoxModifier = this.readyCheckbox.AddComponent<UniformModifier>();
            readyBoxRect.sizeDelta = new Vector2(30, 30);
            readyBoxImage.BorderWidth = 3;
            readyBoxImage.color = new Color32(255, 255, 255, 222); // Slightly glowing white
            readyBoxModifier.Radius = 3;
            this.readyCheckbox.transform.localPosition += new Vector3(150, 0, 0);

            inviteGo.AddComponent<RectTransform>();
            inviteGo.AddComponent<CanvasRenderer>();
            var inviteLayout = inviteGo.AddComponent<LayoutElement>();
            inviteLayout.minHeight = 92;
            var inviteButton = inviteGo.AddComponent<Button>();
            var inviteListButton = inviteGo.AddComponent<ListMenuButton>();
            inviteListButton.setBarHeight = 92f;

            inviteButton.onClick.AddListener(() => {
                var field = typeof(NetworkConnectionHandler).GetField("m_SteamLobby", BindingFlags.Static | BindingFlags.NonPublic);
                var lobby = (ClientSteamLobby) field.GetValue(null);
                lobby.ShowInviteScreenWhenConnected();
            });

            divGo1.AddComponent<RectTransform>();

            backGo.AddComponent<RectTransform>();
            backGo.AddComponent<CanvasRenderer>();
            var backLayout = backGo.AddComponent<LayoutElement>();
            backLayout.minHeight = 92;
            var backButton = backGo.AddComponent<Button>();

            backButton.onClick.AddListener(() => {
                NetworkConnectionHandler.instance.NetworkRestart();
            });

            var backListButton = backGo.AddComponent<ListMenuButton>();
            backListButton.setBarHeight = 92f;

            var mainPageGroupRect = this.grid.AddComponent<RectTransform>();
            var mainPageGroupFitter = this.grid.AddComponent<ContentSizeFitter>();
            mainPageGroupFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
            this.grid.AddComponent<CanvasRenderer>();
            var gridLayout = this.grid.AddComponent<VerticalLayoutGroup>();
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            mainPageGo.AddComponent<RectTransform>();
            this.MainPage = mainPageGo.AddComponent<ListMenuPage>();
            this.MainPage.firstSelected = inviteListButton;
            this.MainPage.Close();

            readyGo.SetActive(false);
            playersGo.SetActive(false);
        }

        private GameObject GetText(string str) {
            var textGo = new GameObject("Text");

            textGo.AddComponent<CanvasRenderer>();
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = str;
            text.color = new Color32(230, 230, 230, 255);
            text.font = RoundsResources.MenuFont;
            text.fontSize = 60;
            text.fontWeight = FontWeight.Regular;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(2050, 92);

            return textGo;
        }

        override public void OnJoinedRoom() {
            var view = this.gameObject.GetComponent<PhotonView>();
            var viewIdKey = RWFMod.GetCustomPropertyKey("privateRoomViewId");

            if (PhotonNetwork.IsMasterClient) {
                PhotonNetwork.AllocateViewID(view);
                var props = PhotonNetwork.CurrentRoom.CustomProperties;
                props.Add(viewIdKey, view.ViewID);
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);

                RWFMod.Log($"\n\n\tRoom join command:\n\tjoin:{PhotonNetwork.CloudRegion}:{PhotonNetwork.CurrentRoom.Name}\n");
            } else {
                view.ViewID = (int) PhotonNetwork.CurrentRoom.CustomProperties[viewIdKey];
            }

            this.gameMode.SetActive(true);

            /* The local player's nickname is also set in NetworkConnectionHandler::OnJoinedRoom, but we'll do it here too so we don't
             * need to worry about timing issues
             */
            if (RWFMod.IsSteamConnected) {
                PhotonNetwork.LocalPlayer.NickName = SteamFriends.GetPersonaName();
            } else {
                PhotonNetwork.LocalPlayer.NickName = $"Player {PhotonNetwork.LocalPlayer.ActorNumber}";
            }

            foreach (var networkPlayer in PhotonNetwork.CurrentRoom.Players.Values.ToList()) {
                this.waitingForResponse.Add(networkPlayer.ActorNumber, false);
            }

            // If we handled this from OnPlayerEnteredRoom handler for other clients, the joined client's nickname might not have been set yet
            view.RPC("UpdatePlayerDisplay", RpcTarget.All);
            base.OnJoinedRoom();
        }

        override public void OnMasterClientSwitched(Photon.Realtime.Player newMaster) {
            NetworkConnectionHandler.instance.NetworkRestart();
            base.OnMasterClientSwitched(newMaster);
        }

        // Compatibility with unmodded clients
        override public void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer) {
            this.waitingForResponse.Add(newPlayer.ActorNumber, false);
            this.StartCoroutine(this.WaitAndUpdatePlayerDisplay());
            base.OnPlayerEnteredRoom(newPlayer);
        }

        override public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer) {
            this.waitingForResponse.Remove(otherPlayer.ActorNumber);
            this.UpdatePlayerDisplay();
            base.OnPlayerLeftRoom(otherPlayer);
        }

        private IEnumerator ToggleReady() {
            if (DevConsole.isTyping) {
                yield break;
            }

            while (PhotonNetwork.CurrentRoom == null) {
                yield return null;
            }

            if (this.IsWaitingForResponse()) {
                yield break;
            }

            foreach (var networkPlayer in PhotonNetwork.CurrentRoom.Players.Values.ToList()) {
                this.waitingForResponse[networkPlayer.ActorNumber] = true;
            }

            if (!this.ready) {
                InputDevice playerDevice = null;

                var m_JoinButtonWasPressedOnDevice = typeof(PlayerAssigner).GetMethod("JoinButtonWasPressedOnDevice", BindingFlags.Instance | BindingFlags.NonPublic);
                var m_ThereIsNoPlayerUsingDevice = typeof(PlayerAssigner).GetMethod("ThereIsNoPlayerUsingDevice", BindingFlags.Instance | BindingFlags.NonPublic);

                for (int i = 0; i < InputManager.ActiveDevices.Count; i++) {
                    InputDevice device = InputManager.ActiveDevices[i];

                    var joinButtonPressed = (bool) m_JoinButtonWasPressedOnDevice.Invoke(PlayerAssigner.instance, new object[] { device });
                    var nobodyUsingDevice = (bool) m_ThereIsNoPlayerUsingDevice.Invoke(PlayerAssigner.instance, new object[] { device });

                    if (joinButtonPressed && nobodyUsingDevice) {
                        playerDevice = device;
                        break;
                    }
                }

                yield return this.CreatePlayer(playerDevice);

                foreach (var networkPlayer in PhotonNetwork.CurrentRoom.Players.Values.ToList()) {
                    this.waitingForResponse[networkPlayer.ActorNumber] = false;
                }
            }

            if (this.ready) {
                this.photonView.RPC("RemovePlayer", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);

                while (this.IsWaitingForResponse()) {
                    yield return null;
                }

                this.UpdatePlayerDisplay();
            }

            this.ready = !this.ready;
            this.UpdateReadyBox();
        }

        private bool IsWaitingForResponse() {
            return this.waitingForResponse.Values.ToList().Any(p => p);
        }

        private void UpdateReadyBox() {
            var img = this.readyCheckbox.GetComponent<ProceduralImage>();
            img.BorderWidth = this.ready ? 0 : 3;
        }

        private IEnumerator CreatePlayer(InputDevice device) {
            yield return PlayerAssigner.instance.CreatePlayer(device, false);
            this.photonView.RPC("UpdatePlayerDisplay", RpcTarget.All);
        }

        public void PlayerJoined(Player player) {
            player.data.isPlaying = false;

            if (PlayerManager.instance.players.Count >= 2 && PlayerManager.instance.players.Count == PhotonNetwork.CurrentRoom.PlayerCount) {
                this.MainPage.Close();
                MainMenuHandler.instance.Close();
                GM_ArmsRace.instance.StartGame();

                this.ready = false;
                this.UpdateReadyBox();
            } else {
                this.UpdatePlayerDisplay();
            }
        }

        [PunRPC]
        private void RemovePlayer(int askingPlayer) {
            var players = PlayerManager.instance.players;

            for (int i = 0; i < players.Count; i++) {
                if (players[i].data.view.OwnerActorNr == askingPlayer) {
                    var m_RemovePlayer = typeof(PlayerAssigner).GetMethod("RemovePlayer", BindingFlags.Instance | BindingFlags.NonPublic);
                    m_RemovePlayer.Invoke(PlayerAssigner.instance, new object[] { players[i].data });

                    // We'll update the player display for the local player after all players have responded
                    if (askingPlayer != PhotonNetwork.LocalPlayer.ActorNumber) {
                        this.UpdatePlayerDisplay();
                    }

                    break;
                }
            }

            this.photonView.RPC("RemovePlayerResponse", PhotonNetwork.CurrentRoom.GetPlayer(askingPlayer), PhotonNetwork.LocalPlayer.ActorNumber);
        }

        [PunRPC]
        private void RemovePlayerResponse(int respondingPlayer) {
            this.waitingForResponse[respondingPlayer] = false;
        }

        private IEnumerator WaitAndUpdatePlayerDisplay() {
            yield return new WaitForSeconds(0.1f);
            this.UpdatePlayerDisplay();
        }

        [PunRPC]
        public void UpdatePlayerDisplay() {
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount >= 2 && !this.versusDisplay.gameObject.activeSelf) {
                this.versusDisplay.gameObject.SetActive(true);
                this.readyListButton.gameObject.SetActive(true);
                this.waiting.SetActive(false);
                ListMenu.instance.SelectButton(this.readyListButton);
            } else if ((PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.PlayerCount < 2) && this.versusDisplay.gameObject.activeSelf) {
                this.versusDisplay.gameObject.SetActive(false);
                this.readyListButton.gameObject.SetActive(false);
                this.waiting.SetActive(true);
            }

            if (this.versusDisplay.gameObject.activeSelf) {
                this.versusDisplay.UpdatePlayers();
            }
        }

        public void Open() {
            ListMenu.instance.OpenPage(this.MainPage);
            this.MainPage.Open();
            this.UpdatePlayerDisplay();
        }
    }
}
