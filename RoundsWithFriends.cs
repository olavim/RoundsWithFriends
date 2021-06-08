using System;
using System.Collections.Generic;
using BepInEx;
using UnityEngine;
using UnityEngine.UI;
using UnboundLib;
using Photon.Pun;
using RWF.GameModes;
using TMPro;

namespace RWF
{
    public static class NetworkEventType
    {
        public static string ClientConnected = "client_connected";
        public static string SetTeamSize = "set_team_size";
    }

    [BepInDependency("com.willis.rounds.unbound", "1.0.0.4")]
    [BepInPlugin(ModId, "RoundsWithFriends", "1.0.0")]
    public class RWFMod : BaseUnityPlugin
    {
        private const string ModId = "io.olavim.rounds.rwf";

#if DEBUG
        public static readonly bool DEBUG = true;
#else
        public static readonly bool DEBUG = false;
#endif

        public static RWFMod instance;

        public static string GetCustomPropertyKey(string prop) {
            return $"{ModId}/{prop}";
        }

        public static void DebugLog(object obj) {
            if (obj == null) {
                obj = "null";
            }
            instance.Logger.LogMessage(obj);
        }

        public static void Log(object obj) {
            if (obj == null) {
                obj = "null";
            }
            instance.Logger.LogInfo(obj);
        }

        public static bool IsSteamConnected {
            get {
                try {
                    Steamworks.InteropHelp.TestIfAvailableClient();
                    return true;
                } catch (Exception e) {
                    return false;
                }
            }
        }

        public int MaxPlayers {
            get {
                return 4;
            }
        }

        public int MinPlayers {
            get {
                return 2;
            }
        }

        public int MaxTeams {
            get {
                return this.GameMode == this.gameModes["Deathmatch"] ? 4 : 2;
            }
        }

        public IGameMode GameMode { get; private set; }

        public Text infoText;
        private Dictionary<string, bool> soundEnabled;
        private Dictionary<string, IGameMode> gameModes = new Dictionary<string, IGameMode>();

        public void Awake() {
            RWFMod.instance = this;

            try {
                Patches.PatchUtils.ApplyPatches(ModId);
                this.Logger.LogInfo("initialized");
            } catch (Exception e) {
                this.Logger.LogError(e.ToString());
            }
        }

        public void Start() {
            this.soundEnabled = new Dictionary<string, bool>();

            Unbound.RegisterHandshake(ModId, () => {
                PhotonNetwork.LocalPlayer.SetModded();
            });
        }

        private void SetGameMode(string gameMode) {
            PlayerManager.instance.SetPropertyValue("PlayerJoinedAction", null);
            PlayerManager.instance.SetFieldValue("PlayerDiedAction", null);

            var charSelectGo = GameObject.Find("Game/UI/UI_MainMenu/Canvas/ListSelector/CharacterSelect");
            if (charSelectGo) {
                var menu = charSelectGo.GetComponent<CharacterSelectionMenu>();
                var menuPlayerJoined = ExtensionMethods.GetMethodInfo(typeof(CharacterSelectionMenu), "PlayerJoined");
                Action<Player> playerJoinedAction = (player) => menu.InvokeMethod("PlayerJoined", player);
                PlayerManager.instance.SetPropertyValue("PlayerJoinedAction", Delegate.Combine(PlayerManager.instance.PlayerJoinedAction, playerJoinedAction));
            }

            this.GameMode.gameObject.SetActive(false);
            this.GameMode = this.gameModes[gameMode];

            PlayerManager.instance.AddPlayerJoinedAction(this.GameMode.PlayerJoined);
            PlayerManager.instance.AddPlayerDiedAction(this.GameMode.PlayerDied);

            this.GameMode.gameObject.SetActive(true);

            this.RedrawCharacterSelections();
            this.RedrawCharacterCreators();
        }

        private void RedrawCharacterSelections() {
            var uiGo = GameObject.Find("/Game/UI").gameObject;
            var mainMenuGo = uiGo.transform.Find("UI_MainMenu").Find("Canvas").gameObject;
            var charSelectionGroupGo = mainMenuGo.transform.Find("ListSelector").Find("CharacterSelect").GetChild(0).gameObject;

            for (int i = 0; i < charSelectionGroupGo.transform.childCount; i++) {
                var charSelGo = charSelectionGroupGo.transform.GetChild(i).gameObject;
                var faceGo = charSelGo.transform.GetChild(0).gameObject;
                var joinGo = charSelGo.transform.GetChild(1).gameObject;
                var readyGo = charSelGo.transform.GetChild(2).gameObject;

                var textColor = PlayerSkinBank.GetPlayerSkinColors(i % this.MaxTeams).winText;
                var faceColor = PlayerSkinBank.GetPlayerSkinColors(i % this.MaxTeams).color;

                joinGo.GetComponentInChildren<GeneralParticleSystem>().particleSettings.color = textColor;
                readyGo.GetComponentInChildren<GeneralParticleSystem>().particleSettings.color = textColor;

                foreach (Transform faceSelector in faceGo.transform.GetChild(0)) {
                    faceSelector.Find("PlayerScaler_Small").Find("Face").GetComponent<SpriteRenderer>().color = faceColor;
                }
            }
        }

        private void RedrawCharacterCreators() {
            var charGo = GameObject.Find("/CharacterCustom");

            for (int i = 1; i < charGo.transform.childCount; i++) {
                var creatorGo = charGo.transform.GetChild(i);
                int playerID = i - 1;
                int teamID = playerID % this.MaxTeams;
                var faceColor = PlayerSkinBank.GetPlayerSkinColors(teamID).color;

                var buttonSource = creatorGo.transform.Find("Canvas").Find("Items").GetChild(0);
                buttonSource.Find("Face").gameObject.GetComponent<Image>().color = faceColor;

                foreach (Transform scaler in creatorGo.transform.Find("Faces")) {
                    scaler.Find("Face").GetComponent<SpriteRenderer>().color = faceColor;
                }
            }
        }

        public void SetSoundEnabled(string key, bool enabled) {
            if (!this.soundEnabled.ContainsKey(key)) {
                this.soundEnabled.Add(key, enabled);
            } else {
                this.soundEnabled[key] = enabled;
            }
        }

        public bool GetSoundEnabled(string key) {
            return this.soundEnabled.ContainsKey(key) ? this.soundEnabled[key] : true;
        }

        public void InjectGameModes() {
            var gameModesGo = GameObject.Find("/Game/Code/Game Modes");

            if (gameModesGo.transform.Find("[GameMode] Deathmatch")) {
                return;
            } else {
                this.gameModes.Clear();
            }

            var versusGo = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/GameMode").transform.Find("Group").Find("Versus").gameObject;
            var characterSelectGo = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/CharacterSelect");

            var versusText = versusGo.GetComponentInChildren<TextMeshProUGUI>();
            versusText.text = "TEAM DEATHMATCH";

            var characterSelectPage = characterSelectGo.GetComponent<ListMenuPage>();
            GameObject.DestroyImmediate(versusGo.GetComponent<Button>());
            var versusButton = versusGo.AddComponent<Button>();

            versusButton.onClick.AddListener(characterSelectPage.Open);
            versusButton.onClick.AddListener(() => this.SetGameMode("ArmsRace"));

            var deathmatchButtonGo = GameObject.Instantiate(versusGo, versusGo.transform.parent);
            deathmatchButtonGo.transform.localScale = Vector3.one;
            deathmatchButtonGo.transform.SetSiblingIndex(1);

            var deathmatchButtonText = deathmatchButtonGo.GetComponentInChildren<TextMeshProUGUI>();
            deathmatchButtonText.text = "DEATHMATCH";

            GameObject.DestroyImmediate(deathmatchButtonGo.GetComponent<Button>());
            var deathmatchButton = deathmatchButtonGo.AddComponent<Button>();

            deathmatchButton.onClick.AddListener(characterSelectPage.Open);
            deathmatchButton.onClick.AddListener(() => this.SetGameMode("Deathmatch"));

            var deathmatchGo = new GameObject("[GameMode] Deathmatch");
            deathmatchGo.SetActive(false);
            deathmatchGo.transform.SetParent(gameModesGo.transform);
            var deathMatch = deathmatchGo.AddComponent<Deathmatch>();

            this.gameModes.Add("ArmsRace", new ArmsRace());
            this.gameModes.Add("Deathmatch", deathMatch);
            this.GameMode = this.gameModes["ArmsRace"];
        }

        public void InjectUIElements() {
            var uiGo = GameObject.Find("/Game/UI");
            var charGo = GameObject.Find("/CharacterCustom");
            var gameGo = uiGo.transform.Find("UI_Game").Find("Canvas").gameObject;
            var mainMenuGo = uiGo.transform.Find("UI_MainMenu").Find("Canvas").gameObject;
            var charSelectionGroupGo = mainMenuGo.transform.Find("ListSelector").Find("CharacterSelect").GetChild(0).gameObject;

            if (!charSelectionGroupGo.transform.Find("CharacterSelect 3")) {
                var charSelectInstanceGo1 = charSelectionGroupGo.transform.GetChild(0).gameObject;
                var charSelectInstanceGo2 = charSelectionGroupGo.transform.GetChild(1).gameObject;

                var charSelectInstanceGo3 = GameObject.Instantiate(charSelectInstanceGo1, charSelectionGroupGo.transform);
                charSelectInstanceGo3.name = "CharacterSelect 3";
                charSelectInstanceGo3.transform.localScale = Vector3.one;

                charSelectInstanceGo3.transform.position = charSelectInstanceGo1.transform.position - new Vector3(0, 6, 0);
                charSelectInstanceGo1.transform.position += new Vector3(0, 6, 0);

                foreach (var portrait in charSelectInstanceGo3.transform.GetChild(0).GetChild(0).GetComponentsInChildren<CharacterCreatorPortrait>()) {
                    portrait.playerId = 2;
                }

                var charSelectInstanceGo4 = GameObject.Instantiate(charSelectInstanceGo2, charSelectionGroupGo.transform);
                charSelectInstanceGo4.name = "CharacterSelect 4";
                charSelectInstanceGo4.transform.localScale = Vector3.one;

                charSelectInstanceGo4.transform.position = charSelectInstanceGo2.transform.position - new Vector3(0, 6, 0);
                charSelectInstanceGo2.transform.position += new Vector3(0, 6, 0);

                charSelectionGroupGo.GetComponent<GoBack>().goBackEvent.AddListener(charSelectInstanceGo3.GetComponent<CharacterSelectionInstance>().ResetMenu);
                charSelectionGroupGo.GetComponent<GoBack>().goBackEvent.AddListener(charSelectInstanceGo4.GetComponent<CharacterSelectionInstance>().ResetMenu);

                foreach (var portrait in charSelectInstanceGo4.transform.GetChild(0).GetChild(0).GetComponentsInChildren<CharacterCreatorPortrait>()) {
                    portrait.playerId = 3;
                }
            }

            if (!gameGo.transform.Find("PrivateRoom")) {
                var privateRoomGo = new GameObject("PrivateRoom");
                privateRoomGo.transform.SetParent(gameGo.transform);
                privateRoomGo.transform.localScale = Vector3.one;

                privateRoomGo.AddComponent<PrivateRoomHandler>();

                var inviteFriendGo = mainMenuGo.transform.Find("ListSelector").Find("Online").Find("Group").Find("Invite friend").gameObject;
                GameObject.DestroyImmediate(inviteFriendGo.GetComponent<Button>());
                var button = inviteFriendGo.AddComponent<Button>();

                button.onClick.AddListener(() => {
                    PrivateRoomHandler.instance.Open();
                    ArtHandler.instance.NextArt();
                    NetworkConnectionHandler.instance.HostPrivate();
                });
            }

            if (!charGo.transform.Find("Creator_Local3")) {
                var creatorGo1 = charGo.transform.GetChild(1).gameObject;
                var creatorGo2 = charGo.transform.GetChild(2).gameObject;

                creatorGo1.transform.localPosition = new Vector3(-15, 8, 0);

                // Looks nicer when the right-side CharacterCreator is a bit further to the right
                creatorGo2.transform.localPosition = new Vector3(18, 8, 0);

                var creatorGo3 = GameObject.Instantiate(creatorGo1, charGo.transform);
                creatorGo3.name = "Creator_Local3";
                creatorGo3.transform.localScale = Vector3.one;
                creatorGo3.GetComponent<CharacterCreator>().playerID = 2;

                var creatorGo4 = GameObject.Instantiate(creatorGo2, charGo.transform);
                creatorGo4.name = "Creator_Local4";
                creatorGo4.transform.localScale = Vector3.one;
                creatorGo4.GetComponent<CharacterCreator>().playerID = 3;
            }

            if (!gameGo.transform.Find("RoundStartText")) {
                var newPos = gameGo.transform.position + new Vector3(0, 2, 0);
                var baseGo = GameObject.Instantiate(gameGo.transform.Find("GameOverText").gameObject, newPos, Quaternion.identity, gameGo.transform);
                baseGo.name = "RoundStartText";
                baseGo.AddComponent<UI.ScalePulse>();
                baseGo.GetComponent<TextMeshProUGUI>().fontSize = 140f;
                baseGo.GetComponent<TextMeshProUGUI>().fontWeight = FontWeight.Bold;
            }
        }
    }
}
