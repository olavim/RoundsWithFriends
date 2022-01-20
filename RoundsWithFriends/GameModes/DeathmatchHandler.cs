namespace RWF.GameModes
{
    public class DeathmatchHandler : RWFGameModeHandler<GM_Deathmatch>
    {
        public DeathmatchHandler() : base(
            name: "Deathmatch",
            gameModeId: "Deathmatch",
            allowTeams: false,
            pointsToWinRound: 2,
            roundsToWinGame: 3,
            // null values mean RWF's instance values
            playersRequiredToStartGame: null,
            maxPlayers: null,
            maxTeams: null,
            maxClients: null)
        {

        }
    }
}