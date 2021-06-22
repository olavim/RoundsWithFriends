using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace RWF.Patches
{
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
