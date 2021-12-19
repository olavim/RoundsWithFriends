using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using RWF.ExtensionMethods;
using System.Reflection.Emit;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(SetTeamColorSpecific), "Start")]
    class SetTeamColorSpecific_Patch_Start
    {
        static void Prefix(SetTeamColorSpecific __instance)
        {
            float alpha = __instance.colors[0].a;

            __instance.colors = PlayerManager.instance.players
                .Select(p => p.colorID())
                .Distinct()
                .Select(id => PlayerSkinBank.GetPlayerSkinColors(id).color)
                .ToArray();

            for (int i = 0; i < __instance.colors.Length; i++)
            {
                __instance.colors[i].a = alpha;
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var f_playerID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var m_colorID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(PlayerExtensions), nameof(PlayerExtensions.colorID));

            foreach (var ins in instructions)
            {
                if (ins.LoadsField(f_playerID))
                {
                    // we want colorID instead of teamID
                    yield return new CodeInstruction(OpCodes.Call, m_colorID); // call the colorID method, which pops the player instance off the stack and leaves the result [colorID, ...]
                }
                else
                {
                    yield return ins;
                }
            }
        }
    }
}
