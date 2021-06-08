using HarmonyLib;
using System.Collections.Generic;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(PlayerSkinHandler), "Init")]
    class PlayerSkinHandler_Patch_Init
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var f_playerID = AccessTools.Field(typeof(Player), "playerID");
            var f_teamID = AccessTools.Field(typeof(Player), "teamID");

            foreach (var ins in instructions) {
                if (ins.LoadsField(f_playerID)) {
                    ins.operand = f_teamID;
                }
            }

            return instructions;
        }
    }
}
