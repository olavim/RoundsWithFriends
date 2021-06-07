using HarmonyLib;
using System.Linq;

namespace RWF.Patches
{
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
