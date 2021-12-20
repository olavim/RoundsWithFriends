using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnboundLib;

namespace RWF
{
    [Serializable]
    public class PlayerAdditionalData
    {
        public int colorID;

        public PlayerAdditionalData()
        {
            this.colorID = -1;
        }
    }
    public static class PlayerExtensions
    {
        public static readonly ConditionalWeakTable<Player, PlayerAdditionalData> data =
            new ConditionalWeakTable<Player, PlayerAdditionalData>();

        public static PlayerAdditionalData GetAdditionalData(this Player player)
        {
            return data.GetOrCreateValue(player);
        }

        public static void AddData(this Player player, PlayerAdditionalData value)
        {
            try
            {
                data.Add(player, value);
            }
            catch (Exception) { }
        }

        public static int colorID(this Player instance) => instance.GetAdditionalData().colorID != -1 ? instance.GetAdditionalData().colorID : instance.teamID;

        static PlayerSkin[] vanillaSkins = new PlayerSkin[] 
        {
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

        public static void AssignColorID(this Player instance, int colorID)
        {
            instance.GetAdditionalData().colorID = colorID;

            // color the player's various objects
            SetTeamColor.TeamColorThis(instance.gameObject, PlayerSkinBank.GetPlayerSkinColors(instance.colorID()));

            // set the player's skin colors
            PlayerSkin playerSkin = instance.colorID() > 3 ? instance.GetTeamColors() : PlayerExtensions.vanillaSkins[instance.colorID()];
            Color color = playerSkin.color;
            Color backgroundColor = playerSkin.backgroundColor;

            // this is kinda messy, but for whatever reason the particlesystem colors for the vanilla skins (teams 0 to 3)
            // do not correspond to any color in PlayerSkin, so they're hardcoded here since this is the only place that these colors are set

            PlayerSkinHandler skinHandler = instance.GetComponentInChildren<PlayerSkinHandler>();
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
    }
}
