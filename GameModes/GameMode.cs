using System.Collections.Generic;
using UnityEngine;

namespace RWF.GameModes
{
    public static class GameMode
    {
        private static Dictionary<string, IGameMode> gameModes = new Dictionary<string, IGameMode>() {
            { "Arms race", new ArmsRaceProxy() },
            { "Deathmatch", new DeathmatchProxy() },
            { "Sandbox", new SandboxProxy() }
        };

        public static IGameMode GetGameMode(string gameModeName) {
            return GameMode.gameModes[gameModeName];
        }

        public static GameObject GetGameObject(string gameModeName) {
            return GameObject.Find("/Game/Code/Game Modes").transform.Find($"[GameMode] {gameModeName}")?.gameObject;
        }
    }
}
