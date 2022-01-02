using System.Collections.Generic;
using System.Linq;

namespace RWF.Algorithms
{
    public class PickOrder
    {
        class PrioritizedPlayer
        {
            public Player player;
            public int priority;

            public PrioritizedPlayer(Player player)
            {
                this.player = player;
                this.priority = 0;
            }
        }

        class PrioritizedTeam
        {
            public int teamId;
            public int priority;
            public List<PrioritizedPlayer> players;

            private int cycleIndex;
            private int cycleRunningIndex;

            public PrioritizedTeam(int teamId)
            {
                this.teamId = teamId;
                this.priority = 0;
                this.players = new List<PrioritizedPlayer>();
                this.cycleIndex = 0;
                this.cycleRunningIndex = 0;
            }

            public void AddPlayer(Player player)
            {
                this.players.Add(new PrioritizedPlayer(player));
            }

            public void NextCycle()
            {
                this.cycleIndex++;
                this.cycleRunningIndex = 0;
            }

            public PrioritizedPlayer GetNextPlayer()
            {
                var player = this.players[(this.cycleIndex + this.cycleRunningIndex) % this.players.Count];
                this.cycleRunningIndex++;
                return player;
            }
        }

        private List<PrioritizedTeam> teamPriorities;

        public PickOrder(List<Player> players)
        {
            Dictionary<int, PrioritizedTeam> teamDict = new Dictionary<int, PrioritizedTeam>() { };

            foreach (Player player in players)
            {
                if (!teamDict.ContainsKey(player.teamID)) { teamDict[player.teamID] = new PrioritizedTeam(player.teamID); }

                teamDict[player.teamID].AddPlayer(player);
            }

            this.teamPriorities = teamDict.Values.ToList();

            this.ReprioritizeTeams();
        }

        private void ReprioritizeTeams()
        {
            // Sort teams based on the most prioritized player of each team
            this.teamPriorities.Sort((a, b) => b.priority - a.priority);
        }

        public List<Player> GetPickOrder(int winningTeam)
        {
            for (int teamIndex = 0; teamIndex < this.teamPriorities.Count; teamIndex++)
            {
                if (this.teamPriorities[teamIndex].teamId != winningTeam)
                {
                    this.teamPriorities[teamIndex].priority += teamIndex * teamIndex;
                }
            }

            var list = new List<Player>();
            var filteredPriorities = this.teamPriorities.Where(p => p.teamId != winningTeam).ToList();
            int pickOrder = 0;

            foreach (var prioritizedPlayer in this.GetPlayers(filteredPriorities))
            {
                list.Add(prioritizedPlayer.player);
                prioritizedPlayer.priority += pickOrder * pickOrder;
                pickOrder++;
            }

            this.ReprioritizeTeams();
            foreach (var priority in filteredPriorities)
            {
                priority.NextCycle();
            }

            return list;
        }

        private IEnumerable<PrioritizedPlayer> GetPlayers(List<PrioritizedTeam> priorities)
        {
            int maxTeamPlayers = priorities.Max(p => p.players.Count);

            for (int playerIndex = 0; playerIndex < maxTeamPlayers; playerIndex++)
            {
                for (int teamIndex = 0; teamIndex < priorities.Count; teamIndex++)
                {
                    if (playerIndex < priorities[teamIndex].players.Count)
                    {
                        yield return priorities[teamIndex].GetNextPlayer();
                    }
                }
            }
        }

        public void HandlePlayerLeft(Player leftPlayer)
        {
            // remove any teams that are now empty
            this.teamPriorities = this.teamPriorities.Where(t => t.players.Where(p => p.player != leftPlayer).Any()).ToList();

            foreach (PrioritizedTeam team in this.teamPriorities)
            {
                team.players = team.players.Where(p => p.player != leftPlayer).ToList();
                team.teamId = team.players.First().player.teamID; // we are guaranteed to have a non-empty player list from the logic above
            }
        }
    }
}