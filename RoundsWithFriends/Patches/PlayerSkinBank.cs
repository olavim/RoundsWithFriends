using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnboundLib;
using System.Reflection;
using UnityEngine;

namespace RWF.Patches
{

    static class ExtraPlayerSkins
    {

        public static PlayerSkin[] skins = ((PlayerSkinBank) typeof(PlayerSkinBank).GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null)).skins.Select(s => s.currentPlayerSkin).Concat(new PlayerSkin[RWFMod.instance.MaxPlayers - ((PlayerSkinBank) typeof(PlayerSkinBank).GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null)).skins.Length]).ToArray();

        public static PlayerSkin[] extraSkins = new PlayerSkin[]
        {
            null,null,null,null,
            new PlayerSkin()
            {
                color = new Color32(159, 163, 44, 255),
                backgroundColor = new Color32(88, 89, 54, 255),
                winText = new Color32(228, 233, 91, 255),
                particleEffect = new Color32(150, 154, 25, 255)
            },
            new PlayerSkin()
            {
                color = new Color32(92,44,163, 255),
                backgroundColor = new Color32(68,54,89,255),
                winText = new Color32(148, 91, 233, 255),
                particleEffect = new Color32(77, 25, 154, 255)
            },
            new PlayerSkin()
            {
                color = new Color32(163,44,76+25,255),
                backgroundColor = new Color32(89,54,63+25,255),
                winText = new Color32(233,91,127+25,255),
                particleEffect = new Color32(154,25,47+25,255)
            },
            new PlayerSkin()
            {
                color = new Color32(44,163,131+25,255),
                backgroundColor = new Color32(54,89,80+25,255),
                winText = new Color32(91,233,197+25,255),
                particleEffect = new Color32(25,154,132+25,255)
            },

        };

        public static PlayerSkin GetPlayerSkinColors(int team)
        {
            if (ExtraPlayerSkins.skins[team] == null)
            {
                PlayerSkin skin = ((PlayerSkinBank) typeof(PlayerSkinBank).GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null)).skins[team % 4].currentPlayerSkin;

                PlayerSkin newSkin = GameObject.Instantiate(skin).gameObject.GetComponent<PlayerSkin>();
                UnityEngine.GameObject.DontDestroyOnLoad(newSkin);
                PlayerSkin skinToSet = ExtraPlayerSkins.extraSkins[team];
                newSkin.color = skinToSet.color;
                newSkin.backgroundColor = skinToSet.backgroundColor;
                newSkin.winText = skinToSet.winText;
                newSkin.particleEffect = skinToSet.particleEffect;
                PlayerSkinParticle newSkinPart = newSkin.GetComponentInChildren<PlayerSkinParticle>();
                ParticleSystem part = newSkinPart.GetComponent<ParticleSystem>();
                ParticleSystem.MainModule main = part.main;
                ParticleSystem.MinMaxGradient startColor = main.startColor;
                startColor.colorMin = skinToSet.backgroundColor;
                startColor.colorMax = skinToSet.color;
                main.startColor = startColor;

                newSkinPart.SetFieldValue("startColor1", skinToSet.backgroundColor);
                newSkinPart.SetFieldValue("startColor2", skinToSet.color);

                ExtraPlayerSkins.skins[team] = newSkin;
            }

            return ExtraPlayerSkins.skins[team];
        }

    }
    [HarmonyPatch(typeof(PlayerSkinBank), "GetPlayerSkin")]
    [HarmonyAfter("pykess.rounds.plugins.moddingutils")]
    class PlayerSkinBank_Patch_GetPlayerSkin
    {
        static bool Prefix(int team, ref PlayerSkinBank.PlayerSkinInstance __result)
        {
            __result = new PlayerSkinBank.PlayerSkinInstance() { currentPlayerSkin = PlayerSkinBank.GetPlayerSkinColors(team) };

            return false;
        }
    }
    [HarmonyPatch(typeof(PlayerSkinBank), "GetPlayerSkinColors")]
    [HarmonyAfter("pykess.rounds.plugins.moddingutils")]
    class PlayerSkinBank_Patch_GetPlayerSkinColors
    {
        static bool Prefix(int team, ref PlayerSkin __result)
        {

            __result = ExtraPlayerSkins.GetPlayerSkinColors(team);

            return false;
        }

        static void SetPlayerSkinColor(Player player, Color colorMaxToSet, Color colorMinToSet)
        {
            if (player.gameObject.GetComponentInChildren<PlayerSkinHandler>().simpleSkin)
            {
                SpriteMask[] sprites = player.gameObject.GetComponentInChildren<SetPlayerSpriteLayer>().transform.root.GetComponentsInChildren<SpriteMask>();
                for (int i = 0; i < sprites.Length; i++)
                {
                    sprites[i].GetComponent<SpriteRenderer>().color = colorMaxToSet;
                }

                return;
            }

            PlayerSkinParticle[] componentsInChildren2 = player.gameObject.GetComponentsInChildren<PlayerSkinParticle>();
            for (int j = 0; j < componentsInChildren2.Length; j++)
            {
                ParticleSystem particleSystem2 = (ParticleSystem) componentsInChildren2[j].GetFieldValue("part");
                ParticleSystem.MainModule main2 = particleSystem2.main;
                ParticleSystem.MinMaxGradient startColor2 = particleSystem2.main.startColor;
                startColor2.colorMin = colorMinToSet;
                startColor2.colorMax = colorMaxToSet;
                main2.startColor = startColor2;
            }
            SetTeamColor[] teamColors = player.transform.root.GetComponentsInChildren<SetTeamColor>();
            for (int j = 0; j < teamColors.Length; j++)
            {
                teamColors[j].Set(new PlayerSkin
                {
                    color = colorMaxToSet,
                    backgroundColor = colorMaxToSet,
                    winText = colorMaxToSet,
                    particleEffect = colorMaxToSet
                });
            }
        }
    }

}
