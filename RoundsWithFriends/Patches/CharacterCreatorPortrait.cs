using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(CharacterCreatorPortrait), "ClickButton")]
    class CharacterCreatorPortrait_Patch_ClickButton
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var ins in instructions)
            {
                if (ins.opcode == OpCodes.Ldc_I4_S && (sbyte)ins.operand == (sbyte)10)
                {
                    ins.operand = RWFMod.MaxPlayersHardLimit;
                }
                yield return ins;
            }
        }
    }
    [HarmonyPatch(typeof(CharacterCreatorPortrait), "EditCharacter")]
    class CharacterCreatorPortrait_Patch_EditCharacter
    {
        static void Postfix(CharacterCreatorPortrait __instance, int ___playerId) {
            if (___playerId != -1) {
                var selectionInstanceGo = __instance.transform.parent.parent.parent.gameObject;
                var selectionInstanceIndex = selectionInstanceGo.transform.GetSiblingIndex();

                // The are two CharacterSelectionInstances next to each other on multiple rows
                var instanceOnLeftSide = (selectionInstanceIndex % 2 == 0);
                var objectsToEnable = new List<GameObject>();

                foreach (Transform child in selectionInstanceGo.transform.parent) {
                    var index = child.GetSiblingIndex();
                    var childOnLeftSide = (index % 2 == 0);

                    if (instanceOnLeftSide == childOnLeftSide) {
                        child.gameObject.SetActive(false);
                        objectsToEnable.Add(child.gameObject);
                    }
                }

                var creator = CharacterCreatorHandler.instance.transform.GetChild(___playerId + 1).GetComponent<CharacterCreator>();
                creator.SetObjectsToEnable(objectsToEnable);
            }
        }
    }
}
