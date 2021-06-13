using HarmonyLib;
using UnityEngine;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(PlayerVelocity), "FixedUpdate")]
    class PlayerVelocity_Patch_FixedUpdate
    {
        static void Postfix(PlayerVelocity __instance, bool ___simulated, bool ___isKinematic, ref Vector2 ___velocity) {
            if (!___simulated && !___isKinematic && RWFMod.instance.IsCeaseFire) {
                ___velocity += Vector2.down * Time.fixedDeltaTime * TimeHandler.timeScale * 20f;
                var deltaPos = Time.fixedDeltaTime * TimeHandler.timeScale * ___velocity;
                __instance.transform.position += new Vector3(deltaPos.x, deltaPos.y, 0);
                __instance.transform.position = new Vector3(__instance.transform.position.x, __instance.transform.position.y, 0f);
            }
        }
    }
}
