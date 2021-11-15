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
    class GeneralizedSpawnPositions
    {
        private static int seed = 0;

        static int NumberOfTeams => TeamIDs.Count();
        static int[] TeamIDs => PlayerManager.instance.players.Select(p => p.teamID).Distinct().ToArray();

        private const float range = 2f;
        private const float maxProject = 1000f;
        private const float groundOffset = 1f;
        private const float maxDistanceAway = 5f;
        private const int maxAttempts = 1000;
        private const int numSamples = 100;
        private const float eps = 0.1f;
        private const float lmargin = 0.1f;
        private const float rmargin = 0.1f;
        private const float tmargin = 0.2f;
        private const float bmargin = 0f;
        private const float minDistanceFromLedge = 1f;
        private static readonly LayerMask groundMask = (LayerMask) LayerMask.GetMask(new string[] { "Default", "IgnorePlayer" });
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
        internal static Dictionary<Player, Vector2> GetSpawnDictionary(List<Player> players, SpawnPoint[] spawnPoints)
        {
            // initialize the RNG to sync clients
            GeneralizedSpawnPositions.rng = new System.Random(GeneralizedSpawnPositions.seed);

            // filter out "default" (0,0) spawn points as well as duplicates
            List<Vector2> spawnPositions = spawnPoints.Select(s => (Vector2) s.localStartPos).Where(s => Vector2.Distance(s,Vector2.zero) > GeneralizedSpawnPositions.eps).Distinct().ToList();

            // if there are no spawn positions, make at least one at random first to get the ball rolling
            if (spawnPositions.Count() == 0)
            {
                spawnPositions = new List<Vector2>() { GeneralizedSpawnPositions.RandomValidPosition() };
            }

            // make sure there enough spawn points for all teams
            while (NumberOfTeams > spawnPositions.Count())
            {
                float bestDistance = -1f;
                Vector2 bestPos = new Vector2(-float.MaxValue, -float.MaxValue);
                // add more spawn points completely at random trying to get a good separation
                for (int _ = 0; _ < GeneralizedSpawnPositions.numSamples; _++)
                {
                    Vector2 pos = RandomValidPosition();
                    if (spawnPositions.All(s => Vector2.Distance(pos, s) > bestDistance))
                    {
                        bestDistance = spawnPositions.Select(s => Vector2.Distance(pos, s)).Min();
                        bestPos = pos;
                    }
                }

                spawnPositions.Add(bestPos);
            }

            Dictionary<Player, Vector2> spawnDictionary = new Dictionary<Player, Vector2>() { };

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
                        spawnPrev = spawnPositions.OrderByDescending(s => teamSpawns.Values.SelectMany(_=>_).Select(t => Vector2.Distance(s,t)).Sum()).First();

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
        private static Vector2 CastToGround(Vector2 position)
        {
            RaycastHit2D raycastHit2D = Physics2D.Raycast(position, Vector2.down, GeneralizedSpawnPositions.maxProject, GeneralizedSpawnPositions.groundMask);
            if (!raycastHit2D.transform)
            {
                return position;
            }
            return position + Vector2.down * (raycastHit2D.distance - GeneralizedSpawnPositions.groundOffset);
        }
        private static bool IsValidPosition(Vector2 position, out RaycastHit2D raycastHit2D)
        {
            raycastHit2D = Physics2D.Raycast(position, Vector2.down, GeneralizedSpawnPositions.range, GeneralizedSpawnPositions.groundMask);

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
        private static Vector2 GetNearbyValidPosition(Vector2 position)
        {
            for (int i = 0; i < GeneralizedSpawnPositions.maxAttempts; i++)
            {
                Vector2 newposition = CastToGround(position + GeneralizedSpawnPositions.maxDistanceAway * GeneralizedSpawnPositions.RandomInUnitCircle());
                if (IsValidSpawnPosition(newposition))
                {
                    return newposition;
                }
            }
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
