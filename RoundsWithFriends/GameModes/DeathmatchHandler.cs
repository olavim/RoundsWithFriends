using UnboundLib;
using UnboundLib.GameModes;

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
                { "roundsToWinGame", 3 }
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