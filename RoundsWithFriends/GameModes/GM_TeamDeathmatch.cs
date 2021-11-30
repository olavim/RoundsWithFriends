using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using UnboundLib;
using UnityEngine.SceneManagement;
using UnboundLib.GameModes;

namespace RWF.GameModes
{
        public class GM_ArmsRace : MonoBehaviour
    {
                private void Awake()
        {
            GM_ArmsRace.instance = this;
        }

                private void Start()
        {
            this.view = base.GetComponent<PhotonView>();
            PlayerManager.instance.SetPlayersSimulated(false);
            PlayerAssigner.instance.maxPlayers = this.playersNeededToStart;
            PlayerAssigner.instance.InvokeMethod("SetPlayersCanJoin", true);
            // === originally GM_ArmsRace Transpiler to skip this (UNBOUND) ===

            //PlayerManager.instance.AddPlayerDiedAction(new Action<Player, int>(this.PlayerDied));
            //PlayerManager playerManager = PlayerManager.instance;
            //typeof(PlayerManager).GetProperty("PlayerJoinedAction").SetValue(playerManager, (Action<Player>) Delegate.Combine(playerManager.PlayerJoinedAction, new Action<Player>(this.PlayerJoined)), null);
            
            // === ===
            // === originally GM_ArmsRace Transpiler to skip this ===

            //ArtHandler.instance.NextArt();
            
            // === ===
            this.playersNeededToStart = 2;
            UIHandler.instance.InvokeMethod("SetNumberOfRounds",this.roundsToWinGame);
            PlayerAssigner.instance.maxPlayers = this.playersNeededToStart;
            if (!PhotonNetwork.OfflineMode)
            {
                UIHandler.instance.ShowJoinGameText("PRESS JUMP\n TO JOIN", PlayerSkinBank.GetPlayerSkinColors(0).winText);
            }

            // === originally GM_ArmsRace Postfix ===
            this.playersNeededToStart = RWFMod.instance.MinPlayers;
            PlayerAssigner.instance.maxPlayers = RWFMod.instance.MaxPlayers;
            UIHandler.instance.HideJoinGameText();
            // === ===
        }

                private void Update()
        {
            /*
            if (Input.GetKey(KeyCode.Alpha4))
            {
                this.playersNeededToStart = 4;
                PlayerAssigner.instance.maxPlayers = this.playersNeededToStart;
            }
            if (Input.GetKey(KeyCode.Alpha2))
            {
                this.playersNeededToStart = 2;
                PlayerAssigner.instance.maxPlayers = this.playersNeededToStart;
            }*/
        }

        
        public void PlayerJoined(Player player)
        {
            // === originally GM_ArmsRace Prefix ===

            // When playing in a private match, we want to pretty much ignore this function since we handle player joins in PrivateRoomHandler
            if (!(NetworkConnectionHandler.instance.IsSearchingQuickMatch() || NetworkConnectionHandler.instance.IsSearchingTwitch()))
            {
                return;
            }

            // === ===

            if (PhotonNetwork.OfflineMode)
            {
                return;
            }
            if (!PhotonNetwork.OfflineMode)
            {
                if (player.data.view.IsMine)
                {
                    UIHandler.instance.ShowJoinGameText("WAITING", PlayerSkinBank.GetPlayerSkinColors(1).winText);
                }
                else
                {
                    UIHandler.instance.ShowJoinGameText("PRESS JUMP\n TO JOIN", PlayerSkinBank.GetPlayerSkinColors(1).winText);
                }
            }
            player.data.isPlaying = false;
            int count = PlayerManager.instance.players.Count;
            if (count >= this.playersNeededToStart)
            {
                this.StartGame();
                return;
            }
            if (PhotonNetwork.OfflineMode)
            {
                if (this.playersNeededToStart - count == 3)
                {
                    UIHandler.instance.ShowJoinGameText("ADD THREE MORE PLAYER TO START", PlayerSkinBank.GetPlayerSkinColors(count).winText);
                }
                if (this.playersNeededToStart - count == 2)
                {
                    UIHandler.instance.ShowJoinGameText("ADD TWO MORE PLAYER TO START", PlayerSkinBank.GetPlayerSkinColors(count).winText);
                }
                if (this.playersNeededToStart - count == 1)
                {
                    UIHandler.instance.ShowJoinGameText("ADD ONE MORE PLAYER TO START", PlayerSkinBank.GetPlayerSkinColors(count).winText);
                }
            }
        }

        
        [PunRPC]
        private void RPCO_RequestSyncUp()
        {
            this.view.RPC("RPCM_ReturnSyncUp", RpcTarget.Others, Array.Empty<object>());
        }

                [PunRPC]
        private void RPCM_ReturnSyncUp()
        {
            this.isWaiting = false;
        }

                private IEnumerator WaitForSyncUp()
        {
            if (PhotonNetwork.OfflineMode)
            {
                yield break;
            }
            this.isWaiting = true;
            this.view.RPC("RPCO_RequestSyncUp", RpcTarget.Others, Array.Empty<object>());
            while (this.isWaiting)
            {
                yield return null;
            }
            yield break;
        }

                public void StartGame()
        {
            if (GameManager.instance.isPlaying)
            {
                return;
            }
            Action startGameAction = this.StartGameAction;
            if (startGameAction != null)
            {
                startGameAction();
            }
            GameManager.instance.isPlaying = true;
            base.StartCoroutine(this.DoStartGame());
        }

                private IEnumerator DoStartGame()
        {
            // === originally GM_ArmsRace Postfix (UNBOUND) ===

            yield return GameModeManager.TriggerHook(GameModeHooks.HookGameStart);

            // === ===

            // === originally GM_ArmsRace Prefix ===

            // Rebuild the top right player card visual to match the number of players
            CardBarHandler.instance.Rebuild();
            UIHandler.instance.InvokeMethod("SetNumberOfRounds", this.roundsToWinGame);
            
            // === ===

            GameManager.instance.battleOngoing = false;
            UIHandler.instance.ShowJoinGameText("LETS GOO!", PlayerSkinBank.GetPlayerSkinColors(1).winText);
            yield return new WaitForSeconds(0.25f);
            UIHandler.instance.HideJoinGameText();
            PlayerManager.instance.SetPlayersSimulated(false);
            PlayerManager.instance.InvokeMethod("SetPlayersVisible", false);
            MapManager.instance.LoadNextLevel(false, false);
            TimeHandler.instance.DoSpeedUp();
            yield return new WaitForSecondsRealtime(1f);
            if (this.pickPhase)
            {
                // === originally GM_ArmsRace Postfix (UNBOUND) ===

                yield return GameModeManager.TriggerHook(GameModeHooks.HookPickStart);

                // === ===

                int num;
                for (int i = 0; i < PlayerManager.instance.players.Count; i = num + 1)
                {
                    yield return base.StartCoroutine(this.WaitForSyncUp());
                    // === originally GM_ArmsRace Postfix (UNBOUND) ===

                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart);

                    // === ===
                    CardChoiceVisuals.instance.Show(i, true);
                    yield return CardChoice.instance.DoPick(1, PlayerManager.instance.players[i].playerID, PickerType.Player);
                    // === originally GM_ArmsRace Postfix (UNBOUND) ===

                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickEnd);

                    // === ===
                    yield return new WaitForSecondsRealtime(0.1f);
                    num = i;
                }
                yield return base.StartCoroutine(this.WaitForSyncUp());
                CardChoiceVisuals.instance.Hide();
                // === originally GM_ArmsRace Postfix (UNBOUND) ===

                yield return GameModeManager.TriggerHook(GameModeHooks.HookPickEnd);

                // === ===
            }
            MapManager.instance.CallInNewMapAndMovePlayers(MapManager.instance.currentLevelID);
            TimeHandler.instance.DoSpeedUp();
            TimeHandler.instance.StartGame();
            GameManager.instance.battleOngoing = true;
            UIHandler.instance.ShowRoundCounterSmall(this.p1Rounds, this.p2Rounds, this.p1Points, this.p2Points);
            PlayerManager.instance.InvokeMethod("SetPlayersVisible", true);

            // === originally GM_ArmsRace Postfix (UNBOUND) ===

            yield return GameModeManager.TriggerHook(GameModeHooks.HookRoundStart);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointStart);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookBattleStart);

            // === ===


            yield break;
        }

                private IEnumerator PointTransition(int winningTeamID, string winTextBefore, string winText)
        {
            // === originally GM_ArmsRace Postfix (UNBOUND) ===

            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointEnd);

            // === ===

            base.StartCoroutine(PointVisualizer.instance.DoSequence(this.p1Points, this.p2Points, winningTeamID == 0));
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

            // === originally GM_ArmsRace Postfix (UNBOUND) ===

            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointStart);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookBattleStart);

            // === ===

            yield break;
        }

                private void PointOver(int winningTeamID)
        {
            int num = this.p1Points;
            int num2 = this.p2Points;
            if (winningTeamID == 0)
            {
                num--;
            }
            else
            {
                num2--;
            }
            string winTextBefore = num.ToString() + " - " + num2.ToString();
            string winText = this.p1Points.ToString() + " - " + this.p2Points.ToString();
            base.StartCoroutine(this.PointTransition(winningTeamID, winTextBefore, winText));
            UIHandler.instance.ShowRoundCounterSmall(this.p1Rounds, this.p2Rounds, this.p1Points, this.p2Points);
        }

                private IEnumerator RoundTransition(int winningTeamID, int killedTeamID)
        {
            // === originally GM_ArmsRace Postfix (UNBOUND) ===

            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointEnd);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookRoundEnd);

            // Check game over after round end trigger to allow more control in triggers
            if (this.p1Rounds >= this.roundsToWinGame || this.p2Rounds >= this.roundsToWinGame)
            {
                this.GameOver(winningTeamID);
                yield break;
            }

            // === ===

            base.StartCoroutine(PointVisualizer.instance.DoWinSequence(this.p1Points, this.p2Points, this.p1Rounds, this.p2Rounds, winningTeamID == 0));
            // === originally GM_ArmsRace Transpiler (RWF and UNBOUND) ===

            this.p1Points = 0;
            this.p2Points = 0;
            
            // === ===
            yield return new WaitForSecondsRealtime(1f);
            MapManager.instance.LoadNextLevel(false, false);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return new WaitForSecondsRealtime(1f);
            TimeHandler.instance.DoSpeedUp();
            // === originally GM_ArmsRace Transpiler (UNBOUND) ===

            yield return GameModeManager.TriggerHook(GameModeHooks.HookPickStart);

            // === ===
            if (this.pickPhase)
            {
                //global::Debug.Log("PICK PHASE");
                PlayerManager.instance.InvokeMethod("SetPlayersVisible", false);
                Player[] players = PlayerManager.instance.GetPlayersInTeam(killedTeamID);
                int num;
                for (int i = 0; i < players.Length; i = num + 1)
                {
                    yield return base.StartCoroutine(this.WaitForSyncUp());
                    // === originally GM_ArmsRace Transpiler (UNBOUND) ===

                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart);

                    // === ===
                    // === originally GM_ArmsRace Transpiler ===

                    CardChoiceVisuals.instance.Show(PlayerManager.instance.players.FindIndex(p => p.playerID == players[i].playerID), true);
                    
                    // === ===
                    yield return CardChoice.instance.DoPick(1, players[i].playerID, PickerType.Player);
                    // === originally GM_ArmsRace Transpiler (UNBOUND) ===

                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickEnd);

                    // === ===
                    yield return new WaitForSecondsRealtime(0.1f);
                    num = i;
                }
                PlayerManager.instance.InvokeMethod("SetPlayersVisible", true);
                // === originally GM_ArmsRace Transpiler (UNBOUND) ===

                yield return GameModeManager.TriggerHook(GameModeHooks.HookPickEnd);

                // === ===
                players = null;
            }
            yield return base.StartCoroutine(this.WaitForSyncUp());
            TimeHandler.instance.DoSlowDown();
            MapManager.instance.CallInNewMapAndMovePlayers(MapManager.instance.currentLevelID);
            PlayerManager.instance.RevivePlayers();
            yield return new WaitForSecondsRealtime(0.3f);
            TimeHandler.instance.DoSpeedUp();
            this.isTransitioning = false;
            GameManager.instance.battleOngoing = true;
            UIHandler.instance.ShowRoundCounterSmall(this.p1Rounds, this.p2Rounds, this.p1Points, this.p2Points);

            // === originally GM_ArmsRace Postfix (UNBOUND) ===
            
            yield return GameModeManager.TriggerHook(GameModeHooks.HookRoundStart);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointStart);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookBattleStart);
            
            // === ===

            yield break;
        }

                private void RoundOver(int winningTeamID, int losingTeamID)
        {
            this.currentWinningTeamID = winningTeamID;
            base.StartCoroutine(this.RoundTransition(winningTeamID, losingTeamID));

            // === originally GM_ArmsRace Transpiler to skip this (RWF and UNBOUND) ===

            //this.p1Points = 0;
            //this.p2Points = 0;
        
            // === ===
        }

                private IEnumerator GameOverTransition(int winningTeamID)
        {
            // === originally GM_ArmsRace Postifx (UNBOUND) ===

            // We're really adding a prefix, but we get access to the IEnumerator in the postfix
            yield return GameModeManager.TriggerHook(GameModeHooks.HookGameEnd);

            // === ===

            UIHandler.instance.ShowRoundCounterSmall(this.p1Rounds, this.p2Rounds, this.p1Points, this.p2Points);
            UIHandler.instance.DisplayScreenText(PlayerManager.instance.GetColorFromTeam(winningTeamID).winText, "VICTORY!", 1f);
            yield return new WaitForSecondsRealtime(2f);
            this.GameOverRematch(winningTeamID);
            yield break;
        }

                private void GameOverRematch(int winningTeamID)
        {
            // === originally GM_ArmsRace Prefix ===

            // Enable rematch if offline or playing against a single unmodded player
            if (!PhotonNetwork.OfflineMode && !(!PhotonNetwork.CurrentRoom.Players.Values.ToList().All(p => p.IsModded()) && PlayerManager.instance.players.Count == 2))
            {
                /* The host client destroys all networked player objects after each game. Otherwise, if someone
                 * joins a lobby after a game has been played, all the previously created player objects will be
                 * created for the new client as well, which causes a host of problems.
                 */
                if (PhotonNetwork.IsMasterClient)
                {
                    foreach (var player in PhotonNetwork.CurrentRoom.Players.Values.ToList())
                    {
                        PhotonNetwork.DestroyPlayerObjects(player);
                    }
                }

                SceneManager.LoadScene(SceneManager.GetActiveScene().name);

                return;
            }

            // === ===

            UIHandler.instance.DisplayScreenTextLoop(PlayerManager.instance.GetColorFromTeam(winningTeamID).winText, "REMATCH?");
            UIHandler.instance.InvokeMethod("DisplayYesNoLoop", (Player)PlayerManager.instance.InvokeMethod("GetFirstPlayerInTeam", winningTeamID), new Action<PopUpHandler.YesNo>(this.GetRematchYesNo));
            MapManager.instance.LoadNextLevel(false, false);
        }

                private void GetRematchYesNo(PopUpHandler.YesNo yesNo)
        {
            if (yesNo == PopUpHandler.YesNo.Yes)
            {
                base.StartCoroutine(this.IDoRematch());
                return;
            }
            this.DoRestart();
        }

                [PunRPC]
        public void RPCA_PlayAgain()
        {
            this.waitingForOtherPlayer = false;
        }

                private IEnumerator IDoRematch()
        {
            if (!PhotonNetwork.OfflineMode)
            {
                base.GetComponent<PhotonView>().RPC("RPCA_PlayAgain", RpcTarget.Others, Array.Empty<object>());
                UIHandler.instance.DisplayScreenTextLoop("WAITING");
                float c = 0f;
                while (this.waitingForOtherPlayer)
                {
                    c += Time.unscaledDeltaTime;
                    if (c > 10f)
                    {
                        this.DoRestart();
                        yield break;
                    }
                    yield return null;
                }
            }
            yield return null;
            UIHandler.instance.StopScreenTextLoop();
            PlayerManager.instance.InvokeMethod("ResetCharacters");
            this.ResetMatch();
            base.StartCoroutine(this.DoStartGame());
            this.waitingForOtherPlayer = true;
            yield break;
        }

                private void ResetMatch()
        {
            this.p1Points = 0;
            this.p1Rounds = 0;
            this.p2Points = 0;
            this.p2Rounds = 0;
            this.isTransitioning = false;
            this.waitingForOtherPlayer = false;
            UIHandler.instance.ShowRoundCounterSmall(this.p1Rounds, this.p2Rounds, this.p1Points, this.p2Points);
            CardBarHandler.instance.ResetCardBards();
            PointVisualizer.instance.ResetPoints();
        }

                private void GameOverContinue(int winningTeamID)
        {
            UIHandler.instance.DisplayScreenTextLoop(PlayerManager.instance.GetColorFromTeam(winningTeamID).winText, "CONTINUE?");
            UIHandler.instance.InvokeMethod("DisplayYesNoLoop", (Player)PlayerManager.instance.InvokeMethod("GetFirstPlayerInTeam", winningTeamID), new Action<PopUpHandler.YesNo>(this.GetContinueYesNo));
            MapManager.instance.LoadNextLevel(false, false);
        }

                private void GetContinueYesNo(PopUpHandler.YesNo yesNo)
        {
            if (yesNo == PopUpHandler.YesNo.Yes)
            {
                this.DoContinue();
                return;
            }
            this.DoRestart();
        }

                private void DoContinue()
        {
            UIHandler.instance.StopScreenTextLoop();
            this.roundsToWinGame += 2;
            UIHandler.instance.InvokeMethod("SetNumberOfRounds", this.roundsToWinGame);
            this.RoundOver(this.currentWinningTeamID, PlayerManager.instance.GetOtherTeam(this.currentWinningTeamID));
        }

                private void DoRestart()
        {
            GameManager.instance.battleOngoing = false;
            if (PhotonNetwork.OfflineMode)
            {
                Application.LoadLevel(Application.loadedLevel);
                return;
            }
            NetworkConnectionHandler.instance.NetworkRestart();
        }

                private void GameOver(int winningTeamID)
        {
            this.currentWinningTeamID = winningTeamID;
            base.StartCoroutine(this.GameOverTransition(winningTeamID));
        }

                public void PlayerDied(Player killedPlayer, int playersAlive)
        {
            if (!PhotonNetwork.OfflineMode)
            {
                //global::Debug.Log("PlayerDied: " + killedPlayer.data.view.Owner.NickName);
            }
            if (PlayerManager.instance.TeamsAlive() < 2)
            {
                TimeHandler.instance.DoSlowDown();
                if (PhotonNetwork.IsMasterClient)
                {
                    this.view.RPC("RPCA_NextRound", RpcTarget.All, new object[]
                    {
                    PlayerManager.instance.GetOtherTeam(PlayerManager.instance.GetLastTeamAlive()),
                    PlayerManager.instance.GetLastTeamAlive(),
                    this.p1Points,
                    this.p2Points,
                    this.p1Rounds,
                    this.p2Rounds
                    });
                }
            }
        }
                [PunRPC]
        public void RPCA_NextRound(int losingTeamID, int winningTeamID, int p1PointsSet, int p2PointsSet, int p1RoundsSet, int p2RoundsSet)
        {
            if (this.isTransitioning)
            {
                return;
            }
            GameManager.instance.battleOngoing = false;
            this.p1Points = p1PointsSet;
            this.p2Points = p2PointsSet;
            this.p1Rounds = p1RoundsSet;
            this.p2Rounds = p2RoundsSet;
            //global::Debug.Log("Winning team: " + winningTeamID);
            //global::Debug.Log("Losing team: " + losingTeamID);
            this.isTransitioning = true;
            GameManager.instance.GameOver(winningTeamID, losingTeamID);
            PlayerManager.instance.SetPlayersSimulated(false);
            if (winningTeamID == 0)
            {
                this.p1Points++;
                if (this.p1Points < this.pointsToWinRound)
                {
                    //global::Debug.Log("Point over, winning team: " + winningTeamID);
                    this.PointOver(winningTeamID);
                    this.pointOverAction();
                    return;
                }
                this.p1Rounds++;
                if (this.p1Rounds >= this.roundsToWinGame)
                {
                    //global::Debug.Log("Game over, winning team: " + winningTeamID);

                    // === originally GM_ArmsRace Transpiler (UNBOUND) ===

                    // Do not call GameOver in RPCA_NextRound. We move game over check to RoundTransition to handle triggers better.
                    //this.GameOver(winningTeamID);
                    this.RoundOver(winningTeamID, losingTeamID);
                    
                    // === ===
                    
                    this.pointOverAction();
                    return;
                }
                //global::Debug.Log("Round over, winning team: " + winningTeamID);
                this.RoundOver(winningTeamID, losingTeamID);
                this.pointOverAction();
                return;
            }
            else
            {
                if (winningTeamID != 1)
                {
                    return;
                }
                this.p2Points++;
                if (this.p2Points < this.pointsToWinRound)
                {
                    //global::Debug.Log("Point over, winning team: " + winningTeamID);
                    this.PointOver(winningTeamID);
                    this.pointOverAction();
                    return;
                }
                this.p2Rounds++;
                if (this.p2Rounds >= this.roundsToWinGame)
                {
                    //global::Debug.Log("Game over, winning team: " + winningTeamID);
                    this.GameOver(winningTeamID);
                    this.pointOverAction();
                    return;
                }
                //global::Debug.Log("Round over, winning team: " + winningTeamID);
                this.RoundOver(winningTeamID, losingTeamID);
                this.pointOverAction();
                return;
            }
        }

                private int playersNeededToStart = 2;

                private int pointsToWinRound = 2;

                public int roundsToWinGame = 5;

                public int p1Points;

                public int p2Points;

                public int p1Rounds;

                public int p2Rounds;

                private PhotonView view;

                public static GM_ArmsRace instance;

                private bool isWaiting;

                public Action StartGameAction;

                public bool pickPhase = true;

                [HideInInspector]
        public bool isPicking;

                private bool waitingForOtherPlayer = true;

                private int currentWinningTeamID = -1;

                public Action pointOverAction;

                private bool isTransitioning;
    }
}
