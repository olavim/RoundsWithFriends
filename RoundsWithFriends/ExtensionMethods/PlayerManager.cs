using UnboundLib;
using System;
using System.Linq;
using UnityEngine;

namespace RWF
{
    public static class PlayerManagerExtensions
    {
        public static Player GetPlayerWithUniqueID(this PlayerManager instance, int uniqueID)
        {
            for (int i = 0; i < instance.players.Count; i++)
            {
                if (instance.players[i].GetUniqueID() == uniqueID)
                {
                    return instance.players[i];
                }
            }
            return null;
        }

        public static void AddPlayerJoinedAction(this PlayerManager instance, Action<Player> action) {
            instance.SetPropertyValue("PlayerJoinedAction", Delegate.Combine(instance.PlayerJoinedAction, action));
        }

        public static Player GetClosestPlayerInOtherTeam(this PlayerManager instance, Vector3 position, int team, bool needVision = false)
        {
            float num = float.MaxValue;

            var alivePlayersInOtherTeam = instance.players
                .Where(p => p.teamID != team)
                .Where(p => !p.data.dead)
                .ToList();

            Player result = null;

            for (int i = 0; i < alivePlayersInOtherTeam.Count; i++)
            {
                float num2 = Vector2.Distance(position, alivePlayersInOtherTeam[i].transform.position);
                if ((!needVision || instance.CanSeePlayer(position, alivePlayersInOtherTeam[i]).canSee) && num2 < num)
                {
                    num = num2;
                    result = alivePlayersInOtherTeam[i];
                }
            }

            return result;
        }
    }
}
