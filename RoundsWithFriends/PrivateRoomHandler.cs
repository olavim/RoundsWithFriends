using InControl;
using Landfall.Network;
using Photon.Pun;
using SoundImplementation;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnboundLib;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
using RWF.UI;

namespace RWF
{
    public static class PrivateRoomPrefabs
    {
        private static GameObject _PrivateRoomCharacterSelectionInstance = null;

        public static GameObject PrivateRoomCharacterSelectionInstance
        {
            get
            {
                if (PrivateRoomPrefabs._PrivateRoomCharacterSelectionInstance != null)
                {
                    return PrivateRoomPrefabs._PrivateRoomCharacterSelectionInstance;
                }

                GameObject orig = UnityEngine.GameObject.Find("Game/UI/UI_MainMenu/Canvas/ListSelector/CharacterSelect/Group").transform.GetChild(0).gameObject;
                GameObject selector = GameObject.Instantiate(orig);
                UnityEngine.GameObject.DontDestroyOnLoad(selector);
                selector.SetActive(true);
                selector.name = "PrivateRoomCharacterSelector";
                selector.GetOrAddComponent<RectTransform>();
                selector.GetOrAddComponent<PhotonView>();
                var sizer = selector.AddComponent<ContentSizeFitter>();
                sizer.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                UnityEngine.GameObject.Destroy(selector.GetComponent<CharacterSelectionInstance>());
                PrivateRoomCharacterSelectionInstance charSelect = selector.AddComponent<PrivateRoomCharacterSelectionInstance>();
                UnityEngine.GameObject.Destroy(charSelect.transform.GetChild(2).gameObject);
                UnityEngine.GameObject.Destroy(charSelect.transform.GetChild(1).gameObject);

                GameObject playerName = new GameObject("PlayerName", typeof(TextMeshProUGUI));
                playerName.transform.SetParent(selector.transform);
                playerName.transform.localScale = Vector3.one;
                playerName.transform.localPosition = new Vector3(0f, 80f, 0f);
                playerName.GetComponent<TextMeshProUGUI>().color = new Color(0.556f, 0.556f, 0.556f, 1f);
                playerName.GetComponent<TextMeshProUGUI>().fontSize = 25f;
                playerName.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

                selector.transform.localPosition = Vector2.zero;

                PhotonNetwork.PrefabPool.RegisterPrefab(selector.name, selector);

                PrivateRoomPrefabs._PrivateRoomCharacterSelectionInstance = selector;

                return PrivateRoomPrefabs._PrivateRoomCharacterSelectionInstance;
            }
            private set { }
        }
    }

    class PrivateRoomHandler : MonoBehaviourPunCallbacks
    {
        private const int ReadyStartGameCountdown = 3;
        private const string DefaultHeaderText = "ROUNDS WITH FRIENDS";

        public static PrivateRoomHandler instance;
        private static string PrevHandlerID;
        private static GameSettings PrevSettings;

        //private Button readyButton;
        //private ListMenuButton readyListButton;
        private TextMeshProUGUI gameModeText;
        private GameObject grid;
        //private GameObject readyCheckbox;
        private GameObject waiting;
        private GameObject header;
        private TextMeshProUGUI headerText;
        private VersusDisplay versusDisplay;
        private bool waitingForToggle;
        private bool lockReadyRequests;
        private Coroutine countdownCoroutine;
        private Queue<Tuple<int, int, bool>> readyRequests;
        internal Dictionary<int, InputDevice> devicesToUse;

        public ListMenuPage MainPage { get; private set; }

        public int NumCharacters => this.PrivateRoomCharacters.Count();

        public LobbyCharacter[] PrivateRoomCharacters => PhotonNetwork.CurrentRoom.Players.Values.ToList().Select(p => p.GetProperty<LobbyCharacter[]>("players")).SelectMany(p => p).Where(p => p != null).ToArray();

        public LobbyCharacter FindLobbyCharacter(int actorID, int localID)
        {
            return this.PrivateRoomCharacters.Where(p => p.actorID == actorID && p.localID == localID).FirstOrDefault();
        }
        public LobbyCharacter FindLobbyCharacter(int uniqueID)
        {
            return this.PrivateRoomCharacters.Where(p => p.uniqueID == uniqueID).FirstOrDefault();
        }

        public bool IsOpen
        {
            get
            {
                return this?.grid?.activeSelf ?? false;
            }
        }

        private static void SaveSettings()
        {
            PrivateRoomHandler.PrevHandlerID = GameModeManager.CurrentHandlerID;
            PrivateRoomHandler.PrevSettings = GameModeManager.CurrentHandler.Settings;
        }

        public static void RestoreSettings()
        {
            PrivateRoomHandler.instance.SyncMethod(nameof(PrivateRoomHandler.SetGameSettings), null, PrivateRoomHandler.PrevHandlerID, PrivateRoomHandler.PrevSettings);
            PrivateRoomHandler.PrevHandlerID = null;
            PrivateRoomHandler.PrevSettings = null;
        }

        private void Awake()
        {
            PrivateRoomHandler.instance = this;

            // load the prefab just once to make sure it's registered
            GameObject prefab = PrivateRoomPrefabs.PrivateRoomCharacterSelectionInstance;
        }

        private void Start()
        {
            this.Init();
            this.BuildUI();
        }

        new private void OnEnable()
        {
            this.waitingForToggle = false;
            this.readyRequests = new Queue<Tuple<int, int, bool>>();
            this.devicesToUse = new Dictionary<int, InputDevice>();
            this.lockReadyRequests = false;
            PhotonNetwork.LocalPlayer.SetProperty("players", new LobbyCharacter[RWFMod.instance.MaxCharactersPerClient]);

            base.OnEnable();
        }

        private void Init()
        {
            this.waitingForToggle = false;
            this.readyRequests = new Queue<Tuple<int, int, bool>>();
            this.devicesToUse = new Dictionary<int, InputDevice>();
            this.lockReadyRequests = false;
        }

        private void BuildUI()
        {
            var rect = this.gameObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            var mainPageGo = new GameObject("Main");
            mainPageGo.transform.SetParent(this.transform);
            mainPageGo.transform.localScale = Vector3.one;

            this.grid = new GameObject("Group");
            this.grid.transform.SetParent(mainPageGo.transform);
            this.grid.transform.localScale = Vector3.one;

            this.header = new GameObject("Header");
            this.header.transform.SetParent(this.grid.transform);
            this.header.transform.localScale = Vector3.one;
            var headerTextGo = GameObject.Instantiate(RoundsResources.FlickeringTextPrefab, this.header.transform);
            headerTextGo.transform.localScale = Vector3.one;
            headerTextGo.transform.localPosition = Vector3.zero;
            var headerGoRect = this.header.AddComponent<RectTransform>();
            var headerGoLayout = this.header.AddComponent<LayoutElement>();
            this.headerText = headerTextGo.GetComponent<TextMeshProUGUI>();
            this.headerText.text = "ROUNDS WITH FRIENDS";
            this.headerText.fontSize = 80;
            this.headerText.fontStyle = FontStyles.Bold;
            this.headerText.enableWordWrapping = false;
            this.headerText.overflowMode = TextOverflowModes.Overflow;
            headerGoLayout.ignoreLayout = false;
            headerGoLayout.minHeight = 92f;

            // three different colors for the header:
            // the ROUNDS WITH colors (66% of the time)
            // the FRIENDS colors (33% of the time)
            // bright white colors (1% of the time)
            switch (UnityEngine.Random.Range(0f,1f))
            {
                case float n when n < 0.66f:
                    this.SetHeaderParticles(5, new Color(0f, 0.22f, 0.5f, 1f), new Color(0.5f, 0.5f, 0f, 1f), new Color(0f, 0.5094f, 0.23f, 1f));
                    break;
                case float n when (n >= 0.66f && n < 0.99f):
                    this.SetHeaderParticles(5, new Color(0.5f, 0.087f, 0f, 1f), new Color(0.25f, 0.25f, 0f, 1f), new Color(0.554f,0.3694f, 0f, 1f));
                    break;
                case float n when (n >= 0.99f):
                    this.SetHeaderParticles(5, new Color(0.5f,0.5f,0.5f, 1f), new Color(1f, 1f, 1f, 1f), new Color(0.25f, 0.25f, 0.25f, 1f));
                    break;
                default:
                    this.SetHeaderParticles(5, new Color(0f, 0.22f, 0.5f, 1f), new Color(0.5f, 0.5f, 0f, 1f), new Color(0f, 0.5094f, 0.23f, 1f));
                    break;

            }

            var playersGo = new GameObject("Players", typeof(PlayerDisplay));
            playersGo.transform.SetParent(this.grid.transform);
            playersGo.transform.localScale = Vector3.one;
            this.versusDisplay = playersGo.GetOrAddComponent<VersusDisplay>();

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

            var waitingGoRect = this.waiting.AddComponent<RectTransform>();
            var waitingGoLayout = this.waiting.AddComponent<LayoutElement>();
            var waitingText = waitingTextGo.GetComponent<TextMeshProUGUI>();
            waitingText.text = "WAITING FOR PLAYERS...";
            waitingText.fontSize = 80;
            waitingGoLayout.ignoreLayout = true;
            waitingGoRect.sizeDelta = new Vector2(900, 300);

            inviteGo.AddComponent<RectTransform>();
            inviteGo.AddComponent<CanvasRenderer>();
            var inviteLayout = inviteGo.AddComponent<LayoutElement>();
            inviteLayout.minHeight = 92;
            var inviteButton = inviteGo.AddComponent<Button>();
            var inviteListButton = inviteGo.AddComponent<ListMenuButton>();
            inviteListButton.setBarHeight = 92f;

            inviteButton.onClick.AddListener(() =>
            {
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

            gameModeButton.onClick.AddListener(() =>
            {
                if (PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
                {
                    string nextGameMode = GameModeManager.CurrentHandlerID == "Team Deathmatch" ? "Deathmatch" : "Team Deathmatch";
                    GameModeManager.SetGameMode(nextGameMode);
                    this.ExecuteAfterGameModeInitialized(nextGameMode, () =>
                    {
                        this.SyncMethod(nameof(PrivateRoomHandler.SetGameSettings), null, GameModeManager.CurrentHandlerID, GameModeManager.CurrentHandler.Settings);
                        this.HandleTeamRules();
                    });
                }
            });

            this.gameModeText = gameModeTextGo.GetComponent<TextMeshProUGUI>();

            divGo1.AddComponent<RectTransform>();

            backGo.AddComponent<RectTransform>();
            backGo.AddComponent<CanvasRenderer>();
            var backLayout = backGo.AddComponent<LayoutElement>();
            backLayout.minHeight = 92;
            var backButton = backGo.AddComponent<Button>();

            backButton.onClick.AddListener(() =>
            {
                // return Canvas to its original position
                this.gameObject.GetComponentInParent<Canvas>().sortingLayerName = "MostFront";
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
        }

        private void HandleTeamRules()
        {
            // prevent players from being on the same team if the new gamemode prohibits it
            if (GameModeManager.CurrentHandler.Settings.TryGetValue("allowTeams", out object allowTeamsObj) && !(bool) allowTeamsObj)
            {
                // teams not allowed, search through players for any players on the same team and assign them the next available colorID
                foreach (LobbyCharacter character in PhotonNetwork.CurrentRoom.Players.Select(kv => kv.Value.GetProperty<LobbyCharacter[]>("players")).SelectMany(p => p).Where(p => p != null))
                {
                    int orig = character.colorID;
                    int newColorID = character.colorID;
                    while (PhotonNetwork.CurrentRoom.Players.Select(kv => kv.Value.GetProperty<LobbyCharacter[]>("players")).SelectMany(p => p).Where(p => p != null && p.uniqueID != character.uniqueID && p.colorID == newColorID).Any())
                    {
                        newColorID = Math.mod((newColorID + 1), RWFMod.MaxColorsHardLimit);
                        if (newColorID == orig)
                        {
                            // make sure its impossible to get stuck in an infinite loop here,
                            // even though prior logic limiting the number of players should prevent this
                            break;
                        }
                    }
                    this.versusDisplay.PlayerSelectorGO(character.uniqueID).GetComponent<PhotonView>().RPC("RPCA_ChangeTeam", RpcTarget.All, newColorID);
                }

            }
        }

        private void ResetHeaderText()
        {
            this.headerText.text = "ROUNDS WITH FRIENDS";
            this.headerText.fontSize = 80;
            this.headerText.fontStyle = FontStyles.Bold;
            this.headerText.enableWordWrapping = false;
            this.headerText.overflowMode = TextOverflowModes.Overflow;
        }
        private void SetHeaderText(string text, float fontSize = 80f)
        {
            this.headerText.text = text;
            this.headerText.fontSize = fontSize;
        }
        private void SetHeaderParticles(float? size = null, Color? color = null, Color? randomAddedColor = null, Color? randomColor = null)
        {
            var particleSystem = this.headerText.GetComponentInChildren<GeneralParticleSystem>();

            if (size != null) { particleSystem.particleSettings.size = (float)size; }
            if (color != null) 
            { 
                particleSystem.particleSettings.color = (Color)color; 
            }
            if (randomAddedColor != null) 
            { 
                particleSystem.particleSettings.randomAddedColor = (Color) randomAddedColor; 
            }
            if (randomColor != null)
            {
                particleSystem.particleSettings.randomColor = (Color)randomColor;
            }
         }

        private GameObject GetText(string str)
        {
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

        private void Update()
        {
            // make sure there are no teams in a gamemode that doesn't allow them
            if (this.IsOpen && PhotonNetwork.IsMasterClient) { this.HandleTeamRules(); }

            /* Ready toggle requests are handled by master client in the order they arrive. If at any point all players are ready
             * (even if there would be more toggle requests remaining), the game starts immediately.
             */
            if (this.readyRequests.Count > 0 && !this.lockReadyRequests)
            {
                var request = this.readyRequests.Dequeue();
                this.HandlePlayerReadyToggle(request.Item1, request.Item2, request.Item3);
            }
        }

        override public void OnJoinedRoom()
        {
            if (!this.IsOpen)
            {
                return;
            }

            // necessary for VersusDisplay characters to render in the correct order
            // must be reverted to MostFront when leaving the lobby
            this.gameObject.GetComponentInParent<Canvas>().sortingLayerName = "UI";

            PhotonNetwork.LocalPlayer.SetProperty("players", new LobbyCharacter[RWFMod.instance.MaxCharactersPerClient]);

            if (RWFMod.DEBUG && PhotonNetwork.IsMasterClient)
            {
                RWFMod.Log($"\n\n\tRoom join command:\n\tjoin:{PhotonNetwork.CloudRegion}:{PhotonNetwork.CurrentRoom.Name}\n");
            }

            if (PhotonNetwork.IsMasterClient)
            {
                if (GameModeManager.CurrentHandler == null)
                {
                    GameModeManager.SetGameMode("Team Deathmatch");
                }

                PrivateRoomHandler.instance.gameModeText.text = GameModeManager.CurrentHandlerID == "Team Deathmatch" ? "TEAM DEATHMATCH" : "DEATHMATCH";
                PrivateRoomHandler.UpdateVersusDisplay();
            }

            /* The local player's nickname is also set in NetworkConnectionHandler::OnJoinedRoom, but we'll do it here too so we don't
             * need to worry about timing issues
             */
            if (RWFMod.IsSteamConnected)
            {
                PhotonNetwork.LocalPlayer.NickName = SteamFriends.GetPersonaName();
            }
            else
            {
                PhotonNetwork.LocalPlayer.NickName = $"Player {PhotonNetwork.LocalPlayer.ActorNumber}";
            }

            // If we handled this from OnPlayerEnteredRoom handler for other clients, the joined client's nickname might not have been set yet
            NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.UpdateVersusDisplay));
            base.OnJoinedRoom();
        }

        override public void OnMasterClientSwitched(Photon.Realtime.Player newMaster)
        {
            NetworkConnectionHandler.instance.NetworkRestart();
            base.OnMasterClientSwitched(newMaster);
        }

        override public void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC_Others(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.SetGameSettings), GameModeManager.CurrentHandlerID, GameModeManager.CurrentHandler.Settings);

                if (RWFMod.DEBUG && RWFMod.instance.gameObject.GetComponent<DebugWindow>().enabled)
                {
                    RWFMod.instance.SyncDebugOptions();
                }
            }

            this.ExecuteAfterSeconds(0.1f, () =>
            {
                PrivateRoomHandler.UpdateVersusDisplay();
            });

            base.OnPlayerEnteredRoom(newPlayer);
        }

        override public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            this.ClearPendingRequests(otherPlayer.ActorNumber);
            PrivateRoomHandler.UpdateVersusDisplay();
            base.OnPlayerLeftRoom(otherPlayer);
        }

        internal IEnumerator ToggleReady(InputDevice deviceReadied, bool doNotReady = false)
        {
            if (DevConsole.isTyping)
            {
                yield break;
            }

            while (PhotonNetwork.CurrentRoom == null)
            {
                yield return null;
            }

            if (this.waitingForToggle)
            {
                yield break;
            }

            LobbyCharacter[] localCharacters = PhotonNetwork.LocalPlayer.GetProperty<LobbyCharacter[]>("players");

            // figure out who pressed ready, if it was a device not yet in use, then add a new player IF there is room
            bool newDevice = !this.devicesToUse.Where(kv => kv.Value == deviceReadied).Any();

            // handle the case of a new device
            if (newDevice && !(localCharacters.Where(p => p != null).Count() < RWFMod.instance.MaxCharactersPerClient))
            {
                // there is no room for another local player
                yield break;
            }
            else if (newDevice)
            {
                int localPlayerNumber = Enumerable.Range(0, RWFMod.instance.MaxCharactersPerClient).Where(i => localCharacters[i] == null).First();
                
                // add a new local player to the first available slot with either their preferred color if its available or the next unused colorID
                // preferred colors are NOT set in the online lobby, but instead in the local lobby - that way they don't change every match a player doesn't get their preferred color
                int colorID = PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey("PreferredColor" + localPlayerNumber.ToString()));
                if (GameModeManager.CurrentHandler.Settings.TryGetValue("allowTeams", out object allowTeamsObj) && !(bool) allowTeamsObj && this.PrivateRoomCharacters.Select(p => p.colorID).Distinct().Contains(colorID))
                {
                    colorID = Enumerable.Range(0, RWFMod.MaxColorsHardLimit).Except(this.PrivateRoomCharacters.Select(p => p.colorID).Distinct()).FirstOrDefault();
                }

                localCharacters[localPlayerNumber] = new LobbyCharacter(PhotonNetwork.LocalPlayer, colorID, localPlayerNumber);

                PhotonNetwork.LocalPlayer.SetProperty("players", localCharacters);

                this.devicesToUse[localPlayerNumber] = deviceReadied;

                SoundPlayerStatic.Instance.PlayPlayerAdded();

                NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.UpdateVersusDisplay));

                yield break;
            }
            else if (!doNotReady)
            {
                this.waitingForToggle = true;

                // the player already exists
                LobbyCharacter playerReadied = localCharacters[this.devicesToUse.Keys.Where(i => this.devicesToUse[i] == deviceReadied).First()];

                bool ready = playerReadied.ready;

                /* Request master client to toggle this player's ready state. The master client will either oblige or, if all players
                 * are ready as a consequence of this toggle, start the game immediately.
                 */
                NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.RequestReady), PhotonNetwork.LocalPlayer.ActorNumber, playerReadied.localID, !ready);

                while (PhotonNetwork.LocalPlayer.GetProperty<LobbyCharacter[]>("players")[playerReadied.localID].ready != !ready)
                {
                    yield return null;
                }

                //this.UpdateReadyBox();
                SoundPlayerStatic.Instance.PlayPlayerAdded();

                yield return new WaitForSeconds(0.1f);
                this.waitingForToggle = false;
            
            }
        }

        // Called from PlayerManager after a player has been created.
        public void PlayerJoined(Player player)
        {
            player.data.isPlaying = false;
        }

        [UnboundRPC]
        public static void SetGameSettings(string gameMode, GameSettings settings)
        {
            GameModeManager.SetGameMode(gameMode);
            GameModeManager.CurrentHandler.SetSettings(settings);

            PrivateRoomHandler.instance.gameModeText.text = GameModeManager.CurrentHandlerID == "Team Deathmatch" ? "TEAM DEATHMATCH" : "DEATHMATCH";
            PrivateRoomHandler.UpdateVersusDisplay();

            NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.SetGameSettingsResponse), PhotonNetwork.LocalPlayer.ActorNumber);
        }

        [UnboundRPC]
        public static void SetGameSettingsResponse(int respondingPlayer)
        {
            if (PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            {
                PrivateRoomHandler.instance.RemovePendingRequest(respondingPlayer, nameof(PrivateRoomHandler.SetGameSettings));
            }
        }

        [UnboundRPC]
        public static void RequestReady(int askingPlayer, int localID, bool ready)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PrivateRoomHandler.instance.readyRequests.Enqueue(new Tuple<int, int, bool>(askingPlayer, localID, ready));
            }
        }

        [UnboundRPC]
        public static void UpdateVersusDisplay()
        {
            var instance = PrivateRoomHandler.instance;

            if (instance == null)
            {
                return;
            }

            if (RWFMod.DEBUG)
            {
                instance.versusDisplay.gameObject.SetActive(true);
                //instance.readyListButton.gameObject.SetActive(true);
                instance.waiting.SetActive(false);
                //ListMenu.instance.SelectButton(instance.readyListButton);
            }

            else if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount >= 2 && !instance.versusDisplay.gameObject.activeSelf)
            {
                instance.versusDisplay.gameObject.SetActive(true);
                //instance.readyListButton.gameObject.SetActive(true);
                instance.waiting.SetActive(false);
                //ListMenu.instance.SelectButton(instance.readyListButton);
            }
            else if ((PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.PlayerCount < 2) && instance.versusDisplay.gameObject.activeSelf)
            {
                instance.versusDisplay.gameObject.SetActive(false);
                //instance.readyListButton.gameObject.SetActive(false);
                instance.waiting.SetActive(true);
            }

            if (instance.versusDisplay.gameObject.activeSelf)
            {
                instance.versusDisplay.UpdatePlayers();
            }
        }

        private void HandlePlayerReadyToggle(int actorID, int localID, bool ready)
        {
            // this is only run on the host client

            var networkPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorID);

            if (networkPlayer == null)
            {
                return;
            }

            int numReady = this.PrivateRoomCharacters.Where(p => p.ready).Count();

            if (ready)
            {
                numReady++;
            }
            else
            {
                numReady--;
            }

            LobbyCharacter character = this.FindLobbyCharacter(actorID, localID);
            character.SetReady(ready);
            LobbyCharacter[] characters = PhotonNetwork.CurrentRoom.Players[actorID].GetProperty<LobbyCharacter[]>("players");
            characters[localID] = character;
            PhotonNetwork.CurrentRoom.GetPlayer(actorID).SetProperty("players", characters);
            NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.RPCA_ReadyPlayer), character, ready);

            //this.versusDisplay.ReadyPlayer(character);

            //networkPlayer.SetProperty("ready", ready);
            //networkPlayer.SetProperty("readyOrder", ready ? numReady - 1 : -1);

            // to start the game, everyone must be ready, there must be at least two clients, and there must be at least two teams
            if (numReady == this.NumCharacters && PhotonNetwork.CurrentRoom.PlayerCount > 1 && this.PrivateRoomCharacters.Select(p => p.colorID).Distinct().Count() > 1)
            {
                this.countdownCoroutine = this.StartCoroutine(this.StartGameCountdown());
            }
            else if (this.countdownCoroutine != null)
            {
                this.StopCoroutine(this.countdownCoroutine);
                NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.RPCA_DisplayCountdown), PrivateRoomHandler.DefaultHeaderText);
            }

            /*
            // If the player unreadied, reassign ready orders so that they grow continuously from 0.
            if (!ready)
            {
                int nextReadyOrder = 0;

                var readyPlayers = PhotonNetwork.CurrentRoom.Players.Values.ToList()
                    .Where(p => p.GetProperty<bool>("ready"))
                    .OrderBy(p => p.GetProperty<int>("readyOrder"));

                foreach (var readyPlayer in readyPlayers)
                {
                    readyPlayer.SetProperty("readyOrder", nextReadyOrder);
                    nextReadyOrder++;
                }
            }*/

        }

        private IEnumerator StartGameCountdown()
        {
            // start a countdown, during which players can unready to cancel

            for (int t = PrivateRoomHandler.ReadyStartGameCountdown; t >= 0; t--)
            {
                NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.RPCA_DisplayCountdown), t > 0 ? t.ToString() : "GO!");
                yield return new WaitForSecondsRealtime(1f);
            }

            int numReady = this.PrivateRoomCharacters.Where(p => p.ready).Count();
            if (numReady != this.NumCharacters) 
            {
                this.ResetHeaderText();
                yield break; 
            }

            this.lockReadyRequests = true;

            // Tell all clients to create their players. The game begins once players have been created.
            this.StartCoroutine(this.StartGamePreparation());
            yield break;
        }
        [UnboundRPC]
        private static void RPCA_DisplayCountdown(string text)
        {
            SoundPlayerStatic.Instance.PlayButtonClick();
            PrivateRoomHandler.instance.SetHeaderText(text);
        }

        [UnboundRPC]
        private static void RPCA_ReadyPlayer(LobbyCharacter character, bool ready)
        {
            character.SetReady(ready);
            VersusDisplay.instance.ReadyPlayer(character, ready);

            //NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.UpdatePlayerDisplay));
        }

        private IEnumerator StartGamePreparation()
        {

            foreach (var player in this.PrivateRoomCharacters.OrderBy(p => p.teamID).ThenBy(p => p.uniqueID))
            {
                yield return this.SyncMethod(nameof(PrivateRoomHandler.CreatePlayer), player.actorID, player.actorID, player.localID);
            }

            NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.StartGame));
        }

        [UnboundRPC]
        public static void CreatePlayer(int actorID, int localID)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == actorID)
            {
                var instance = PrivateRoomHandler.instance;
                instance.StartCoroutine(instance.CreatePlayerCoroutine(actorID, localID));
            }
        }

        private IEnumerator CreatePlayerCoroutine(int actorID, int localID)
        {
            this.MainPage.Close();
            MainMenuHandler.instance.Close();
            UIHandler.instance.ShowJoinGameText("LETS GOO!", PlayerSkinBank.GetPlayerSkinColors(1).winText);

            RWFMod.instance.SetSoundEnabled("PlayerAdded", false);
            //PlayerAssigner.instance.SetFieldValue("hasCreatedLocalPlayer", false);
            LobbyCharacter lobbyCharacter = this.FindLobbyCharacter(actorID, localID);
            yield return PlayerAssigner.instance.CreatePlayer(lobbyCharacter, this.devicesToUse[localID]);
            RWFMod.instance.SetSoundEnabled("PlayerAdded", true);

            //Player newPlayer = PlayerManager.instance.players[PlayerManager.instance.players.Count() - 1];

            NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.CreatePlayerResponse), PhotonNetwork.LocalPlayer.ActorNumber);
        }

        [UnboundRPC]
        public static void CreatePlayerResponse(int respondingPlayer)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PrivateRoomHandler.instance.RemovePendingRequest(respondingPlayer, nameof(PrivateRoomHandler.CreatePlayer));
            }
        }

        [UnboundRPC]
        public static void StartGame()
        {
            // return Canvas to its original position
            PrivateRoomHandler.instance.gameObject.GetComponentInParent<Canvas>().sortingLayerName = "MostFront";

            SoundPlayerStatic.Instance.PlayMatchFound();

            var instance = PrivateRoomHandler.instance;
            instance.StopAllCoroutines();
            GameModeManager.CurrentHandler.StartGame();

            if (PhotonNetwork.IsMasterClient)
            {
                PrivateRoomHandler.SaveSettings();
            }
            //PhotonNetwork.LocalPlayer.SetProperty("ready", false);
            //PhotonNetwork.LocalPlayer.SetProperty("readyOrder", -1);

            //instance.UpdateReadyBox();
        }

        public void Open()
        {
            this.ExecuteAfterFrames(1, () =>
            {
                // necessary for VersusDisplay characters to render in the correct order
                // must be reverted to MostFront when leaving the lobby
                this.gameObject.GetComponentInParent<Canvas>().sortingLayerName = "UI";

                ListMenu.instance.OpenPage(this.MainPage);
                this.MainPage.Open();
                ArtHandler.instance.NextArt();
                PrivateRoomHandler.UpdateVersusDisplay();
            });
        }
    }
}
