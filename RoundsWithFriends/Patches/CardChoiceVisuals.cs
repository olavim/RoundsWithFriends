using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(CardChoiceVisuals), "Show")]
    class CardChoiceVisuals_Patch_Show
    {
        static int GetLocalIDFromPlayerID(int playerID)
        {
            return PlayerManager.instance.players[playerID].GetAdditionalData().localID;
        }
        static int GetColorIDFromPlayerID(int playerID)
        {
            return PlayerManager.instance.players[playerID].colorID();
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            int faceIdx = -1;
            int colorIdx = -1;

            var m_GetPlayerSkinColors = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(PlayerSkinBank), nameof(PlayerSkinBank.GetPlayerSkinColors));
            var m_getLocalID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(CardChoiceVisuals_Patch_Show), nameof(CardChoiceVisuals_Patch_Show.GetLocalIDFromPlayerID));
            var m_getColorID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(CardChoiceVisuals_Patch_Show), nameof(CardChoiceVisuals_Patch_Show.GetColorIDFromPlayerID));
            var f_selectedPlayerFaces = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(CharacterCreatorHandler), "selectedPlayerFaces");
            
            // replace skin[0] with skin[player.localID] in online lobbies
            for (int i = 1; i < instructions.Count() - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_0 && codes[i-1].LoadsField(f_selectedPlayerFaces) && codes[i+1].opcode == OpCodes.Ldelem_Ref)
                {
                    faceIdx = i;
                    break;
                }
            }

            if (faceIdx == -1)
            {
                throw new Exception("[CardChoiceVisuals.Show PATCH] FACE INSTRUCTION NOT FOUND");
            }

            codes[faceIdx] = new CodeInstruction(OpCodes.Ldarg_1); // loads pickerID onto the stack instead of the constant 0 [pickerID, ...]
            codes.Insert(faceIdx + 1, new CodeInstruction(OpCodes.Call, m_getLocalID)); // calls GetLocalIDFromPlayerID, taking pickerID off the stack and leaving the result [localID, ...]

            // replace GetPlayerSkinColors(playerID) with GetPlayerSkinColors(player.colorID()) always
            for (int i = 1; i < instructions.Count() - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_1 && codes[i - 1].opcode == OpCodes.Ldarg_0 && codes[i + 1].Calls(m_GetPlayerSkinColors))
                {
                    colorIdx = i + 1;
                    break;
                }
            }
            if (colorIdx == -1)
            {
                throw new Exception("[CardChoiceVisuals.Show PATCH] COLOR INSTRUCTION NOT FOUND");
            }

            codes.Insert(colorIdx, new CodeInstruction(OpCodes.Call, m_getColorID)); // calls GetColorIDFromPlayerID, taking the pickerID off the stack and leaving the colorID [colorID, ...]

            return codes.AsEnumerable();
        }
    }
}
