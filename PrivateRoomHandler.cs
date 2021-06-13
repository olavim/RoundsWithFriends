using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Landfall.Network;
using InControl;
using Steamworks;
using SoundImplementation;
using UnboundLib;
using UnboundLib.Networking;
using UnboundLib.GameModes;

namespace RWF
{
    class PrivateRoomHandler : MonoBehaviourPunCallbacks
    {
        public static PrivateRoomHandler instance;

        private Button readyButton;
        private ListMenuButton readyListButton;
        private TextMeshProUGUI gameModeText;
        private GameObject grid;
        private GameObject readyCheckbox;
        private GameObject waiting;
        private VersusDisplay versusDisplay;
        private bool waitingForToggle;
        private bool lockReadyRequests;
        private Queue<Tuple<int, bool>> readyRequests;
        private bool spamReady;
        private InputDevice deviceToUse;

        public ListMenuPage MainPage { get; private set; }

        private static void OnSceneLoad(Scene scene, LoadSceneMode mode) {
            // Map changes also cause scene loads. We don't want to open the lobby during those...
            if (scene.name == "Main") {
                SceneManager.sceneLoaded -= PrivateRoomHandler.OnSceneLoad;
                PrivateRoomHandler.instance.Open();
            }
        }

        private void Awake() {
            PrivateRoomHandler.instance = this;

            if (RWFMod.DEBUG) {
                this.gameObject.AddComponent<PhotonLagSimulationGui>();
            }
        }

        private void Start() {
            this.Init();
            this.BuildUI();
        }

        private void Init() {
            this.spamReady = false;
            this.waitingForToggle = false;
            this.readyRequests = new Queue<Tuple<int, bool>>();
            this.deviceToUse = null;
            this.lockReadyRequests = false;
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

            var waitingTextGo = GameObject.Instantiate(RoundsResources.FlickeringTextPrefab, this.waiting.transform);
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

            var gameModeGo = new GameObject("GameMode");
            gameModeGo.transform.SetParent(this.grid.transform);
            gameModeGo.transform.localScale = Vector3.one;

            var gameModeTextGo = GetText(GameModeManager.CurrentHandlerID == "Deathmatch" ? "DEATHMATCH" : "TEAM DEATHMATCH");
            gameModeTextGo.transform.SetParent(gameModeGo.transform);
            gameModeTextGo.transform.localScale = Vector3.one;

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

            gameModeGo.AddComponent<RectTransform>();
            gameModeGo.AddComponent<CanvasRenderer>();
            var gameModeLayout = gameModeGo.AddComponent<LayoutElement>();
            gameModeLayout.minHeight = 92;
            var gameModeButton = gameModeGo.AddComponent<Button>();
            var gameModeListButton = gameModeGo.AddComponent<ListMenuButton>();
            gameModeListButton.setBarHeight = 92f;

            gameModeButton.onClick.AddListener(() => {
                if (PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
                {
                    string nextGameMode = GameModeManager.CurrentHandlerID == "ArmsRace" ? "Deathmatch" : "ArmsRace";
                    GameModeManager.SetGameMode(nextGameMode);

                    this.SyncMethod(nameof(PrivateRoomHandler.SetGameSettings), null, GameModeManager.CurrentHandlerID, GameModeManager.CurrentHandler.Settings);
                }
            });

            this.gameModeText = gameModeTextGo.GetComponent<TextMeshProUGUI>();

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

        private void Update() {
            if (RWFMod.DEBUG) {
                if (Input.GetKeyDown(KeyCode.T)) {
                    this.spamReady = !this.spamReady;
                }

                if (this.spamReady) {
                    this.StartCoroutine(this.ToggleReady());
                }
            }

            /* Ready toggle requests are handled by master client in the order they arrive. If at any point all players are ready
             * (even if there would be more toggle requests remaining), the game starts immediately.
             */
            if (this.readyRequests.Count > 0 && !this.lockReadyRequests) {
                var request = this.readyRequests.Dequeue();
                this.HandlePlayerReadyToggle(request.Item1, request.Item2);
            }
        }

        override public void OnJoinedRoom() {
            PhotonNetwork.LocalPlayer.SetProperty("ready", false);
            PhotonNetwork.LocalPlayer.SetProperty("readyOrder", -1);

            if (RWFMod.DEBUG && PhotonNetwork.IsMasterClient) {
                RWFMod.Log($"\n\n\tRoom join command:\n\tjoin:{PhotonNetwork.CloudRegion}:{PhotonNetwork.CurrentRoom.Name}\n");
            }

            if (PhotonNetwork.IsMasterClient) {
                if (GameModeManager.CurrentHandler == null) {
                    GameModeManager.SetGameMode("ArmsRace");
                }
                PrivateRoomHandler.SetGameSettings(GameModeManager.CurrentHandlerID, GameModeManager.CurrentHandler.Settings);
            }

            /* The local player's nickname is also set in NetworkConnectionHandler::OnJoinedRoom, but we'll do it here too so we don't
             * need to worry about timing issues
             */
            if (RWFMod.IsSteamConnected) {
                PhotonNetwork.LocalPlayer.NickName = SteamFriends.GetPersonaName();
            } else {
                PhotonNetwork.LocalPlayer.NickName = $"Player {PhotonNetwork.LocalPlayer.ActorNumber}";
            }

            // If we handled this from OnPlayerEnteredRoom handler for other clients, the joined client's nickname might not have been set yet
            NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.UpdatePlayerDisplay));
            base.OnJoinedRoom();
        }

        override public void OnMasterClientSwitched(Photon.Realtime.Player newMaster) {
            NetworkConnectionHandler.instance.NetworkRestart();
            base.OnMasterClientSwitched(newMaster);
        }

        override public void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer) {
            if (PhotonNetwork.IsMasterClient) {
                NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.SetGameSettings), GameModeManager.CurrentHandlerID, GameModeManager.CurrentHandler.Settings);
            }

            this.ExecuteAfterSeconds(0.1f, () => {
                PrivateRoomHandler.UpdatePlayerDisplay();
            });

            base.OnPlayerEnteredRoom(newPlayer);
        }

        override public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer) {
            this.ClearPendingRequests(otherPlayer.ActorNumber);
            PrivateRoomHandler.UpdatePlayerDisplay();
            base.OnPlayerLeftRoom(otherPlayer);
        }

        private IEnumerator ToggleReady() {
            if (DevConsole.isTyping) {
                yield break;
            }

            while (PhotonNetwork.CurrentRoom == null) {
                yield return null;
            }

            if (this.waitingForToggle) {
                yield break;
            }

            this.waitingForToggle = true;

            var ready = PhotonNetwork.LocalPlayer.GetProperty<bool>("ready");

            if (!ready) {
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

                this.deviceToUse = playerDevice;
            }

            /* Request master client to toggle this client's ready state. The master client will either oblige or, if all players
             * are ready as a consequence of this toggle, start the game immediately.
             */
            NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.RequestReady), PhotonNetwork.LocalPlayer.ActorNumber, !ready);

            while (PhotonNetwork.LocalPlayer.GetProperty<bool>("ready") != !ready) {
                yield return null;
            }

            this.UpdateReadyBox();
            SoundPlayerStatic.Instance.PlayPlayerAdded();

            yield return new WaitForSeconds(0.1f);
            this.waitingForToggle = false;
        }

        private void UpdateReadyBox() {
            var img = this.readyCheckbox.GetComponent<ProceduralImage>();
            img.BorderWidth = PhotonNetwork.LocalPlayer.GetProperty<bool>("ready") ? 0 : 3;
        }

        // Called from PlayerManager after a player has been created.
        public void PlayerJoined(Player player) {
            player.data.isPlaying = false;
        }

        [UnboundRPC]
        public static void SetGameSettings(string gameMode, GameSettings settings) {
            GameModeManager.SetGameMode(gameMode);
            GameModeManager.CurrentHandler.SetSettings(settings);

            PrivateRoomHandler.instance.gameModeText.text = GameModeManager.CurrentHandlerID == "ArmsRace" ? "TEAM DEATHMATCH" : "DEATHMATCH";
            PrivateRoomHandler.UpdatePlayerDisplay();

            NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.SetGameSettingsResponse), PhotonNetwork.LocalPlayer.ActorNumber);
        }

        [UnboundRPC]
        public static void SetGameSettingsResponse(int respondingPlayer) {
            if (PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) {
                PrivateRoomHandler.instance.RemovePendingRequest(respondingPlayer, nameof(PrivateRoomHandler.SetGameSettings));
            }
        }

        [UnboundRPC]
        public static void RequestReady(int askingPlayer, bool ready) {
            if (PhotonNetwork.IsMasterClient) {
                PrivateRoomHandler.instance.readyRequests.Enqueue(new Tuple<int, bool>(askingPlayer, ready));
            }
        }

        [UnboundRPC]
        public static void UpdatePlayerDisplay() {
            var instance = PrivateRoomHandler.instance;

            if (instance == null) {
                return;
            }

            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount >= 2 && !instance.versusDisplay.gameObject.activeSelf) {
                instance.versusDisplay.gameObject.SetActive(true);
                instance.readyListButton.gameObject.SetActive(true);
                instance.waiting.SetActive(false);
                ListMenu.instance.SelectButton(instance.readyListButton);
            } else if ((PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.PlayerCount < 2) && instance.versusDisplay.gameObject.activeSelf) {
                instance.versusDisplay.gameObject.SetActive(false);
                instance.readyListButton.gameObject.SetActive(false);
                instance.waiting.SetActive(true);
            }

            if (instance.versusDisplay.gameObject.activeSelf) {
                instance.versusDisplay.UpdatePlayers();
            }
        }

        private void HandlePlayerReadyToggle(int playerId, bool ready) {
            var player = PhotonNetwork.CurrentRoom.GetPlayer(playerId);

            if (player == null) {
                return;
            }

            int numReady = PhotonNetwork.CurrentRoom.Players.Values.ToList().Where(p => p.GetProperty<bool>("ready")).Count();

            if (ready) {
                numReady++;
            } else {
                numReady--;
            }

            player.SetProperty("ready", ready);
            player.SetProperty("readyOrder", ready ? numReady - 1 : -1);

            if (numReady == PhotonNetwork.CurrentRoom.PlayerCount) {
                this.lockReadyRequests = true;

                // Tell all clients to create their players. The game begins once players have been created.
                this.StartCoroutine(this.StartGamePreparation());
            }

            // If the player unreadied, reassign ready orders so that they grow continuously from 0.
            if (!ready) {
                int nextReadyOrder = 0;

                var readyPlayers = PhotonNetwork.CurrentRoom.Players.Values.ToList()
                    .Where(p => p.GetProperty<bool>("ready"))
                    .OrderBy(p => p.GetProperty<int>("readyOrder"));

                foreach (var readyPlayer in readyPlayers) {
                    readyPlayer.SetProperty("readyOrder", nextReadyOrder);
                    nextReadyOrder++;
                }
            }

            NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.UpdatePlayerDisplay));
        }

        private IEnumerator StartGamePreparation() {
            var players = PhotonNetwork.CurrentRoom.Players.Values.ToList();

            foreach (var player in players.OrderBy(p => p.GetProperty<int>("readyOrder"))) {
                yield return this.SyncMethod(nameof(PrivateRoomHandler.CreatePlayer), player.ActorNumber, player.ActorNumber);
            }

            NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.StartGame));
        }

        [UnboundRPC]
        public static void CreatePlayer(int playerId) {
            if (PhotonNetwork.LocalPlayer.ActorNumber == playerId) {
                var instance = PrivateRoomHandler.instance;
                instance.StartCoroutine(instance.CreatePlayerCoroutine());
            }
        }

        private IEnumerator CreatePlayerCoroutine() {
            this.MainPage.Close();
            MainMenuHandler.instance.Close();
            UIHandler.instance.ShowJoinGameText("LETS GOO!", PlayerSkinBank.GetPlayerSkinColors(1).winText);

            RWFMod.instance.SetSoundEnabled("PlayerAdded", false);
            yield return PlayerAssigner.instance.CreatePlayer(this.deviceToUse, false);
            RWFMod.instance.SetSoundEnabled("PlayerAdded", true);

            NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.CreatePlayerResponse), PhotonNetwork.LocalPlayer.ActorNumber);
        }

        [UnboundRPC]
        public static void CreatePlayerResponse(int respondingPlayer) {
            if (PhotonNetwork.IsMasterClient) {
                PrivateRoomHandler.instance.RemovePendingRequest(respondingPlayer, nameof(PrivateRoomHandler.CreatePlayer));
            }
        }

        [UnboundRPC]
        public static void StartGame() {
            var instance = PrivateRoomHandler.instance;
            instance.StopAllCoroutines();
            GameModeManager.CurrentHandler.StartGame();

            // The main scene is reloaded after the game. After the reload is done, we want to reopen the lobby.
            SceneManager.sceneLoaded += PrivateRoomHandler.OnSceneLoad;

            PhotonNetwork.LocalPlayer.SetProperty("ready", false);
            PhotonNetwork.LocalPlayer.SetProperty("readyOrder", -1);

            instance.UpdateReadyBox();
        }

        public void Open() {
            this.ExecuteAfterFrames(1, () => {
                ListMenu.instance.OpenPage(this.MainPage);
                this.MainPage.Open();
                ArtHandler.instance.NextArt();
                PrivateRoomHandler.UpdatePlayerDisplay();
            });
        }
    }
}
