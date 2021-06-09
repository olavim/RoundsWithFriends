namespace RWF.GameModes
{
	public class DeathmatchProxy : IGameMode
	{
		public string Name {
			get { return "Deathmatch"; }
		}

		public bool IsCeaseFire {
			get {
				return GM_Deathmatch.instance.IsCeaseFire;
			}
		}

		public void SetActive(bool active) {
			GameMode.GetGameObject(this.Name).SetActive(active);
		}

		public void StartGame() {
			GM_Deathmatch.instance.StartGame();
		}

		public void PlayerJoined(Player player) {
			GM_Deathmatch.instance.PlayerJoined(player);
		}

		public void PlayerDied(Player player, int playersAlive) {
			GM_Deathmatch.instance.PlayerDied(player, playersAlive);
		}
	}
}