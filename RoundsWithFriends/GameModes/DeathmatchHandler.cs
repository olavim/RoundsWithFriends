namespace RWF.GameModes
{
    public class DeathmatchHandler : RWFGameModeHandler<GM_Deathmatch>
    {
        internal const string GameModeName = "Deathmatch";
        internal const string GameModeID = "Deathmatch";
        public DeathmatchHandler() : base(
            name: GameModeName,
            gameModeId: GameModeID,
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