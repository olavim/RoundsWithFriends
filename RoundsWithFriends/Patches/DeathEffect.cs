using System;
using System.Collections.Generic;
using HarmonyLib;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(DeathEffect), "PlayDeath")]
    class DeathEffect_Patch_PlayDeath
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
}
