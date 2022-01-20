using UnboundLib;
using UnboundLib.GameModes;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RWF.GameModes
{
    public class RWFGameModeHandler<TGameMode> : GameModeHandler<TGameMode> where TGameMode : RWFGameMode
    {
        private readonly string _Name;
        public override string Name
        {
            get { return this._Name; }
        }

        public override GameSettings Settings { get; protected set; }

        public RWFGameModeHandler(string name, string gameModeId, bool allowTeams, int pointsToWinRound = 2, int roundsToWinGame = 5, int? playersRequiredToStartGame = null, int? maxPlayers = null, int? maxTeams = null, int? maxClients = null) : base(gameModeId)
        {
            this._Name = name;
            this.Settings = new GameSettings()
            {
                { "pointsToWinRound", pointsToWinRound},
                { "roundsToWinGame", roundsToWinGame},
                { "allowTeams", allowTeams },
                { "playersRequiredToStartGame", UnityEngine.Mathf.Clamp(playersRequiredToStartGame ?? 2, 1, RWFMod.MaxPlayersHardLimit) },
                { "maxPlayers", UnityEngine.Mathf.Clamp(maxPlayers ?? RWFMod.instance.MaxPlayers, 1, RWFMod.MaxPlayersHardLimit) },
                { "maxTeams", UnityEngine.Mathf.Clamp(maxTeams ?? RWFMod.instance.MaxTeams, 1, RWFMod.MaxColorsHardLimit) },
                { "maxClients", UnityEngine.Mathf.Clamp(maxClients ?? RWFMod.instance.MaxClients, 1, RWFMod.MaxPlayersHardLimit) }
            };
        }

        public override void SetActive(bool active)
        {
            this.GameMode.gameObject.SetActive(active);
        }

        public override void PlayerJoined(Player player)
        {
            this.GameMode.PlayerJoined(player);
        }

        public override void PlayerDied(Player player, int playersAlive)
        {
            this.GameMode.PlayerDied(player, playersAlive);
        }
        public override void PlayerLeft(Player leftPlayer)
        {
            // store old teamIDs so that we can make a dictionary of old to new teamIDs
            Dictionary<Player, int> oldTeamIDs = PlayerManager.instance.players.ToDictionary(p => p, p => p.teamID);

            // UnboundLib handles PlayerManager fixing, which includes reassigning playerIDs and teamIDs
            // as well as card bar fixing
            base.PlayerLeft(leftPlayer);

            // get new teamIDs
            Dictionary<Player, int> newTeamIDs = PlayerManager.instance.players.ToDictionary(p => p, p => p.teamID);

            // update team scores
            Dictionary<int, int> newTeamPoints = new Dictionary<int, int>() { };
            Dictionary<int, int> newTeamRounds = new Dictionary<int, int>() { };

            foreach (Player player in newTeamIDs.Keys)
            {
                if (!newTeamPoints.Keys.Contains(newTeamIDs[player]))
                {
                    newTeamPoints[newTeamIDs[player]] = this.GameMode.teamPoints[oldTeamIDs[player]];
                }
                if (!newTeamRounds.Keys.Contains(newTeamIDs[player]))
                {
                    newTeamRounds[newTeamIDs[player]] = this.GameMode.teamRounds[oldTeamIDs[player]];
                }
            }

            this.GameMode.teamPoints = newTeamPoints;
            this.GameMode.teamRounds = newTeamRounds;

            // fix score counter
            UIHandler.instance.roundCounter.GetData().teamPoints = newTeamPoints;
            UIHandler.instance.roundCounter.GetData().teamRounds = newTeamRounds;
            UIHandler.instance.roundCounterSmall.GetData().teamPoints = newTeamPoints;
            UIHandler.instance.roundCounterSmall.GetData().teamRounds = newTeamRounds;

        }

        public override TeamScore GetTeamScore(int teamID)
        {
            return new TeamScore(this.GameMode.teamPoints[teamID], this.GameMode.teamRounds[teamID]);
        }

        public override void SetTeamScore(int teamID, TeamScore score)
        {
            this.GameMode.teamPoints[teamID] = score.points;
            this.GameMode.teamRounds[teamID] = score.rounds;
        }

        public override int[] GetGameWinners()
        {
            return this.GameMode.teamRounds.Keys.Where(tID => this.GameMode.teamRounds[tID] >= (int) GameModeManager.CurrentHandler.Settings["roundsToWinGame"]).ToArray();
        }

        public override void StartGame()
        {
            this.GameMode.StartGame();
        }

        public override void ResetGame()
        {
            this.GameMode.ResetMatch();
        }

        public override void ChangeSetting(string name, object value)
        {
            base.ChangeSetting(name, value);

            if (name == "roundsToWinGame")
            {
                UIHandler.instance.InvokeMethod("SetNumberOfRounds", (int) value);
            }
        }
    }
}