using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(SetTeamColorSpecific), "Start")]
    class SetTeamColorSpecific_Patch_Start
    {
        static void Prefix(SetTeamColorSpecific __instance)
        {
            float alpha = __instance.colors[0].a;

            __instance.colors = PlayerManager.instance.players
                .Select(p => p.teamID)
                .Distinct()
                .Select(id => PlayerSkinBank.GetPlayerSkinColors(id).color)
                .ToArray();

            for (int i = 0; i < __instance.colors.Length; i++)
            {
                __instance.colors[i].a = alpha;
            }
        }

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
