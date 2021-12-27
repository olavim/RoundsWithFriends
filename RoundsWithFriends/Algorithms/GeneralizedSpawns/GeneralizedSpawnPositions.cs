using UnityEngine;
using UnboundLib.Networking;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace RWF.Algorithms
{
    public class GeneralizedSpawnPositions
    {
        private const string debugObjName = "__RWF_DEBUG_POINT__";
        private const float characterWidth = 0.9f;
        private const float range = 2f;
        private const float maxProject = 1000f;
        private const float groundOffset = 1f;
        private const float maxDistanceAway = 10f;
        private const float minDistanceAway = 3f;
        private const int maxAttempts = 1000;
        private const int numSamples = 50;
        private const int numRows = 20;
        private const int numCols = 32;
        private const float lmargin = 0.025f;
        private const float rmargin = 0.025f;
        private const float tmargin = 0.15f;
        private const float bmargin = 0f;
        private const float minDistanceFromLedge = 1f;

        private static readonly float eps = 1.5f;
        private static readonly LayerMask groundMask = (LayerMask) LayerMask.GetMask(new string[] { "Default", "IgnorePlayer", "IgnoreMap" });

        private static int NumberOfTeams => TeamIDs.Count();
        private static int[] TeamIDs => PlayerManager.instance.players.Select(p => p.teamID).Distinct().ToArray();
        private static int seed = 0;
        private static System.Random rng = new System.Random();

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

        private static float RandomRange(float l = 0f, float u = 1f)
        {
            return (float) ((u - l) * GeneralizedSpawnPositions.rng.NextDouble() + l);
        }

        private static Vector2 RandomInUnitCircle()
        {
            // Believe it or not, rejection sampling is actually the most efficient way to do this
            float x = GeneralizedSpawnPositions.RandomRange(-1f, 1f);
            float y = GeneralizedSpawnPositions.RandomRange(-1f, 1f);
            while (x * x + y * y > 1f)
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
                // Remove debug objects
                while (GameObject.Find(GeneralizedSpawnPositions.debugObjName))
                {
                    UnityEngine.GameObject.DestroyImmediate(GameObject.Find(GeneralizedSpawnPositions.debugObjName));
                }
            }

            // Initialize the RNG to sync clients
            GeneralizedSpawnPositions.rng = new System.Random(GeneralizedSpawnPositions.seed);

            // Blank spawn dictionary
            var spawnDictionary = new Dictionary<Player, Vector2>() { };

            // Filter out "default" (0,0) spawn points as well as duplicates
            var spawnPositions = spawnPoints.Select(s => (Vector2) s.localStartPos).Where(s => Vector2.Distance(s, Vector2.zero) > GeneralizedSpawnPositions.eps).Distinct().ToList();

            /* The mesh algorithm is relatively expensive, and so its a good idea to only use it when necessary.
             * Thus, we only use it if there are not enough spawn points for all teams at this point.
             */

            // Make sure there enough spawn points for all teams
            if (GeneralizedSpawnPositions.NumberOfTeams > spawnPositions.Count())
            {
                // Not enough, generate and use mesh to find valid points
                var map = MapManager.instance.currentMap.Map;
                var defaultPoints = GeneralizedSpawnPositions.GetDefaultPoints(GeneralizedSpawnPositions.eps);
                var vertices = MapGraph.GetVertices(map, spawnPositions, defaultPoints, out List<Vector2> spawnVertices, colliderOffset: GeneralizedSpawnPositions.groundOffset, eps: GeneralizedSpawnPositions.eps);
                var mapGraph = new MapGraph(vertices, GeneralizedSpawnPositions.characterWidth);

                // If there are no spawn positions, grab one from the largest connected sub-graph
                if (spawnPositions.Count() == 0)
                {
                    List<Vector2> largestSubgraph = mapGraph.LargestSubgraph().Select(i => mapGraph.vertices[i]).ToList();

                    // choose random valid point from largest subgraph
                    Vector2 firstSpawn = Vector2.zero;
                    bool valid = false;
                    for (int _ = 0; _ < GeneralizedSpawnPositions.maxAttempts; _++)
                    {
                        firstSpawn = largestSubgraph[GeneralizedSpawnPositions.RandomRange(0, largestSubgraph.Count())];
                        if (GeneralizedSpawnPositions.IsValidSpawnPosition(firstSpawn))
                        {
                            valid = true;
                            break;
                        }
                    }
                    if (!valid)
                    {
                        // fallback - cannot be RandomValidPosition since it must be a member of the graph
                        for (int _ = 0; _ < GeneralizedSpawnPositions.maxAttempts; _++)
                        {
                            firstSpawn = mapGraph.vertices[GeneralizedSpawnPositions.RandomRange(0, mapGraph.width)];
                            if (GeneralizedSpawnPositions.IsValidSpawnPosition(firstSpawn))
                            {
                                valid = true;
                                break;
                            }
                        }
                    }

                    spawnPositions = new List<Vector2>() { firstSpawn };
                }

                // If in debug mode, check to see if the draw option is enabled
                if (RWFMod.DEBUG)
                {
                    DrawSpawnMesh(mapGraph, spawnPositions, players);
                }

                // List of valid positions to sample
                var sampleSpace = new List<Vector2>() { };

                // Only add positions which are both spawnVertices and that are connected to the spawn points in the graph
                foreach (Vector2 spawnPosition in spawnPositions)
                {
                    sampleSpace.AddRange(mapGraph.NodesConnectedToNode(spawnPosition, false).Select(i => mapGraph.vertices[i]).Intersect(spawnVertices));
                }

                // Add positions from this sample space to the list of spawn positions
                while (GeneralizedSpawnPositions.NumberOfTeams > spawnPositions.Count())
                {
                    float bestDistance = -1f;
                    var bestPos = Vector2.zero;

                    // Add more spawn points completely at random trying to get a good (but not necessarily optimal) separation
                    for (int _ = 0; _ < GeneralizedSpawnPositions.numSamples; _++)
                    {
                        var pos = Vector2.zero;
                        for (int __ = 0; __ < GeneralizedSpawnPositions.maxAttempts; __++)
                        {
                            pos = sampleSpace[GeneralizedSpawnPositions.RandomRange(0, sampleSpace.Count())];
                            if (IsValidSpawnPosition(pos))
                            {
                                break;
                            }
                        }

                        float worstDistance = spawnPositions.Select(s => Vector2.Distance(pos, s)).Min();
                        if (worstDistance > bestDistance)
                        {
                            bestDistance = worstDistance;
                            bestPos = pos;
                        }
                    }

                    spawnPositions.Add(bestPos);
                }
            }


            // Are there enough spawn points for all players?
            if (players.Count() <= spawnPositions.Count())
            {
                var teamSpawns = new Dictionary<int, List<Vector2>>() { };
                foreach (int teamID in TeamIDs)
                {
                    teamSpawns[teamID] = new List<Vector2>() { };
                }

                bool firstTeam = true;

                // If so, then place teammates next to each other
                foreach (int teamID in TeamIDs.OrderBy(_ => GeneralizedSpawnPositions.RandomRange()).ToArray()) // Shuffle teams
                {
                    // Shuffle players in teams
                    var playersInTeam = PlayerManager.instance.GetPlayersInTeam(teamID).OrderBy(_ => GeneralizedSpawnPositions.RandomRange()).ToArray();
                    var spawnPrev = Vector2.zero;
                    if (firstTeam)
                    {
                        // Pick a spawn point at random for the very first player
                        spawnPrev = spawnPositions.OrderBy(_ => GeneralizedSpawnPositions.RandomRange()).First();
                        firstTeam = false;
                    }
                    else
                    {
                        // Otherwise, for the first player in a subsequent team, pick the spawn point furthest from all other teams
                        spawnPrev = spawnPositions.OrderByDescending(s => teamSpawns.Values.SelectMany(_ => _).Select(t => Vector2.Distance(s, t)).Sum()).First();

                    }
                    spawnPositions.Remove(spawnPrev);
                    spawnDictionary[playersInTeam[0]] = spawnPrev;
                    teamSpawns[teamID].Add(spawnPrev);
                    // Then pick closest spawn points for remaining teammates
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
                // If not, then place teammates around the same spawn point. the above code guarantees we will have enough spawn points for each team to have one.
                var shuffledTeamIDs = TeamIDs.OrderBy(_ => GeneralizedSpawnPositions.RandomRange()).ToArray(); // Shuffle teams
                var teamSpawns = new List<Vector2>() { };

                // Pick a random spawn for the first team
                var teamSpawn = spawnPositions.OrderBy(_ => GeneralizedSpawnPositions.RandomRange()).First();
                spawnPositions.Remove(teamSpawn);
                teamSpawns.Add(teamSpawn);

                // Assign spawns in random order
                foreach (Player player in PlayerManager.instance.GetPlayersInTeam(shuffledTeamIDs[0]).OrderBy(_ => GeneralizedSpawnPositions.RandomRange()).ToArray())
                {
                    spawnDictionary[player] = GetNearbyValidPosition(teamSpawn, true, spawnDictionary.Values.ToList());
                }

                // Now choose successive team spawns by order of maximum distance from all other team spawns
                for (int i = 1; i < shuffledTeamIDs.Count(); i++)
                {
                    teamSpawn = spawnPositions.OrderByDescending(s => teamSpawns.Select(t => Vector2.Distance(s, t)).Sum()).First();
                    spawnPositions.Remove(teamSpawn);
                    teamSpawns.Add(teamSpawn);

                    // Assign spawns in random order
                    foreach (Player player in PlayerManager.instance.GetPlayersInTeam(shuffledTeamIDs[i]).OrderBy(_ => GeneralizedSpawnPositions.RandomRange()).ToArray())
                    {
                        spawnDictionary[player] = GetNearbyValidPosition(teamSpawn, true, spawnDictionary.Values.ToList());
                    }
                }
            }

            return spawnDictionary;
        }

        private static void DrawSpawnMesh(MapGraph mapGraph, List<Vector2> spawnPositions, List<Player> players)
        {
            if (RWFMod.instance.debugOptions.showSpawns)
            {
                var validPoints = spawnPositions.SelectMany(s => mapGraph.NodesConnectedToNode(s, false).Select(i => mapGraph.vertices[i])).Distinct().ToList();

                foreach (Vector2 v in validPoints)
                {
                    var go = new GameObject(GeneralizedSpawnPositions.debugObjName, typeof(TextMeshPro));
                    go.GetComponent<TextMeshPro>().text = ".";
                    go.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.MidlineGeoAligned;
                    go.GetComponent<TextMeshPro>().color = new Color(0f, 1f, 0f, 0.05f);
                    go.transform.position = v;
                }

                foreach (Vector2 v in mapGraph.vertices.Where(v => !validPoints.Contains(v)))
                {

                    var go = new GameObject(GeneralizedSpawnPositions.debugObjName, typeof(TextMeshPro));
                    go.GetComponent<TextMeshPro>().text = ".";
                    go.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.MidlineGeoAligned;
                    go.GetComponent<TextMeshPro>().color = new Color(0f, 0f, 0f, 1f);
                    go.transform.position = v;
                }

                foreach (Vector2 v in spawnPositions)
                {
                    var go = new GameObject(GeneralizedSpawnPositions.debugObjName, typeof(TextMeshPro));
                    go.GetComponent<TextMeshPro>().text = ".";
                    go.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.MidlineGeoAligned;
                    go.GetComponent<TextMeshPro>().color = Color.white;
                    go.transform.position = v;
                }
            }
        }

        private static List<Vector2> GetDefaultPoints(float eps = 0f)
        {
            var points = new List<Vector2>() { };

            Vector2 min = MainCam.instance.cam.FixedScreenToWorldPoint(new Vector2(GeneralizedSpawnPositions.lmargin * FixedScreen.fixedWidth, GeneralizedSpawnPositions.bmargin * Screen.height));
            Vector2 max = MainCam.instance.cam.FixedScreenToWorldPoint(new Vector2((1f - GeneralizedSpawnPositions.rmargin) * FixedScreen.fixedWidth, (1f - GeneralizedSpawnPositions.tmargin) * Screen.height));

            float dx = (max - min).x / (float) GeneralizedSpawnPositions.numCols;
            float dy = (max - min).y / (float) GeneralizedSpawnPositions.numRows;

            Vector2 point;
            Vector2 groundedPoint;

            // Add the basic grid, projected to the nearest ground, only add if it's successfully grounded and is not within eps of any other point
            for (float i = 0.5f; i < GeneralizedSpawnPositions.numCols; i++)
            {
                for (float j = 0.5f; j < GeneralizedSpawnPositions.numRows; j++)
                {
                    point = min + new Vector2(i * dx, j * dy);
                    groundedPoint = GeneralizedSpawnPositions.CastToGround(point, out bool grounded);
                    if (grounded && !points.Where(v => Vector2.Distance(v, groundedPoint) <= eps).Any())
                    {
                        points.Add(groundedPoint);
                    }
                }
            }

            return points;
        }

        private static Vector2 CastToGround(Vector2 position)
        {
            var hit = Physics2D.Raycast(position, Vector2.down, GeneralizedSpawnPositions.maxProject, GeneralizedSpawnPositions.groundMask);
            return hit.transform
                ? position + Vector2.down * (hit.distance - GeneralizedSpawnPositions.groundOffset)
                : position;
        }

        private static Vector2 CastToGround(Vector2 position, out bool success)
        {
            var hit = Physics2D.Raycast(position, Vector2.down, GeneralizedSpawnPositions.maxProject, GeneralizedSpawnPositions.groundMask);

            if (!hit.transform || hit.distance <= GeneralizedSpawnPositions.eps)
            {
                success = false;
                return position;
            }

            success = true;
            return position + Vector2.down * hit.distance + hit.normal * GeneralizedSpawnPositions.groundOffset;
        }

        private static bool IsValidPosition(Vector2 position, out RaycastHit2D raycastHit2D)
        {

            raycastHit2D = Physics2D.Raycast(position, Vector2.down, GeneralizedSpawnPositions.range, GeneralizedSpawnPositions.groundMask);

            // Check if point is inside the margins
            var screenPoint = MainCam.instance.cam.FixedWorldToScreenPoint(position);
            if (
                screenPoint.x / FixedScreen.fixedWidth <= GeneralizedSpawnPositions.lmargin ||
                screenPoint.x / FixedScreen.fixedWidth >= 1f - GeneralizedSpawnPositions.rmargin ||
                screenPoint.y / Screen.height <= GeneralizedSpawnPositions.bmargin ||
                screenPoint.y / Screen.height >= 1f - GeneralizedSpawnPositions.tmargin
            )
            {
                return false;
            }

            bool hitNearby = raycastHit2D.transform && raycastHit2D.distance > 0.1f;
            bool hitDamageBox = raycastHit2D.collider && raycastHit2D.collider.GetComponent<DamageBox>();

            return hitNearby && !hitDamageBox;
        }

        private static bool IsValidSpawnPosition(Vector2 position)
        {
            bool centerValid = IsValidPosition(position, out RaycastHit2D hit);

            if (!centerValid)
            {
                return false;
            }

            var rightVector = GeneralizedSpawnPositions.minDistanceFromLedge * (Vector2) Vector3.Cross(hit.normal, Vector3.forward).normalized;

            // Check left and right
            return IsValidPosition(position - rightVector, out RaycastHit2D _) && IsValidPosition(position + rightVector, out RaycastHit2D _);
        }

        private static Vector2 GetNearbyValidPosition(Vector2 position, bool requireLOS = true, List<Vector2> avoidPoints = null)
        {
            // Select a random position in the ring with inner radius minDistance and outer radius maxDistance.
            // The distribution is NOT uniform. Values with smaller radius are more likely.

            // If the position is far enough from the ground (and therefore not a valid position) then it must be a spawnpoint placed in the air
            // In that case, just return a random nearby point - disregarding any floor requirements
            if (Vector2.Distance(position, CastToGround(position)) > GeneralizedSpawnPositions.maxDistanceAway)
            {
                return position + GeneralizedSpawnPositions.RandomRange(GeneralizedSpawnPositions.minDistanceAway, GeneralizedSpawnPositions.maxDistanceAway) * GeneralizedSpawnPositions.RandomOnUnitCircle();
            }

            for (int i = 0; i < GeneralizedSpawnPositions.maxAttempts; i++)
            {
                float r = GeneralizedSpawnPositions.RandomRange(GeneralizedSpawnPositions.minDistanceAway, GeneralizedSpawnPositions.maxDistanceAway);
                var newPosition = CastToGround(position + r * GeneralizedSpawnPositions.RandomOnUnitCircle());
                float dist = (avoidPoints == null || avoidPoints.Count() == 0) ? Vector2.Distance(newPosition, position) : avoidPoints.Select(v => Vector2.Distance(v, newPosition)).Max();

                if (IsValidSpawnPosition(newPosition) && dist <= GeneralizedSpawnPositions.maxDistanceAway && dist >= GeneralizedSpawnPositions.minDistanceAway)
                {
                    // Check for line-of-sight if required
                    if (requireLOS)
                    {
                        var dir = newPosition - position;

                        if (Physics2D.CircleCast(position, GeneralizedSpawnPositions.characterWidth / 2f, dir, dist, GeneralizedSpawnPositions.groundMask))
                        {
                            // The ray hit something, and therefore there is no line-of-sight, so try again
                            continue;
                        }
                    }

                    return newPosition;
                }
            }

            // If we required LOS and it failed, try again without
            if (requireLOS)
            {
                return GetNearbyValidPosition(position, false, avoidPoints);
            }
            // If we required avoiding points, but not LOS, try it again with no requirements
            else if (avoidPoints != null)
            {
                return GetNearbyValidPosition(position, false, null);
            }

            // If all else fails, just return a random valid position
            return RandomValidPosition();
        }

        private static Vector2 RandomValidPosition()
        {
            for (int i = 0; i < GeneralizedSpawnPositions.maxAttempts; i++)
            {
                var randomPos = new Vector2(
                    GeneralizedSpawnPositions.RandomRange(GeneralizedSpawnPositions.lmargin, 1f - GeneralizedSpawnPositions.rmargin) * FixedScreen.fixedWidth,
                    GeneralizedSpawnPositions.RandomRange(GeneralizedSpawnPositions.bmargin, 1f - GeneralizedSpawnPositions.tmargin) * Screen.height
                );
                var position = GeneralizedSpawnPositions.CastToGround(MainCam.instance.cam.FixedScreenToWorldPoint(randomPos));
                if (IsValidSpawnPosition(position))
                {
                    return position;
                }
            }
            return Vector2.zero;
        }
    }
}