using HarmonyLib;
using UnityEngine;

namespace MapEditor.Patches
{
    [HarmonyPatch(typeof(ListMenu), "SelectButton")]
    public class ListMenu_Patch_SelectButton
    {
        private static void Prefix(ListMenuButton buttonToSelect)
        {
            if (buttonToSelect != null && buttonToSelect.gameObject.name.Contains("(short)"))
            {
                ListMenu.instance.bar.transform.localScale = new Vector3(buttonToSelect.GetComponent<RectTransform>().sizeDelta.x, ListMenu.instance.bar.transform.localScale.y);
            }
            else
            {
                ListMenu.instance.bar.transform.localScale = new Vector3(4000, ListMenu.instance.bar.transform.localScale.y);
            }
        }
    }
}