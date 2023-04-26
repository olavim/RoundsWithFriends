using System.Collections.Generic;
using System.Linq;

namespace RWF.Algorithms
{
    public interface IPickOrderStrategy
    {
        void AddPlayer(Player player);
        void RefreshOrder(int[] winningTeamIDs);
        IEnumerable<Player> GetPlayers(int[] winningTeamIDs);
    }

    class RoundRobinStrategy : IPickOrderStrategy
    {
        private Dictionary<int, List<Player>> playerOrders;
        private List<int> teamOrder;

        public RoundRobinStrategy()
        {
            this.playerOrders = new Dictionary<int, List<Player>>();
            this.teamOrder = new List<int>();
        }

        public void AddPlayer(Player player)
        {
            if (!this.playerOrders.ContainsKey(player.teamID))
            {
                this.playerOrders.Add(player.teamID, new List<Player>());
                this.teamOrder.Add(player.teamID);
            }

            this.playerOrders[player.teamID].Add(player);
        }

        public void RefreshOrder(int[] winningTeamIDs)
        {
            foreach (var key in this.playerOrders.Keys)
            {
                if (!winningTeamIDs.Contains(key))
                {
                    this.playerOrders[key].Add(this.playerOrders[key][0]);
                    this.playerOrders[key].RemoveAt(0);
                }
            }

            foreach (int winningTeamID in winningTeamIDs)
            {
                int winningTeamIndex = this.teamOrder.IndexOf(winningTeamID);

                if (winningTeamIndex != -1)
                {
                    this.teamOrder.RemoveAt(winningTeamIndex);
                    this.teamOrder.Add(this.teamOrder[0]);
                    this.teamOrder.RemoveAt(0);
                    this.teamOrder.Insert(winningTeamIndex, winningTeamID);
                }
                else
                {
                    this.teamOrder.Add(this.teamOrder[0]);
                    this.teamOrder.RemoveAt(0);
                }
            }
        }

        public IEnumerable<Player> GetPlayers(int[] winningTeamIDs)
        {
            int maxTeamPlayers = this.playerOrders.Max(p => p.Value.Count);

            for (int playerIndex = 0; playerIndex < maxTeamPlayers; playerIndex++)
            {
                foreach (int teamID in this.teamOrder.Where(id => !winningTeamIDs.Contains(id)))
                {
                    var playerOrder = this.playerOrders[teamID];
                    if (playerIndex < playerOrder.Count)
                    {
                        yield return playerOrder[playerIndex];
                    }
                }
            }
        }
    }

    public class PickOrder
    {

        private IPickOrderStrategy strategy;

        public PickOrder(List<Player> players)
        {
            this.strategy = new RoundRobinStrategy();

            foreach (Player player in players)
            {
                strategy.AddPlayer(player);
            }
        }

        public void SetStrategy(IPickOrderStrategy strategy)
        {
            this.strategy = strategy;
        }

        public List<Player> GetPickOrder(int[] winningTeamIDs)
        {
            var list = this.strategy.GetPlayers(winningTeamIDs).ToList();
            this.strategy.RefreshOrder(winningTeamIDs);
            return list;
        }
    }
}
