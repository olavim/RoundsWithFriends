using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace RWF
{
    public static class Collider2DExtensions
    {
        internal static List<Vector2> GetVertices(this Collider2D collider, float offset = 0f)
        {

            // if there is a polygon collider, use .points
            if (collider is PolygonCollider2D)
            {
                return ((PolygonCollider2D) collider).points.Select(p => (Vector2) collider.transform.TransformPoint(p)).Select(p => p + (p - (Vector2) collider.bounds.center).normalized * offset).ToList();
            }

            // if there is a box collider, calculate vertices in world space
            if (collider is BoxCollider2D)
            {
                Vector2 size = ((BoxCollider2D) collider).size * 0.5f;
                return new List<Vector2>() {
                    (Vector2) collider.transform.TransformPoint(new Vector2(size.x, size.y)) + new Vector2(offset, offset),
                    (Vector2) collider.transform.TransformPoint(new Vector2(size.x, -size.y)) + new Vector2(offset, -offset),
                    (Vector2) collider.transform.TransformPoint(new Vector2(-size.x, size.y)) + new Vector2(-offset, offset),
                    (Vector2) collider.transform.TransformPoint(new Vector2(-size.x, -size.y)) + new Vector2(-offset, -offset)
                };
            }

            // otherwise use the Axis-Aligned Bounding Box as a rough approximation
            return new List<Vector2>() {
                (Vector2) collider.bounds.min - offset * Vector2.one,
                (Vector2) collider.bounds.max + offset * Vector2.one,
                new Vector2(collider.bounds.min.x - offset, collider.bounds.max.y + offset),
                new Vector2(collider.bounds.max.x + offset, collider.bounds.min.y - offset)
            };
        }
    }
}
