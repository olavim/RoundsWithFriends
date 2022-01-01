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

            public PrioritizedTeam(int teamId)
            {
                this.teamId = teamId;
                this.priority = 0;
                this.players = new List<PrioritizedPlayer>();
            }

            public void AddPlayer(Player player)
            {
                this.players.Add(new PrioritizedPlayer(player));
            }

            public void Reprioritize()
            {
                this.players.Sort((a, b) => b.priority - a.priority);
            }
        }

        private List<PrioritizedTeam> teams;
        public PickOrder(List<Player> players)
        {
            Dictionary<int, PrioritizedTeam> teamDict = new Dictionary<int, PrioritizedTeam>() { };
            
            foreach (Player player in players)
            {
                if (!teamDict.ContainsKey(player.teamID)) { teamDict[player.teamID] = new PrioritizedTeam(player.teamID); }

                teamDict[player.teamID].AddPlayer(player);
            }

            this.teams = teamDict.Values.ToList();

            this.Reprioritize();
        }

        private void Reprioritize()
        {
            for (int i = 0; i < this.teams.Count; i++)
            {
                this.teams[i].Reprioritize();
            }

            // Sort teams based on the most prioritized player of each team
            this.teams.Sort((a, b) => b.priority - a.priority);
        }

        public List<Player> GetPickOrder(int winningTeam)
        {
            for (int teamIndex = 0; teamIndex < this.teams.Count; teamIndex++)
            {
                if (this.teams[teamIndex].teamId != winningTeam)
                {
                    this.teams[teamIndex].priority += teamIndex * teamIndex;
                }
            }

            var list = new List<Player>();
            int pickOrder = 0;

            foreach (var prioritizedPlayer in this.GetPlayers(winningTeam))
            {
                list.Add(prioritizedPlayer.player);
                prioritizedPlayer.priority += pickOrder * pickOrder;
                pickOrder++;
            }

            this.Reprioritize();
            return list;
        }

        private IEnumerable<PrioritizedPlayer> GetPlayers(int winningTeam)
        {
            int maxTeamPlayers = this.teams.Select(t => t.players.Count()).Max();
            var filteredPriorities = this.teams.Where(p => p.teamId != winningTeam).ToList();

            for (int playerIndex = 0; playerIndex < maxTeamPlayers; playerIndex++)
            {
                for (int teamIndex = 0; teamIndex < filteredPriorities.Count; teamIndex++)
                {
                    if (playerIndex < filteredPriorities[teamIndex].players.Count)
                    {
                        yield return filteredPriorities[teamIndex].players[playerIndex];
                    }
                }
            }
        }

        public void HandlePlayerLeft(Player leftPlayer)
        {
            // remove any teams that are now empty
            this.teams = this.teams.Where(t => t.players.Where(p => p.player != leftPlayer).Any()).ToList();

            foreach (PrioritizedTeam team in this.teams)
            {
                team.players = team.players.Where(p => p.player != leftPlayer).ToList();
                team.teamId = team.players.First().player.teamID; // we are guaranteed to have a non-empty player list from the logic above
            }
        }
    }
}