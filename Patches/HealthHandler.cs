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
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_simulated = ExtensionMethods.GetFieldInfo(typeof(PlayerVelocity), "simulated");

            var f_rwfInstance = AccessTools.Field(typeof(RWFMod), "instance");
            var f_gameSettings = ExtensionMethods.GetFieldInfo(typeof(RWFMod), "gameSettings");
            var m_gameMode = ExtensionMethods.GetPropertyInfo(typeof(GameSettings), "GameMode").GetGetMethod();
            var m_isCeaseFire = ExtensionMethods.GetPropertyInfo(typeof(GameModes.IGameMode), "IsCeaseFire").GetGetMethod();

            for (int i = 0; i < list.Count; i++) {
                if (list[i].LoadsField(f_simulated) && list[i + 1].opcode == OpCodes.Brtrue) {
                    var label = (Label) list[i + 1].operand;

                    newInstructions.Add(list[i]);
                    newInstructions.Add(list[i + 1]);
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldsfld, f_rwfInstance));
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldfld, f_gameSettings));
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
