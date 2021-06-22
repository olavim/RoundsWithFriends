using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

namespace RWF
{
    class VersusDisplay : MonoBehaviour
    {
        private Dictionary<int, int> teamPlayers = new Dictionary<int, int>();

        private void Start() {
            this.gameObject.AddComponent<CanvasRenderer>();
            var fitter = this.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var horizLayout = this.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizLayout.childAlignment = TextAnchor.MiddleCenter;

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

            return go;
        }

        public GameObject AddVSText() {
            var teamCount = Mathf.Min(PhotonNetwork.CurrentRoom.PlayerCount, RWFMod.instance.MaxTeams);

            var go = GameObject.Instantiate(RoundsResources.FlickeringTextPrefab, this.transform);
            go.name = "VS";
            go.transform.localScale = Vector3.one;

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = 200;

            var vsParticleSystem = go.GetComponentInChildren<GeneralParticleSystem>();
            vsParticleSystem.particleSettings.size = 4;
            vsParticleSystem.particleSettings.color = Color.white;
            vsParticleSystem.particleSettings.randomColor = new Color32(255, 0, 255, 255);
            vsParticleSystem.particleSettings.randomAddedColor = new Color32(72, 60, 43, 255);

            var vsText = go.GetComponent<TextMeshProUGUI>();
            vsText.fontSize = teamCount > 2 ? 60 : 100;
            vsText.font = RoundsResources.MenuFont;
            vsText.alignment = TextAlignmentOptions.Center;
            vsText.text = "VS";

            return go;
        }

        public void UpdatePlayers() {
            this.StopAllCoroutines();
            this.UpdatePlayersCoroutine();
        }

        private void UpdatePlayersCoroutine() {
            this.teamPlayers.Clear();

            while (this.transform.childCount > 0) {
                GameObject.DestroyImmediate(this.transform.GetChild(0).gameObject);
            }

            this.transform.localPosition = new Vector3(0, this.transform.localPosition.y, 0);
            var groups = new List<GameObject>();
            var teamCount = Mathf.Min(PhotonNetwork.CurrentRoom.PlayerCount, RWFMod.instance.MaxTeams);

            for (int i = 0; i < teamCount; i++) {
                this.teamPlayers.Add(i, 0);

                if (i != 0) {
                    this.AddVSText();
                }

                groups.Add(this.AddPlayerGroup());
            }

            if (PhotonNetwork.CurrentRoom != null) {
                var networkPlayers = PhotonNetwork.CurrentRoom.Players.Values.ToList();

                networkPlayers.Sort((np1, np2) => {
                    bool ready1 = np1.GetProperty<bool>("ready");
                    bool ready2 = np2.GetProperty<bool>("ready");
                    int order1 = np1.GetProperty<int>("readyOrder");
                    int order2 = np2.GetProperty<int>("readyOrder");

                    if (ready1 && ready2) {
                        return order1 - order2;
                    }

                    if (ready1 || ready2) {
                        return ready1 ? -1 : 1;
                    }

                    return np1.ActorNumber - np2.ActorNumber;
                });

                foreach (var networkPlayer in networkPlayers) {
                    var team = this.GetNetworkPlayerTeam(networkPlayer);

                    var playerGo = new GameObject("NetworkPlayer");
                    playerGo.transform.SetParent(groups[team].transform);
                    playerGo.transform.localScale = Vector3.one;

                    var playerNameGo = CreatePlayerName(networkPlayer.NickName, team, networkPlayer.GetProperty<bool>("ready"));
                    playerNameGo.transform.SetParent(playerGo.transform);
                    playerNameGo.transform.localScale = Vector3.one;

                    playerGo.AddComponent<RectTransform>();
                    playerGo.AddComponent<VerticalLayoutGroup>();
                    var sizer = playerGo.AddComponent<ContentSizeFitter>();
                    sizer.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    sizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                    this.teamPlayers[team] = this.teamPlayers[team] + 1;
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(this.gameObject.GetComponent<RectTransform>());

            var middleChild = this.transform.GetChild(Mathf.FloorToInt(this.transform.childCount / 2f));
            this.transform.localPosition -= new Vector3(middleChild.localPosition.x, 0, 0);
        }

        private GameObject CreatePlayerName(string name, int team, bool isReady) {
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
                particleSystem.particleSettings.color = PlayerSkinBank.GetPlayerSkinColors(team).winText;
                particleSystem.particleSettings.randomAddedColor = PlayerSkinBank.GetPlayerSkinColors(team).backgroundColor;
                particleSystem.particleSettings.randomColor = new Color32(255, 0, 255, 255);
            } else {
                playerNameGo.GetComponent<Mask>().enabled = false;
                playerNameGo.transform.Find("UI_ParticleSystem").gameObject.SetActive(false);
            }

            return playerNameGo;
        }

        private int GetNetworkPlayerTeam(Photon.Realtime.Player networkPlayer) {
            if (networkPlayer.GetProperty<bool>("ready")) {
                return networkPlayer.GetProperty<int>("readyOrder") % RWFMod.instance.MaxTeams;
            }

            for (int i = 0; i < this.teamPlayers.Count - 1; i++) {
                if (this.teamPlayers[i] > this.teamPlayers[i + 1]) {
                    return i + 1;
                }
            }

            return 0;
        }
    }
}
