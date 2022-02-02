namespace RWF.GameModes
{
    public class TeamDeathmatchHandler : RWFGameModeHandler<GM_TeamDeathmatch>
    {
        internal const string GameModeName = "Team Deathmatch";
        internal const string GameModeID = "Team Deathmatch";
        public TeamDeathmatchHandler() : base(
            name: GameModeName,
            gameModeId: GameModeID,
            allowTeams: true,
            pointsToWinRound: 2,
            roundsToWinGame: 5,
            // null values mean RWF's instance values
            playersRequiredToStartGame: null,
            maxPlayers: null,
            maxTeams: null,
            maxClients: null,
            description: "Team Deathmatch is a team based game mode where each team must be the last one standing to win the point."
            )
        {

        }
    }
}