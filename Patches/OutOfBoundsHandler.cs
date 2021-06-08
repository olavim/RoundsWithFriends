using HarmonyLib;
using System.Collections.Generic;
using UnboundLib;
using System.Reflection.Emit;
using System.Linq;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(OutOfBoundsHandler), "LateUpdate")]
    class OutOfBoundsHandler_Patch_LateUpdate
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_simulated = ExtensionMethods.GetFieldInfo(typeof(PlayerVelocity), "simulated");

            var f_rwfInstance = AccessTools.Field(typeof(RWFMod), "instance");
            var m_gameMode = ExtensionMethods.GetPropertyInfo(typeof(RWFMod), "GameMode").GetGetMethod();
            var m_isCeaseFire = ExtensionMethods.GetPropertyInfo(typeof(GameModes.IGameMode), "IsRoundStartCeaseFire").GetGetMethod();

            for (int i = 0; i < list.Count; i++) {
                if (list[i].LoadsField(f_simulated) && list[i + 1].opcode == OpCodes.Brtrue) {
                    var label = (Label) list[i + 1].operand;

                    newInstructions.Add(list[i]);
                    newInstructions.Add(list[i + 1]);
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldsfld, f_rwfInstance));
                    newInstructions.Add(new CodeInstruction(OpCodes.Callvirt, m_gameMode));
                    newInstructions.Add(new CodeInstruction(OpCodes.Callvirt, m_isCeaseFire));
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
