using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using TMPro;
using UnboundLib;

namespace RWF
{
	public class UIHandlerAdditionalData
	{
		public bool disableTexts = false;
	}

	public static class UIHandlerExtensions
	{
		private static readonly ConditionalWeakTable<UIHandler, UIHandlerAdditionalData> additionalData = new ConditionalWeakTable<UIHandler, UIHandlerAdditionalData>();

		public static UIHandlerAdditionalData GetData(this UIHandler instance)
		{
			return additionalData.GetOrCreateValue(instance);
		}

		// Overload for the existing ShowRoundCounterSmall method to support more than two teams
		public static void ShowRoundCounterSmall(this UIHandler instance, Dictionary<int, int> teamPoints, Dictionary<int, int> teamRounds) {
			instance.roundCounterSmall.gameObject.SetActive(true);
			instance.roundCounterSmall.UpdateRounds(teamRounds);
			instance.roundCounterSmall.UpdatePoints(teamPoints);
			if (instance.roundCounterAnimSmall.currentState != CodeAnimationInstance.AnimationUse.In) {
				instance.roundCounterAnimSmall.PlayIn();
			}
		}

		public static void DisplayRoundStartText(this UIHandler instance, string text) {
			var uiGo = GameObject.Find("/Game/UI");
			var gameGo = uiGo.transform.Find("UI_Game").Find("Canvas").gameObject;
			var roundStartTextGo = gameGo.transform.Find("RoundStartText");

			var roundStartTextPart = roundStartTextGo.GetComponentInChildren<GeneralParticleSystem>();
			var roundStartText = roundStartTextGo.GetComponent<TextMeshProUGUI>();
			var roundStartPulse = roundStartTextGo.GetComponent<UI.ScalePulse>();

			roundStartTextPart.particleSettings.color = Color.white;
			roundStartTextPart.duration = 60f;
			roundStartTextPart.loop = true;
			roundStartTextPart.Play();
			roundStartText.text = text;
			instance.StopAllCoroutines();
			instance.StartCoroutine(roundStartPulse.StartPulse());
		}

		public static void HideRoundStartText(this UIHandler instance) {
			var uiGo = GameObject.Find("/Game/UI");
			var gameGo = uiGo.transform.Find("UI_Game").Find("Canvas").gameObject;
			var roundStartTextGo = gameGo.transform.Find("RoundStartText");

			var roundStartTextPart = roundStartTextGo.GetComponentInChildren<GeneralParticleSystem>();
			var roundStartPulse = roundStartTextGo.GetComponent<UI.ScalePulse>();

			roundStartTextPart.loop = false;
		}

		public static void DisableTexts(this UIHandler instance, float duration)
        {
			instance.GetData().disableTexts = true;
			instance.ExecuteAfterSeconds(duration, () => instance.GetData().disableTexts = false);
        }
	}
}
