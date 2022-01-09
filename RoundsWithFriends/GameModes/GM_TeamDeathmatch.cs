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
using RWF.UI;

namespace RWF.GameModes
{
	public class GM_TeamDeathmatch : MonoBehaviour
	{
		public static GM_TeamDeathmatch instance;

		internal Dictionary<int, int> teamPoints = new Dictionary<int, int>();
		internal Dictionary<int, int> teamRounds = new Dictionary<int, int>();

		private bool isTransitioning;
		private int playersNeededToStart = 2;
		private int currentWinningTeamID = -1;

		private void Awake() {
			GM_TeamDeathmatch.instance = this;
		}

		private void Start() {
			this.StartCoroutine(this.Init());
		}

		public void OnDisable()
		{
			this.ResetMatch();
			this.teamPoints.Clear();
			this.teamRounds.Clear();
		}

		private IEnumerator Init()
		{
			yield return GameModeManager.TriggerHook(GameModeHooks.HookInitStart);

			PlayerManager.instance.SetPlayersSimulated(false);
			PlayerAssigner.instance.maxPlayers = this.playersNeededToStart;

			this.playersNeededToStart = RWFMod.instance.MinPlayers;
			PlayerAssigner.instance.maxPlayers = RWFMod.instance.MaxPlayers;

			yield return GameModeManager.TriggerHook(GameModeHooks.HookInitEnd);
		}

		[UnboundRPC]
		public static void RPC_SyncBattleStart(int requestingPlayer) {
			NetworkingManager.RPC(typeof(GM_TeamDeathmatch), nameof(GM_TeamDeathmatch.RPC_SyncBattleStartResponse), requestingPlayer, PhotonNetwork.LocalPlayer.ActorNumber);
		}

		[UnboundRPC]
		public static void RPC_SyncBattleStartResponse(int requestingPlayer, int readyPlayer) {
			if (PhotonNetwork.LocalPlayer.ActorNumber == requestingPlayer)
			{
				GM_TeamDeathmatch.instance.RemovePendingRequest(readyPlayer, nameof(GM_TeamDeathmatch.RPC_SyncBattleStart));
			}
		}

		private IEnumerator SyncBattleStart() {
			if (PhotonNetwork.OfflineMode) {
				yield break;
			}

			yield return this.SyncMethod(nameof(GM_TeamDeathmatch.RPC_SyncBattleStart), null, PhotonNetwork.LocalPlayer.ActorNumber);

            // wait an extra time equivalent to the maximum ping in the lobby minus this client's ping
            float maxPing = PhotonNetwork.CurrentRoom.Players.Select(kv => (float)(int) kv.Value.CustomProperties["Ping"]).Max() * 0.001f;
            float myPing = (float)(int)PhotonNetwork.LocalPlayer.CustomProperties["Ping"] * 0.001f;
            float wait = UnityEngine.Mathf.Abs(maxPing - myPing) < 0.001f ? 0f : maxPing - myPing;

            if (wait > 0f) { yield return new WaitForSecondsRealtime(wait); }
		}

		[UnboundRPC]
		public static void RPC_RequestSync(int requestingPlayer) {
			NetworkingManager.RPC(typeof(GM_TeamDeathmatch), nameof(GM_TeamDeathmatch.RPC_SyncResponse), requestingPlayer, PhotonNetwork.LocalPlayer.ActorNumber);
		}

		[UnboundRPC]
		public static void RPC_SyncResponse(int requestingPlayer, int readyPlayer) {
			if (PhotonNetwork.LocalPlayer.ActorNumber == requestingPlayer)
			{
				GM_TeamDeathmatch.instance.RemovePendingRequest(readyPlayer, nameof(GM_TeamDeathmatch.RPC_RequestSync));
			}
		}

		private IEnumerator WaitForSyncUp() {
			if (PhotonNetwork.OfflineMode) {
				yield break;
			}

			yield return this.SyncMethod(nameof(GM_TeamDeathmatch.RPC_RequestSync), null, PhotonNetwork.LocalPlayer.ActorNumber);
		}

		public void PlayerJoined(Player player) {
            if (!this.teamPoints.ContainsKey(player.teamID)) { this.teamPoints.Add(player.teamID, 0); }
            if (!this.teamRounds.ContainsKey(player.teamID)) { this.teamRounds.Add(player.teamID, 0); }
		}

		public void PlayerDied(Player killedPlayer, int teamsAlive) {
			if (teamsAlive == 1) {
				TimeHandler.instance.DoSlowDown();

				if (PhotonNetwork.IsMasterClient) {
					NetworkingManager.RPC(
						typeof(GM_TeamDeathmatch),
						nameof(GM_TeamDeathmatch.RPCA_NextRound),
						PlayerManager.instance.GetLastPlayerAlive().teamID,
						this.teamPoints,
						this.teamRounds
					);
				}
			}
		}

		public void StartGame() {
			if (GameManager.instance.isPlaying) {
				return;
			}

            // clear teams and redo them
            this.teamPoints.Clear();
            this.teamRounds.Clear();
            foreach (Player player in PlayerManager.instance.players)
            {
                this.PlayerJoined(player);
            }
            // set up pick order
            PlayerManager.instance.ResetPickOrder();

            GameManager.instance.isPlaying = true;
			this.StartCoroutine(this.DoStartGame());
		}

		private IEnumerator DoStartGame() {
			CardBarHandler.instance.Rebuild();
			UIHandler.instance.InvokeMethod("SetNumberOfRounds", (int) GameModeManager.CurrentHandler.Settings["roundsToWinGame"]);
			ArtHandler.instance.NextArt();

			yield return GameModeManager.TriggerHook(GameModeHooks.HookGameStart);

			GameManager.instance.battleOngoing = false;

			UIHandler.instance.ShowJoinGameText("LETS GOO!", PlayerSkinBank.GetPlayerSkinColors(1).winText);
			yield return new WaitForSecondsRealtime(0.25f);
			UIHandler.instance.HideJoinGameText();

            PlayerSpotlight.CancelFade(true);

			PlayerManager.instance.SetPlayersSimulated(false);
			PlayerManager.instance.InvokeMethod("SetPlayersVisible", false);
			MapManager.instance.LoadNextLevel(false, false);
			TimeHandler.instance.DoSpeedUp();

			yield return new WaitForSecondsRealtime(1f);

			yield return GameModeManager.TriggerHook(GameModeHooks.HookPickStart);
            List<Player> pickOrder = PlayerManager.instance.GetPickOrder(null);

            foreach (Player player in pickOrder)
            {
                yield return this.WaitForSyncUp();

				yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart);

				CardChoiceVisuals.instance.Show(player.playerID, true);
				yield return CardChoice.instance.DoPick(1, player.playerID, PickerType.Player);

				yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickEnd);

				yield return new WaitForSecondsRealtime(0.1f);
			}

			yield return this.WaitForSyncUp();
			CardChoiceVisuals.instance.Hide();

			yield return GameModeManager.TriggerHook(GameModeHooks.HookPickEnd);

            PlayerSpotlight.FadeIn();
			MapManager.instance.CallInNewMapAndMovePlayers(MapManager.instance.currentLevelID);
			TimeHandler.instance.DoSpeedUp();
			TimeHandler.instance.StartGame();
			GameManager.instance.battleOngoing = true;
			UIHandler.instance.ShowRoundCounterSmall(this.teamPoints, this.teamRounds);
			PlayerManager.instance.InvokeMethod("SetPlayersVisible", true);

			this.StartCoroutine(this.DoRoundStart());
		}

		private IEnumerator RoundTransition(int winningTeamID)
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

            List<Player> pickOrder = PlayerManager.instance.GetPickOrder(winningTeamID);

            foreach (Player player in pickOrder)
            {
                if (player.teamID != winningTeamID) {
					yield return this.WaitForSyncUp();

					yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart);

					CardChoiceVisuals.instance.Show(player.playerID, true);
					yield return CardChoice.instance.DoPick(1, player.playerID, PickerType.Player);

					yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickEnd);

					yield return new WaitForSecondsRealtime(0.1f);
				}
			}

			PlayerManager.instance.InvokeMethod("SetPlayersVisible", true);

			yield return GameModeManager.TriggerHook(GameModeHooks.HookPickEnd);

			yield return this.StartCoroutine(this.WaitForSyncUp());
            PlayerSpotlight.FadeIn();

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

		private IEnumerator PointTransition(int winningTeamID)
		{
			yield return GameModeManager.TriggerHook(GameModeHooks.HookPointEnd);

			this.StartCoroutine(PointVisualizer.instance.DoSequence(this.teamPoints, this.teamRounds, winningTeamID));
			yield return new WaitForSecondsRealtime(1f);

			MapManager.instance.LoadNextLevel(false, false);

			yield return new WaitForSecondsRealtime(0.5f);
			yield return this.WaitForSyncUp();
            PlayerSpotlight.FadeIn();

			MapManager.instance.CallInNewMapAndMovePlayers(MapManager.instance.currentLevelID);
			PlayerManager.instance.RevivePlayers();

			yield return new WaitForSecondsRealtime(0.3f);

			TimeHandler.instance.DoSpeedUp();
			GameManager.instance.battleOngoing = true;
			this.isTransitioning = false;
			UIHandler.instance.ShowRoundCounterSmall(this.teamPoints, this.teamRounds);

			this.StartCoroutine(this.DoPointStart());
		}

		private IEnumerator DoRoundStart() {
            // Wait for MapManager to set all players to playing after map transition
			while (PlayerManager.instance.players.ToList().Any(p => !(bool) p.data.isPlaying)) {
				yield return null;
			}

			//PlayerManager.instance.SetPlayersSimulated(false);
            yield return this.WaitForSyncUp();
            PlayerSpotlight.FadeOut();

			yield return GameModeManager.TriggerHook(GameModeHooks.HookRoundStart);
			yield return GameModeManager.TriggerHook(GameModeHooks.HookPointStart);

			var sounds = GameObject.Find("/SonigonSoundEventPool");

            yield return this.SyncBattleStart();

			for (int i = 3; i >= 1; i--) {
				UIHandler.instance.DisplayRoundStartText($"{i}");
				SoundManager.Instance.Play(PointVisualizer.instance.sound_UI_Arms_Race_A_Ball_Shrink_Go_To_Left_Corner, this.transform);
				yield return new WaitForSecondsRealtime(0.5f);
			}

			SoundManager.Instance.Play(PointVisualizer.instance.sound_UI_Arms_Race_C_Ball_Pop_Shake, this.transform);
			UIHandler.instance.DisplayRoundStartText("FIGHT");
			PlayerManager.instance.SetPlayersSimulated(true);

			yield return GameModeManager.TriggerHook(GameModeHooks.HookBattleStart);

			this.ExecuteAfterSeconds(1f, () => {
				UIHandler.instance.HideRoundStartText();
			});
		}

		private IEnumerator DoPointStart()
		{
            // Wait for MapManager to set all players to playing after map transition
			while (PlayerManager.instance.players.ToList().Any(p => !(bool) p.data.isPlaying)) {
				yield return null;
			}

			//PlayerManager.instance.SetPlayersSimulated(false);
            yield return this.WaitForSyncUp();
            PlayerSpotlight.FadeOut();

			yield return GameModeManager.TriggerHook(GameModeHooks.HookPointStart);

			var sounds = GameObject.Find("/SonigonSoundEventPool");

            yield return this.SyncBattleStart();

			for (int i = 3; i >= 1; i--)
			{
				UIHandler.instance.DisplayRoundStartText($"{i}");
				SoundManager.Instance.Play(PointVisualizer.instance.sound_UI_Arms_Race_A_Ball_Shrink_Go_To_Left_Corner, this.transform);
				yield return new WaitForSecondsRealtime(0.5f);
			}

			SoundManager.Instance.Play(PointVisualizer.instance.sound_UI_Arms_Race_C_Ball_Pop_Shake, this.transform);
			UIHandler.instance.DisplayRoundStartText("FIGHT");
			PlayerManager.instance.SetPlayersSimulated(true);

			yield return GameModeManager.TriggerHook(GameModeHooks.HookBattleStart);

			this.ExecuteAfterSeconds(1f, () => {
				UIHandler.instance.HideRoundStartText();
			});
		}

		private void RoundOver(int winningTeamID)
		{
			this.currentWinningTeamID = winningTeamID;

			foreach (var teamID in this.teamPoints.Keys.ToList()) {
				this.teamPoints[teamID] = 0;
			}

			this.StartCoroutine(this.RoundTransition(winningTeamID));
		}

		private void PointOver(int winningTeamID)
		{
			this.currentWinningTeamID = winningTeamID;
			this.StartCoroutine(this.PointTransition(winningTeamID));
		}

		private IEnumerator GameOverTransition(int winningTeamID)
		{
			yield return GameModeManager.TriggerHook(GameModeHooks.HookGameEnd);

			UIHandler.instance.ShowRoundCounterSmall(this.teamPoints, this.teamRounds);
			UIHandler.instance.DisplayScreenText(PlayerManager.instance.GetColorFromPlayer(winningTeamID).winText, "VICTORY!", 1f);
			yield return new WaitForSecondsRealtime(2f);
			this.GameOverRematch(winningTeamID);
			yield break;
		}

		private void GameOverRematch(int winningPlayerID) {
			if (PhotonNetwork.OfflineMode) {
				var winningPlayer = PlayerManager.instance.players.Find(p => p.playerID == winningPlayerID);
				UIHandler.instance.DisplayScreenTextLoop(PlayerManager.instance.GetColorFromPlayer(winningPlayerID).winText, "REMATCH?");
				UIHandler.instance.popUpHandler.StartPicking(winningPlayer, this.GetRematchYesNo);
				MapManager.instance.LoadNextLevel(false, false);
				return;
			}

			if (PhotonNetwork.IsMasterClient) {
				foreach (var player in PhotonNetwork.CurrentRoom.Players.Values.ToList()) {
					PhotonNetwork.DestroyPlayerObjects(player);
				}
			}

			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}

		private void GetRematchYesNo(PopUpHandler.YesNo yesNo) {
			if (yesNo == PopUpHandler.YesNo.Yes) {
				base.StartCoroutine(this.IDoRematch());
				return;
			}
			this.DoRestart();
		}

		private IEnumerator IDoRematch() {
			yield return null;
			this.ResetMatch();
			this.StartCoroutine(this.DoStartGame());
		}

		public void ResetMatch()
		{
			UIHandler.instance.StopScreenTextLoop();
			PlayerManager.instance.InvokeMethod("ResetCharacters");

			foreach (var player in PlayerManager.instance.players) {
				this.teamPoints[player.teamID] = 0;
				this.teamRounds[player.teamID] = 0;
			}

			this.isTransitioning = false;
			UIHandler.instance.ShowRoundCounterSmall(this.teamPoints, this.teamRounds);
			CardBarHandler.instance.ResetCardBards();
			PointVisualizer.instance.ResetPoints();
		}

		private void DoRestart() {
			GameManager.instance.battleOngoing = false;
			if (PhotonNetwork.OfflineMode) {
				SceneManager.LoadScene(SceneManager.GetActiveScene().name);
				return;
			}
			NetworkConnectionHandler.instance.NetworkRestart();
		}

		private void GameOver(int winningPlayerID) {
			this.currentWinningTeamID = winningPlayerID;
			base.StartCoroutine(this.GameOverTransition(winningPlayerID));
		}

		[UnboundRPC]
		public static void RPCA_NextRound(int winningTeamID, Dictionary<int, int> teamPoints, Dictionary<int, int> teamRounds) {
			var instance = GM_TeamDeathmatch.instance;

			if (instance.isTransitioning) {
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