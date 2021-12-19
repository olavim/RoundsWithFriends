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
    public static class ExtraPlayerSkins
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
            // TEAM 1
            new PlayerSkin()
            {
                color = new Color(0.6392157f, 0.2862745f, 0.1686275f, 1f),
                backgroundColor = new Color(0.3490196f, 0.2392157f, 0.2117647f, 1f),
                winText = new Color(0.9137255f, 0.4980392f, 0.3568628f, 1f),
                particleEffect = new Color(0.6f, 0.2588235f, 0.09803922f, 1f)
            },
            // TEAM 2
            new PlayerSkin()
            {
                color = new Color(0.1647059f, 0.3098039f, 0.5843138f, 1f),
                backgroundColor = new Color(0.2196078f, 0.254902f, 0.3098039f, 1f),
                winText = new Color(0.3568628f, 0.6f, 0.9137255f, 1f),
                particleEffect = new Color(0.09803922f, 0.3215686f, 0.6039216f, 1f)
            },
            // TEAM 3
            new PlayerSkin()
            {
                color = new Color(0.6313726f, 0.2705882f, 0.2705882f, 1f),
                backgroundColor = new Color(0.3490196f, 0.2117647f, 0.2117647f, 1f),
                winText = new Color(0.9137255f, 0.3568628f, 0.3568628f, 1f),
                particleEffect = new Color(0.6039216f, 0.09803922f, 0.09803922f, 1f)
            },
            // TEAM 4
            new PlayerSkin()
            {
                color = new Color(0.2627451f, 0.5372549f, 0.3254902f, 1f),
                backgroundColor = new Color(0.2196078f, 0.3098039f, 0.2784314f, 1f),
                winText = new Color(0f, 0.6862745f, 0.2666667f, 1f),
                particleEffect = new Color(0.07843138f, 0.5529412f, 0.2784314f, 1f)
            },
            // TEAM 5
            new PlayerSkin()
            {
                color = new Color(0.6235294f, 0.6392157f, 0.172549f, 1f),
                backgroundColor = new Color(0.345098f, 0.3490196f, 0.2117647f, 1f),
                winText = new Color(0.8941177f, 0.9137255f, 0.3568628f, 1f),
                particleEffect = new Color(0.5882353f, 0.6039216f, 0.09803922f, 1f)
            },
            // TEAM 6
            new PlayerSkin()
            {
                color = new Color(0.3607843f, 0.172549f, 0.6392157f, 1f),
                backgroundColor = new Color(0.2666667f, 0.2117647f, 0.3490196f, 1f),
                winText = new Color(0.5803922f, 0.3568628f, 0.9137255f, 1f),
                particleEffect = new Color(0.3019608f, 0.09803922f, 0.6039216f, 1f)
            },
            // TEAM 7
            new PlayerSkin()
            {
                color = new Color(0.6392157f, 0.172549f, 0.3960784f, 1f),
                backgroundColor = new Color(0.3490196f, 0.2117647f, 0.345098f, 1f),
                winText = new Color(0.9137255f, 0.3568628f, 0.5960785f, 1f),
                particleEffect = new Color(0.6039216f, 0.09803922f, 0.282353f, 1f)
            },
            // TEAM 8
            new PlayerSkin()
            {
                color = new Color(0.172549f, 0.6392157f, 0.6117647f, 1f),
                backgroundColor = new Color(0.2117647f, 0.3490196f, 0.4117647f, 1f),
                winText = new Color(0.3568628f, 0.9137255f, 0.8705882f, 1f),
                particleEffect = new Color(0.09803922f, 0.6039216f, 0.6156863f, 1f)
            },
            // TEAM 9
            new PlayerSkin()
            {
                color = new Color(0.6392157f, 0.3607843f, 0.2705882f, 1f),
                backgroundColor = new Color(0.3490196f, 0.282353f, 0.2666667f, 1f),
                winText = new Color(0.9137255f, 0.6039216f, 0.5019608f, 1f),
                particleEffect = new Color(0.6f, 0.3215686f, 0.1921569f, 1f)
            },
            // TEAM 10
            new PlayerSkin()
            {
                color = new Color(0.254902f, 0.3686275f, 0.5843138f, 1f),
                backgroundColor = new Color(0.2666667f, 0.282353f, 0.3098039f, 1f),
                winText = new Color(0.5019608f, 0.682353f, 0.9137255f, 1f),
                particleEffect = new Color(0.1921569f, 0.372549f, 0.6039216f, 1f)
            },
            // TEAM 11
            new PlayerSkin()
            {
                color = new Color(0.6313726f, 0.3686275f, 0.3686275f, 1f),
                backgroundColor = new Color(0.3490196f, 0.2666667f, 0.2666667f, 1f),
                winText = new Color(0.9137255f, 0.5019608f, 0.5019608f, 1f),
                particleEffect = new Color(0.6039216f, 0.1921569f, 0.1921569f, 1f)
            },
            // TEAM 12
            new PlayerSkin()
            {
                color = new Color(0.345098f, 0.5372549f, 0.3882353f, 1f),
                backgroundColor = new Color(0.2666667f, 0.3098039f, 0.2941177f, 1f),
                winText = new Color(0.1058824f, 0.6862745f, 0.3333333f, 1f),
                particleEffect = new Color(0.1647059f, 0.5529412f, 0.3294118f, 1f)
            },
            // TEAM 13
            new PlayerSkin()
            {
                color = new Color(0.627451f, 0.6392157f, 0.3764706f, 1f),
                backgroundColor = new Color(0.345098f, 0.3490196f, 0.3215686f, 1f),
                winText = new Color(0.9019608f, 0.9137255f, 0.6470588f, 1f),
                particleEffect = new Color(0.5921569f, 0.6039216f, 0.2901961f, 1f)
            },
            // TEAM 14
            new PlayerSkin()
            {
                color = new Color(0.4196078f, 0.2745098f, 0.6392157f, 1f),
                backgroundColor = new Color(0.2980392f, 0.2666667f, 0.3490196f, 1f),
                winText = new Color(0.6666667f, 0.5019608f, 0.9137255f, 1f),
                particleEffect = new Color(0.3568628f, 0.1921569f, 0.6039216f, 1f)
            },
            // TEAM 15
            new PlayerSkin()
            {
                color = new Color(0.6392157f, 0.2745098f, 0.4470588f, 1f),
                backgroundColor = new Color(0.3490196f, 0.2666667f, 0.345098f, 1f),
                winText = new Color(0.9137255f, 0.5019608f, 0.6784314f, 1f),
                particleEffect = new Color(0.6039216f, 0.1921569f, 0.3411765f, 1f)
            },
            // TEAM 16
            new PlayerSkin()
            {
                color = new Color(0.2745098f, 0.6392157f, 0.6156863f, 1f),
                backgroundColor = new Color(0.2745098f, 0.3686275f, 0.4117647f, 1f),
                winText = new Color(0.5019608f, 0.9137255f, 0.8784314f, 1f),
                particleEffect = new Color(0.1960784f, 0.6039216f, 0.6156863f, 1f)
            },
            // TEAM 17
            new PlayerSkin()
            {
                color = new Color(0.4392157f, 0.1960784f, 0.1137255f, 1f),
                backgroundColor = new Color(0.2470588f, 0.1686275f, 0.1490196f, 1f),
                winText = new Color(0.7137255f, 0.3882353f, 0.2784314f, 1f),
                particleEffect = new Color(0.4f, 0.172549f, 0.0627451f, 1f)
            },
            // TEAM 18
            new PlayerSkin()
            {
                color = new Color(0.1058824f, 0.2f, 0.3843137f, 1f),
                backgroundColor = new Color(0.145098f, 0.172549f, 0.2078431f, 1f),
                winText = new Color(0.2784314f, 0.4666667f, 0.7137255f, 1f),
                particleEffect = new Color(0.0627451f, 0.2117647f, 0.4039216f, 1f)
            },
            // TEAM 19
            new PlayerSkin()
            {
                color = new Color(0.4313726f, 0.1843137f, 0.1843137f, 1f),
                backgroundColor = new Color(0.2470588f, 0.1490196f, 0.1490196f, 1f),
                winText = new Color(0.7137255f, 0.2784314f, 0.2784314f, 1f),
                particleEffect = new Color(0.4039216f, 0.0627451f, 0.0627451f, 1f)
            },
            // TEAM 20
            new PlayerSkin()
            {
                color = new Color(0.1647059f, 0.3372549f, 0.2039216f, 1f),
                backgroundColor = new Color(0.145098f, 0.2078431f, 0.1882353f, 1f),
                winText = new Color(0f, 0.4862745f, 0.1882353f, 1f),
                particleEffect = new Color(0.04705882f, 0.3529412f, 0.1764706f, 1f)
            },
            // TEAM 21
            new PlayerSkin()
            {
                color = new Color(0.427451f, 0.4392157f, 0.1176471f, 1f),
                backgroundColor = new Color(0.2431373f, 0.2470588f, 0.1490196f, 1f),
                winText = new Color(0.6980392f, 0.7137255f, 0.2784314f, 1f),
                particleEffect = new Color(0.3921569f, 0.4039216f, 0.0627451f, 1f)
            },
            // TEAM 22
            new PlayerSkin()
            {
                color = new Color(0.2470588f, 0.1176471f, 0.4392157f, 1f),
                backgroundColor = new Color(0.1882353f, 0.1490196f, 0.2470588f, 1f),
                winText = new Color(0.4509804f, 0.2784314f, 0.7137255f, 1f),
                particleEffect = new Color(0.2f, 0.0627451f, 0.4039216f, 1f)
            },
            // TEAM 23
            new PlayerSkin()
            {
                color = new Color(0.4392157f, 0.1176471f, 0.2705882f, 1f),
                backgroundColor = new Color(0.2470588f, 0.1490196f, 0.2431373f, 1f),
                winText = new Color(0.7137255f, 0.2784314f, 0.4627451f, 1f),
                particleEffect = new Color(0.4039216f, 0.0627451f, 0.1882353f, 1f)
            },
            // TEAM 24
            new PlayerSkin()
            {
                color = new Color(0.1176471f, 0.4392157f, 0.4196078f, 1f),
                backgroundColor = new Color(0.1568628f, 0.2627451f, 0.3098039f, 1f),
                winText = new Color(0.2784314f, 0.7137255f, 0.6784314f, 1f),
                particleEffect = new Color(0.0627451f, 0.4039216f, 0.4156863f, 1f)
            },
            // TEAM 25
            new PlayerSkin()
            {
                color = new Color(0.4392157f, 0.2470588f, 0.1843137f, 1f),
                backgroundColor = new Color(0.2470588f, 0.2f, 0.1882353f, 1f),
                winText = new Color(0.7137255f, 0.4705882f, 0.3882353f, 1f),
                particleEffect = new Color(0.4f, 0.2117647f, 0.1254902f, 1f)
            },
            // TEAM 26
            new PlayerSkin()
            {
                color = new Color(0.1647059f, 0.2392157f, 0.3843137f, 1f),
                backgroundColor = new Color(0.1803922f, 0.1882353f, 0.2078431f, 1f),
                winText = new Color(0.3882353f, 0.5294118f, 0.7137255f, 1f),
                particleEffect = new Color(0.1254902f, 0.2470588f, 0.4039216f, 1f)
            },
            // TEAM 27
            new PlayerSkin()
            {
                color = new Color(0.4313726f, 0.2509804f, 0.2509804f, 1f),
                backgroundColor = new Color(0.2470588f, 0.1882353f, 0.1882353f, 1f),
                winText = new Color(0.7137255f, 0.3882353f, 0.3882353f, 1f),
                particleEffect = new Color(0.4039216f, 0.1254902f, 0.1254902f, 1f)
            },
            // TEAM 28
            new PlayerSkin()
            {
                color = new Color(0.2156863f, 0.3372549f, 0.2431373f, 1f),
                backgroundColor = new Color(0.1803922f, 0.2078431f, 0.1960784f, 1f),
                winText = new Color(0.07450981f, 0.4862745f, 0.2352941f, 1f),
                particleEffect = new Color(0.1019608f, 0.3529412f, 0.2078431f, 1f)
            },
            // TEAM 29
            new PlayerSkin()
            {
                color = new Color(0.427451f, 0.4392157f, 0.254902f, 1f),
                backgroundColor = new Color(0.2431373f, 0.2470588f, 0.227451f, 1f),
                winText = new Color(0.7019608f, 0.7137255f, 0.5019608f, 1f),
                particleEffect = new Color(0.3921569f, 0.4039216f, 0.1921569f, 1f)
            },
            // TEAM 30
            new PlayerSkin()
            {
                color = new Color(0.2862745f, 0.1882353f, 0.4392157f, 1f),
                backgroundColor = new Color(0.2117647f, 0.1882353f, 0.2470588f, 1f),
                winText = new Color(0.5176471f, 0.3882353f, 0.7137255f, 1f),
                particleEffect = new Color(0.2352941f, 0.1254902f, 0.4039216f, 1f)
            },
            // TEAM 31
            new PlayerSkin()
            {
                color = new Color(0.4392157f, 0.1882353f, 0.3058824f, 1f),
                backgroundColor = new Color(0.2470588f, 0.1882353f, 0.2431373f, 1f),
                winText = new Color(0.7137255f, 0.3882353f, 0.5294118f, 1f),
                particleEffect = new Color(0.4039216f, 0.1254902f, 0.227451f, 1f)
            },
            // TEAM 32
            new PlayerSkin()
            {
                color = new Color(0.1882353f, 0.4392157f, 0.4196078f, 1f),
                backgroundColor = new Color(0.2078431f, 0.2784314f, 0.3098039f, 1f),
                winText = new Color(0.3882353f, 0.7137255f, 0.682353f, 1f),
                particleEffect = new Color(0.1294118f, 0.4039216f, 0.4156863f, 1f)
            }

        };
        public static string GetTeamColorName(int teamID)
        {
            // team names as colors

            switch (teamID)
            {
                case 0:
                    return "Orange";
                case 1:
                    return "Blue";
                case 2:
                    return "Red";
                case 3:
                    return "Green";
                case 4:
                    return "Yellow";
                case 5:
                    return "Purple";
                case 6:
                    return "Magenta";
                case 7:
                    return "Cyan";
                case 8:
                    return "Tangerine";
                case 9:
                    return "Light Blue";
                case 10:
                    return "Peach";
                case 11:
                    return "Lime";
                case 12:
                    return "Light Yellow";
                case 13:
                    return "Orchid";
                case 14:
                    return "Pink";
                case 15:
                    return "Aquamarine";
                case 16:
                    return "Dark Orange";
                case 17:
                    return "Dark Blue";
                case 18:
                    return "Dark Red";
                case 19:
                    return "Dark Green";
                case 20:
                    return "Dark Yellow";
                case 21:
                    return "Indigo";
                case 22:
                    return "Telemagenta";
                case 23:
                    return "Teal";
                case 24:
                    return "Burnt Orange";
                case 25:
                    return "Midnight Blue";
                case 26:
                    return "Maroon";
                case 27:
                    return "Evergreen";
                case 28:
                    return "Gold";
                case 29:
                    return "Violet";
                case 30:
                    return "Ruby";
                case 31:
                    return "Dark Cyan";
                default:
                    return (teamID + 1).ToString();
            }
        }
        public static PlayerSkin GetPlayerSkinColors(int colorID)
        {
            // if somehow the requested id is greater than the total number of extra skins, just loop it
            colorID = colorID % ExtraPlayerSkins.numberOfSkins;

            // if the skin gameobject hasn't been made yet, make it
            if (ExtraPlayerSkins.skins[colorID] == null)
            {

                PlayerSkin skin = ((PlayerSkinBank) typeof(PlayerSkinBank).GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null)).skins[colorID % 4].currentPlayerSkin;

                PlayerSkin newSkin = GameObject.Instantiate(skin).gameObject.GetComponent<PlayerSkin>();
                UnityEngine.GameObject.DontDestroyOnLoad(newSkin);
                PlayerSkin skinToSet = ExtraPlayerSkins.extraSkinBases[colorID];
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

                ExtraPlayerSkins.skins[colorID] = newSkin;
            }

            return ExtraPlayerSkins.skins[colorID];
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
