using System;
using UnityEngine;

namespace RWF.GameModes
{
	public class ArmsRaceProxy : IGameMode
	{
		public string Name {
			get { return "Arms race"; }
		}

		public bool IsCeaseFire {
			get {
				return false;
            }
        }

		public void SetActive(bool active) {
			GameMode.GetGameObject(this.Name).SetActive(active);
        }

		public void StartGame() {
			GM_ArmsRace.instance.StartGame();
		}

		public void PlayerJoined(Player player) {
			GM_ArmsRace.instance.PlayerJoined(player);
		}

		public void PlayerDied(Player player, int playersAlive) {
			GM_ArmsRace.instance.PlayerDied(player, playersAlive);
		}
	}
}
