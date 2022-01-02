using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnboundLib;
using Photon.Pun;
using ExitGames.Client.Photon;
using UnboundLib.Extensions;

namespace RWF
{
    [Serializable]
    public class PlayerAdditionalData
    {
        public int localID;
        private int uniqueID; // this value is not meant to be read or written directly. only through PlayerExtensions.Get/SetUniqueID
        public LobbyCharacter character;

        public PlayerAdditionalData()
        {
            this.localID = 0;
            this.uniqueID = 1;
            this.character = null;
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

        public static void AssignCharacter(this Player instance, LobbyCharacter character, int playerID)
        {
            instance.GetAdditionalData().character = character;

            PhotonNetwork.LocalPlayer.SetCharacter(character);

            instance.AssignLocalID(character.localID);
            instance.AssignUniqueID(character.uniqueID);
            instance.AssignColorID(character.colorID);
            instance.AssignTeamID(character.teamID);
            instance.AssignPlayerID(playerID);

            PlayerManager.instance.PlayerJoined(instance);
        }

        public static void AssignUniqueID(this Player instance, int uniqueID)
        {
            instance.GetAdditionalData().SetFieldValue("uniqueID", uniqueID);
        }
        public static int GetUniqueID(this Player instance)
        {
            // return uniqueID if it has been assigned (i.e. is negative), otherwise just return the actorID

            int uniqueID = (int)instance.GetAdditionalData().GetFieldValue("uniqueID");

            return uniqueID < 0 ? uniqueID : instance.data.view.ControllerActorNr;
        }

        public static void AssignLocalID(this Player instance, int localID)
        {
            instance.GetAdditionalData().localID = localID;
        }
    }
}
