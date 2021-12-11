using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RWF.Algorithms
{
    public class MapGraph : Graph
    {
        // Class representing a graph for the navmesh of a map

        private static readonly LayerMask groundMask = (LayerMask) LayerMask.GetMask(new string[] { "Default", "IgnorePlayer" });
        private const float voidMargin = 0.01f;

        public static List<Vector2> GetVertices(Map map, List<Vector2> spawnPositions, List<Vector2> defaultPoints, out List<Vector2> spawnPoints, float colliderOffset = 1f, float eps = 0.1f)
        {
            var vertices = new List<Vector2>(defaultPoints);

            // Add default points to spawn points
            spawnPoints = new List<Vector2>(defaultPoints);

            var min = MainCam.instance.cam.FixedScreenToWorldPoint(new Vector2(MapGraph.voidMargin * FixedScreen.fixedWidth, MapGraph.voidMargin * Screen.height));
            var max = MainCam.instance.cam.FixedScreenToWorldPoint(new Vector2((1f - MapGraph.voidMargin) * FixedScreen.fixedWidth, (1f - MapGraph.voidMargin) * Screen.height));

            foreach (Collider2D collider in map.gameObject.GetComponentsInChildren<Collider2D>(false))
            {
                var colliderVertices = collider.GetVertices(colliderOffset).OrderByDescending(v => v.y).ToList();

                // Additionally, add the midpoint of the top two vertices to both the spawn points (if its not a duplicate) and the vertices
                var topMid = (colliderVertices[0] + colliderVertices[1]) / 2f;
                colliderVertices.Add(topMid);
                if (!spawnPoints.Where(v => Vector2.Distance(topMid, v) <= eps).Any())
                {
                    spawnPoints.Add(topMid);
                }

                foreach (Vector2 vert in colliderVertices)
                {
                    // Only add the new vertex if it is in the area defined by the margins and not a duplicate (within eps) of another
                    if (vert.x <= max.x && vert.x >= min.x && vert.y <= max.y && vert.y >= min.y && !vertices.Where(v => Vector2.Distance(v, vert) <= eps).Any())
                    {
                        vertices.Add(vert);
                    }
                }
            }
            // Purge points that are too near colliders
            var newVertices = new List<Vector2>() { };
            foreach (var vertex in vertices)
            {
                // Check if any colliders are within a distance epsilon/2 (eps/2) - if so, then discard the vertex
                if (!Physics2D.OverlapCircle(vertex, colliderOffset * 0.9f, MapGraph.groundMask))
                {
                    newVertices.Add(vertex);
                }
            }
            vertices = newVertices;

            spawnPoints = spawnPoints.Intersect(vertices).ToList();

            // Add back the original spawnPositons to both the vertices and the spawnPoints and remove EXACT duplicates
            vertices.AddRange(spawnPositions);
            spawnPoints.AddRange(spawnPositions);

            return vertices.Distinct().ToList();
        }

        public MapGraph(List<Vector2> vertices, float rayWidth = 0f) : base(vertices, true, false)
        {
            // Start with a completely connected graph
            // Then cut the connections immediately
            this.CutConnections(rayWidth);
        }

        // Remove connections that intersect with colliders
        public void CutConnections(float rayWidth = 0f)
        {
            // Remove connections that intersect with colliders
            if (rayWidth == 0f)
            {
                for (int i = 0; i < this.width; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        var hit = Physics2D.Raycast(this.vertices[i], this.vertices[j] - this.vertices[i], Vector2.Distance(this.vertices[i], this.vertices[j]), MapGraph.groundMask);
                        if (hit.transform)
                        {
                            this[i, j] = false;
                        }
                    }
                }
            }
            else
            {
                Vector2 direction;
                for (int i = 0; i < this.width; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        direction = (this.vertices[j] - this.vertices[i]).normalized;
                        // Start rayWidth/2 + 0.01f towards the target and end rayWidth/2 + 0.01f before the target, since we don't care about intersections "behind" the points
                        if (Physics2D.CircleCast(this.vertices[i] + direction * (rayWidth / 2f + 0.01f), rayWidth / 2f, direction, Vector2.Distance(this.vertices[i], this.vertices[j]) - rayWidth / 2f - 0.01f, MapGraph.groundMask))
                        {
                            this[i, j] = false;
                        }
                    }
                }
            }
        }
    }
}