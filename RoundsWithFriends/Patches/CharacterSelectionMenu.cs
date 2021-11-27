using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnboundLib;
using System.Collections;
using TMPro;
using RWF.UI;

namespace RWF.Patches
{
    static class LayoutConstants
    {
        public const float maxSize = 2f;
        public const float minSize = 1f;
        public const float maxHSpacing = -1000f;
        public const float minHSpacing = -200f;
        public const float maxVSpacing = 400f;
        public const float minVSpacing = 100f;
        public const float timeToMoveOver = 0.5f;

        public const float singleOffset = 16.668f;
    }

    [HarmonyPatch(typeof(CharacterSelectionMenu), "Start")]
    class CharacterSelectionMenu_Patch_Start
    {
        static void Postfix(CharacterSelectionMenu __instance)
        {
            GameObject group = __instance?.gameObject?.transform?.GetChild(0)?.gameObject;
            if (group == null) { return; }

            if (group?.GetComponent<VerticalLayoutGroup>() != null)
            {
                UnityEngine.GameObject.DestroyImmediate(group.GetComponent<VerticalLayoutGroup>());
            }

            GridLayoutGroup grid = group.GetOrAddComponent<GridLayoutGroup>();

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.spacing = new Vector2(LayoutConstants.maxHSpacing, LayoutConstants.maxVSpacing);
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.childAlignment = TextAnchor.MiddleCenter;

            foreach (Transform characterSelectionTransform in __instance.transform.GetChild(0))
            {
                if (characterSelectionTransform.gameObject != null)
                {
                    characterSelectionTransform.transform.localScale = LayoutConstants.maxSize * Vector3.one;
                    characterSelectionTransform.transform.position = new Vector3(100000f, 0f, 0f);
                    characterSelectionTransform.gameObject.SetActive(false);
                }
            }
        }
    }
    [HarmonyPatch(typeof(CharacterSelectionMenu), "PlayerJoined")]
    class CharacterSelectionMenu_Patch_PlayerJoined
    {
        static bool Prefix(CharacterSelectionMenu __instance, Player joinedPlayer)
        {
            __instance.transform.GetChild(0).GetComponent<GridLayoutGroup>().enabled = false;
            __instance.transform.GetChild(0).GetChild(PlayerManager.instance.players.Count - 1).transform.position = new Vector3(100000f, 0f, 0f);
            __instance.transform.GetChild(0).GetChild(PlayerManager.instance.players.Count - 1).gameObject.SetActive(true);
            __instance.transform.GetChild(0).GetChild(PlayerManager.instance.players.Count - 1).GetComponent<CharacterSelectionInstance>().StartPicking(joinedPlayer);

            RWFMod.instance.ExecuteAfterFrames(1, () =>
            {
                __instance.transform.GetChild(0).GetComponent<GridLayoutGroup>().enabled = true;
            });

            return false;
        }
    }

    [HarmonyPatch(typeof(CharacterSelectionMenu), "Update")]
    class CharacterSelectionMenu_Patch_Update
    {

        static float t = 0f;
        static float currentScale = LayoutConstants.maxSize;
        static float currentHSpacing = LayoutConstants.maxHSpacing;
        static float currentVSpacing = LayoutConstants.maxVSpacing;

        static void Prefix(CharacterSelectionMenu __instance)
        {
            if (PlayerManager.instance.players.Count <= 1)
            {
                __instance.transform.position = new Vector3(LayoutConstants.singleOffset, 0f, 0f);
            }
            else
            {
                __instance.transform.position = Vector3.zero;
            }

            foreach (Transform characterSelectionInstance in __instance.transform.GetChild(0))
            {
                if (characterSelectionInstance.GetSiblingIndex() >= PlayerManager.instance.players.Count && characterSelectionInstance.gameObject != null && characterSelectionInstance.gameObject.activeSelf)
                {
                    characterSelectionInstance.transform.localScale = LayoutConstants.maxSize * Vector3.one;
                    characterSelectionInstance.gameObject.SetActive(false);
                }
            }

            if (t <= 0f)
            {
                int numRows = UnityEngine.Mathf.CeilToInt((float)PlayerManager.instance.players.Count / 2f);

                float scale = UnityEngine.Mathf.Lerp(LayoutConstants.maxSize, LayoutConstants.minSize, (float) (numRows - 1) / (float) (UnityEngine.Mathf.Ceil(RWFMod.instance.MaxPlayers / 2f) - 1));
                float Hspacing = UnityEngine.Mathf.Lerp(LayoutConstants.maxHSpacing, LayoutConstants.minHSpacing, (float) (numRows - 1) / (float) (UnityEngine.Mathf.Ceil(RWFMod.instance.MaxPlayers / 2f) - 1));
                float Vspacing = UnityEngine.Mathf.Lerp(LayoutConstants.maxVSpacing, LayoutConstants.minVSpacing, (float) (numRows - 1) / (float) (UnityEngine.Mathf.Ceil(RWFMod.instance.MaxPlayers / 2f) - 1));

                if (scale != CharacterSelectionMenu_Patch_Update.currentScale || Hspacing != CharacterSelectionMenu_Patch_Update.currentHSpacing || Vspacing != CharacterSelectionMenu_Patch_Update.currentVSpacing)
                {
                    t = LayoutConstants.timeToMoveOver;
                    RWFMod.instance.StartCoroutine(ChangeSize(__instance, scale, Hspacing, Vspacing, CharacterSelectionMenu_Patch_Update.currentScale, CharacterSelectionMenu_Patch_Update.currentHSpacing, CharacterSelectionMenu_Patch_Update.currentVSpacing));
                    CharacterSelectionMenu_Patch_Update.currentScale = scale;
                    CharacterSelectionMenu_Patch_Update.currentHSpacing = Hspacing;
                    CharacterSelectionMenu_Patch_Update.currentVSpacing = Vspacing;

                }

            }

        }

        static IEnumerator ChangeSize(CharacterSelectionMenu menu, float scale, float HSpacing, float VSpacing, float prevscale, float prevHSpacing, float prevVSpacing)
        {
            GameObject group = menu?.gameObject?.transform?.GetChild(0)?.gameObject;
            if (group == null) { yield break; }
            GridLayoutGroup grid = group.GetOrAddComponent<GridLayoutGroup>();

            while (CharacterSelectionMenu_Patch_Update.t > 0f)
            {
                foreach (Transform characterSelectionInstance in menu.transform.GetChild(0))
                {
                    if (characterSelectionInstance.gameObject != null)
                    {
                        characterSelectionInstance.transform.localScale = UnityEngine.Mathf.Lerp(scale, prevscale, CharacterSelectionMenu_Patch_Update.t/LayoutConstants.timeToMoveOver)  * Vector3.one;
                    }
                }
                if (grid != null)
                {
                    grid.spacing = new Vector2(UnityEngine.Mathf.Lerp(HSpacing, prevHSpacing, CharacterSelectionMenu_Patch_Update.t / LayoutConstants.timeToMoveOver), UnityEngine.Mathf.Lerp(VSpacing, prevVSpacing, CharacterSelectionMenu_Patch_Update.t / LayoutConstants.timeToMoveOver));
                }
                CharacterSelectionMenu_Patch_Update.t -= TimeHandler.deltaTime;
                yield return null;
            }
            CharacterSelectionMenu_Patch_Update.t = 0f;
            foreach (Transform characterSelectionInstance in menu.transform.GetChild(0))
            {
                if (characterSelectionInstance.gameObject != null)
                {
                    characterSelectionInstance.transform.localScale = scale * Vector3.one;
                }
            }
            if (grid != null)
            {
                grid.spacing = new Vector2(HSpacing, VSpacing);
            }

            yield break;
        }

    }
}
