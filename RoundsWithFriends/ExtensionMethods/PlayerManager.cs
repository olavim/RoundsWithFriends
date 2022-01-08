using RWF.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnboundLib;
using UnityEngine;

namespace RWF
{
    [Serializable]
    public class PlayerManagerAdditionalData
    {
        public PickOrder pickOrder;
        public PlayerManagerAdditionalData()
        {
            this.pickOrder = null;
        }
    }
    public static class PlayerManagerExtensions
    {
        public static readonly ConditionalWeakTable<PlayerManager, PlayerManagerAdditionalData> data =
            new ConditionalWeakTable<PlayerManager, PlayerManagerAdditionalData>();

        public static PlayerManagerAdditionalData GetAdditionalData(this PlayerManager playerManager)
        {
            return data.GetOrCreateValue(playerManager);
        }

        public static void AddData(this PlayerManager playerManager, PlayerManagerAdditionalData value)
        {
            try
            {
                data.Add(playerManager, value);
            }
            catch (Exception) { }
        }

        public static void SetPlayersKinematic(this PlayerManager playerManager, bool kinematic)
        {
            foreach (Player player in playerManager.players)
            {
                player.data.playerVel.SetFieldValue("isKinematic", kinematic);
            }
        }

        public static void ResetPickOrder(this PlayerManager playerManager)
        {
            playerManager.GetAdditionalData().pickOrder = new PickOrder(playerManager.players);
        }

        public static List<Player> GetPickOrder(this PlayerManager playerManager, int? winningTeamID = -1)
        {
            return playerManager.GetAdditionalData().pickOrder.GetPickOrder(winningTeamID ?? -1);
        }

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
