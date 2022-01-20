namespace RWF.GameModes
{
    public class TeamDeathmatchHandler : RWFGameModeHandler<GM_TeamDeathmatch>
    {
        public TeamDeathmatchHandler() : base(
            name: "Team Deathmatch",
            gameModeId: "Team Deathmatch",
            allowTeams: true,
            pointsToWinRound: 2,
            roundsToWinGame: 5,
            // null values mean RWF's instance values
            playersRequiredToStartGame: null,
            maxPlayers: null,
            maxTeams: null,
            maxClients: null)
        {

        }
    }
}