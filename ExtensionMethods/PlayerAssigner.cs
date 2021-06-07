using UnboundLib;

namespace RWF
{
    public static class PlayerAssignerExtensions
    {
        public static void SetPlayersCanJoin(this PlayerAssigner instance, bool value) {
            instance.SetFieldValue("playersCanJoin", value);
        }
    }
}
