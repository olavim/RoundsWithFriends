using UnityEngine;
using UnboundLib;

namespace RWF.GameModes
{
	public class SandboxProxy : IGameMode
	{
		public string Name {
			get { return "Sandbox"; }
		}

		public GameObject gameObject {
			get {
				return GameObject.Find("/Game/Code/Game Modes").transform.Find("[GameMode] Test").gameObject;
			}
		}

		public bool IsRoundStartCeaseFire {
			get {
				return false;
			}
		}

		public void SetActive(bool active) {
			if (!active) {
				this.gameObject.SetActive(false);
			}
		}

		public void StartGame() {
			this.gameObject.SetActive(true);
		}

		public void PlayerJoined(Player player) {
			GM_Test.instance.InvokeMethod("PlayerWasAdded", player);
		}

		public void PlayerDied(Player player, int playersAlive) {
			GM_Test.instance.InvokeMethod("PlayerDied", player, playersAlive);
		}
	}
}
