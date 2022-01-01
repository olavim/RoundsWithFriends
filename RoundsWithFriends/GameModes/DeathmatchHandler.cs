using UnboundLib;
using UnboundLib.GameModes;
using System.Collections.Generic;
using System.Linq;

namespace RWF.GameModes
{
    public class DeathmatchHandler : GameModeHandler<GM_Deathmatch>
    {
        public override string Name
        {
            get { return "Deathmatch"; }
        }

        public override GameSettings Settings { get; protected set; }

        public DeathmatchHandler() : base("Deathmatch")
        {
            this.Settings = new GameSettings() {
                { "pointsToWinRound", 2 },
                { "roundsToWinGame", 3 },
                { "allowTeams", false }
            };
        }

        public override void SetActive(bool active)
        {
            this.GameMode.gameObject.SetActive(active);
        }

        public override void PlayerJoined(Player player)
        {
            GM_Deathmatch.instance.PlayerJoined(player);
        }

        public override void PlayerDied(Player player, int playersAlive)
        {
            GM_Deathmatch.instance.PlayerDied(player, playersAlive);
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            List<Player> disconnected = PlayerManager.instance.players.Where(p => p.data.view.ControllerActorNr == otherPlayer.ActorNumber).ToList();

            // get new teamIDs
            Dictionary<int, List<Player>> teams = new Dictionary<int, List<Player>>() { };
            foreach (Player player in PlayerManager.instance.players.Except(disconnected).OrderBy(p=>p.teamID).ThenBy(p=>p.playerID))
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
                    newTeamPoints[newTeamIDs[player]] = GM_Deathmatch.instance.teamPoints[player.teamID];
                }
                if (!newTeamRounds.Keys.Contains(newTeamIDs[player]))
                {
                    newTeamRounds[newTeamIDs[player]] = GM_Deathmatch.instance.teamRounds[player.teamID];
                }
            }

            GM_Deathmatch.instance.teamPoints = newTeamPoints;
            GM_Deathmatch.instance.teamRounds = newTeamRounds;

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
            GM_Deathmatch.instance.StartGame();
        }

        public override void ResetGame()
        {
            GM_Deathmatch.instance.ResetMatch();
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