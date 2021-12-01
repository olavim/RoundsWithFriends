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

    internal static class ExtraPlayerSkins
    {
        public static PlayerSkin[] skins = ((PlayerSkinBank) typeof(PlayerSkinBank).GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null)).skins.Select(s => s.currentPlayerSkin).Concat(new PlayerSkin[RWFMod.MaxPlayersHardLimit - ((PlayerSkinBank) typeof(PlayerSkinBank).GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null)).skins.Length]).ToArray();
        public static int numberOfSkins
        {
            get
            {
                return ExtraPlayerSkins.skins.Count();
            }
            private set { }
        }
        private static readonly PlayerSkin[] extraSkinBases = new PlayerSkin[]
        {
            // PLAYER 1            new PlayerSkin()
            {
                color = new Color(0.6392157f, 0.2862745f, 0.1686275f, 1f),
                backgroundColor = new Color(0.3490196f, 0.2392157f, 0.2117647f, 1f),
                winText = new Color(0.9137255f, 0.4980392f, 0.3568628f, 1f),
                particleEffect = new Color(0.6f, 0.2588235f, 0.09803922f, 1f)
            },
            // PLAYER 2
            new PlayerSkin()
            {
                color = new Color(0.1647059f, 0.3098039f, 0.5843138f, 1f),
                backgroundColor = new Color(0.2196078f, 0.254902f, 0.3098039f, 1f),
                winText = new Color(0.3568628f, 0.6f, 0.9137255f, 1f),
                particleEffect = new Color(0.09803922f, 0.3215686f, 0.6039216f, 1f)
            },
            // PLAYER 3
            new PlayerSkin()
            {
                color = new Color(0.6313726f, 0.2705882f, 0.2705882f, 1f),
                backgroundColor = new Color(0.3490196f, 0.2117647f, 0.2117647f, 1f),
                winText = new Color(0.9137255f, 0.3568628f, 0.3568628f, 1f),
                particleEffect = new Color(0.6039216f, 0.09803922f, 0.09803922f, 1f)
            },
            // PLAYER 4
            new PlayerSkin()
            {
                color = new Color(0.2627451f, 0.5372549f, 0.3254902f, 1f),
                backgroundColor = new Color(0.2196078f, 0.3098039f, 0.2784314f, 1f),
                winText = new Color(0f, 0.6862745f, 0.2666667f, 1f),
                particleEffect = new Color(0.07843138f, 0.5529412f, 0.2784314f, 1f)
            },
            // PLAYER 5
            new PlayerSkin()
            {
                color = new Color(0.6235294f, 0.6392157f, 0.172549f, 1f),
                backgroundColor = new Color(0.345098f, 0.3490196f, 0.2117647f, 1f),
                winText = new Color(0.8941177f, 0.9137255f, 0.3568628f, 1f),
                particleEffect = new Color(0.5882353f, 0.6039216f, 0.09803922f, 1f)
            },
            // PLAYER 6
            new PlayerSkin()
            {
                color = new Color(0.3607843f, 0.172549f, 0.6392157f, 1f),
                backgroundColor = new Color(0.2666667f, 0.2117647f, 0.3490196f, 1f),
                winText = new Color(0.5803922f, 0.3568628f, 0.9137255f, 1f),
                particleEffect = new Color(0.3019608f, 0.09803922f, 0.6039216f, 1f)
            },
            // PLAYER 7
            new PlayerSkin()
            {
                color = new Color(0.6392157f, 0.172549f, 0.3960784f, 1f),
                backgroundColor = new Color(0.3490196f, 0.2117647f, 0.345098f, 1f),
                winText = new Color(0.9137255f, 0.3568628f, 0.5960785f, 1f),
                particleEffect = new Color(0.6039216f, 0.09803922f, 0.282353f, 1f)
            },
            // PLAYER 8
            new PlayerSkin()
            {
                color = new Color(0.172549f, 0.6392157f, 0.6117647f, 1f),
                backgroundColor = new Color(0.2117647f, 0.3490196f, 0.4117647f, 1f),
                winText = new Color(0.3568628f, 0.9137255f, 0.8705882f, 1f),
                particleEffect = new Color(0.09803922f, 0.6039216f, 0.6156863f, 1f)
            },
            // PLAYER 9
            new PlayerSkin()
            {
                color = new Color(0.6392157f, 0.3607843f, 0.2705882f, 1f),
                backgroundColor = new Color(0.3490196f, 0.282353f, 0.2666667f, 1f),
                winText = new Color(0.9137255f, 0.6039216f, 0.5019608f, 1f),
                particleEffect = new Color(0.6f, 0.3215686f, 0.1921569f, 1f)
            },
            // PLAYER 10
            new PlayerSkin()
            {
                color = new Color(0.254902f, 0.3686275f, 0.5843138f, 1f),
                backgroundColor = new Color(0.2666667f, 0.282353f, 0.3098039f, 1f),
                winText = new Color(0.5019608f, 0.682353f, 0.9137255f, 1f),
                particleEffect = new Color(0.1921569f, 0.372549f, 0.6039216f, 1f)
            },
            // PLAYER 11
            new PlayerSkin()
            {
                color = new Color(0.6313726f, 0.3686275f, 0.3686275f, 1f),
                backgroundColor = new Color(0.3490196f, 0.2666667f, 0.2666667f, 1f),
                winText = new Color(0.9137255f, 0.5019608f, 0.5019608f, 1f),
                particleEffect = new Color(0.6039216f, 0.1921569f, 0.1921569f, 1f)
            },
            // PLAYER 12
            new PlayerSkin()
            {
                color = new Color(0.345098f, 0.5372549f, 0.3882353f, 1f),
                backgroundColor = new Color(0.2666667f, 0.3098039f, 0.2941177f, 1f),
                winText = new Color(0.1058824f, 0.6862745f, 0.3333333f, 1f),
                particleEffect = new Color(0.1647059f, 0.5529412f, 0.3294118f, 1f)
            },
            // PLAYER 13
            new PlayerSkin()
            {
                color = new Color(0.627451f, 0.6392157f, 0.3764706f, 1f),
                backgroundColor = new Color(0.345098f, 0.3490196f, 0.3215686f, 1f),
                winText = new Color(0.9019608f, 0.9137255f, 0.6470588f, 1f),
                particleEffect = new Color(0.5921569f, 0.6039216f, 0.2901961f, 1f)
            },
            // PLAYER 14
            new PlayerSkin()
            {
                color = new Color(0.4196078f, 0.2745098f, 0.6392157f, 1f),
                backgroundColor = new Color(0.2980392f, 0.2666667f, 0.3490196f, 1f),
                winText = new Color(0.6666667f, 0.5019608f, 0.9137255f, 1f),
                particleEffect = new Color(0.3568628f, 0.1921569f, 0.6039216f, 1f)
            },
            // PLAYER 15
            new PlayerSkin()
            {
                color = new Color(0.6392157f, 0.2745098f, 0.4470588f, 1f),
                backgroundColor = new Color(0.3490196f, 0.2666667f, 0.345098f, 1f),
                winText = new Color(0.9137255f, 0.5019608f, 0.6784314f, 1f),
                particleEffect = new Color(0.6039216f, 0.1921569f, 0.3411765f, 1f)
            },
            // PLAYER 16
            new PlayerSkin()
            {
                color = new Color(0.2745098f, 0.6392157f, 0.6156863f, 1f),
                backgroundColor = new Color(0.2745098f, 0.3686275f, 0.4117647f, 1f),
                winText = new Color(0.5019608f, 0.9137255f, 0.8784314f, 1f),
                particleEffect = new Color(0.1960784f, 0.6039216f, 0.6156863f, 1f)
            },
            // PLAYER 17
            new PlayerSkin()
            {
                color = new Color(0.4392157f, 0.1960784f, 0.1137255f, 1f),
                backgroundColor = new Color(0.2470588f, 0.1686275f, 0.1490196f, 1f),
                winText = new Color(0.7137255f, 0.3882353f, 0.2784314f, 1f),
                particleEffect = new Color(0.4f, 0.172549f, 0.0627451f, 1f)
            },
            // PLAYER 18
            new PlayerSkin()
            {
                color = new Color(0.1058824f, 0.2f, 0.3843137f, 1f),
                backgroundColor = new Color(0.145098f, 0.172549f, 0.2078431f, 1f),
                winText = new Color(0.2784314f, 0.4666667f, 0.7137255f, 1f),
                particleEffect = new Color(0.0627451f, 0.2117647f, 0.4039216f, 1f)
            },
            // PLAYER 19
            new PlayerSkin()
            {
                color = new Color(0.4313726f, 0.1843137f, 0.1843137f, 1f),
                backgroundColor = new Color(0.2470588f, 0.1490196f, 0.1490196f, 1f),
                winText = new Color(0.7137255f, 0.2784314f, 0.2784314f, 1f),
                particleEffect = new Color(0.4039216f, 0.0627451f, 0.0627451f, 1f)
            },
            // PLAYER 20
            new PlayerSkin()
            {
                color = new Color(0.1647059f, 0.3372549f, 0.2039216f, 1f),
                backgroundColor = new Color(0.145098f, 0.2078431f, 0.1882353f, 1f),
                winText = new Color(0f, 0.4862745f, 0.1882353f, 1f),
                particleEffect = new Color(0.04705882f, 0.3529412f, 0.1764706f, 1f)
            },
            // PLAYER 21
            new PlayerSkin()
            {
                color = new Color(0.427451f, 0.4392157f, 0.1176471f, 1f),
                backgroundColor = new Color(0.2431373f, 0.2470588f, 0.1490196f, 1f),
                winText = new Color(0.6980392f, 0.7137255f, 0.2784314f, 1f),
                particleEffect = new Color(0.3921569f, 0.4039216f, 0.0627451f, 1f)
            },
            // PLAYER 22
            new PlayerSkin()
            {
                color = new Color(0.2470588f, 0.1176471f, 0.4392157f, 1f),
                backgroundColor = new Color(0.1882353f, 0.1490196f, 0.2470588f, 1f),
                winText = new Color(0.4509804f, 0.2784314f, 0.7137255f, 1f),
                particleEffect = new Color(0.2f, 0.0627451f, 0.4039216f, 1f)
            },
            // PLAYER 23
            new PlayerSkin()
            {
                color = new Color(0.4392157f, 0.1176471f, 0.2705882f, 1f),
                backgroundColor = new Color(0.2470588f, 0.1490196f, 0.2431373f, 1f),
                winText = new Color(0.7137255f, 0.2784314f, 0.4627451f, 1f),
                particleEffect = new Color(0.4039216f, 0.0627451f, 0.1882353f, 1f)
            },
            // PLAYER 24
            new PlayerSkin()
            {
                color = new Color(0.1176471f, 0.4392157f, 0.4196078f, 1f),
                backgroundColor = new Color(0.1568628f, 0.2627451f, 0.3098039f, 1f),
                winText = new Color(0.2784314f, 0.7137255f, 0.6784314f, 1f),
                particleEffect = new Color(0.0627451f, 0.4039216f, 0.4156863f, 1f)
            },
            // PLAYER 25
            new PlayerSkin()
            {
                color = new Color(0.4392157f, 0.2470588f, 0.1843137f, 1f),
                backgroundColor = new Color(0.2470588f, 0.2f, 0.1882353f, 1f),
                winText = new Color(0.7137255f, 0.4705882f, 0.3882353f, 1f),
                particleEffect = new Color(0.4f, 0.2117647f, 0.1254902f, 1f)
            },
            // PLAYER 26
            new PlayerSkin()
            {
                color = new Color(0.1647059f, 0.2392157f, 0.3843137f, 1f),
                backgroundColor = new Color(0.1803922f, 0.1882353f, 0.2078431f, 1f),
                winText = new Color(0.3882353f, 0.5294118f, 0.7137255f, 1f),
                particleEffect = new Color(0.1254902f, 0.2470588f, 0.4039216f, 1f)
            },
            // PLAYER 27
            new PlayerSkin()
            {
                color = new Color(0.4313726f, 0.2509804f, 0.2509804f, 1f),
                backgroundColor = new Color(0.2470588f, 0.1882353f, 0.1882353f, 1f),
                winText = new Color(0.7137255f, 0.3882353f, 0.3882353f, 1f),
                particleEffect = new Color(0.4039216f, 0.1254902f, 0.1254902f, 1f)
            },
            // PLAYER 28
            new PlayerSkin()
            {
                color = new Color(0.2156863f, 0.3372549f, 0.2431373f, 1f),
                backgroundColor = new Color(0.1803922f, 0.2078431f, 0.1960784f, 1f),
                winText = new Color(0.07450981f, 0.4862745f, 0.2352941f, 1f),
                particleEffect = new Color(0.1019608f, 0.3529412f, 0.2078431f, 1f)
            },
            // PLAYER 29
            new PlayerSkin()
            {
                color = new Color(0.427451f, 0.4392157f, 0.254902f, 1f),
                backgroundColor = new Color(0.2431373f, 0.2470588f, 0.227451f, 1f),
                winText = new Color(0.7019608f, 0.7137255f, 0.5019608f, 1f),
                particleEffect = new Color(0.3921569f, 0.4039216f, 0.1921569f, 1f)
            },
            // PLAYER 30
            new PlayerSkin()
            {
                color = new Color(0.2862745f, 0.1882353f, 0.4392157f, 1f),
                backgroundColor = new Color(0.2117647f, 0.1882353f, 0.2470588f, 1f),
                winText = new Color(0.5176471f, 0.3882353f, 0.7137255f, 1f),
                particleEffect = new Color(0.2352941f, 0.1254902f, 0.4039216f, 1f)
            },
            // PLAYER 31
            new PlayerSkin()
            {
                color = new Color(0.4392157f, 0.1882353f, 0.3058824f, 1f),
                backgroundColor = new Color(0.2470588f, 0.1882353f, 0.2431373f, 1f),
                winText = new Color(0.7137255f, 0.3882353f, 0.5294118f, 1f),
                particleEffect = new Color(0.4039216f, 0.1254902f, 0.227451f, 1f)
            },
            // PLAYER 32
            new PlayerSkin()
            {
                color = new Color(0.1882353f, 0.4392157f, 0.4196078f, 1f),
                backgroundColor = new Color(0.2078431f, 0.2784314f, 0.3098039f, 1f),
                winText = new Color(0.3882353f, 0.7137255f, 0.682353f, 1f),
                particleEffect = new Color(0.1294118f, 0.4039216f, 0.4156863f, 1f)
            }

        };
        public static PlayerSkin GetPlayerSkinColors(int team)
        {
            // if somehow the requested id is greater than the total number of extra skins, just loop it
            team = team % ExtraPlayerSkins.numberOfSkins;

            // if the skin gameobject hasn't been made yet, make it
            if (ExtraPlayerSkins.skins[team] == null)
            {

                PlayerSkin skin = ((PlayerSkinBank) typeof(PlayerSkinBank).GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null)).skins[team % 4].currentPlayerSkin;

                PlayerSkin newSkin = GameObject.Instantiate(skin).gameObject.GetComponent<PlayerSkin>();
                UnityEngine.GameObject.DontDestroyOnLoad(newSkin);
                PlayerSkin skinToSet = ExtraPlayerSkins.extraSkinBases[team];
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
