using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using RWF.Patches;
using UnboundLib.GameModes;
using InControl;
using RWF.UI;
using UnboundLib;

namespace RWF
{
    class VersusDisplay : MonoBehaviour
    {
        private Dictionary<int, int> teamPlayers = new Dictionary<int, int>();
        private Dictionary<int, int> colorToTeam = new Dictionary<int, int>() { };
        private Dictionary<int, int> teamToColor = new Dictionary<int, int>() { };

        private void Start() {
            this.gameObject.AddComponent<CanvasRenderer>();
            var fitter = this.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var horizLayout = this.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizLayout.childAlignment = TextAnchor.MiddleCenter;
            horizLayout.spacing = 20;

            this.UpdatePlayers();
        }

        public GameObject AddPlayerGroup() {
            var go = new GameObject("Players");
            go.transform.SetParent(this.transform);
            go.transform.localScale = Vector3.one;

            go.AddComponent<RectTransform>();
            var layoutGroup = go.AddComponent<VerticalLayoutGroup>();
            var sizer = go.AddComponent<ContentSizeFitter>();
            sizer.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.spacing = 35;

            return go;
        }

        public void UpdatePlayers() {
            UnityEngine.Debug.Log("STOP ALL COROUTINES");
            this.StopAllCoroutines();
            this.UpdatePlayersCoroutine();
        }

        private void UpdatePlayersCoroutine() {
            this.teamPlayers.Clear();
            this.colorToTeam.Clear();
            this.teamToColor.Clear();
            UnityEngine.Debug.Log("UPDATE PLAYERS");
            while (this.transform.childCount > 0)
            {
                GameObject.DestroyImmediate(this.transform.GetChild(0).gameObject);
            }

            this.transform.localPosition = new Vector3(0, this.transform.localPosition.y, 0);
            List<GameObject> groups = new List<GameObject>();
            int teamCount = PhotonNetwork.CurrentRoom.Players.Select(kv => kv.Value.GetProperty<LobbyCharacter[]>("players")).SelectMany(p => p).Where(p=>p!=null).Select(p => p.colorID).Distinct().Count();

            for (int i = 0; i < teamCount; i++) {
                this.teamPlayers.Add(i, 0);

                groups.Add(this.AddPlayerGroup());
            }

            if (PhotonNetwork.CurrentRoom != null) {
                UnityEngine.Debug.Log("current room is not null");
                List<LobbyCharacter> players = PhotonNetwork.CurrentRoom.Players.Select(kv => kv.Value.GetProperty<LobbyCharacter[]>("players")).SelectMany(p => p).Where(p => p != null).ToList();

                players.OrderBy(p => p.colorID);

                // assign teamIDs according to colorIDs
                int nextTeamID = 0;
                foreach (LobbyCharacter player in players) {
                    UnityEngine.Debug.Log("ASSIGN TEAM TO " + player.NickName);
                    if (colorToTeam.TryGetValue(player.colorID, out int teamID))
                    {
                        player.teamID = teamID;
                    }
                    else
                    {
                        player.teamID = nextTeamID;
                        colorToTeam[player.colorID] = nextTeamID;
                        teamToColor[player.teamID] = player.colorID;
                        nextTeamID++;
                    }

                    UnityEngine.Debug.Log("MAKE LOBBY PLAYER GO");
                    var playerGo = new GameObject("LobbyPlayer");
                    playerGo.transform.SetParent(groups[player.teamID].transform);
                    playerGo.transform.localScale = Vector3.one;

                    /*
                    UnityEngine.Debug.Log("MAKE PLAYER NAME GO");
                    var playerNameGo = this.CreatePlayerName(player.NickName, player.colorID, player.ready);
                    playerNameGo.transform.SetParent(playerGo.transform);
                    playerNameGo.transform.localScale = Vector3.one;
                    */
                    UnityEngine.Debug.Log("MAKE CHARACTER SELECTOR");
                    this.ExecuteAfterFrames(1, () =>
                    {
                        GameObject charSelect = this.CreatePlayerSelector(player.NickName, player, player.IsMine ? PrivateRoomHandler.instance.devicesToUse[player.localID] : null, player.IsMine);
                        charSelect.transform.SetParent(playerGo.transform);
                        charSelect.transform.localScale = Vector3.one;
                        UnityEngine.Debug.Log("PLAYER SELECTOR CREATED. TEAMID: " + player.teamID.ToString());
                    });

                    playerGo.AddComponent<RectTransform>();
                    playerGo.AddComponent<VerticalLayoutGroup>();
                    var sizer = playerGo.AddComponent<ContentSizeFitter>();
                    sizer.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    sizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                    this.teamPlayers[player.teamID] = this.teamPlayers[player.teamID] + 1;
                }

                // add team names to the top of each group
                foreach (int teamID in this.teamPlayers.Keys.OrderBy(i => i))
                {
                    var teamGo = new GameObject("TeamName");
                    teamGo.transform.SetParent(groups[teamID].transform);
                    teamGo.transform.localScale = Vector3.one;
                    teamGo.transform.SetAsFirstSibling();

                    teamGo.AddComponent<RectTransform>();
                    teamGo.AddComponent<VerticalLayoutGroup>();
                    var sizer = teamGo.AddComponent<ContentSizeFitter>();
                    sizer.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    sizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                    var teamNameGo = this.CreateTeamName(ExtraPlayerSkins.GetTeamColorName(this.teamToColor[teamID]).ToUpper(), this.teamToColor[teamID]);
                    teamNameGo.transform.SetParent(teamGo.transform);
                    teamNameGo.transform.localScale = Vector3.one;
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(this.gameObject.GetComponent<RectTransform>());

            //var middleChild = this.transform.GetChild(Mathf.FloorToInt(this.transform.childCount / 2f));
            //this.transform.localPosition -= new Vector3(middleChild.localPosition.x, 0, 0);
        }
        private GameObject CreatePlayerSelector(string name, LobbyCharacter character, InputDevice device, bool inControl)
        {
            UnityEngine.Debug.Log("START");
            GameObject orig = UnityEngine.GameObject.Find("Game/UI/UI_MainMenu/Canvas/ListSelector/CharacterSelect/Group").transform.GetChild(0).gameObject;
            UnityEngine.Debug.Log("FOUND");
            GameObject selector = GameObject.Instantiate(orig);
            UnityEngine.Debug.Log("CLONED");
            selector.SetActive(true);
            selector.name = $"CharacterSelector {name}";
            selector.GetOrAddComponent<RectTransform>();
            UnityEngine.Debug.Log("NAME AND RECT");
            var sizer = selector.AddComponent<ContentSizeFitter>();
            sizer.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            UnityEngine.Debug.Log("SIZER");
            //UnityEngine.GameObject.Destroy(selector.GetComponent<CharacterSelectionInstance>());
            PrivateRoomCharacterSelectionInstance charSelect = selector.AddComponent<PrivateRoomCharacterSelectionInstance>();
            UnityEngine.Debug.Log("SELECTOR");
            this.ExecuteAfterFrames(5, () => charSelect.StartPicking(character, device, inControl));
            UnityEngine.Debug.Log("START PICKING");
            selector.transform.localPosition = Vector2.zero;
            if (selector != null) { UnityEngine.Debug.Log("SELECTOR CREATED SUCCESSFULLY"); }
            return selector;
        }

        private GameObject CreatePlayerName(string name, int colorID, bool isReady) {
            var playerNameGo = GameObject.Instantiate(RoundsResources.FlickeringTextPrefab);
            playerNameGo.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 92);

            var nameText = playerNameGo.GetComponent<TextMeshProUGUI>();
            nameText.fontSize = 40;
            nameText.font = RoundsResources.MenuFont;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.enableWordWrapping = false;
            nameText.color = new Color32(85, 90, 98, 255);
            nameText.text = name;
            nameText.autoSizeTextContainer = true;

            var sizer = playerNameGo.AddComponent<ContentSizeFitter>();
            sizer.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var particleSystem = playerNameGo.GetComponentInChildren<GeneralParticleSystem>();

            if (isReady) {
                particleSystem.particleSettings.size = 4;
                particleSystem.particleSettings.color = PlayerSkinBank.GetPlayerSkinColors(colorID).winText;
                particleSystem.particleSettings.randomAddedColor = PlayerSkinBank.GetPlayerSkinColors(colorID).backgroundColor;
                particleSystem.particleSettings.randomColor = new Color32(255, 0, 255, 255);
            } else {
                playerNameGo.GetComponent<Mask>().enabled = false;
                playerNameGo.transform.Find("UI_ParticleSystem").gameObject.SetActive(false);
            }

            return playerNameGo;
        }
        private GameObject CreateTeamName(string name, int colorID)
        {
            var teamNameGo = GameObject.Instantiate(RoundsResources.FlickeringTextPrefab);
            teamNameGo.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 92);

            var nameText = teamNameGo.GetComponent<TextMeshProUGUI>();
            nameText.fontSize = 45;
            nameText.font = RoundsResources.MenuFont;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.overflowMode = TextOverflowModes.Overflow;
            nameText.enableWordWrapping = true;
            nameText.color = new Color32(85, 90, 98, 255);
            nameText.text = $"{((GameModeManager.CurrentHandler.Settings.TryGetValue("allowTeams", out object allowTeamsObj2) && !(bool) allowTeamsObj2) ? "" : "TEAM ")}"+name;
            nameText.autoSizeTextContainer = true;

            var sizer = teamNameGo.AddComponent<ContentSizeFitter>();
            sizer.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var particleSystem = teamNameGo.GetComponentInChildren<GeneralParticleSystem>();

            particleSystem.particleSettings.size = 4;
            particleSystem.particleSettings.color = PlayerSkinBank.GetPlayerSkinColors(colorID).winText;
            particleSystem.particleSettings.randomAddedColor = PlayerSkinBank.GetPlayerSkinColors(colorID).backgroundColor;
            particleSystem.particleSettings.randomColor = new Color32(255, 0, 255, 255);
            


            return teamNameGo;
        }
    }
}
