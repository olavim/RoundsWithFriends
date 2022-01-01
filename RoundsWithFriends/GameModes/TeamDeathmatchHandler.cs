using UnboundLib;
using UnboundLib.GameModes;
using System.Collections.Generic;
using System.Linq;

namespace RWF.GameModes
{
    public class TeamDeathmatchHandler : GameModeHandler<GM_TeamDeathmatch>
    {
        public override string Name
        {
            get { return "Team Deathmatch"; }
        }

        public override GameSettings Settings { get; protected set; }

        public TeamDeathmatchHandler() : base("Team Deathmatch")
        {
            this.Settings = new GameSettings() {
                { "pointsToWinRound", 2 },
                { "roundsToWinGame", 5 },
                { "allowTeams", true }
            };
        }

        public override void SetActive(bool active)
        {
            this.GameMode.gameObject.SetActive(active);
        }

        public override void PlayerJoined(Player player)
        {
            GM_TeamDeathmatch.instance.PlayerJoined(player);
        }

        public override void PlayerDied(Player player, int playersAlive)
        {
            GM_TeamDeathmatch.instance.PlayerDied(player, playersAlive);
        }
        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            List<Player> disconnected = PlayerManager.instance.players.Where(p => p.data.view.ControllerActorNr == otherPlayer.ActorNumber).ToList();

            // get new teamIDs
            Dictionary<int, List<Player>> teams = new Dictionary<int, List<Player>>() { };
            foreach (Player player in PlayerManager.instance.players.Except(disconnected).OrderBy(p => p.teamID).ThenBy(p => p.playerID))
            {
                if (!teams.ContainsKey(player.teamID)) { teams[player.teamID] = new List<Player>() { }; }

                teams[player.teamID].Add(player);
            }

            Dictionary<Player, int> newTeamIDs = new Dictionary<Player, int>() { };

            int teamID = 0;
            foreach (int oldID in teams.Keys)
            {
                foreach (Player player in teams[oldID])
                {
                    newTeamIDs[player] = teamID;
                }
                teamID++;
            }

            // update team scores
            Dictionary<int, int> newTeamPoints = new Dictionary<int, int>() { };
            Dictionary<int, int> newTeamRounds = new Dictionary<int, int>() { };

            foreach (Player player in newTeamIDs.Keys)
            {
                if (!newTeamPoints.Keys.Contains(newTeamIDs[player]))
                {
                    newTeamPoints[newTeamIDs[player]] = GM_TeamDeathmatch.instance.teamPoints[player.teamID];
                }
                if (!newTeamRounds.Keys.Contains(newTeamIDs[player]))
                {
                    newTeamRounds[newTeamIDs[player]] = GM_TeamDeathmatch.instance.teamRounds[player.teamID];
                }
            }

            GM_TeamDeathmatch.instance.teamPoints = newTeamPoints;
            GM_TeamDeathmatch.instance.teamRounds = newTeamRounds;

            // fix score counter
            UIHandler.instance.roundCounter.GetData().teamPoints = newTeamPoints;
            UIHandler.instance.roundCounter.GetData().teamRounds = newTeamRounds;
            UIHandler.instance.roundCounterSmall.GetData().teamPoints = newTeamPoints;
            UIHandler.instance.roundCounterSmall.GetData().teamRounds = newTeamRounds;

            // UnboundLib handles PlayerManager fixing, which includes reassigning playerIDs and teamIDs
            // as well as card bar fixing
            base.OnPlayerLeftRoom(otherPlayer);

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

        public override void StartGame()
        {
            GM_TeamDeathmatch.instance.StartGame();
        }

        public override void ResetGame()
        {
            GM_TeamDeathmatch.instance.ResetMatch();
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