using System.Collections.Generic;
using HarmonyLib;
using UnboundLib;
using UnityEngine;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(Player), "Start")]
    class Player_Patch_Start
    {
        static void Postfix(Player __instance) {
            if (__instance.data.view.IsMine) {

            }
        }
    }

    [HarmonyPatch(typeof(Player), "AssignTeamID")]
    class Player_Patch_AssignTeamID
    {
        static void Postfix(Player __instance) {
            SetTeamColor.TeamColorThis(__instance.gameObject, PlayerSkinBank.GetPlayerSkinColors(__instance.teamID));
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            // Somewhy the AssignTeamID method assigns playerID to teamID when player joins a room the second time
            var f_playerID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var f_teamID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "teamID");

            foreach (var ins in instructions) {
                if (ins.LoadsField(f_playerID)) {
                    // Instead of `this.teamID = playerID`, we obviously want `this.teamID = teamID`
                    ins.operand = f_teamID;
                }

                yield return ins;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "ReadTeamID")]
    class Player_Patch_ReadTeamID
    {
        static void Postfix(Player __instance) {
            SetTeamColor.TeamColorThis(__instance.gameObject, PlayerSkinBank.GetPlayerSkinColors(__instance.teamID));
        }
    }

    [HarmonyPatch(typeof(Player), "SetColors")]
    class Player_Patch_SetColors
    {
        static PlayerSkin[] vanillaSkins = new PlayerSkin[] {
            // TEAM 1
            new PlayerSkin()
            {
                color = new Color(0.7264f, 0.3429f, 0.2364f, 1f),
                backgroundColor = new Color(0.4717f, 0.1967f, 0.1224f, 1f),
            },
            // TEAM 2
            new PlayerSkin()
            {
                color = new Color(0.2811f, 0.4211f, 0.7358f, 1f),
                backgroundColor = new Color(0.1874f, 0.2577f, 0.4906f, 1f),
            },
            // TEAM 3
            new PlayerSkin()
            {
                color = new Color(0.6314f, 0.2706f, 0.2771f, 1f),
                backgroundColor = new Color(0.5569f, 0.1991f, 0.1882f, 1f),
            },
            // TEAM 4
            new PlayerSkin()
            {
                color = new Color(0.3222f, 0.5283f, 0.2716f, 1f),
                backgroundColor = new Color(0.2223f, 0.3679f, 0.1649f, 1f),
            }
        };

        static void Postfix(Player __instance)
        {
            // set the player's skin colors as well
            PlayerSkin playerSkin = __instance.teamID > 3 ? __instance.GetTeamColors() : Player_Patch_SetColors.vanillaSkins[__instance.teamID];
            Color color = playerSkin.color;
            Color backgroundColor = playerSkin.backgroundColor;

            // this is kinda messy, but for whatever reason the particlesystem colors for the vanilla skins (teams 0 to 3)
            // do not correspond to any color in PlayerSkin, so they're hardcoded here since this is the only place that these colors are set

            PlayerSkinHandler skinHandler = __instance.GetComponentInChildren<PlayerSkinHandler>();
            try
            {
                foreach (PlayerSkinParticle skin in (PlayerSkinParticle[]) skinHandler.GetFieldValue("skins"))
                {
                    skin.SetFieldValue("startColor1", backgroundColor);
                    skin.SetFieldValue("startColor2", color);
                    ParticleSystem.MainModule main = (ParticleSystem.MainModule) skin.GetFieldValue("main");
                    ParticleSystem.MinMaxGradient startColor = main.startColor;
                    startColor.colorMin = backgroundColor;
                    startColor.colorMax = color;
                    main.startColor = startColor;
                }
            }
            catch { }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // for some reason the game uses the playerID to set the team color instead of the teamID
            var f_playerID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var f_teamID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "teamID");

            foreach (var ins in instructions)
            {
                if (ins.LoadsField(f_playerID))
                {
                    // Instead of `this.teamID = playerID`, we obviously want `this.teamID = teamID`
                    ins.operand = f_teamID;
                }

                yield return ins;
            }
        }
    }
    [HarmonyPatch(typeof(Player), "GetTeamColors")]
    class Player_Patch_GetTeamColors
    {

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // for some reason the game uses the playerID to set the team color instead of the teamID
            var f_playerID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var f_teamID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "teamID");

            foreach (var ins in instructions)
            {
                if (ins.LoadsField(f_playerID))
                {
                    // Instead of `this.teamID = playerID`, we obviously want `this.teamID = teamID`
                    ins.operand = f_teamID;
                }

                yield return ins;
            }
        }
    }
}
