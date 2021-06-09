using System;
using UnityEngine;

namespace RWF.GameModes
{
    public interface IGameMode
    {
        bool IsCeaseFire { get; }
        string Name { get; }

        void SetActive(bool active);
        void StartGame();
        void PlayerJoined(Player player);
        void PlayerDied(Player player, int playersAlive);
    }
}
