using UnboundLib;

namespace RWF.GameModes
{
	public class SandboxProxy : IGameMode
	{
		public string Name {
			get { return "Test"; }
		}

		public bool IsCeaseFire {
			get {
				return false;
			}
		}

		public void SetActive(bool active) {
			if (!active) {
				GameMode.GetGameObject(this.Name).SetActive(false);
			}
		}

		public void StartGame() {
			GameMode.GetGameObject(this.Name).SetActive(true);
		}

		public void PlayerJoined(Player player) {
			GM_Test.instance.InvokeMethod("PlayerWasAdded", player);
		}

		public void PlayerDied(Player player, int playersAlive) {
			GM_Test.instance.InvokeMethod("PlayerDied", player, playersAlive);
		}
	}
}
