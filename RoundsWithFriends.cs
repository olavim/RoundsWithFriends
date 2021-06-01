using System;
using System.Collections.Generic;
using BepInEx;
using UnityEngine;
using UnityEngine.UI;
using UnboundLib;
using Photon.Pun;

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

        public Text infoText;
        private Dictionary<string, bool> soundEnabled;

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
    }
}
