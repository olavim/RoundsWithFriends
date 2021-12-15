using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RWF.Algorithms
{
    // Represents a graph for the navmesh of a map
    public class MapGraph : Graph
    {
        private static readonly LayerMask groundMask = (LayerMask) LayerMask.GetMask(new string[] { "Default", "IgnorePlayer", "IgnoreMap" });
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
                // Check if any colliders are too close - if so, then discard the vertex
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
            if (rayWidth == 0f)
            {
                for (int i = 0; i < this.width; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        RaycastHit2D raycastHit2D = Physics2D.Raycast(this.vertices[i], this.vertices[j] - this.vertices[i], Vector2.Distance(this.vertices[i], this.vertices[j]), MapGraph.groundMask);
                        if (raycastHit2D.transform)
                        {
                            this[i, j] = false;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < this.width; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        Vector2 dir = this.vertices[j] - this.vertices[i];
                        float dist = Vector2.Distance(this.vertices[i], this.vertices[j]);

                        // Check node adjacency by Raycasting between them. if successful, two more raycasts tangent to the circle of radius raywidth are done and must pass in order to consider small gaps as traversable.
                        
                        if (Physics2D.Raycast(this.vertices[i], dir, dist, MapGraph.groundMask).transform != null)
                        {
                            Vector2 radius = (rayWidth/2f) * Vector3.Cross(dir, Vector3.forward).normalized;

                            if (Physics2D.Raycast(this.vertices[i] + radius, dir, dist, MapGraph.groundMask).transform != null || Physics2D.Raycast(this.vertices[i] - radius, dir, dist, MapGraph.groundMask).transform != null)
                            {
                                this[i, j] = false;
                            }
                        }

                    }
                }
            }
        }
    }
}