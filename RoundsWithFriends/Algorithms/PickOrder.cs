using System.Collections.Generic;
using System.Linq;

namespace RWF.Algorithms
{
    public class PickOrder
    {
        class TeamPriority
        {
            public int teamId;
            public int priority;
            public List<Player> players;

            private int cycleBaseIndex;
            private int cycleRunningIndex;

            public TeamPriority(int teamId)
            {
                this.teamId = teamId;
                this.priority = 0;

                this.players = new List<Player>();
                this.cycleBaseIndex = 0;
                this.cycleRunningIndex = 0;
            }

            public void AddPlayer(Player player)
            {
                this.players.Add(player);
            }

            public void NextCycle()
            {
                this.cycleBaseIndex++;
                this.cycleRunningIndex = 0;
            }

            public void ResetCycle()
            {
                this.cycleRunningIndex = 0;
            }

            public Player GetNextPlayer()
            {
                Player player = this.players[(this.cycleBaseIndex + this.cycleRunningIndex) % this.players.Count];
                this.cycleRunningIndex++;
                return player;
            }
        }

        private List<TeamPriority> priorities; // list of team priorities, order is NOT linked to teamID
        private Dictionary<int, List<Player>> teams; // players in teams keyed by teamID
        public PickOrder(List<Player> players)
        {
            this.teams = new Dictionary<int, List<Player>>() { };

            this.priorities = new List<TeamPriority>();
            foreach (Player player in players) 
            {
                if (!this.teams.ContainsKey(player.teamID)) { this.teams[player.teamID] = new List<Player>() { }; }

                this.teams[player.teamID].Add(player);
            }

            foreach (int teamID in this.teams.Keys)
            {
                TeamPriority team = new TeamPriority(teamID);
                this.priorities.Add(team);

                foreach (Player player in this.teams[teamID])
                {
                    team.AddPlayer(player);
                }
            }

            this.Reprioritize();
        }

        private void Reprioritize()
        {
            // Sort teams based on the most prioritized player of each team
            this.priorities.Sort((a, b) => b.priority - a.priority);
        }

        public List<Player> GetPickOrder(int winningTeam)
        {
            var list = new List<Player>(); // List of players
            int maxTeamPlayers = this.teams.Select(kv => kv.Value.Count()).Max();

            var filteredPriorities = this.priorities.Where(p => p.teamId != winningTeam).ToList();

            for (int teamIndex = 0; teamIndex < this.priorities.Count; teamIndex++)
            {
                if (this.priorities[teamIndex].teamId != winningTeam)
                {
                    this.priorities[teamIndex].priority += teamIndex * teamIndex;
                }
            }

            for (int playerIndex = 0; playerIndex < maxTeamPlayers; playerIndex++)
            {
                for (int teamIndex = 0; teamIndex < filteredPriorities.Count; teamIndex++)
                {
                    if (playerIndex >= filteredPriorities[teamIndex].players.Count)
                    {
                        continue;
                    }

                    list.Add(filteredPriorities[teamIndex].GetNextPlayer());
                }
            }

            filteredPriorities[0].NextCycle();

            this.Reprioritize();
            return list;
        }
    }
}