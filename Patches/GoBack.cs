using HarmonyLib;
using InControl;
using UnityEngine;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(GoBack), "Update")]
    class GoBack_Patch_Update
    {
        static bool Prefix() {
			/* When a CharacterCreator is open, pressing the back button should close the creator
			 * instead of going back to the previous menu.
			 */
			if (CharacterCreatorHandler.instance.SomeoneIsEditing()) {
				for (int i = 0; i < InputManager.ActiveDevices.Count; i++) {
					if (InputManager.ActiveDevices[i].Action2.WasPressed) {
						CharacterCreatorHandler.instance.CloseMenus();
					}
				}

				if (Input.GetKeyDown(KeyCode.Escape)) {
					CharacterCreatorHandler.instance.CloseMenus();
				}

				return false;
			}

			return true;
		}
    }
}
