using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using UnboundLib;
using System.Reflection.Emit;
using System.Linq;
using System;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(HealthHandler), "RPCA_Die")]
    class HealthHandler_Patch_RPCA_Die
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // for some reason the game uses the playerID to set the team color instead of the teamID
            var f_playerID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var f_teamID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "teamID");

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
                throw new Exception("[RPCA_Die PATCH] INSTRUCTION NOT FOUND");
            }
            // Instead of `this.teamID = playerID`, we obviously want `this.teamID = teamID`
            ins[idx].operand = f_teamID;

            return ins.AsEnumerable();
        }
    }
    [HarmonyPatch(typeof(HealthHandler), "RPCA_Die_Phoenix")]
    class HealthHandler_Patch_RPCA_Die_Phoenix
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // for some reason the game uses the playerID to set the team color instead of the teamID
            var f_playerID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var f_teamID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "teamID");

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
                throw new Exception("[RPCA_Die_Phoenix PATCH] INSTRUCTION NOT FOUND");
            }
            // Instead of `this.teamID = playerID`, we obviously want `this.teamID = teamID`
            ins[idx].operand = f_teamID;

            return ins.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(HealthHandler), "Revive")]
    class HealthHandler_Patch_Revive
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // for some reason the game uses the playerID to set the team color instead of the teamID
            var f_playerID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var f_teamID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "teamID");

            foreach (var ins in instructions)
            {
                if (ins.LoadsField(f_playerID))
                {
                    // Instead of `this.teamID = playerID`, we obviously want `this.teamID = teamID`
                    ins.operand = f_teamID;
                }

                yield return ins;
            }
        }
    }

    [HarmonyPatch(typeof(HealthHandler), "TakeForce")]
    class HealthHandler_Patch_TakeForce
    {
        static bool IsCeaseFire()
        {
            return RWFMod.instance.IsCeaseFire;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_simulated = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(PlayerVelocity), "simulated");
            var m_isCeaseFire = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(HealthHandler_Patch_TakeForce), "IsCeaseFire");

            for (int i = 0; i < list.Count; i++) {
                if (list[i].LoadsField(f_simulated) && list[i + 1].opcode == OpCodes.Brtrue) {
                    var label = (Label) list[i + 1].operand;

                    newInstructions.Add(list[i]);
                    newInstructions.Add(list[i + 1]);
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_isCeaseFire));
                    newInstructions.Add(new CodeInstruction(OpCodes.Brtrue, label));

                    i++;
                } else {
                    newInstructions.Add(list[i]);
                }
            }

            return newInstructions;
        }
    }
}
