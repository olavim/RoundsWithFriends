using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RWF.Algorithms
{
    // Class representing an undirected graph using an adjacency matrix
    public class Graph
    {
        /* Undirected graphs are guaranteed to have a symmetric adjacency matrix, and for this reason the adjacencyMatrix field
         * is private and is changed only through the Graph[i,j] indexer, which forces symmetry.
         *
         * In this case, we also restrict the graph to always have nodes self-connected.
         */
        public Graph(bool[,] adjacencyMatrix, Vector2[] vertices)
        {
            this.adjacencyMatrix = adjacencyMatrix;
            this.vertices = vertices.ToList();

            for (int i = 0; i < vertices.Length; i++)
            {
                this.adjacencyMatrix[i, i] = true;
            }
        }

        public Graph(IEnumerable<Vector2> vertices, bool fullyConnected = false, bool sortVertices = false)
        {
            var list = vertices.ToList();

            // Construct a graph from a list of nodes, to be connected later
            this.adjacencyMatrix = new bool[list.Count(), list.Count()];
            this.vertices = sortVertices ? Graph.SortByDistance(list) : list;

            for (int i = 0; i < list.Count(); i++)
            {
                this[i, i] = fullyConnected;
            }
        }

        public List<Vector2> vertices { get; private set; }
        public bool[,] adjacencyMatrix { get; private set; }

        public int width => this.adjacencyMatrix.GetLength(0);
        public int height => this.adjacencyMatrix.GetLength(1);

        public bool this[int i, int j]
        {
            get
            {
                // Nodes are always self-connected
                return (i == j) || this.adjacencyMatrix[i, j];
            }

            // Setter guarantees symmetry so accessors need not keep track of which half of the matrix they were using
            set
            {
                // Nodes are always self-connected
                this.adjacencyMatrix[i, j] = (i == j) || value;
                this.adjacencyMatrix[j, i] = (i == j) || value;
            }
        }

        // Get a row/column from the adjacency matrix
        public bool[] this[int i] => Enumerable.Range(0, this.height).Select(j => this.adjacencyMatrix[i, j]).ToArray();

        public int GetNodeIndex(Vector2 vertex)
        {
            return this.vertices.IndexOf(vertex);
        }

        public int[] NodesDirectlyConnectedToNode(int i, bool excludeSelf = true)
        {
            var connections = this[i];
            return Enumerable.Range(0, this.height).Where(j => connections[j] && (i != j || !excludeSelf)).ToArray();
        }

        public int[] NodesConnectedToNode(int i, bool excludeSelf = true)
        {
            // Depth-first exhaustive search starting from i
            int pos = i;
            int posIDX = 0;
            var visited = new List<int>() { pos };
            var topmost = this.NodesDirectlyConnectedToNode(i, true);

            while (!(pos == i && !topmost.Except(visited).Any()))
            {
                var toSearch = this.NodesDirectlyConnectedToNode(pos, true).Except(visited).ToArray();

                if (!toSearch.Any())
                {
                    pos = visited[posIDX - 1];
                    posIDX--;
                    continue;
                }

                pos = toSearch[0];
                posIDX++;
                visited.Add(pos);
            }

            if (excludeSelf)
            {
                visited.Remove(i);
            }

            return visited.ToArray();

        }

        public int[] NodesConnectedToNode(Vector2 vertex, bool excludeSelf = true)
        {
            return this.NodesConnectedToNode(this.GetNodeIndex(vertex), excludeSelf);
        }

        public bool NodesAreDirectlyConnected(int i, int j)
        {
            return this[i, j];
        }

        public bool NodesAreDirectlyConnected(Vector2 vertex1, Vector2 vertex2)
        {
            return this.NodesAreDirectlyConnected(this.GetNodeIndex(vertex1), this.GetNodeIndex(vertex2));
        }

        public bool NodesAreConnected(int i, int j)
        {
            return (i == j) || this.NodesConnectedToNode(i).Contains(j);
        }

        public bool NodesAreConnected(Vector2 vertex1, Vector2 vertex2)
        {
            return this.NodesAreConnected(this.GetNodeIndex(vertex1), this.GetNodeIndex(vertex2));
        }

        internal static List<Vector2> SortByDistance(List<Vector2> lst)
        {
            var output = new List<Vector2>();
            output.Add(lst[0]);
            lst.Remove(output[0]);
            int x = 0;

            for (int i = 1; i < lst.Count + x; i++)
            {
                output.Add(lst[NearestVector2(output[output.Count - 1], lst)]);
                lst.Remove(output[output.Count - 1]);
                x++;
            }

            output.AddRange(lst);
            return output;
        }

        private static int NearestVector2(Vector2 srcPt, List<Vector2> lookIn)
        {
            var smallestDistance = new KeyValuePair<float, int>();
            for (int i = 0; i < lookIn.Count; i++)
            {
                float distance = (srcPt - lookIn[i]).sqrMagnitude;
                if (i == 0)
                {
                    smallestDistance = new KeyValuePair<float, int>(distance, i);
                }
                else
                {
                    if (distance < smallestDistance.Key)
                    {
                        smallestDistance = new KeyValuePair<float, int>(distance, i);
                    }
                }
            }
            return smallestDistance.Value;
        }
    }
}