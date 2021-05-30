using System;
using System.Collections;
using BepInEx;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;

namespace RWF
{
    public static class NetworkEventType
    {
        public static string ClientConnected = "client_connected";
        public static string SetTeamSize = "set_team_size";
    }

    [BepInPlugin("io.olavim.plugins.rounds.rwf", "RoundsWithFriends", "1.0.0")]
    public class RWFMod : BaseUnityPlugin
    {
        public static RWFMod instance;

        public static readonly bool DEBUG = false;

        public static string GetCustomPropertyKey(string prop) {
            return "io.olavim.plugins.rounds.teams/" + prop;
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

        private int _teamSize = 2;

        public int TeamSize {
            get {
                return this._teamSize;
            }
        }

        public int MaxPlayers {
            get {
                return this._teamSize * 2;
            }
        }

        public int MinPlayers {
            get {
                return 2;
            }
        }

        public Text infoText;
        private bool returnToLobby = false;

        public void Awake() {
            RWFMod.instance = this;

            try {
                PatchUtils.ApplyPatches();
                this.gameObject.AddComponent<NetworkManager>();
                this.Logger.LogInfo("initialized");

                SceneManager.sceneLoaded += OnSceneLoaded;
            } catch (Exception e) {
                this.Logger.LogError(e.ToString());
            }
        }

        private IEnumerator ReturnToLobby() {
            yield return null;
            PrivateRoomHandler.instance.Open();
            PrivateRoomHandler.instance.UpdatePlayerDisplay();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (scene.name == "Main" && this.returnToLobby) {
                this.returnToLobby = false;
                this.StartCoroutine(ReturnToLobby());
            }
        }

        public void Start() {
            this.SetTeamSize(2);
        }

        public void ReturnToLobbyOnSceneLoad() {
            this.returnToLobby = true;
        }

        public void InjectUIElements() {
            var uiGo = GameObject.Find("/Game/UI");
            var gameGo = uiGo.transform.Find("UI_Game").Find("Canvas").gameObject;
            var mainMenuGo = uiGo.transform.Find("UI_MainMenu").Find("Canvas").gameObject;
            var charSelectionGroupGo = mainMenuGo.transform.Find("ListSelector").Find("CharacterSelect").GetChild(0).gameObject;

            if (!charSelectionGroupGo.transform.Find("CharacterSelect 3")) {
                var charSelectInstanceGo1 = charSelectionGroupGo.transform.GetChild(0).gameObject;
                var charSelectInstanceGo2 = charSelectionGroupGo.transform.GetChild(1).gameObject;

                var charSelectInstanceGo3 = UnityEngine.Object.Instantiate(charSelectInstanceGo1);
                charSelectInstanceGo3.name = "CharacterSelect 3";
                charSelectInstanceGo3.transform.SetParent(charSelectionGroupGo.transform);
                charSelectInstanceGo3.transform.localScale = Vector3.one;

                charSelectInstanceGo3.transform.position = charSelectInstanceGo1.transform.position - new Vector3(0, 6, 0);
                charSelectInstanceGo1.transform.position += new Vector3(0, 6, 0);

                var charSelectInstanceGo4 = UnityEngine.Object.Instantiate(charSelectInstanceGo2);
                charSelectInstanceGo4.name = "CharacterSelect 4";
                charSelectInstanceGo4.transform.SetParent(charSelectionGroupGo.transform);
                charSelectInstanceGo4.transform.localScale = Vector3.one;

                charSelectInstanceGo4.transform.position = charSelectInstanceGo2.transform.position - new Vector3(0, 6, 0);
                charSelectInstanceGo2.transform.position += new Vector3(0, 6, 0);

                charSelectionGroupGo.GetComponent<GoBack>().goBackEvent.AddListener(charSelectInstanceGo3.GetComponent<CharacterSelectionInstance>().ResetMenu);
                charSelectionGroupGo.GetComponent<GoBack>().goBackEvent.AddListener(charSelectInstanceGo4.GetComponent<CharacterSelectionInstance>().ResetMenu);
            }

            if (!gameGo.transform.Find("PrivateRoom")) {
                var privateRoomGo = new GameObject("PrivateRoom");
                privateRoomGo.transform.SetParent(gameGo.transform);
                privateRoomGo.transform.localScale = Vector3.one;

                privateRoomGo.AddComponent<PrivateRoomHandler>();

                var inviteFriendGo = mainMenuGo.transform.Find("ListSelector").Find("Online").Find("Group").Find("Invite friend").gameObject;
                UnityEngine.Object.DestroyImmediate(inviteFriendGo.GetComponent<Button>());
                var button = inviteFriendGo.AddComponent<Button>();

                button.onClick.AddListener(() => {
                    PrivateRoomHandler.instance.Open();
                    ArtHandler.instance.NextArt();
                    NetworkConnectionHandler.instance.HostPrivate();
                });
            }
        }

        public void SetTeamSize(int teamSize) {
            try {
                this._teamSize = teamSize;

                if (GM_ArmsRace.instance != null) {
                    GM_ArmsRace.instance.SetPlayersNeededToStart(2);
                }

                if (PlayerAssigner.instance != null) {
                    PlayerAssigner.instance.maxPlayers = teamSize * 2;
                }

                this.SyncOptions();
            } catch (Exception e) {
                this.Logger.LogError(e.ToString());
            }
        }

        public void SyncOptions() {
            // Sync team size to clients via room custom properties
            if (PhotonNetwork.IsMasterClient) {
                this.SyncCustomProperty("teamSize", this.TeamSize);
            }
        }

        // Syncs RoomInfo.CustomProperies if some value has changed
        private void SyncCustomProperty<T>(string _key, T newValue) where T : IComparable {
            string key = RWFMod.GetCustomPropertyKey(_key);
            ExitGames.Client.Photon.Hashtable props = PhotonNetwork.CurrentRoom.CustomProperties;

            bool changed = false;

            if (!props.ContainsKey(key)) {
                props.Add(key, newValue);
                changed = true;
            }

            T oldValue = (T) props[key];

            if (newValue.CompareTo(oldValue) != 0) {
                props[key] = newValue;
                changed = true;
            }

            if (changed) {
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }
        }
    }
}
