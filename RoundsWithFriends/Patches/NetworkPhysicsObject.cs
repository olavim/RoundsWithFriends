using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnboundLib;
using System.Reflection.Emit;
using UnityEngine;

namespace RWF.Patches
{
    // patch to fix lag caused by Null Reference exception when players interact with hurtboxes while they are being moved
    [HarmonyPatch(typeof(NetworkPhysicsObject), "Push")]
    class NetworkPhysicsObject_Patch_Push
    {
        static bool Prefix(Collider2D ___col)
        {
            return (___col != null);
        }
    }
}
