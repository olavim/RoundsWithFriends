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
using UnboundLib.Networking;

namespace RWF
{
    class VersusDisplay : MonoBehaviour
    {
        // the idea with the new versus display is to create all of the team groups immediately and just activate/deactivate them
        // as well as never destroy player objects once created, unless the player leaves

        private Dictionary<int, int> colorToTeam = new Dictionary<int, int>() { };
        private Dictionary<int, int> teamToColor = new Dictionary<int, int>() { };
        private Dictionary<int, GameObject> _teamGroupGOs = new Dictionary<int, GameObject>() { };
        private Dictionary<int, GameObject> _playerGOs = new Dictionary<int, GameObject>() { };
        private Dictionary<int, GameObject> _playerSelectorGOs = new Dictionary<int, GameObject>() { };
        private List<int> _playerSelectorGOsCreated = new List<int>() { };
        private GameObject _holder;

        private GameObject holder
        {
            get
            {
                if (this._holder == null)
                {
                    // child object to hold all pre-prepared playerGOs
                    this._holder = new GameObject("Hold");
                    this._holder.transform.SetParent(this.transform);
                    this._holder.SetActive(false);
                }
                return this._holder;
            }
        }

        internal GameObject TeamGroupGO(int teamID, int colorID)
        {
            if (!this._teamGroupGOs.TryGetValue(teamID, out GameObject teamGroupGO))
            {
                teamGroupGO = new GameObject($"Team {teamID}");
                teamGroupGO.transform.SetParent(this.transform);
                teamGroupGO.transform.SetSiblingIndex(teamID);
                teamGroupGO.transform.localScale = Vector3.one;

                teamGroupGO.AddComponent<RectTransform>();
                var layoutGroup = teamGroupGO.AddComponent<VerticalLayoutGroup>();
                var sizer = teamGroupGO.AddComponent<ContentSizeFitter>();
                sizer.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                sizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                layoutGroup.childAlignment = TextAnchor.MiddleCenter;
                layoutGroup.spacing = 50f;

                var teamGo = new GameObject($"TeamName {teamID}");
                teamGo.transform.SetParent(teamGroupGO.transform);
                teamGo.transform.localScale = Vector3.one;
                teamGo.transform.SetAsFirstSibling();

                teamGo.AddComponent<RectTransform>();
                teamGo.AddComponent<VerticalLayoutGroup>();
                var sizer1 = teamGo.AddComponent<ContentSizeFitter>();
                sizer1.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                sizer1.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                GameObject teamNameGo = GameObject.Instantiate(RoundsResources.FlickeringTextPrefab);
                teamNameGo.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 92);

                var nameText = teamNameGo.GetComponent<TextMeshProUGUI>();
                nameText.fontSize = 45;
                nameText.font = RoundsResources.MenuFont;
                nameText.alignment = TextAlignmentOptions.Center;
                nameText.overflowMode = TextOverflowModes.Overflow;
                nameText.enableWordWrapping = true;
                nameText.color = new Color32(85, 90, 98, 255);
                nameText.text = ExtraPlayerSkins.GetTeamColorName(colorID).ToUpper();
                nameText.fontStyle = FontStyles.Bold;
                nameText.autoSizeTextContainer = true;

                var sizer2 = teamNameGo.AddComponent<ContentSizeFitter>();
                sizer2.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

                // add grid layout group for players
                GameObject teamGridGO = new GameObject("Grid");
                teamGridGO.transform.SetParent(teamGroupGO.transform);
                teamGridGO.transform.SetAsLastSibling();
                teamGridGO.transform.localScale = Vector3.one;

                teamGridGO.AddComponent<RectTransform>();
                var layoutGroup1 = teamGridGO.AddComponent<GridLayoutGroup>();
                var sizer3 = teamGridGO.AddComponent<ContentSizeFitter>();
                sizer3.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                sizer3.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                layoutGroup1.childAlignment = TextAnchor.MiddleCenter;
                layoutGroup1.spacing = new Vector2(150f, 75);
                layoutGroup1.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layoutGroup1.constraintCount = 2;
                layoutGroup1.startCorner = GridLayoutGroup.Corner.UpperLeft;
                layoutGroup1.startAxis = GridLayoutGroup.Axis.Horizontal;
                layoutGroup1.cellSize = new Vector2(0f, 100f);


                var particleSystem = teamNameGo.GetComponentInChildren<GeneralParticleSystem>();

                particleSystem.particleSettings.size = 4;
                particleSystem.particleSettings.color = PlayerSkinBank.GetPlayerSkinColors(colorID).winText;
                particleSystem.particleSettings.randomAddedColor = PlayerSkinBank.GetPlayerSkinColors(colorID).backgroundColor;
                particleSystem.particleSettings.randomColor = PlayerSkinBank.GetPlayerSkinColors(colorID).color;

                teamNameGo.transform.SetParent(teamGo.transform);
                teamNameGo.transform.localScale = Vector3.one;
                teamNameGo.transform.SetAsFirstSibling();

                this._teamGroupGOs[teamID] = teamGroupGO;
            }

            if (teamGroupGO.transform.GetChild(0).GetChild(0).GetComponentInChildren<GeneralParticleSystem>().particleSettings.color != PlayerSkinBank.GetPlayerSkinColors(colorID).winText)
            {
                teamGroupGO.transform.GetChild(0).GetChild(0).GetComponentInChildren<GeneralParticleSystem>().Stop();
                teamGroupGO.transform.GetChild(0).GetChild(0).GetComponentInChildren<GeneralParticleSystem>().StopAllCoroutines();
                teamGroupGO.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = ExtraPlayerSkins.GetTeamColorName(colorID).ToUpper();
                teamGroupGO.transform.GetChild(0).GetChild(0).GetComponentInChildren<GeneralParticleSystem>().particleSettings.color = PlayerSkinBank.GetPlayerSkinColors(colorID).winText;
                teamGroupGO.transform.GetChild(0).GetChild(0).GetComponentInChildren<GeneralParticleSystem>().particleSettings.randomAddedColor = PlayerSkinBank.GetPlayerSkinColors(colorID).backgroundColor;
                teamGroupGO.transform.GetChild(0).GetChild(0).GetComponentInChildren<GeneralParticleSystem>().particleSettings.randomColor = PlayerSkinBank.GetPlayerSkinColors(colorID).color;
                ((ObjectPool) teamGroupGO.transform.GetChild(0).GetChild(0).GetComponentInChildren<GeneralParticleSystem>().GetFieldValue("particlePool")).ClearPool();
                teamGroupGO.transform.GetChild(0).GetChild(0).GetComponentInChildren<GeneralParticleSystem>().Play();
            }
            return teamGroupGO;
        }

        internal GameObject PlayerGO(int uniqueID)
        {
            if (!this._playerGOs.TryGetValue(uniqueID, out GameObject playerGO))
            {
                playerGO = new GameObject($"LobbyPlayer {uniqueID}");
                LobbyCharacter lobbyCharacter = LobbyCharacter.GetLobbyCharacter(uniqueID);
                GameObject teamGroupGO = this.TeamGroupGO(lobbyCharacter.teamID, lobbyCharacter.colorID);
                teamGroupGO.SetActive(true);
                playerGO.transform.SetParent(teamGroupGO.transform.GetChild(1));
                playerGO.transform.localScale = Vector3.one;
                playerGO.AddComponent<RectTransform>();
                playerGO.AddComponent<VerticalLayoutGroup>();
                var sizer = playerGO.AddComponent<ContentSizeFitter>();
                sizer.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                sizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                this._playerGOs[uniqueID] = playerGO;
            }

            return playerGO;
        }

        internal GameObject PlayerSelectorGO(int uniqueID)
        {
            if (!this._playerSelectorGOs.TryGetValue(uniqueID, out GameObject playerSelectorGO) && !this._playerSelectorGOsCreated.Contains(uniqueID))
            {
                LobbyCharacter player = LobbyCharacter.GetLobbyCharacter(uniqueID);
                this.CreatePlayerSelector(player.NickName, player, player.IsMine ? PrivateRoomHandler.instance.devicesToUse[player.localID] : null, this.PlayerGO(player.uniqueID).transform);
            }

            UnityEngine.Debug.Log($"UNIQUE ID: {uniqueID} - {(playerSelectorGO == null ? "NULL" : "EXISTS")}");

            return playerSelectorGO;
        }
        internal void SetPlayerSelectorGO(int uniqueID, GameObject playerSelectorGO)
        {
            this._playerSelectorGOs[uniqueID] = playerSelectorGO;
        }



        public static VersusDisplay instance;

        private void Awake()
        {
            VersusDisplay.instance = this;
        }

        private void Start() {
            this.gameObject.GetOrAddComponent<CanvasRenderer>();
            var fitter = this.gameObject.GetOrAddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var horizLayout = this.gameObject.GetOrAddComponent<HorizontalLayoutGroup>();
            horizLayout.childAlignment = TextAnchor.MiddleCenter;
            horizLayout.spacing = 100;

            /*
            // add team names to the top of each group
            for (int teamID = 0; teamID < RWFMod.MaxTeamsHardLimit; teamID++)
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

                var teamNameGo = this.CreateTeamName(ExtraPlayerSkins.GetTeamColorName(teamID).ToUpper(), teamID);
                teamNameGo.transform.SetParent(teamGo.transform);
                teamNameGo.transform.localScale = Vector3.one;
            }

            foreach (GameObject group in this.groups)
            {
                group.SetActive(false);
            }*/

            this.UpdatePlayers();
        }
        public void UpdatePlayers() {
            UnityEngine.Debug.Log("STOP ALL COROUTINES");
            this.StopAllCoroutines();
            this.StartCoroutine(this.UpdatePlayersCoroutine());
        }

        private IEnumerator UpdatePlayersCoroutine() {
            this.colorToTeam.Clear();
            this.teamToColor.Clear();
            //yield return this.WaitForSyncUp();
            UnityEngine.Debug.Log("UPDATE PLAYERS");

            // wait until all character creator instances exist
            bool wait = true;
            while (wait)
            { 
                wait = PhotonNetwork.CurrentRoom.Players.Select(kv => kv.Value.GetProperty<LobbyCharacter[]>("players")).SelectMany(p => p).Where(p => p != null && this.PlayerSelectorGO(p.uniqueID) == null).Any();

                yield return null;
            }

            if (PhotonNetwork.CurrentRoom != null) {
                UnityEngine.Debug.Log("current room is not null");
                List<LobbyCharacter> players = PhotonNetwork.CurrentRoom.Players.Select(kv => kv.Value.GetProperty<LobbyCharacter[]>("players")).SelectMany(p => p).Where(p => p != null).ToList();

                // assign teamIDs according to colorIDs
                int nextTeamID = 0;
                foreach (LobbyCharacter player in players.OrderBy(p => p.colorID)) {
                    UnityEngine.Debug.Log("ASSIGN TEAM TO " + player.NickName);
                    if (this.colorToTeam.TryGetValue(player.colorID, out int teamID))
                    {
                        player.teamID = teamID;
                    }
                    else
                    {
                        player.teamID = nextTeamID;
                        this.colorToTeam[player.colorID] = nextTeamID;
                        this.teamToColor[player.teamID] = player.colorID;
                        nextTeamID++;
                    }
                    UnityEngine.Debug.Log($"Player: {player.NickName}, TEAMID: {player.teamID}");

                    //yield return this.WaitForSyncUp();

                    GameObject teamGroupGO = this.TeamGroupGO(player.teamID, player.colorID);
                    teamGroupGO.SetActive(true);
                    this.PlayerGO(player.uniqueID).SetActive(true);
                    this.PlayerGO(player.uniqueID).transform.SetParent(teamGroupGO.transform.GetChild(1));
                    this.PlayerGO(player.uniqueID).transform.SetAsLastSibling();

                }
                this.HideEmptyTeams(players.Select(p => p.teamID).ToArray());
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(this.gameObject.GetComponent<RectTransform>());
            
            yield break;
            //var middleChild = this.transform.GetChild(Mathf.FloorToInt(this.transform.childCount / 2f));
            //this.transform.localPosition -= new Vector3(middleChild.localPosition.x, 0, 0);
        }

        private void HideEmptyTeams(int[] teamIDs)
        {
            foreach (int i in this._teamGroupGOs.Keys.Where(k => !teamIDs.Contains(k)))
            {
                this._teamGroupGOs[i].SetActive(false);
            }
        }

        private void CreatePlayerSelector(string name, LobbyCharacter character, InputDevice device, Transform parent)
        {
            if (!character.IsMine || this._playerSelectorGOsCreated.Contains(character.uniqueID)) { return; }
            this._playerSelectorGOsCreated.Add(character.uniqueID);
            parent.gameObject.SetActive(true);
            this.TeamGroupGO(character.teamID, character.colorID).SetActive(true);
            PhotonNetwork.Instantiate(
                PrivateRoomPrefabs.PrivateRoomCharacterSelectionInstance.name,
                parent.position,
                parent.rotation,
                0,
                new object[] { character.actorID, character.localID, device != null ? InputManager.ActiveDevices.IndexOf(device) : -1, name}
            );
        }
        private IEnumerator WaitForSyncUp()
        {
            if (PhotonNetwork.OfflineMode)
            {
                yield break;
            }

            yield return this.SyncMethod(nameof(VersusDisplay.RPC_RequestSync), null, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        [UnboundRPC]
        public static void RPC_RequestSync(int requestingPlayer)
        {
            NetworkingManager.RPC(typeof(VersusDisplay), nameof(VersusDisplay.RPC_SyncResponse), requestingPlayer, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        [UnboundRPC]
        public static void RPC_SyncResponse(int requestingPlayer, int readyPlayer)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == requestingPlayer)
            {
                VersusDisplay.instance.RemovePendingRequest(readyPlayer, nameof(VersusDisplay.RPC_RequestSync));
            }
        }
    }
}
