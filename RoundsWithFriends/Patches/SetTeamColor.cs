using System;
using HarmonyLib;
using UnityEngine;
using System.Linq;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(SetTeamColor),"TeamColorThis")]
    class SetTeamColor_Patch_TeamColorThis
    {
        static void Postfix(GameObject go, PlayerSkin teamColor)
        {
            // if the object to color is a player, make sure any unparented objects (smoke effects) are colored properly as well
            if (go?.GetComponent<PlayerJump>() != null && go.GetComponent<PlayerJump>().jumpPart.Where(j => j?.gameObject != null).Any())
            {
                SetTeamColor.TeamColorThis(go.GetComponent<PlayerJump>().jumpPart.Where(j => j?.gameObject != null).First().gameObject.transform.parent.gameObject, teamColor);
            }
        }
    }
}
