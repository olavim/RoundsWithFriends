using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using System.Reflection.Emit;
using Photon.Pun;
using System.Reflection;

namespace RWF.Patches
{
    [HarmonyPatch]
    class Gun_Patch_FireBurst
    {
        static Type GetNestedMoveType()
        {
            var nestedTypes = typeof(Gun).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic);
            Type nestedType = null;

            foreach (var type in nestedTypes)
            {
                if (type.Name.Contains("FireBurst"))
                {
                    nestedType = type;
                    break;
                }
            }

            return nestedType;
        }

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(GetNestedMoveType(), "MoveNext");
        }
        static int GetPlayerUniqueID(PhotonView view)
        {
            return view.GetComponent<Player>().GetUniqueID();
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var p_actorID = UnboundLib.ExtensionMethods.GetPropertyInfo(typeof(PhotonView), "OwnerActorNr");
            var m_uniqueID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(Gun_Patch_FireBurst), nameof(Gun_Patch_FireBurst.GetPlayerUniqueID));

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
    [HarmonyPatch(typeof(Gun), "ApplyProjectileStats")]
    class Gun_Patch_ApplyProjectileStats
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var f_playerID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var m_colorID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(PlayerExtensions), nameof(PlayerExtensions.colorID));

            List<CodeInstruction> ins = instructions.ToList();

            int idx = -1;

            for (int i = 0; i < ins.Count(); i++)
            {
                // we only want to change the first occurence here
                if (ins[i].LoadsField(f_playerID))
                {
                    idx = i;
                    break;
                }
            }
            if (idx == -1)
            {
                throw new Exception("[ApplyProjectileStats PATCH] INSTRUCTION NOT FOUND");
            }
            // get colorID instead of playerID
            ins[idx] = new CodeInstruction(OpCodes.Call, m_colorID);

            return ins.AsEnumerable();
        }
    }
}
