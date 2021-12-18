using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(Gun), "ApplyProjectileStats")]
    class Gun_Patch_ApplyProjectileStats
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
}
