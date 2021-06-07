using System.Collections.Generic;

namespace RWF
{
    public static class UIHandlerExtensions
    {
		// Overload for the existing ShowRoundCounterSmall method to support more than two teams
		public static void ShowRoundCounterSmall(this UIHandler instance, Dictionary<int, int> teamPoints, Dictionary<int, int> teamRounds) {
			instance.roundCounterSmall.gameObject.SetActive(true);
			instance.roundCounterSmall.UpdateRounds(teamRounds);
			instance.roundCounterSmall.UpdatePoints(teamPoints);
			if (instance.roundCounterAnimSmall.currentState != CodeAnimationInstance.AnimationUse.In) {
				instance.roundCounterAnimSmall.PlayIn();
			}
		}
    }
}
