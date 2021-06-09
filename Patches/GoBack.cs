using HarmonyLib;
using InControl;
using UnityEngine;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(GoBack), "Update")]
    class GoBack_Patch_Update
    {
        static bool Prefix() {
			bool backPressed = false;

			for (int i = 0; i < InputManager.ActiveDevices.Count; i++) {
				if (InputManager.ActiveDevices[i].Action2.WasPressed) {
					backPressed = true;
				}
			}

			if (Input.GetKeyDown(KeyCode.Escape)) {
				backPressed = true;
			}

			if (backPressed) {
				/* When a CharacterCreator is open, pressing the back button should close the creator
				 * instead of going back to the previous menu.
				 */
				if (CharacterCreatorHandler.instance.SomeoneIsEditing()) {
					CharacterCreatorHandler.instance.CloseMenus();
					return false;
				}

				RWFMod.instance.gameSettings.SetGameMode((string)null);
			}

			return true;
		}
    }
}
