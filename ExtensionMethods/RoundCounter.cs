using UnboundLib;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

namespace RWF
{
    public class RoundCounterAdditionalData
    {
        public Dictionary<int, int> teamPoints;
        public Dictionary<int, int> teamRounds;
    }

    public static class RoundCounterExtensions
    {
        private static readonly ConditionalWeakTable<RoundCounter, RoundCounterAdditionalData> additionalData = new ConditionalWeakTable<RoundCounter, RoundCounterAdditionalData>();

        public static RoundCounterAdditionalData GetData(this RoundCounter instance) {
            return additionalData.GetOrCreateValue(instance);
        }

        public static void UpdatePoints(this RoundCounter instance, Dictionary<int, int> teamPoints) {
            instance.p1Points = teamPoints[0];
            instance.p2Points = teamPoints[1];
            instance.GetData().teamPoints = teamPoints.ToDictionary(e => e.Key, e => e.Value);
            instance.InvokeMethod("ReDraw");
        }

        public static void UpdateRounds(this RoundCounter instance, Dictionary<int, int> teamRounds) {
            instance.p1Rounds = teamRounds[0];
            instance.p2Rounds = teamRounds[1];
            instance.GetData().teamRounds = teamRounds.ToDictionary(e => e.Key, e => e.Value);
            instance.InvokeMethod("ReDraw");
        }
    }
}
