using HarmonyLib;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RWF.Patches
{
    // patch to make point balls part of a grid layout group
    [HarmonyPatch(typeof(PointVisualizer), "Start")]
    [HarmonyPriority(Priority.First)]
    class PointVisualizer_Patch_Start
    {
        static void Prefix(PointVisualizer __instance, ref Transform ___orangeBall, ref Transform ___blueBall)
        {
            if (__instance?.transform?.Find("Group") != null)
            {
                return;
            }
            GameObject group = new GameObject("Group", typeof(GridLayoutGroup));
            group.transform.SetParent(__instance.transform);
            group.transform.localPosition += 100f * Vector3.down;
            group.transform.SetSiblingIndex(1);

            GridLayoutGroup grid = group.GetComponent<GridLayoutGroup>();
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 8;
            grid.transform.localScale = Vector3.one;
            grid.spacing = new Vector2(20f, 20f);

            ___orangeBall.SetParent(group.transform);
            ___blueBall.SetParent(group.transform);
        }
    }

    [HarmonyPatch(typeof(PointVisualizer), "Close")]
    class PointVisualizer_Patch_Close
    {
        static void Postfix(PointVisualizer __instance) {
            var data = __instance.GetData();

            if (data.teamBall != null) {
                foreach (var ball in data.teamBall.Values.ToList()) {
                    ball.SetActive(false);
                }
            }
        }
    }
}
