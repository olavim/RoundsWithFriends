using UnboundLib.GameModes;

namespace RWF.GameModes
{
	public class DeathmatchHandler : GameModeHandler<GM_Deathmatch>
	{
		public override string Name {
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

		public override void SetActive(bool active) {
			if (!active)
            {
				this.GameMode.Reset();
            }

			this.GameMode.gameObject.SetActive(active);
		}

		public override void StartGame() {
			GM_Deathmatch.instance.StartGame();
		}

		public override void PlayerJoined(Player player) {
			GM_Deathmatch.instance.PlayerJoined(player);
		}

		public override void PlayerDied(Player player, int playersAlive) {
			GM_Deathmatch.instance.PlayerDied(player, playersAlive);
		}
	}
}