using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Sonigon;
using HarmonyLib;
using UnboundLib;
using UnboundLib.Networking;
using System.Reflection;
using UnityEngine;
using Photon.Pun;
using TMPro;

namespace RWF.Patches
{


    [HarmonyPatch(typeof(MapManager), "CallInNewMapAndMovePlayers")]
    class MapManager_Patch_CallInNewMapAndMovePlayers
    {
        static void Prefix(MapManager __instance, PhotonView ___view, int mapID)
        {
            if (!PhotonNetwork.IsMasterClient && !PhotonNetwork.OfflineMode)
            {
                return;
            }
            NetworkingManager.RPC(typeof(GeneralizedSpawnPositions), nameof(GeneralizedSpawnPositions.RPCA_SetSeed), new object[] { UnityEngine.Random.Range(int.MinValue, int.MaxValue) });
        }
    }

    class Graph
    {
        // class representing an undirected graph using an adjacency matrix

        /*
         * undirected graphs are guaranteed to have a symmetric adjacency matrix, and for this reason the _adjMat field
         * is private and is changed only through the Graph[i,j] indexer, which forces symmetry
         * 
         * in this case, we also restrict the graph to always have nodes self-connected
         * 
         */

        public Graph(bool[,] adjacenyMatrix, Vector2[] vertices)
        {
            // construct a graph from an adjacency matrix and a list of nodes
            this._adjMat = adjacenyMatrix;
            this._vertices = vertices.ToList();

            // nodes are always self-connected
            for (int i = 0; i < vertices.Length; i++)
            {
                this._adjMat[i, i] = true;
            }
        }
        public Graph(Vector2[] vertices, bool fullyConnected = false, bool sortVertices = false)
        {
            // construct a graph from an array of nodes, to be connected later
            this._adjMat = new bool[vertices.Length, vertices.Length];
            this._vertices = sortVertices ? Graph.SortByDistance(new List<Vector2>(vertices)) : new List<Vector2>(vertices);

            for (int i = 0; i < vertices.Length; i++)
            {
                for (int j = 0; j < vertices.Length; j++)
                {
                    this[i, j] = fullyConnected;
                }
            }
        }
        public Graph(List<Vector2> vertices, bool fullyConnected = false, bool sortVertices = false)
        {
            // construct a graph from a list of nodes, to be connected later
            this._adjMat = new bool[vertices.Count(), vertices.Count()];
            this._vertices = sortVertices ? Graph.SortByDistance(new List<Vector2>(vertices)) : new List<Vector2>(vertices);

            for (int i = 0; i < vertices.Count(); i++)
            {
                for (int j = 0; j < vertices.Count(); j++)
                {
                    this[i, j] = fullyConnected;
                }

            }
        }

        private bool[,] _adjMat = null;
        private List<Vector2> _vertices = null;
        public List<Vector2> vertices
        {
            get
            {
                return this._vertices;
            }
            private set { }
        }
        public bool[,] adjacencyMatrix
        {
            get
            {
                return this._adjMat;
            }
            private set { }
        }

        public int width
        {
            get
            {
                return this._adjMat.GetLength(0);
            }
            private set { }
        }
        public int height
        {
            get
            {
                return this._adjMat.GetLength(1);
            }
            private set { }
        }

        public bool this[int i, int j]
        {
            get
            {
                // nodes are always self-connected
                return (i==j) || this._adjMat[i, j];
            }

            // setter guarantees symmetry so accessors need not keep track of which half of the matrix they were using
            set
            {
                // nodes are always self-connected
                this._adjMat[i, j] = (i==j) || value;
                this._adjMat[j, i] = (i==j) || value;
            }
        }
        public bool[] this[int i]
        {
            // get a row/column from the adjacency matrix
            get
            {
                return Enumerable.Range(0, this.height).Select(j => this._adjMat[i,j]).ToArray();
            }
            private set { }
        }

        public int GetNodeIndex(Vector2 vertex)
        {
            // returns -1 if not found
            return this._vertices.IndexOf(vertex);
        }
        public int[] NodesDirectlyConnectedToNode(int i, bool excludeSelf = true)
        {
            bool[] connections = this[i];
            return Enumerable.Range(0, this.height).Where(j => connections[j] && (i != j || !excludeSelf)).ToArray();
        }
        public int[] NodesConnectedToNode(int i, bool excludeSelf = true)
        {
            // depth-first exhaustive search starting from i
            int pos = i;
            int posIDX = 0;
            List<int> visited = new List<int>() { pos };
            int[] topmost = this.NodesDirectlyConnectedToNode(i, true);
            while (!(pos == i && !topmost.Except(visited).Any()))
            {
                int[] toSearch = this.NodesDirectlyConnectedToNode(pos, true).Except(visited).ToArray();
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

            if (excludeSelf) { visited.Remove(i); }

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
            return (i==j) || this.NodesConnectedToNode(i).Contains(j);
        }
        public bool NodesAreConnected(Vector2 vertex1, Vector2 vertex2)
        {
            return this.NodesAreConnected(this.GetNodeIndex(vertex1), this.GetNodeIndex(vertex2));
        }

        internal static List<Vector2> SortByDistance(List<Vector2> lst)
        {
            List<Vector2> output = new List<Vector2>();
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
            KeyValuePair<float, int> smallestDistance = new KeyValuePair<float, int>();
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

    class MapGraph : Graph
    {
        // class representing a graph for the navmesh of a map
        private static readonly LayerMask groundMask = (LayerMask) LayerMask.GetMask(new string[] { "Default", "IgnorePlayer" });
        private const float voidMargin = 0.01f;
        public static List<Vector2> GetVertices(Map map, List<Vector2> spawnPositions, List<Vector2> defaultPoints, float colliderOffset = 1f, float eps = 0.1f)
        {
            List<Vector2> vertices = new List<Vector2> (spawnPositions.Concat(defaultPoints));

            Vector2 min = MainCam.instance.transform.GetComponent<Camera>().FixedScreenToWorldPoint(new Vector2(MapGraph.voidMargin * FixedScreen.fixedWidth, MapGraph.voidMargin * Screen.height));
            Vector2 max = MainCam.instance.transform.GetComponent<Camera>().FixedScreenToWorldPoint(new Vector2((1f - MapGraph.voidMargin) * FixedScreen.fixedWidth, (1f - MapGraph.voidMargin) * Screen.height));

            Vector2 center;
            Vector2 newVert;
            Vector2 offsetVert;

            // add points around the bounds of colliders
            foreach (SFPolygon sfpoly in map.gameObject.GetComponentsInChildren<Collider2D>(false).Select(c => c?.GetComponent<SFPolygon>()).Where(s => s != null))
            {
                center = sfpoly.GetComponent<Collider2D>().bounds.center;

                foreach (Vector2 vert in sfpoly.verts)
                {
                    newVert = sfpoly.transform.TransformPoint(vert);
                    offsetVert = newVert + colliderOffset * (newVert - center).normalized;
                    if (offsetVert.x <= max.x && offsetVert.x >= min.x && offsetVert.y <= max.y && offsetVert.y >= min.y)
                    {
                        vertices.Add(offsetVert);
                    }
                }
            }

            // purge points that are inside of colliders
            List<Vector2> newVertices = new List<Vector2>() { };
            foreach (Vector2 vertex in vertices)
            {
                // cast a ray upwards - if it hits something within a distance epsilon (eps) then discard the vertex
                RaycastHit2D raycastHit2D = Physics2D.Raycast(vertex, Vector2.up, eps, MapGraph.groundMask);
                if (!raycastHit2D.transform)
                {
                    newVertices.Add(vertex);
                }
            }
            vertices = newVertices;

            // purge duplicate points
            newVertices = new List<Vector2>() { vertices[0] };
            for (int i = 1; i < vertices.Count(); i++)
            {
                if (!newVertices.Where(v => Vector2.Distance(v, vertices[i]) <= eps).Any())
                {
                    newVertices.Add(vertices[i]);
                }
            }

            vertices = newVertices;

            // add back the original spawnPositons and remove EXACT duplicates
            vertices.AddRange(spawnPositions);

            return vertices.Distinct().ToList();
        }
        public MapGraph(List<Vector2> vertices) : base(vertices, true, false)
        {
            // start with a completely connected graph
            // then cut the connections immediately
            this.CutConnections();
        }

        public void CutConnections()
        {
            // remove connections that intersect with colliders

            for (int i = 0; i < this.width; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    RaycastHit2D raycastHit2D = Physics2D.Raycast(this.vertices[i], (this.vertices[j]-this.vertices[i]).normalized, Vector2.Distance(this.vertices[i], this.vertices[j]), MapGraph.groundMask);
                    if (raycastHit2D.transform)
                    {
                        this[i, j] = false;
                    }
                }
            }
        }


    }

    class GeneralizedSpawnPositions
    {
        private static int seed = 0;

        static int NumberOfTeams => TeamIDs.Count();
        static int[] TeamIDs => PlayerManager.instance.players.Select(p => p.teamID).Distinct().ToArray();

        private const float range = 2f;
        private const float maxProject = 1000f;
        private const float groundOffset = 1f;
        private const float maxDistanceAway = 5f;
        private const float minDistanceAway = 1f;
        private const int maxAttempts = 1000;
        private const int numSamples = 50;
        private const int numRows = 20;
        private const int numCols = 32;
        private const float eps = 0.5f;
        private const float lmargin = 0.05f;
        private const float rmargin = 0.05f;
        private const float tmargin = 0.15f;
        private const float bmargin = 0f;
        private const float minDistanceFromLedge = 1f;
        private static readonly LayerMask groundMask = (LayerMask) LayerMask.GetMask(new string[] { "Default", "IgnorePlayer" });
        private static System.Random rng = new System.Random();

        private const string debugObjName = "__RWF_DEBUG_POINT___";

        internal static void SetSeed(int newSeed)
        {
            GeneralizedSpawnPositions.seed = newSeed;
        }
        [UnboundRPC]
        internal static void RPCA_SetSeed(int seed)
        {
            GeneralizedSpawnPositions.SetSeed(seed);
        }
        private static int RandomRange(int l, int u)
        {
            return GeneralizedSpawnPositions.rng.Next(l, u);
        }
        private static float RandomRange(float l=0f, float u=1f)
        {
            return (float) ((u - l) * GeneralizedSpawnPositions.rng.NextDouble() + l);
        }
        private static Vector2 RandomInUnitCircle()
        {
            // believe it or not, rejection sampling is actually the most efficient way to do this
            float x = GeneralizedSpawnPositions.RandomRange(-1f, 1f);
            float y = GeneralizedSpawnPositions.RandomRange(-1f, 1f);
            while (x*x + y*y > 1f)
            {
                x = GeneralizedSpawnPositions.RandomRange(-1f, 1f);
                y = GeneralizedSpawnPositions.RandomRange(-1f, 1f);
            }
            return new Vector2(x, y);
        }
        private static Vector2 RandomOnUnitCircle()
        {
            float theta = GeneralizedSpawnPositions.RandomRange(0f, UnityEngine.Mathf.PI);

            return new Vector2(UnityEngine.Mathf.Cos(theta), UnityEngine.Mathf.Sin(theta));
        }
        internal static Dictionary<Player, Vector2> GetSpawnDictionary(List<Player> players, SpawnPoint[] spawnPoints)
        {
            if (RWFMod.DEBUG)
            {
                // remove debug objects
                while (GameObject.Find(GeneralizedSpawnPositions.debugObjName))
                {
                    UnityEngine.GameObject.DestroyImmediate(GameObject.Find(GeneralizedSpawnPositions.debugObjName));
                }
            }

            // initialize the RNG to sync clients
            GeneralizedSpawnPositions.rng = new System.Random(GeneralizedSpawnPositions.seed);

            // blank spawn dictionary
            Dictionary<Player, Vector2> spawnDictionary = new Dictionary<Player, Vector2>() { };

            // filter out "default" (0,0) spawn points as well as duplicates
            List<Vector2> spawnPositions = spawnPoints.Select(s => (Vector2) s.localStartPos).Where(s => Vector2.Distance(s,Vector2.zero) > GeneralizedSpawnPositions.eps).Distinct().ToList();

            // if there are no spawn positions, make at least one at random first to get the ball rolling
            if (spawnPositions.Count() == 0)
            {
                spawnPositions = new List<Vector2>() { GeneralizedSpawnPositions.RandomValidPosition() };
            }

            /*
             * The mesh algorithm is relatively expensive, and so its a good idea to only use it when necessary.
             * Thus, we only use it if there are not enough spawn points for all teams at this point
             * 
             */

            // make sure there enough spawn points for all teams
            if (NumberOfTeams > spawnPositions.Count())
            {
                // not enough, generate and use mesh to find valid points
                MapGraph mapGraph = new MapGraph(MapGraph.GetVertices(MapManager.instance.currentMap.Map, spawnPositions, GeneralizedSpawnPositions.GetDefaultPoints(), colliderOffset: GeneralizedSpawnPositions.groundOffset, eps: GeneralizedSpawnPositions.eps));

                // if in debug mode, check to see if the draw option is enabled
                if (RWFMod.DEBUG)
                {
                    DrawSpawnMesh(mapGraph, spawnPositions, players);
                }

                // list of valid positions to sample
                List<Vector2> sampleSpace = new List<Vector2>() { };

                foreach (Vector2 spawnPosition in spawnPositions)
                {                    
                    sampleSpace.AddRange(mapGraph.NodesConnectedToNode(spawnPosition, false).Select(i => mapGraph.vertices[i]));
                }

                // remove exact duplicates
                sampleSpace = sampleSpace.Distinct().ToList();

                // add positions from this sample space to the list of spawn positions
                while (NumberOfTeams > spawnPositions.Count())
                {
                    float bestDistance = -1f;
                    Vector2 bestPos = Vector2.zero;
                    // add more spawn points completely at random trying to get a good (but not necessarily optimal) separation
                    for (int _ = 0; _ < GeneralizedSpawnPositions.numSamples; _++)
                    {
                        Vector2 pos = Vector2.zero;
                        for (int __ = 0; __ < GeneralizedSpawnPositions.maxAttempts; __++)
                        {
                            pos = sampleSpace[GeneralizedSpawnPositions.RandomRange(0, sampleSpace.Count())];
                            if (IsValidSpawnPosition(pos))
                            {
                                break;
                            }
                        }
                        if (spawnPositions.All(s => Vector2.Distance(pos, s) > bestDistance))
                        {
                            bestDistance = spawnPositions.Select(s => Vector2.Distance(pos, s)).Min();
                            bestPos = pos;
                        }
                    }

                    spawnPositions.Add(bestPos);
                }
            }


            // are there enough spawn points for all players?
            if (players.Count() <= spawnPositions.Count())
            {
                Dictionary<int, List<Vector2>> teamSpawns = new Dictionary<int, List<Vector2>>() { };
                foreach (int teamID in TeamIDs)
                {
                    teamSpawns[teamID] = new List<Vector2>() { };
                }

                bool firstTeam = true;

                // if so, then place teammates next to each other
                foreach (int teamID in TeamIDs.OrderBy(_ => GeneralizedSpawnPositions.RandomRange()).ToArray()) // shuffle teams
                {
                    // shuffle players in teams
                    Player[] playersInTeam = PlayerManager.instance.GetPlayersInTeam(teamID).OrderBy(_ => GeneralizedSpawnPositions.RandomRange()).ToArray();
                    Vector2 spawnPrev = Vector2.zero;
                    if (firstTeam)
                    {
                        // pick a spawn point at random for the very first player
                        spawnPrev = spawnPositions.OrderBy(_ => GeneralizedSpawnPositions.RandomRange()).First();
                        firstTeam = false;
                    }
                    else
                    {
                        // otherwise, for the first player in a subsequent team, pick the spawn point furthest from all other teams
                        spawnPrev = spawnPositions.OrderByDescending(s => teamSpawns.Values.SelectMany(_ => _).Select(t => Vector2.Distance(s, t)).Sum()).First();

                    }
                    spawnPositions.Remove(spawnPrev);
                    spawnDictionary[playersInTeam[0]] = spawnPrev;
                    teamSpawns[teamID].Add(spawnPrev);
                    // then pick closest spawn points for remaining teammates
                    for (int i = 1; i < playersInTeam.Count(); i++)
                    {
                        spawnPrev = spawnPositions.OrderBy(s => Vector2.Distance(spawnPrev, s)).First();
                        spawnPositions.Remove(spawnPrev);
                        spawnDictionary[playersInTeam[i]] = spawnPrev;
                        teamSpawns[teamID].Add(spawnPrev);
                    }
                }

            }
            else
            {
                // if not, then place teammates around the same spawn point. the above code guarantees we will have enough spawn points for each team to have one
                int[] shuffledTeamIDs = TeamIDs.OrderBy(_ => GeneralizedSpawnPositions.RandomRange()).ToArray(); // shuffle teams
                List<Vector2> teamSpawns = new List<Vector2>() { };

                // pick a random spawn for the first team
                Vector2 teamSpawn = spawnPositions.OrderBy(_ => GeneralizedSpawnPositions.RandomRange()).First();
                spawnPositions.Remove(teamSpawn);
                teamSpawns.Add(teamSpawn);
                // assign spawns in random order
                foreach (Player player in PlayerManager.instance.GetPlayersInTeam(shuffledTeamIDs[0]).OrderBy(_ => GeneralizedSpawnPositions.RandomRange()).ToArray())
                {
                    spawnDictionary[player] = GetNearbyValidPosition(teamSpawn);
                }

                // now choose successive team spawns by order of maximum distance from all other team spawns
                for (int i = 1; i < shuffledTeamIDs.Count(); i++)
                {
                    teamSpawn = spawnPositions.OrderByDescending(s => teamSpawns.Select(t => Vector2.Distance(s, t)).Sum()).First();
                    spawnPositions.Remove(teamSpawn);
                    teamSpawns.Add(teamSpawn);
                    // assign spawns in random order
                    foreach (Player player in PlayerManager.instance.GetPlayersInTeam(shuffledTeamIDs[i]).OrderBy(_ => GeneralizedSpawnPositions.RandomRange()).ToArray())
                    {
                        spawnDictionary[player] = GetNearbyValidPosition(teamSpawn);
                    }
                }
            }
            
            return spawnDictionary;
        }
        private static void DrawSpawnMesh(MapGraph mapGraph, List<Vector2> spawnPositions, List<Player> players)
        {

            if (RWFMod.instance.debugOptions.showSpawns)
            {

                List<Vector2> validPoints = spawnPositions.SelectMany(s => mapGraph.NodesConnectedToNode(s, false).Select(i => mapGraph.vertices[i])).Distinct().ToList();

                foreach (Vector2 v in validPoints)
                {
                    GameObject go = new GameObject(GeneralizedSpawnPositions.debugObjName, typeof(TextMeshPro));
                    go.GetComponent<TextMeshPro>().text = ".";
                    go.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.MidlineGeoAligned;
                    go.GetComponent<TextMeshPro>().color = new Color(0f, 1f, 0f, 0.25f);
                    go.transform.position = v;
                }
                
                foreach (Vector2 v in mapGraph.vertices.Where(v => !validPoints.Contains(v)))
                {

                    GameObject go = new GameObject(GeneralizedSpawnPositions.debugObjName, typeof(TextMeshPro));
                    go.GetComponent<TextMeshPro>().text = ".";
                    go.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.MidlineGeoAligned;
                    go.GetComponent<TextMeshPro>().color = new Color(0f, 0f, 0f, 1f);
                    go.transform.position = v;
                }

                foreach (Vector2 v in spawnPositions)
                {
                    GameObject go = new GameObject(GeneralizedSpawnPositions.debugObjName, typeof(TextMeshPro));
                    go.GetComponent<TextMeshPro>().text = ".";
                    go.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.MidlineGeoAligned;
                    go.GetComponent<TextMeshPro>().color = Color.white;
                    go.transform.position = v;
                }
            }
        }

        private static List<Vector2> GetDefaultPoints()
        {
            List<Vector2> points = new List<Vector2>() { };

            Vector2 min = MainCam.instance.transform.GetComponent<Camera>().FixedScreenToWorldPoint(new Vector2(GeneralizedSpawnPositions.lmargin * FixedScreen.fixedWidth, GeneralizedSpawnPositions.bmargin * Screen.height));
            Vector2 max = MainCam.instance.transform.GetComponent<Camera>().FixedScreenToWorldPoint(new Vector2((1f-GeneralizedSpawnPositions.rmargin) * FixedScreen.fixedWidth, (1f - GeneralizedSpawnPositions.tmargin) * Screen.height));
            
            float dx = (max - min).x / (float) GeneralizedSpawnPositions.numCols;
            float dy = (max - min).y / (float) GeneralizedSpawnPositions.numRows;

            Vector2 point;
            Vector2 groundedPoint;

            // add the basic grid, projected to the nearest ground
            for (float i = 0.5f; i < GeneralizedSpawnPositions.numCols; i++)
            {
                for (float j = 0.5f; j < GeneralizedSpawnPositions.numRows; j++)
                {
                    point = min + new Vector2(i * dx, j * dy);
                    groundedPoint = GeneralizedSpawnPositions.CastToGround(point, out bool grounded);
                    if (grounded)
                    {
                        points.Add(groundedPoint);
                    }
                }
            }

            return points;
        }
        private static Vector2 CastToGround(Vector2 position)
        {
            RaycastHit2D raycastHit2D = Physics2D.Raycast(position, Vector2.down, GeneralizedSpawnPositions.maxProject, GeneralizedSpawnPositions.groundMask);
            if (!raycastHit2D.transform)
            {
                return position;
            }
            return position + Vector2.down * (raycastHit2D.distance - GeneralizedSpawnPositions.groundOffset);
        }
        private static Vector2 CastToGround(Vector2 position, out bool success)
        {
            RaycastHit2D raycastHit2D = Physics2D.Raycast(position, Vector2.down, GeneralizedSpawnPositions.maxProject, GeneralizedSpawnPositions.groundMask);
            if (!raycastHit2D.transform || raycastHit2D.distance <= GeneralizedSpawnPositions.eps)
            {
                success = false;
                return position;
            }
            success = true;
            return position + Vector2.down * (raycastHit2D.distance - GeneralizedSpawnPositions.groundOffset);
        }
        private static bool IsValidPosition(Vector2 position, out RaycastHit2D raycastHit2D)
        {

            raycastHit2D = Physics2D.Raycast(position, Vector2.down, GeneralizedSpawnPositions.range, GeneralizedSpawnPositions.groundMask);

            // check if point is inside the margins
            Vector2 screenPoint = MainCam.instance.transform.GetComponent<Camera>().FixedWorldToScreenPoint(position);
            if (screenPoint.x / FixedScreen.fixedWidth <= GeneralizedSpawnPositions.lmargin || screenPoint.x / FixedScreen.fixedWidth >= 1f - GeneralizedSpawnPositions.rmargin || screenPoint.y / Screen.height <= GeneralizedSpawnPositions.bmargin || screenPoint.y / Screen.height >= 1f - GeneralizedSpawnPositions.tmargin)
            {
                return false;
            }

            if (raycastHit2D.transform && raycastHit2D.distance > 0.1f)
            {
                if (raycastHit2D.collider && raycastHit2D.collider.GetComponent<DamageBox>() != null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        private static bool IsValidSpawnPosition(Vector2 position)
        {
            bool centerValid = IsValidPosition(position, out RaycastHit2D raycastHit2D);
            if (!centerValid)
            {
                return false;
            }

            // check left and right
            return IsValidPosition(position + GeneralizedSpawnPositions.minDistanceFromLedge * (Vector2) Vector3.Cross(raycastHit2D.normal, Vector3.forward).normalized, out RaycastHit2D _) && IsValidPosition(position - GeneralizedSpawnPositions.minDistanceFromLedge * (Vector2) Vector3.Cross(raycastHit2D.normal, Vector3.forward).normalized, out RaycastHit2D _);

        }
        private static Vector2 GetNearbyValidPosition(Vector2 position, bool requireLOS = true)
        {
            // select a random position in the ring with inner radius minDistance and outer radius maxDistance
            // the distribution is NOT uniform. values with smaller radius are more likely

            // if the position is significantly far from the ground (and therefore not a valid position) then it must be a spawnpoint placed in the air
            // in that case, just return a random nearby point - disregarding any floor requirements
            if (Vector2.Distance(position, CastToGround(position)) > GeneralizedSpawnPositions.maxDistanceAway)
            {
                return position + GeneralizedSpawnPositions.RandomRange(GeneralizedSpawnPositions.minDistanceAway, GeneralizedSpawnPositions.maxDistanceAway) * GeneralizedSpawnPositions.RandomOnUnitCircle();
            }

            for (int i = 0; i < GeneralizedSpawnPositions.maxAttempts; i++)
            {
                float r = GeneralizedSpawnPositions.RandomRange(GeneralizedSpawnPositions.minDistanceAway, GeneralizedSpawnPositions.maxDistanceAway);
                Vector2 newposition = CastToGround(position + r * GeneralizedSpawnPositions.RandomOnUnitCircle());
                if (IsValidSpawnPosition(newposition) && Vector2.Distance(newposition, position) <= GeneralizedSpawnPositions.maxDistanceAway && Vector2.Distance(newposition, position) >= GeneralizedSpawnPositions.minDistanceAway)
                {
                    // check for line-of-sight if required
                    if (requireLOS)
                    {
                        RaycastHit2D raycastHit2D = Physics2D.Raycast(position, (newposition-position).normalized, Vector2.Distance(newposition, position), GeneralizedSpawnPositions.groundMask);
                        if (raycastHit2D.transform)
                        {
                            // the ray hit something, and therefore there is no line-of-sight, so try again
                            continue;
                        }
                    }

                    return newposition;
                }
            }

            // if we required LOS and it failed, try again without
            if (requireLOS)
            {
                return GetNearbyValidPosition(position, false);
            }

            // if all else fails, just return a random valid position
            return RandomValidPosition();
        }
        private static Vector2 RandomValidPosition()
        {
            for (int i = 0; i < GeneralizedSpawnPositions.maxAttempts; i++)
            {
                Vector2 position = CastToGround(MainCam.instance.transform.GetComponent<Camera>().FixedScreenToWorldPoint(new Vector2(GeneralizedSpawnPositions.RandomRange(GeneralizedSpawnPositions.lmargin, 1f-GeneralizedSpawnPositions.rmargin) * FixedScreen.fixedWidth, GeneralizedSpawnPositions.RandomRange(GeneralizedSpawnPositions.bmargin, 1f-GeneralizedSpawnPositions.tmargin) * Screen.height)));
                if (IsValidSpawnPosition(position)) { return position; }
            }
            return Vector2.zero;
        }
    }

    
    // extension methods for dealing with ultrawide displays
    internal static class CameraExtension
    {
        private static float correction => (Screen.width - FixedScreen.fixedWidth) / 2f;
        internal static Vector3 FixedWorldToScreenPoint(this Camera camera, Vector3 worldPoint)
        {
            Vector3 fixedScreenPoint = camera.WorldToScreenPoint(worldPoint);
            if (!FixedScreen.isUltraWide) { return fixedScreenPoint; }

            return new Vector3(fixedScreenPoint.x - correction, fixedScreenPoint.y, fixedScreenPoint.z);
        }
        internal static Vector3 FixedScreenToWorldPoint(this Camera camera, Vector3 fixedScreenPoint)
        {
            Vector3 worldPoint = camera.ScreenToWorldPoint(fixedScreenPoint);
            if (!FixedScreen.isUltraWide) { return worldPoint; }

            return new Vector3(fixedScreenPoint.x + correction, fixedScreenPoint.y, fixedScreenPoint.z);
        }
    }

    // extension for dealing with ultrawide displays
    internal static class FixedScreen
    {
        internal static bool isUltraWide => ((float) Screen.width / (float) Screen.height - FixedScreen.ratio >= FixedScreen.eps);
        private const float ratio = 16f / 9f;
        private const float eps = 1E-1f;
        internal static int fixedWidth
        {
            get
            {
                if (FixedScreen.isUltraWide)
                {
                    // widescreen (or at least nonstandard screen)
                    // we assume the height is correct (since the game seems to scale to force the height to match)
                    return (int) UnityEngine.Mathf.RoundToInt(Screen.height * FixedScreen.ratio);
                }
                else
                {
                    return Screen.width;
                }
            }
            private set { }
        }
    }
}
