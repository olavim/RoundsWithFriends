using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using System.Reflection.Emit;
using Photon.Pun;

namespace RWF.Patches
{
    /* 
     * All patches here are intended to switch out references to actorID with references to Player.uniqueID
     */

    [HarmonyPatch(typeof(ApplyCardStats), "Pick")]
    class ApplyCardStats_Patch_Pick
    {
        static int GetPlayerUniqueID(PhotonView view)
        {
            return view.GetComponent<Player>().GetUniqueID();
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var p_actorID = UnboundLib.ExtensionMethods.GetPropertyInfo(typeof(PhotonView), "ControllerActorNr");
            var m_uniqueID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(ApplyCardStats_Patch_Pick), nameof(ApplyCardStats_Patch_Pick.GetPlayerUniqueID));

            foreach (var ins in instructions)
            {
                if (ins.GetsProperty(p_actorID))
                {
                    yield return new CodeInstruction(OpCodes.Call, m_uniqueID); // call the uniqueID method, which pops the photonview instance off the stack and leaves the result [uniqueID, ...]
                }
                else
                {
                    yield return ins;
                }
            }
        }
    }
}
