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
                    newTeamPoints[newTeamIDs[player]] = GM_Deathmatch.instance.teamPoints[oldTeamIDs[player]];
                }
                if (!newTeamRounds.Keys.Contains(newTeamIDs[player]))
                {
                    newTeamRounds[newTeamIDs[player]] = GM_Deathmatch.instance.teamRounds[oldTeamIDs[player]];
                }
            }

            GM_Deathmatch.instance.teamPoints = newTeamPoints;
            GM_Deathmatch.instance.teamRounds = newTeamRounds;

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