using UnboundLib;
using System;

namespace RWF
{
    public static class PlayerManagerExtensions
    {
        public static void AddPlayerJoinedAction(this PlayerManager instance, Action<Player> action) {
            instance.SetPropertyValue("PlayerJoinedAction", Delegate.Combine(instance.PlayerJoinedAction, action));
        }
    }
}
