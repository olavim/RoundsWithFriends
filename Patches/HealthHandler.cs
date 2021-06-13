using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using UnboundLib;
using System.Reflection.Emit;
using System.Linq;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(HealthHandler), "Revive")]
    class HealthHandler_Patch_Revive
    {
        static void Postfix(SpriteRenderer ___hpSprite, Player ___player) {
            // For reasons unknown, "Health" is actually player color
            ___hpSprite.color = PlayerSkinBank.GetPlayerSkinColors(___player.teamID).color;
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

            var f_simulated = ExtensionMethods.GetFieldInfo(typeof(PlayerVelocity), "simulated");
            var m_isCeaseFire = ExtensionMethods.GetMethodInfo(typeof(HealthHandler_Patch_TakeForce), "IsCeaseFire");

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
