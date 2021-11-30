using UnboundLib;
using UnboundLib.GameModes;

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
                { "roundsToWinGame", 5 }
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