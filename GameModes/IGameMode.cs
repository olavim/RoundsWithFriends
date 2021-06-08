using System;
using UnityEngine;

namespace RWF.GameModes
{
    public interface IGameMode
    {
        GameObject gameObject { get; }
        bool IsRoundStartCeaseFire { get; }
        string Name { get; }

        void StartGame();
        void PlayerJoined(Player player);
        void PlayerDied(Player player, int playersAlive);
    }
}
