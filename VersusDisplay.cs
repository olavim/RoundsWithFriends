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

            var horizLayout = this.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizLayout.childAlignment = TextAnchor.MiddleCenter;

            var team1Go = new GameObject("Team1");
            team1Go.transform.SetParent(this.transform);
            team1Go.transform.localScale = Vector3.one;

            var vsGo = GameObject.Instantiate(RoundsResources.FlickeringTextPrefab, this.transform);
            vsGo.name = "VS";
            vsGo.transform.localScale = Vector3.one;

            var team2Go = new GameObject("Team2");
            team2Go.transform.SetParent(this.transform);
            team2Go.transform.localScale = Vector3.one;

            var team1Rect = team1Go.AddComponent<RectTransform>();
            var team1LayoutGroup = team1Go.AddComponent<VerticalLayoutGroup>();
            var team1Sizer = team1Go.AddComponent<ContentSizeFitter>();
            team1Sizer.verticalFit = ContentSizeFitter.FitMode.MinSize;
            team1Rect.sizeDelta = new Vector2(400, 300);
            team1LayoutGroup.childAlignment = TextAnchor.MiddleRight;

            var vsParticleSystem = vsGo.GetComponentInChildren<GeneralParticleSystem>();
            vsParticleSystem.particleSettings.size = 4;
            vsParticleSystem.particleSettings.color = Color.white;
            vsParticleSystem.particleSettings.randomColor = new Color32(255, 0, 255, 255);
            vsParticleSystem.particleSettings.randomAddedColor = new Color32(72, 60, 43, 255);

            var vsText = vsGo.GetComponent<TextMeshProUGUI>();
            vsText.fontSize = 100;
            vsText.font = RoundsResources.MenuFont;
            vsText.alignment = TextAlignmentOptions.Center;
            vsText.text = "VS";

            var team2Rect = team2Go.AddComponent<RectTransform>();
            var team2LayoutGroup = team2Go.AddComponent<VerticalLayoutGroup>();
            var team2Sizer = team2Go.AddComponent<ContentSizeFitter>();
            team2Sizer.verticalFit = ContentSizeFitter.FitMode.MinSize;
            team2Rect.sizeDelta = new Vector2(400, 300);
            team2LayoutGroup.childAlignment = TextAnchor.MiddleLeft;

            this.UpdatePlayers();
        }

        public void UpdatePlayers() {
            this.StopAllCoroutines();
            this.StartCoroutine(this.UpdatePlayersCoroutine());
        }

        private IEnumerator UpdatePlayersCoroutine() {
            yield return null;

            this.teamPlayers.Clear();
            this.teamPlayers.Add(0, 0);
            this.teamPlayers.Add(1, 0);

            var team1Go = this.transform.GetChild(0).gameObject;
            var team2Go = this.transform.GetChild(2).gameObject;

            foreach (Transform child in team1Go.transform) {
                GameObject.Destroy(child.gameObject);
            }

            foreach (Transform child in team2Go.transform) {
                GameObject.Destroy(child.gameObject);
            }

            // Wait one frame for children to be destroyed
            yield return null;

            var playerNameHeight = 60;

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
                    var teamGo = team == 0 ? team1Go : team2Go;

                    var playerGo = new GameObject("NetworkPlayer");
                    playerGo.transform.SetParent(teamGo.transform);
                    playerGo.transform.localScale = Vector3.one;

                    var playerNameGo = CreatePlayerName(networkPlayer.NickName, team, networkPlayer.GetProperty<bool>("ready"));
                    playerNameGo.transform.SetParent(playerGo.transform);
                    playerNameGo.transform.localScale = Vector3.one;

                    playerGo.AddComponent<RectTransform>();
                    var layout = playerGo.AddComponent<LayoutElement>();
                    layout.minHeight = playerNameHeight;

                    this.teamPlayers[team] = this.teamPlayers[team] + 1;
                }
            }

            LayoutRebuilder.MarkLayoutForRebuild(this.gameObject.GetComponent<RectTransform>());
        }

        private GameObject CreatePlayerName(string name, int team, bool isReady) {
            var playerNameGo = GameObject.Instantiate(RoundsResources.FlickeringTextPrefab);
            playerNameGo.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 92);

            var nameText = playerNameGo.GetComponent<TextMeshProUGUI>();
            nameText.fontSize = 40;
            nameText.font = RoundsResources.MenuFont;
            nameText.alignment = team == 0 ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.enableWordWrapping = false;
            nameText.color = new Color32(85, 90, 98, 255);
            nameText.text = name;

            var particleSystem = playerNameGo.GetComponentInChildren<GeneralParticleSystem>();

            if (isReady) {
                particleSystem.particleSettings.size = 4;
                particleSystem.particleSettings.color = team == 0 ? new Color32(197, 103, 0, 255) : new Color32(0, 117, 197, 255);
                particleSystem.particleSettings.randomColor = new Color32(255, 0, 255, 255);
                particleSystem.particleSettings.randomAddedColor = team == 0 ? new Color32(72, 60, 43, 255) : new Color32(43, 46, 72, 255);
            } else {
                playerNameGo.GetComponent<Mask>().enabled = false;
                playerNameGo.transform.Find("UI_ParticleSystem").gameObject.SetActive(false);
            }

            return playerNameGo;
        }

        private int GetNetworkPlayerTeam(Photon.Realtime.Player networkPlayer) {
            if (networkPlayer.GetProperty<bool>("ready")) {
                return networkPlayer.GetProperty<int>("readyOrder") % 2;
            }

            return this.teamPlayers[0] > this.teamPlayers[1] ? 1 : 0;
        }
    }
}
