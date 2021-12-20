using System;
using System.Runtime.CompilerServices;
using InControl;

namespace RWF
{
    [Serializable]
    public class PlayerActionsAdditionalData
    {
        public PlayerAction increaseColorID;
        public PlayerAction decreaseColorID;


        public PlayerActionsAdditionalData()
        {
            this.increaseColorID = null;
            this.decreaseColorID = null;
        }
    }
    public static class PlayerActionsExtension
    {
        public static readonly ConditionalWeakTable<PlayerActions, PlayerActionsAdditionalData> data =
            new ConditionalWeakTable<PlayerActions, PlayerActionsAdditionalData>();

        public static PlayerActionsAdditionalData GetAdditionalData(this PlayerActions playerActions)
        {
            return data.GetOrCreateValue(playerActions);
        }

        public static void AddData(this PlayerActions playerActions, PlayerActionsAdditionalData value)
        {
            try
            {
                data.Add(playerActions, value);
            }
            catch (Exception) { }
        }
    }
}
