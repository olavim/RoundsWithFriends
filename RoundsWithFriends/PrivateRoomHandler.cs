using InControl;
using Landfall.Network;
using Photon.Pun;
using SoundImplementation;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnboundLib;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnityEngine;
using UnityEngine.UI;
using RWF.UI;
using UnboundLib.Utils.UI;

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
                var sizer = selector.GetOrAddComponent<ContentSizeFitter>();
                sizer.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                UnityEngine.GameObject.Destroy(selector.GetComponent<CharacterSelectionInstance>());
                PrivateRoomCharacterSelectionInstance charSelect = selector.GetOrAddComponent<PrivateRoomCharacterSelectionInstance>();
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

        private static readonly Color disabledTextColor = new Color32(150, 150, 150, 16);
        private static readonly Color enabledTextColor = new Color32(230, 230, 230, 255);

        public static PrivateRoomHandler instance;
        private static string PrevHandlerID;
        private static GameSettings PrevSettings;

        private GameObject grid;
        private GameObject header;
        private GameObject gamemodeHeader;
        // private GameObject gameModeListObject;
        private GameObject gameModeButton;
        private TextMeshProUGUI headerText;
        private TextMeshProUGUI gamemodeHeaderText;
        private TextMeshProUGUI inviteText;
        private TextMeshProUGUI gamemodeText;
        // private TextMeshProUGUI gameModeText;
        private VersusDisplay versusDisplay;
        private bool _lockReadies;
        private bool lockReadies
        {
            get
            {
                return PrivateRoomHandler.instance._lockReadies;
            }
            set
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.RPCA_LockReadies), value);
                }
                else
                {
                    NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.RPCH_RequestLockReadies), new object[] { });
                }
            }
        }
        private Coroutine countdownCoroutine;
        internal Dictionary<int, InputDevice> devicesToUse;


        public ListMenuPage MainPage { get; private set; }

        public int NumCharacters => this.PrivateRoomCharacters.Count();
        public int NumCharactersReady => this.PrivateRoomCharacters.Where(p => p.ready).Count();

        public LobbyCharacter[] PrivateRoomCharacters => PhotonNetwork.CurrentRoom.Players.Values.ToList().Select(p => p.GetProperty<LobbyCharacter[]>("players")).SelectMany(p => p).Where(p => p != null).ToArray();

        public LobbyCharacter FindLobbyCharacter(int actorID, int localID)
        {
            return this.PrivateRoomCharacters.Where(p => p.actorID == actorID && p.localID == localID).FirstOrDefault();
        }
        public LobbyCharacter FindLobbyCharacter(int uniqueID)
        {
            return this.PrivateRoomCharacters.Where(p => p.uniqueID == uniqueID).FirstOrDefault();
        }
        public LobbyCharacter FindLobbyCharacter(InputDevice device)
        {
            if (!this.devicesToUse.Values.Contains(device)) { return null; }
            int localID = this.devicesToUse.Where(kv => kv.Value == device).Select(kv => kv.Key).FirstOrDefault();
            int actorID = PhotonNetwork.LocalPlayer.ActorNumber;
            return this.FindLobbyCharacter(actorID, localID);
        }

        public bool IsOpen
        {
            get
            {
                return this?.grid?.activeSelf ?? false;
            }
        }

        // bool that represents if all of the game mode's requirements are fulfilled
        private bool GameCanStart => this.NumCharactersReady == this.NumCharacters // is everyone ready?
                                    && PhotonNetwork.CurrentRoom.PlayerCount > 1 // is there more than just one client?
                                    && PhotonNetwork.CurrentRoom.PlayerCount <= (GameModeManager.CurrentHandler.Settings.TryGetValue(RWFMod.MaxClientsKey, out object maxC) ? (int) maxC : RWFMod.instance.MaxClients) // are there too many clients?
                                    && this.PrivateRoomCharacters.Select(p => p.colorID).Distinct().Count() > 1 // is there more than one player?
                                    && this.NumCharactersReady >= (GameModeManager.CurrentHandler.Settings.TryGetValue(RWFMod.PlayersRequiredToStartGameKey, out object req) ? (int) req : RWFMod.instance.MinPlayers) // are there enough players for this game mode? 
                                    && this.NumCharactersReady <= (GameModeManager.CurrentHandler.Settings.TryGetValue(RWFMod.MaxPlayersKey, out object maxP) ? (int) maxP : RWFMod.MaxPlayersHardLimit) // are there too many players for this game mode?
                                    && this.PrivateRoomCharacters.Select(p => p.colorID).Distinct().Count() <= (GameModeManager.CurrentHandler.Settings.TryGetValue(RWFMod.MaxTeamsKey, out object maxT) ? (int) maxT : RWFMod.instance.MaxTeams); // are there too many teams?



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
            this.devicesToUse = new Dictionary<int, InputDevice>();
            this.lockReadies = false;
            PhotonNetwork.LocalPlayer.SetProperty("players", new LobbyCharacter[RWFMod.instance.MaxCharactersPerClient]);

            base.OnEnable();
        }

        private void Init()
        {
            this.devicesToUse = new Dictionary<int, InputDevice>();
            this.lockReadies = false;
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

            this.SetTextParticles(this.headerText.gameObject, 5, new Color(0f, 0.22f, 0.5f, 1f), new Color(0.5f, 0.5f, 0f, 1f), new Color(0f, 0.5094f, 0.23f, 1f));
   

            this.gamemodeHeader = new GameObject("GameModeHeader");
            this.gamemodeHeader.transform.SetParent(this.grid.transform);
            this.gamemodeHeader.transform.localScale = Vector3.one;
            var gamemodeTextGo = GameObject.Instantiate(RoundsResources.FlickeringTextPrefab, this.gamemodeHeader.transform);
            gamemodeTextGo.transform.localScale = Vector3.one;
            gamemodeTextGo.transform.localPosition = Vector3.zero;
            var gamemodeGoRect = this.gamemodeHeader.AddComponent<RectTransform>();
            var gamemodeGoLayout = this.gamemodeHeader.AddComponent<LayoutElement>();
            this.gamemodeHeaderText = gamemodeTextGo.GetComponent<TextMeshProUGUI>();
            this.gamemodeHeaderText.text = GameModeManager.CurrentHandler?.Name?.ToUpper() ?? "CONNECTING...";
            this.gamemodeHeaderText.fontSize = 60;
            this.gamemodeHeaderText.fontStyle = FontStyles.Bold;
            this.gamemodeHeaderText.enableWordWrapping = false;
            this.gamemodeHeaderText.overflowMode = TextOverflowModes.Overflow;
            gamemodeGoLayout.ignoreLayout = false;
            gamemodeGoLayout.minHeight = 92f;

            this.SetTextParticles(this.gamemodeHeaderText.gameObject, 5, new Color(0.5f, 0.087f, 0f, 1f), new Color(0.25f, 0.25f, 0f, 1f), new Color(0.554f, 0.3694f, 0f, 1f));


            var playersGo = new GameObject("Players", typeof(PlayerDisplay));
            playersGo.transform.SetParent(this.grid.transform);
            playersGo.transform.localScale = Vector3.one;
            this.versusDisplay = playersGo.GetOrAddComponent<VersusDisplay>();

            var keybindGo = new GameObject("Keybinds");
            keybindGo.transform.SetParent(this.grid.transform);
            keybindGo.transform.localScale = Vector3.one;
            var keybindHints1 = GameObject.Instantiate(KeybindHints.KeybindPrefab, keybindGo.transform).AddComponent<KeybindHints.ControllerBasedHints>();
            keybindHints1.hints = new[] { "[A/D]", "[LEFT STICK]" };
            keybindHints1.action = "CHANGE TEAM";
            keybindHints1.gameObject.SetActive(true);
            keybindHints1.gameObject.AddComponent<KeybindHints.DisableIfSet>();
            var keybindHints2 = GameObject.Instantiate(KeybindHints.KeybindPrefab, keybindGo.transform).AddComponent<KeybindHints.ControllerBasedHints>();
            keybindHints2.hints = new[] { "[SPACE]", "[START]" };
            keybindHints2.action = "JOIN/READY";
            keybindHints2.gameObject.SetActive(true);
            keybindHints2.gameObject.AddComponent<KeybindHints.DisableIfSet>();
            var keybindHints3 = GameObject.Instantiate(KeybindHints.KeybindPrefab, keybindGo.transform).AddComponent<KeybindHints.ControllerBasedHints>();
            keybindHints3.hints = new[] { "[ESC]", "[B]" };
            keybindHints3.action = "UNREADY/LEAVE";
            keybindHints3.gameObject.SetActive(true);
            keybindHints3.gameObject.AddComponent<KeybindHints.DisableIfSet>();
            var keybindHints4 = GameObject.Instantiate(KeybindHints.KeybindPrefab, keybindGo.transform).AddComponent<KeybindHints.ControllerBasedHints>();
            keybindHints4.hints = new[] { "[Q/E]", "[LB/RB]" };
            keybindHints4.action = "CHANGE FACE";
            keybindHints4.gameObject.SetActive(true);
            keybindHints4.gameObject.AddComponent<KeybindHints.DisableIfSet>();
            var keybindRect = keybindGo.AddComponent<RectTransform>();
            var keybindLayout = keybindGo.AddComponent<LayoutElement>();
            var keybindGroup = keybindGo.AddComponent<HorizontalLayoutGroup>();
            keybindGroup.childAlignment = TextAnchor.MiddleCenter;
            keybindGroup.spacing = 450f;
            keybindLayout.ignoreLayout = false;
            keybindLayout.minHeight = 50f;

            var divGo1 = new GameObject("Divider1");
            divGo1.transform.SetParent(this.grid.transform);
            divGo1.transform.localScale = Vector3.one;

            var inviteGo = new GameObject("Invite");
            inviteGo.transform.SetParent(this.grid.transform);
            inviteGo.transform.localScale = Vector3.one;

            var inviteTextGo = GetText("INVITE");
            inviteTextGo.transform.SetParent(inviteGo.transform);
            inviteTextGo.transform.localScale = Vector3.one;
            this.inviteText = inviteTextGo.GetComponent<TextMeshProUGUI>();
            this.inviteText.color = (PhotonNetwork.CurrentRoom != null) ? PrivateRoomHandler.enabledTextColor : PrivateRoomHandler.disabledTextColor;

            // this.gameModeListObject = new GameObject("GameMode");
            // this.gameModeListObject.transform.SetParent(this.grid.transform);
            // this.gameModeListObject.transform.localScale = Vector3.one;

            // var gameModeTextGo = GetText(GameModeManager.CurrentHandler?.Name?.ToUpper() ?? "GAMEMODE");
            // gameModeTextGo.transform.SetParent(this.gameModeListObject.transform);
            // gameModeTextGo.transform.localScale = Vector3.one;
            
            // GamemodeScrollView.Create(this.grid.transform);
            
            this.gameModeButton = MenuHandler.CreateButton("select game mode", this.grid, () => { });
            var gmLayoutElement = this.gameModeButton.GetComponent<LayoutElement>();
            gmLayoutElement.minHeight = 92f;
            gmLayoutElement.minWidth = 5000f;
            this.gameModeButton.GetComponent<ListMenuButton>().setBarHeight = 92f;
            this.gamemodeText = this.gameModeButton.GetComponentInChildren<TextMeshProUGUI>();
            this.gamemodeText.color = (PhotonNetwork.CurrentRoom != null) ? PrivateRoomHandler.enabledTextColor : PrivateRoomHandler.disabledTextColor;
            this.gameModeButton.GetComponent<Button>().enabled = false;
            
            var backGo = new GameObject("Back");
            backGo.transform.SetParent(this.grid.transform);
            backGo.transform.localScale = Vector3.one;

            var backTextGo = GetText("BACK");
            backTextGo.transform.SetParent(backGo.transform);
            backTextGo.transform.localScale = Vector3.one;

            inviteGo.AddComponent<RectTransform>();
            inviteGo.AddComponent<CanvasRenderer>();
            var inviteLayout = inviteGo.AddComponent<LayoutElement>();
            inviteLayout.minHeight = 92;
            var inviteButton = inviteGo.AddComponent<Button>();
            var inviteListButton = inviteGo.AddComponent<ListMenuButton>();
            inviteListButton.setBarHeight = 92f;

            inviteButton.onClick.AddListener(() =>
            {
                if (PhotonNetwork.CurrentRoom == null) { return; }
                var field = typeof(NetworkConnectionHandler).GetField("m_SteamLobby", BindingFlags.Static | BindingFlags.NonPublic);
                var lobby = (ClientSteamLobby) field.GetValue(null);
                lobby.ShowInviteScreenWhenConnected();
            });

            // this.gameModeListObject.AddComponent<RectTransform>();
            // this.gameModeListObject.AddComponent<CanvasRenderer>();
            // var gameModeLayout = this.gameModeListObject.AddComponent<LayoutElement>();
            // gameModeLayout.minHeight = 92;
            // var gameModeButton = this.gameModeListObject.AddComponent<Button>();
            // var gameModeListButton = this.gameModeListObject.AddComponent<ListMenuButton>();
            // gameModeListButton.setBarHeight = 92f;
            //
            // gameModeButton.onClick.AddListener(() =>
            // {
            //     if (PhotonNetwork.CurrentRoom == null) { return; }
            //     if (PhotonNetwork.IsMasterClient)
            //     {
            //         // cycle through gamemodes alphabetically, skipping Sandbox and ArmsRace
            //         string[] gameModes = GameModeManager.Handlers.Keys.Where(k=> k != GameModeManager.SandBoxID && k != GameModeManager.ArmsRaceID).OrderBy(k => GameModeManager.Handlers[k].Name).ToArray();
            //         string nextGameMode = gameModes[Math.mod(Array.IndexOf(gameModes, GameModeManager.CurrentHandlerID) + 1, gameModes.Count())];
            //         GameModeManager.SetGameMode(nextGameMode);
            //         this.UnreadyAllPlayers();
            //         this.ExecuteAfterGameModeInitialized(nextGameMode, () =>
            //         {
            //             this.SyncMethod(nameof(PrivateRoomHandler.SetGameSettings), null, GameModeManager.CurrentHandlerID, GameModeManager.CurrentHandler.Settings);
            //             this.HandleTeamRules();
            //         });
            //     }
            // });
            //
            // this.gameModeText = gameModeTextGo.GetComponent<TextMeshProUGUI>();
            // this.gameModeText.color = (PhotonNetwork.CurrentRoom != null) ? PrivateRoomHandler.enabledTextColor : PrivateRoomHandler.disabledTextColor;
            
            
            // Gamemode ui menu
            var gamemodeMenu = GameObject.Instantiate(RWFMod.gmUIBundle.LoadAsset<GameObject>("GamemodeMenu"), this.grid.transform.parent);
            gamemodeMenu.GetComponent<RectTransform>().anchoredPosition= new Vector2(1920*2, 0);
            var menuManager = gamemodeMenu.AddComponent<GamemodeMenuManager>();
            menuManager.lobbyMenuObject = this.grid;
            menuManager.Init();
            
            this.gameModeButton.GetComponent<Button>().onClick.AddListener(() => {
                if (PhotonNetwork.IsMasterClient) { menuManager.Open(); }
            });

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
                KeybindHints.ClearHints();
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

        public void HandleTeamRules()
        {
            // prevent players from being on the same team if the new gamemode prohibits it
            if (!GameModeManager.CurrentHandler.AllowTeams)
            {
                if (this.PrivateRoomCharacters.Select(p => p.colorID).Distinct().Count() == this.NumCharacters)
                {
                    return;
                }
                int sgn = +1;
                // teams not allowed, search through players for any players on the same team and assign them the next available colorID
                // order by ascending uniqueID to favor players who joined earlier getting their preffered color
                foreach (LobbyCharacter character in this.PrivateRoomCharacters.OrderBy(p => p.uniqueID))
                {
                    int orig = character.colorID;
                    int newColorID = character.colorID;
                    while (this.PrivateRoomCharacters.Where(p => p.uniqueID != character.uniqueID && p.colorID == newColorID).Any())
                    {
                        newColorID = Math.mod((newColorID + sgn), RWFMod.MaxColorsHardLimit);
                        if (newColorID == orig)
                        {
                            // make sure its impossible to get stuck in an infinite loop here,
                            // even though prior logic limiting the number of players should prevent this
                            break;
                        }
                    }
                    sgn *= -1;
                    if (orig != newColorID) { this.versusDisplay.PlayerSelectorGO(character.uniqueID).GetComponent<PhotonView>().RPC(nameof(PrivateRoomCharacterSelectionInstance.RPCA_ChangeTeam), RpcTarget.All, newColorID); }
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
        private void SetTextParticles(GameObject text, float? size = null, Color? color = null, Color? randomAddedColor = null, Color? randomColor = null)
        {
            var particleSystem = text?.GetComponentInChildren<GeneralParticleSystem>();

            if (particleSystem == null) { return; }

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

        public GameObject GetText(string str)
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
            // don't try to do this right as the game is starting
            if (this.IsOpen && PhotonNetwork.IsMasterClient && !this.lockReadies)
            {
                // for the first 2-3 frames when a player joins, HandleTeamRules throws a NullReference, so we catch it
                try { this.HandleTeamRules(); }
                catch { }

                // check if enough players are ready to start
                if (this.countdownCoroutine == null && this.GameCanStart)
                {
                    this.countdownCoroutine = this.StartCoroutine(this.StartGameCountdown());
                }
                else if (this.countdownCoroutine != null && !(this.NumCharactersReady == this.NumCharacters && PhotonNetwork.CurrentRoom.PlayerCount > 1 && this.PrivateRoomCharacters.Select(p => p.colorID).Distinct().Count() > 1))
                {
                    this.StopCoroutine(this.countdownCoroutine);
                    this.countdownCoroutine = null;
                    NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.RPCA_DisplayCountdown), PrivateRoomHandler.DefaultHeaderText);
                }
            }
        }

        override public void OnJoinedRoom()
        {
            if (!this.IsOpen)
            {
                return;
            }

            // set text colors to enabled, hide gamemode button if this player is not host
            this.inviteText.color = PrivateRoomHandler.enabledTextColor;
            this.gamemodeText.color = PrivateRoomHandler.enabledTextColor;
            // this.gameModeText.color = PrivateRoomHandler.enabledTextColor;
            if (PhotonNetwork.IsMasterClient)
            {
                // GamemodeScrollView.scrollView.SetActive(true);
                this.gameModeButton.gameObject.SetActive(true);
                this.gameModeButton.GetComponent<Button>().enabled = true;
            }
            else
            {
                this.gameModeButton.gameObject.SetActive(false);
                this.gameModeButton.GetComponent<Button>().enabled = false;
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
                    // default to TDM
                    GameModeManager.SetGameMode(RWF.GameModes.TeamDeathmatchHandler.GameModeID);
                }

                // GamemodeScrollView.SetGameMode(GameModeManager.CurrentHandler?.Name);
                // PrivateRoomHandler.instance.gameModeText.text = GameModeManager.CurrentHandler?.Name?.ToUpper() ?? "";
                PrivateRoomHandler.instance.gamemodeHeaderText.text = GameModeManager.CurrentHandler?.Name?.ToUpper() ?? "";
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

            // disable the ability to join the player queue for a few seconds to allow it to initialize
            PlayerDisplay.instance.disableCountdown = 2f;

            // If we handled this from OnPlayerEnteredRoom handler for other clients, the joined client's nickname might not have been set yet
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

                this.lockReadies = false;

            }

            base.OnPlayerEnteredRoom(newPlayer);
        }

        override public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            this.ClearPendingRequests(otherPlayer.ActorNumber);
            base.OnPlayerLeftRoom(otherPlayer);
        }
        
        internal IEnumerator RemovePlayer(LobbyCharacter character)
        {

            LobbyCharacter[] localCharacters = PhotonNetwork.LocalPlayer.GetProperty<LobbyCharacter[]>("players");

            localCharacters[character.localID] = null;

            PhotonNetwork.LocalPlayer.SetProperty("players", localCharacters);

            this.devicesToUse.Remove(character.localID);

            SoundPlayerStatic.Instance.PlayPlayerBallDisappear();

            yield break;
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

            if (this.lockReadies)
            {
                yield break;
            }

            LobbyCharacter[] localCharacters = PhotonNetwork.LocalPlayer.GetProperty<LobbyCharacter[]>("players");
            if (localCharacters == null)
            {
                yield break;
            }

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
                
                // add a new local player to the first available slot with either their preferred color if its available, if it is not, pick the nearest valid color
                // preferred colors are NOT set in the online lobby, but instead in the local lobby - that way they don't change every match a player doesn't get their preferred color
                int colorID = PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey("PreferredColor" + localPlayerNumber.ToString()));
                if (!GameModeManager.CurrentHandler.AllowTeams && this.PrivateRoomCharacters.Select(p => p.colorID).Distinct().Contains(colorID))
                {
                    colorID = Enumerable.Range(0, RWFMod.MaxColorsHardLimit).Except(this.PrivateRoomCharacters.Select(p => p.colorID).Distinct()).OrderBy(c => UnityEngine.Mathf.Abs(c-colorID)).FirstOrDefault();
                }
                localCharacters[localPlayerNumber] = new LobbyCharacter(PhotonNetwork.LocalPlayer, colorID, localPlayerNumber);

                PhotonNetwork.LocalPlayer.SetProperty("players", localCharacters);

                this.devicesToUse[localPlayerNumber] = deviceReadied;

                SoundPlayerStatic.Instance.PlayPlayerAdded();

                yield break;
            }
            else if (!doNotReady)
            {

                // the player already exists
                LobbyCharacter playerReadied = localCharacters[this.devicesToUse.Keys.Where(i => this.devicesToUse[i] == deviceReadied).First()];
                if (playerReadied == null)
                {
                    this.devicesToUse.Remove(this.devicesToUse.Keys.Where(i => this.devicesToUse[i] == deviceReadied).First());
                    yield break;
                }
                // update this character's ready and save it to the Photon player's properties to update on all clients
                playerReadied.SetReady(!playerReadied.ready);
                localCharacters[playerReadied.localID] = playerReadied;

                PhotonNetwork.LocalPlayer.SetProperty("players", localCharacters);

                SoundPlayerStatic.Instance.PlayPlayerAdded();
            
                yield return new WaitForSeconds(0.1f);
            
            }
        }

        public void UnreadyAllPlayers()
        {
            if (!PhotonNetwork.IsMasterClient) { return; }
            foreach (int actorID in this.PrivateRoomCharacters.Select(p => p.actorID))
            {
                NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(RPCS_UnreadyAllPlayers), actorID);
            }
        }
        [UnboundRPC]
        private static void RPCS_UnreadyAllPlayers(int actorID)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber != actorID) { return; }

            LobbyCharacter[] localCharacters = PhotonNetwork.LocalPlayer.GetProperty<LobbyCharacter[]>("players");
            for (int i = 0; i < localCharacters.Count(); i++)
            {
                localCharacters[i].SetReady(false);
            }
            PhotonNetwork.LocalPlayer.SetProperty("players", localCharacters);
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

            PrivateRoomHandler.instance.gamemodeHeaderText.text = GameModeManager.CurrentHandler?.Name?.ToUpper() ?? "";

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
        private IEnumerator StartGameCountdown()
        {
            // start a countdown, during which players can unready to cancel

            for (int t = PrivateRoomHandler.ReadyStartGameCountdown; t >= 0; t--)
            {
                NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.RPCA_DisplayCountdown), t > 0 ? t.ToString() : "GO!");
                yield return new WaitForSecondsRealtime(1f);
            }
            int numReady = this.PrivateRoomCharacters.Where(p => p.ready).Count();
            if (numReady != this.NumCharacters || !this.GameCanStart) 
            {
                this.ResetHeaderText();
                yield break; 
            }

            this.lockReadies = true;

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
        private static void RPCA_LockReadies(bool lockReady)
        {
            PrivateRoomHandler.instance._lockReadies = lockReady;
        }
        [UnboundRPC]
        private static void RPCH_RequestLockReadies()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(RPCA_LockReadies), PrivateRoomHandler.instance.lockReadies);
            }
        }

        private IEnumerator StartGamePreparation()
        {
            // this is only executed on the host, so its safe to use Random()

            Dictionary<int, int> colorToTeam = new Dictionary<int, int>() { };

            // assign teamIDs according to colorIDs, in a random order
            int nextTeamID = 0;
            foreach (LobbyCharacter player in this.PrivateRoomCharacters.OrderBy(_ => UnityEngine.Random.Range(0f,1f)))
            {
                if (colorToTeam.TryGetValue(player.colorID, out int teamID))
                {
                }
                else
                {
                    colorToTeam[player.colorID] = nextTeamID;
                    nextTeamID++;
                }
            }

            yield return this.SyncMethod(nameof(PrivateRoomHandler.AssignTeamIDs), null, colorToTeam);

            foreach (var player in this.PrivateRoomCharacters.OrderBy(p => p.teamID).ThenBy(_ => UnityEngine.Random.Range(0f,1f)))
            {
                yield return this.SyncMethod(nameof(PrivateRoomHandler.CreatePlayer), player.actorID, player);
            }

            NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.StartGame));
            
            this.grid.transform.parent.GetChild(1).gameObject.SetActive(false);
        }
        [UnboundRPC]
        public static void AssignTeamIDs(Dictionary<int,int> colorIDtoTeamID)
        {
            LobbyCharacter[] localCharacters = PhotonNetwork.LocalPlayer.GetProperty<LobbyCharacter[]>("players");

            // assign teamIDs according to colorIDs
            for (int localID = 0; localID < localCharacters.Count(); localID++)
            {
                if (localCharacters[localID] == null) { continue; }
                localCharacters[localID].teamID = colorIDtoTeamID[localCharacters[localID].colorID];
            }

            PhotonNetwork.LocalPlayer.SetProperty("players", localCharacters);

            NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.AssignTeamIDsResponse), PhotonNetwork.LocalPlayer.ActorNumber);
        }

        [UnboundRPC]
        public static void CreatePlayer(LobbyCharacter player)
        {
            if (player.IsMine)
            {
                var instance = PrivateRoomHandler.instance;
                instance.StartCoroutine(instance.CreatePlayerCoroutine(player));
                VersusDisplay.instance.PlayerSelectorGO(player.uniqueID).GetComponent<PrivateRoomCharacterSelectionInstance>().Created();
            }
        }

        private IEnumerator CreatePlayerCoroutine(LobbyCharacter lobbyCharacter)
        {
            this.MainPage.Close();
            MainMenuHandler.instance.Close();

            RWFMod.instance.SetSoundEnabled("PlayerAdded", false);
            yield return PlayerAssigner.instance.CreatePlayer(lobbyCharacter, this.devicesToUse[lobbyCharacter.localID]);
            RWFMod.instance.SetSoundEnabled("PlayerAdded", true);

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
        public static void AssignTeamIDsResponse(int respondingPlayer)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PrivateRoomHandler.instance.RemovePendingRequest(respondingPlayer, nameof(PrivateRoomHandler.AssignTeamIDs));
            }
        }

        [UnboundRPC]
        public static void StartGame()
        {
            UIHandler.instance.ShowJoinGameText("LETS GOO!", PlayerSkinBank.GetPlayerSkinColors(1).winText);
            
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

        }

        [UnboundRPC]
        public static void RPC_RequestSync(int requestingPlayer)
        {
            NetworkingManager.RPC(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.RPC_SyncResponse), requestingPlayer, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        [UnboundRPC]
        public static void RPC_SyncResponse(int requestingPlayer, int readyPlayer)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == requestingPlayer)
            {
                PrivateRoomHandler.instance.RemovePendingRequest(readyPlayer, nameof(PrivateRoomHandler.RPC_RequestSync));
            }
        }

        private IEnumerator WaitForSyncUp()
        {
            if (PhotonNetwork.OfflineMode)
            {
                yield break;
            }

            yield return this.SyncMethod(nameof(PrivateRoomHandler.RPC_RequestSync), null, PhotonNetwork.LocalPlayer.ActorNumber);
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
            });
        }
    }
}
