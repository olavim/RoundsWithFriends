using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnboundLib;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using Photon.Pun;
using Sonigon;

namespace RWF
{
    class RoundEndHandler : MonoBehaviour
    {
        private static RoundEndHandler instance;

        private int gmOriginalMaxRounds = -1;
        private bool waitingForHost = false;


        public void Awake()
        {
            RoundEndHandler.instance = this;
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, this.OnRoundEnd);
        }

        private IEnumerator OnRoundEnd(IGameModeHandler gm)
        {
            int maxRounds = (int) gm.Settings["roundsToWinGame"];
            var teams = PlayerManager.instance.players.Select(p => p.teamID).Distinct();
            int? winnerTeam = teams.Select(id => (int?) id).FirstOrDefault(id => gm.GetTeamScore(id.Value).rounds >= maxRounds);

            if (winnerTeam != null)
            {
                UIHandler.instance.DisplayScreenText(PlayerManager.instance.GetColorFromTeam(winnerTeam.Value).winText, "VICTORY!", 1f);

                yield return new WaitForSeconds(2f);

                this.waitingForHost = true;

                PlayerManager.instance.RevivePlayers();
                PlayerManager.instance.InvokeMethod("SetPlayersVisible", false);

                if (PhotonNetwork.IsMasterClient || PhotonNetwork.OfflineMode)
                {
                    var choices = new List<string>() { "CONTINUE", "REMATCH", "EXIT" };
                    UI.PopUpMenu.instance.Open(choices, this.OnGameOverChoose);
                }
                else
                {
                    string hostName = PhotonNetwork.CurrentRoom.Players.Values.First(p => p.IsMasterClient).NickName;
                    UIHandler.instance.ShowJoinGameText($"WAITING FOR {hostName}", PlayerSkinBank.GetPlayerSkinColors(1).winText);
                }

                MapManager.instance.LoadNextLevel(false, false);

                while (this.waitingForHost)
                {
                    yield return null;
                }

                UIHandler.instance.HideJoinGameText();
            }

            yield break;
        }

        private void OnGameOverChoose(string choice)
        {
            if (choice == "REMATCH")
            {
                SoundManager.Instance.Play(RoundsResources.GetSound("UI_Card_Pick_SE"), RoundEndHandler.instance.transform);
                NetworkingManager.RPC(typeof(RoundEndHandler), nameof(RoundEndHandler.Rematch));
            }

            if (choice == "CONTINUE")
            {
                SoundManager.Instance.Play(RoundsResources.GetSound("UI_Card_Pick_SE"), RoundEndHandler.instance.transform);
                NetworkingManager.RPC(typeof(RoundEndHandler), nameof(RoundEndHandler.Continue));
            }

            if (choice == "EXIT")
            {
                NetworkingManager.RPC(typeof(RoundEndHandler), nameof(RoundEndHandler.Exit));
            }
        }

        [UnboundRPC]
        public static void Rematch()
        {
            var gm = GameModeManager.CurrentHandler;

            if (RoundEndHandler.instance.gmOriginalMaxRounds != -1)
            {
                gm.ChangeSetting("roundsToWinGame", RoundEndHandler.instance.gmOriginalMaxRounds);
                RoundEndHandler.instance.gmOriginalMaxRounds = -1;
            }

            UIHandler.instance.DisableTexts(1f);

            GameManager.instance.isPlaying = false;
            gm.GameMode.StopAllCoroutines();
            gm.ResetGame();
            gm.StartGame();

            RoundEndHandler.instance.waitingForHost = false;
        }

        [UnboundRPC]
        public static void Continue()
        {
            var gm = GameModeManager.CurrentHandler;

            int maxRounds = (int) gm.Settings["roundsToWinGame"];

            if (RoundEndHandler.instance.gmOriginalMaxRounds == -1)
            {
                RoundEndHandler.instance.gmOriginalMaxRounds = maxRounds;
            }

            UIHandler.instance.DisableTexts(1f);

            gm.ChangeSetting("roundsToWinGame", maxRounds + 2);

            RoundEndHandler.instance.waitingForHost = false;
        }

        [UnboundRPC]
        public static void Exit()
        {
            var gm = GameModeManager.CurrentHandler;

            if (RoundEndHandler.instance.gmOriginalMaxRounds != -1)
            {
                gm.ChangeSetting("roundsToWinGame", RoundEndHandler.instance.gmOriginalMaxRounds);
                RoundEndHandler.instance.gmOriginalMaxRounds = -1;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                foreach (var player in PhotonNetwork.CurrentRoom.Players.Values.ToList())
                {
                    PhotonNetwork.DestroyPlayerObjects(player);
                }
            }

            gm.GameMode.StopAllCoroutines();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

            RoundEndHandler.instance.waitingForHost = false;
        }
    }
}
