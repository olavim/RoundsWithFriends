﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnboundLib;
using UnboundLib.Networking;
using UnboundLib.GameModes;
using Sonigon;

namespace RWF.GameModes
{
    public class RWFGameMode : MonoBehaviour
    {
        public static RWFGameMode instance;

        public Dictionary<int, int> teamPoints = new Dictionary<int, int>();
        public Dictionary<int, int> teamRounds = new Dictionary<int, int>();

        protected bool isTransitioning;
        protected int playersNeededToStart = 2;
        protected int currentWinningTeamID = -1;

        protected virtual void Awake()
        {
            RWFGameMode.instance = this;
        }

        protected virtual void Start()
        {
            this.StartCoroutine(this.Init());
        }

        public virtual void OnDisable()
        {
            this.ResetMatch();
            this.teamPoints.Clear();
            this.teamRounds.Clear();
        }

        protected virtual IEnumerator Init()
        {
            yield return GameModeManager.TriggerHook(GameModeHooks.HookInitStart);

            PlayerManager.instance.SetPlayersSimulated(false);
            PlayerAssigner.instance.maxPlayers = this.playersNeededToStart;

            this.playersNeededToStart = RWFMod.instance.MinPlayers;
            PlayerAssigner.instance.maxPlayers = RWFMod.instance.MaxPlayers;

            yield return GameModeManager.TriggerHook(GameModeHooks.HookInitEnd);
        }

        [UnboundRPC]
        public static void RPC_RequestSync(int requestingPlayer)
        {
            NetworkingManager.RPC(typeof(RWFGameMode), nameof(RWFGameMode.RPC_SyncResponse), requestingPlayer, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        [UnboundRPC]
        public static void RPC_SyncResponse(int requestingPlayer, int readyPlayer)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == requestingPlayer)
            {
                RWFGameMode.instance.RemovePendingRequest(readyPlayer, nameof(RWFGameMode.RPC_RequestSync));
            }
        }

        protected virtual IEnumerator WaitForSyncUp()
        {
            if (PhotonNetwork.OfflineMode)
            {
                yield break;
            }

            yield return this.SyncMethod(nameof(RWFGameMode.RPC_RequestSync), null, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        public virtual void PlayerJoined(Player player)
        {
            if (!this.teamPoints.ContainsKey(player.teamID)) { this.teamPoints.Add(player.teamID, 0); }
            if (!this.teamRounds.ContainsKey(player.teamID)) { this.teamRounds.Add(player.teamID, 0); }
        }

        public virtual void PlayerDied(Player killedPlayer, int teamsAlive)
        {
            if (teamsAlive == 1)
            {
                TimeHandler.instance.DoSlowDown();

                if (PhotonNetwork.IsMasterClient)
                {
                    NetworkingManager.RPC(
                        typeof(RWFGameMode),
                        nameof(RWFGameMode.RPCA_NextRound),
                        PlayerManager.instance.GetLastPlayerAlive().teamID,
                        this.teamPoints,
                        this.teamRounds
                    );
                }
            }
        }

        public virtual void StartGame()
        {
            if (GameManager.instance.isPlaying)
            {
                return;
            }

            // clear teams and redo them
            this.teamPoints.Clear();
            this.teamRounds.Clear();
            foreach (Player player in PlayerManager.instance.players)
            {
                this.PlayerJoined(player);
            }

            GameManager.instance.isPlaying = true;
            this.StartCoroutine(this.DoStartGame());
        }

        protected virtual IEnumerator DoStartGame()
        {
            CardBarHandler.instance.Rebuild();
            UIHandler.instance.InvokeMethod("SetNumberOfRounds", (int) GameModeManager.CurrentHandler.Settings["roundsToWinGame"]);
            ArtHandler.instance.NextArt();

            yield return GameModeManager.TriggerHook(GameModeHooks.HookGameStart);

            GameManager.instance.battleOngoing = false;

            UIHandler.instance.ShowJoinGameText("LETS GOO!", PlayerSkinBank.GetPlayerSkinColors(1).winText);
            yield return new WaitForSeconds(0.25f);
            UIHandler.instance.HideJoinGameText();

            PlayerManager.instance.SetPlayersSimulated(false);
            PlayerManager.instance.InvokeMethod("SetPlayersVisible", false);
            MapManager.instance.LoadNextLevel(false, false);
            TimeHandler.instance.DoSpeedUp();

            yield return new WaitForSecondsRealtime(1f);

            yield return GameModeManager.TriggerHook(GameModeHooks.HookPickStart);

            for (int i = 0; i < PlayerManager.instance.players.Count; i++)
            {
                yield return this.WaitForSyncUp();

                yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart);

                CardChoiceVisuals.instance.Show(i, true);
                yield return CardChoice.instance.DoPick(1, PlayerManager.instance.players[i].playerID, PickerType.Player);

                yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickEnd);

                yield return new WaitForSecondsRealtime(0.1f);
            }

            yield return this.WaitForSyncUp();
            CardChoiceVisuals.instance.Hide();

            yield return GameModeManager.TriggerHook(GameModeHooks.HookPickEnd);

            MapManager.instance.CallInNewMapAndMovePlayers(MapManager.instance.currentLevelID);
            TimeHandler.instance.DoSpeedUp();
            TimeHandler.instance.StartGame();
            GameManager.instance.battleOngoing = true;
            UIHandler.instance.ShowRoundCounterSmall(this.teamPoints, this.teamRounds);
            PlayerManager.instance.InvokeMethod("SetPlayersVisible", true);

            this.StartCoroutine(this.DoRoundStart());
        }

        protected virtual IEnumerator RoundTransition(int winningTeamID)
        {
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointEnd);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookRoundEnd);

            if (this.teamRounds[winningTeamID] >= (int) GameModeManager.CurrentHandler.Settings["roundsToWinGame"])
            {
                this.GameOver(winningTeamID);
                yield break;
            }

            this.StartCoroutine(PointVisualizer.instance.DoWinSequence(this.teamPoints, this.teamRounds, winningTeamID));

            yield return new WaitForSecondsRealtime(1f);
            MapManager.instance.LoadNextLevel(false, false);

            yield return new WaitForSecondsRealtime(1.3f);

            PlayerManager.instance.SetPlayersSimulated(false);
            TimeHandler.instance.DoSpeedUp();

            yield return GameModeManager.TriggerHook(GameModeHooks.HookPickStart);

            PlayerManager.instance.InvokeMethod("SetPlayersVisible", false);
            var players = PlayerManager.instance.players;

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].teamID != winningTeamID)
                {
                    yield return base.StartCoroutine(this.WaitForSyncUp());

                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart);

                    CardChoiceVisuals.instance.Show(i, true);
                    yield return CardChoice.instance.DoPick(1, players[i].playerID, PickerType.Player);

                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickEnd);

                    yield return new WaitForSecondsRealtime(0.1f);
                }
            }

            PlayerManager.instance.InvokeMethod("SetPlayersVisible", true);

            yield return GameModeManager.TriggerHook(GameModeHooks.HookPickEnd);

            yield return this.StartCoroutine(this.WaitForSyncUp());

            TimeHandler.instance.DoSlowDown();
            MapManager.instance.CallInNewMapAndMovePlayers(MapManager.instance.currentLevelID);
            PlayerManager.instance.RevivePlayers();

            yield return new WaitForSecondsRealtime(0.3f);

            TimeHandler.instance.DoSpeedUp();
            GameManager.instance.battleOngoing = true;
            this.isTransitioning = false;
            UIHandler.instance.ShowRoundCounterSmall(this.teamPoints, this.teamRounds);

            this.StartCoroutine(this.DoRoundStart());
        }

        protected virtual IEnumerator PointTransition(int winningTeamID)
        {
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointEnd);

            this.StartCoroutine(PointVisualizer.instance.DoSequence(this.teamPoints, this.teamRounds, winningTeamID));
            yield return new WaitForSecondsRealtime(1f);

            MapManager.instance.LoadNextLevel(false, false);

            yield return new WaitForSecondsRealtime(0.5f);
            yield return base.StartCoroutine(this.WaitForSyncUp());

            MapManager.instance.CallInNewMapAndMovePlayers(MapManager.instance.currentLevelID);
            PlayerManager.instance.RevivePlayers();

            yield return new WaitForSecondsRealtime(0.3f);

            TimeHandler.instance.DoSpeedUp();
            GameManager.instance.battleOngoing = true;
            this.isTransitioning = false;
            UIHandler.instance.ShowRoundCounterSmall(this.teamPoints, this.teamRounds);

            this.StartCoroutine(this.DoPointStart());
        }

        protected virtual IEnumerator DoRoundStart()
        {
            // Wait for MapManager to set all players to simulated after map transition
            while (PlayerManager.instance.players.ToList().Any(p => !(bool) p.data.playerVel.GetFieldValue("simulated")))
            {
                yield return null;
            }

            PlayerManager.instance.SetPlayersSimulated(false);

            yield return GameModeManager.TriggerHook(GameModeHooks.HookRoundStart);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointStart);

            var sounds = GameObject.Find("/SonigonSoundEventPool");


            for (int i = 4; i >= 1; i--)
            {
                UIHandler.instance.DisplayRoundStartText($"{i}");
                SoundManager.Instance.Play(PointVisualizer.instance.sound_UI_Arms_Race_A_Ball_Shrink_Go_To_Left_Corner, this.transform);
                yield return new WaitForSeconds(0.5f);
            }

            SoundManager.Instance.Play(PointVisualizer.instance.sound_UI_Arms_Race_C_Ball_Pop_Shake, this.transform);
            UIHandler.instance.DisplayRoundStartText("FIGHT");
            PlayerManager.instance.SetPlayersSimulated(true);

            yield return GameModeManager.TriggerHook(GameModeHooks.HookBattleStart);

            this.ExecuteAfterSeconds(1f, () => {
                UIHandler.instance.HideRoundStartText();
            });
        }

        protected virtual IEnumerator DoPointStart()
        {
            // Wait for MapManager to set all players to simulated after map transition
            while (PlayerManager.instance.players.ToList().Any(p => !(bool) p.data.playerVel.GetFieldValue("simulated")))
            {
                yield return null;
            }

            PlayerManager.instance.SetPlayersSimulated(false);

            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointStart);

            var sounds = GameObject.Find("/SonigonSoundEventPool");


            for (int i = 4; i >= 1; i--)
            {
                UIHandler.instance.DisplayRoundStartText($"{i}");
                SoundManager.Instance.Play(PointVisualizer.instance.sound_UI_Arms_Race_A_Ball_Shrink_Go_To_Left_Corner, this.transform);
                yield return new WaitForSeconds(0.5f);
            }

            SoundManager.Instance.Play(PointVisualizer.instance.sound_UI_Arms_Race_C_Ball_Pop_Shake, this.transform);
            UIHandler.instance.DisplayRoundStartText("FIGHT");
            PlayerManager.instance.SetPlayersSimulated(true);

            yield return GameModeManager.TriggerHook(GameModeHooks.HookBattleStart);

            this.ExecuteAfterSeconds(1f, () => {
                UIHandler.instance.HideRoundStartText();
            });
        }

        protected virtual void RoundOver(int winningTeamID)
        {
            this.currentWinningTeamID = winningTeamID;

            foreach (var teamID in this.teamPoints.Keys.ToList())
            {
                this.teamPoints[teamID] = 0;
            }

            this.StartCoroutine(this.RoundTransition(winningTeamID));
        }

        protected virtual void PointOver(int winningTeamID)
        {
            this.currentWinningTeamID = winningTeamID;
            this.StartCoroutine(this.PointTransition(winningTeamID));
        }

        protected virtual IEnumerator GameOverTransition(int winningTeamID)
        {
            yield return GameModeManager.TriggerHook(GameModeHooks.HookGameEnd);

            UIHandler.instance.ShowRoundCounterSmall(this.teamPoints, this.teamRounds);
            UIHandler.instance.DisplayScreenText(PlayerManager.instance.GetColorFromPlayer(winningTeamID).winText, "VICTORY!", 1f);
            yield return new WaitForSecondsRealtime(2f);
            this.GameOverRematch(winningTeamID);
            yield break;
        }

        protected virtual void GameOverRematch(int winningPlayerID)
        {
            if (PhotonNetwork.OfflineMode)
            {
                var winningPlayer = PlayerManager.instance.players.Find(p => p.playerID == winningPlayerID);
                UIHandler.instance.DisplayScreenTextLoop(PlayerManager.instance.GetColorFromPlayer(winningPlayerID).winText, "REMATCH?");
                UIHandler.instance.popUpHandler.StartPicking(winningPlayer, this.GetRematchYesNo);
                MapManager.instance.LoadNextLevel(false, false);
                return;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                foreach (var player in PhotonNetwork.CurrentRoom.Players.Values.ToList())
                {
                    PhotonNetwork.DestroyPlayerObjects(player);
                }
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        protected virtual void GetRematchYesNo(PopUpHandler.YesNo yesNo)
        {
            if (yesNo == PopUpHandler.YesNo.Yes)
            {
                base.StartCoroutine(this.IDoRematch());
                return;
            }
            this.DoRestart();
        }

        protected virtual IEnumerator IDoRematch()
        {
            yield return null;
            this.ResetMatch();
            this.StartCoroutine(this.DoStartGame());
        }

        public virtual void ResetMatch()
        {
            UIHandler.instance.StopScreenTextLoop();
            PlayerManager.instance.InvokeMethod("ResetCharacters");

            foreach (var player in PlayerManager.instance.players)
            {
                this.teamPoints[player.teamID] = 0;
                this.teamRounds[player.teamID] = 0;
            }

            this.isTransitioning = false;
            UIHandler.instance.ShowRoundCounterSmall(this.teamPoints, this.teamRounds);
            CardBarHandler.instance.ResetCardBards();
            PointVisualizer.instance.ResetPoints();
        }

        protected virtual void DoRestart()
        {
            GameManager.instance.battleOngoing = false;
            if (PhotonNetwork.OfflineMode)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                return;
            }
            NetworkConnectionHandler.instance.NetworkRestart();
        }

        protected virtual void GameOver(int winningPlayerID)
        {
            this.currentWinningTeamID = winningPlayerID;
            base.StartCoroutine(this.GameOverTransition(winningPlayerID));
        }

        [UnboundRPC]
        public static void RPCA_NextRound(int winningTeamID, Dictionary<int, int> teamPoints, Dictionary<int, int> teamRounds)
        {
            var instance = RWFGameMode.instance;

            if (instance.isTransitioning)
            {
                return;
            }

            GameManager.instance.battleOngoing = false;
            instance.teamPoints = teamPoints;
            instance.teamRounds = teamRounds;
            instance.isTransitioning = true;

            PlayerManager.instance.SetPlayersSimulated(false);

            instance.teamPoints[winningTeamID] = instance.teamPoints[winningTeamID] + 1;

            if (instance.teamPoints[winningTeamID] < (int) GameModeManager.CurrentHandler.Settings["pointsToWinRound"])
            {
                instance.PointOver(winningTeamID);
                return;
            }

            instance.teamRounds[winningTeamID] = instance.teamRounds[winningTeamID] + 1;
            instance.RoundOver(winningTeamID);
        }
    }
}