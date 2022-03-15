using System.Runtime.CompilerServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.ProceduralImage;
using UnboundLib;
using Sonigon;
using UnityEngine.UI;
using RWF.Patches;
using UnboundLib.GameModes;
using UnboundLib.Extensions;
using UnboundLib.Utils;
using System.Linq;

namespace RWF
{
    public static class PointVisualizerExtensions
    {        
        public class PointVisualizerAdditionalData
		{
			public Dictionary<int, Vector3> teamBallVelocity;
			public Dictionary<int, GameObject> teamBall;
		}

        private static readonly ConditionalWeakTable<PointVisualizer, PointVisualizerAdditionalData> additionalData = new ConditionalWeakTable<PointVisualizer, PointVisualizerAdditionalData>();

        public static PointVisualizerAdditionalData GetData(this PointVisualizer instance) {
            return additionalData.GetOrCreateValue(instance);
        }

        // Overload for the existing ResetBalls method to support more than two teams
        public static void ResetBalls(this PointVisualizer instance, int numTeams) {
			while (instance.transform.GetChild(1).childCount > 2) {
				GameObject.DestroyImmediate(instance.transform.GetChild(1).GetChild(2).gameObject);
			}

			var data = instance.GetData();
			data.teamBallVelocity = new Dictionary<int, Vector3>();
			data.teamBall = new Dictionary<int, GameObject>();

			for (int i = 0; i < numTeams; i++) {
				data.teamBallVelocity.Add(i, Vector3.zero);
			}

			var orangeBall = instance.transform.GetChild(1).Find("Orange").gameObject;
			var blueBall = instance.transform.GetChild(1).Find("Blue").gameObject;

			data.teamBall.Add(0, orangeBall);
			data.teamBall.Add(1, blueBall);

			float ballBaseSize = (float)instance.GetFieldValue("ballBaseSize");
            orangeBall.GetComponent<RectTransform>().sizeDelta = Vector2.one * ballBaseSize;
            blueBall.GetComponent<RectTransform>().sizeDelta = Vector2.one * ballBaseSize;

			int xPos = -150 - (150 * (numTeams - 2));
			orangeBall.GetComponent<RectTransform>().anchoredPosition = new Vector3(xPos, 0, 0);
            blueBall.GetComponent<RectTransform>().anchoredPosition = new Vector3(xPos + 300, 0, 0);

            for (int i = 0; i < numTeams; i++) {
                GameObject ball = null;
                
                if (i <= 1)
                {
                    ball = i == 0 ? orangeBall : blueBall;
                }

                else if (i > 1)
                {
                    ball = GameObject.Instantiate(orangeBall, instance.transform.GetChild(1));
                    ball.transform.localScale = Vector3.one;
                    ball.GetComponent<RectTransform>().anchoredPosition = new Vector3(xPos + (300 * i), 0, 0);
                }

                ball.transform.Find("Fill").localRotation = Quaternion.Euler(new Vector3(0f, 0f, 180f));
                ball.transform.Find("Fill").GetComponent<ProceduralImage>().color = PlayerSkinBank.GetPlayerSkinColors(PlayerManager.instance.GetPlayersInTeam(i)[0].colorID()).color;
                ball.transform.Find("Border").GetComponent<ProceduralImage>().color = PlayerSkinBank.GetPlayerSkinColors(PlayerManager.instance.GetPlayersInTeam(i)[0].colorID()).color;
                //ball.transform.Find("Mid").GetComponent<ProceduralImage>().color = PlayerSkinBank.GetPlayerSkinColors(PlayerManager.instance.GetPlayersInTeam(i)[0].colorID()).color;
                ball.transform.Find("Mid").GetComponent<ProceduralImage>().enabled = false;

                if (i > 1)
                {
                    data.teamBall.Add(i, ball);
                }
            }

            instance.transform.GetChild(1).gameObject.GetOrAddComponent<GridLayoutGroup>().constraintCount = UnityEngine.Mathf.Clamp(numTeams, 1, 8);

        }

        // Overload for the existing DoWinSequence method to support more than two teams
        public static IEnumerator DoWinSequence(this PointVisualizer instance, Dictionary<int, int> teamPoints, Dictionary<int, int> teamRounds, int winnerTeamID) {
			yield return new WaitForSecondsRealtime(0.35f);
			SoundManager.Instance.Play(instance.soundWinRound, instance.transform.GetChild(1));

			int teamCount = teamPoints.Count;
			var pointPos = (Vector3) UIHandler.instance.roundCounterSmall.InvokeMethod("GetPointPos", winnerTeamID);

			instance.ResetBalls(teamCount);
			instance.bg.SetActive(true);

			instance.transform.GetChild(1).Find("Orange").gameObject.SetActive(true);
			instance.transform.GetChild(1).Find("Blue").gameObject.SetActive(true);

			for (int i = 0; i < teamCount; i++) {
				instance.transform.GetChild(1).GetChild(i).gameObject.SetActive(true);
			}

			yield return new WaitForSecondsRealtime(0.2f);

			GamefeelManager.instance.AddUIGameFeelOverTime(10f, 0.1f);

			instance.DoShowPoints(teamPoints, winnerTeamID);

			yield return new WaitForSecondsRealtime(0.35f);

			SoundManager.Instance.Play(instance.sound_UI_Arms_Race_A_Ball_Shrink_Go_To_Left_Corner, instance.transform);

			float c = 0f;
			float ballSmallSize = (float) instance.GetFieldValue("ballSmallSize");
			float bigBallScale = (float) instance.GetFieldValue("bigBallScale");

			while (c < instance.timeToScale) {
				var rt = instance.GetData().teamBall[winnerTeamID].GetComponent<RectTransform>();
				rt.sizeDelta = Vector2.LerpUnclamped(rt.sizeDelta, Vector2.one * ballSmallSize, instance.scaleCurve.Evaluate(c / instance.timeToScale));
				c += Time.unscaledDeltaTime;
				yield return null;
			}

			yield return new WaitForSecondsRealtime(instance.timeBetween);

			c = 0f;

			while (c < instance.timeToMove) {
				var trans = instance.GetData().teamBall[winnerTeamID].transform;
				trans.position = Vector3.LerpUnclamped(trans.position, pointPos, instance.scaleCurve.Evaluate(c / instance.timeToMove));
				c += Time.unscaledDeltaTime;
				yield return null;
			}

			SoundManager.Instance.Play(instance.sound_UI_Arms_Race_B_Ball_Go_Down_Then_Expand, instance.transform);

			instance.GetData().teamBall[winnerTeamID].transform.position = pointPos;

			yield return new WaitForSecondsRealtime(instance.timeBetween);
			c = 0f;

			while (c < instance.timeToMove) {
				for (int i = 0; i < teamCount; i++) {
					if (i != winnerTeamID) {
						var trans = instance.GetData().teamBall[i].transform;
						trans.position = Vector3.LerpUnclamped(trans.position, CardChoiceVisuals.instance.transform.position, instance.scaleCurve.Evaluate(c / instance.timeToMove));
					}
                }

				c += Time.unscaledDeltaTime;
				yield return null;
			}

			for (int i = 0; i < teamCount; i++) {
				if (i != winnerTeamID) {
					instance.GetData().teamBall[i].transform.position = CardChoiceVisuals.instance.transform.position;
				}
			}

			yield return new WaitForSecondsRealtime(instance.timeBetween);
			c = 0f;

			while (c < instance.timeToScale) {
				for (int i = 0; i < teamCount; i++) {
					if (i != winnerTeamID) {
						var rt = instance.GetData().teamBall[i].GetComponent<RectTransform>();
						rt.sizeDelta = Vector2.LerpUnclamped(rt.sizeDelta, Vector2.one * bigBallScale, instance.scaleCurve.Evaluate(c / instance.timeToScale));
					}
				}

				c += Time.unscaledDeltaTime;
				yield return null;
			}

			SoundManager.Instance.Play(instance.sound_UI_Arms_Race_C_Ball_Pop_Shake, instance.transform);
			GamefeelManager.instance.AddUIGameFeelOverTime(10f, 0.2f);

            // removing this fixes player skin + face desync issues
            /*
			for (int i = 0; i < teamCount; i++) {
				if (i != winnerTeamID) {
					CardChoiceVisuals.instance.Show(i, false);
					break;
				}
			}*/

			UIHandler.instance.roundCounterSmall.UpdateRounds(teamRounds);
			UIHandler.instance.roundCounterSmall.UpdatePoints(teamPoints);

			// Reset fill amounts to prevent visual artifacts when the point visualizer is shown again
			for (int i = 0; i < teamPoints.Count; i++) {
				var ball = instance.GetData().teamBall[i];
				var fill = ball.transform.Find("Fill").GetComponent<ProceduralImage>();
				fill.fillAmount = 0f;
			}

            instance.InvokeMethod("Close");
		}

		// Overload for the existing DoSequence method to support more than two teams
		public static IEnumerator DoSequence(this PointVisualizer instance, Dictionary<int, int> teamPoints, Dictionary<int, int> teamRounds, int winnerTeamID)
		{
			yield return new WaitForSecondsRealtime(0.45f);

			SoundManager.Instance.Play(instance.soundWinRound, instance.transform);
			instance.ResetBalls(teamPoints.Count);
			instance.bg.SetActive(true);

			instance.transform.GetChild(1).Find("Orange").gameObject.SetActive(true);
			instance.transform.GetChild(1).Find("Blue").gameObject.SetActive(true);

			for (int i = 0; i < teamPoints.Count; i++)
			{
				instance.transform.GetChild(1).GetChild(i).gameObject.SetActive(true);
			}

			yield return new WaitForSecondsRealtime(0.2f);

			GamefeelManager.instance.AddUIGameFeelOverTime(10f, 0.1f);
			instance.DoShowPoints(teamPoints, winnerTeamID);

			yield return new WaitForSecondsRealtime(1.8f);

			for (int i = 0; i < teamPoints.Count; i++)
			{
				instance.GetData().teamBall[i].GetComponent<CurveAnimation>().PlayOut();
			}

			yield return new WaitForSecondsRealtime(0.25f);

			instance.InvokeMethod("Close");
		}

		// Overload for the existing DoShowPoints method to support more than two teams
		public static void DoShowPoints(this PointVisualizer instance, Dictionary<int, int> teamPoints, int winnerTeamID) {
			for (int i = 0; i < teamPoints.Count; i++) {
				var ball = instance.GetData().teamBall[i];
				var fill = ball.transform.Find("Fill").GetComponent<ProceduralImage>();
                fill.fillMethod = Image.FillMethod.Radial360;

				if (i == winnerTeamID) {
					fill.fillAmount = teamPoints[i] == 0 ? 1f : (float)teamPoints[i] / (int)GameModeManager.CurrentHandler.Settings["pointsToWinRound"];
				} else {
					fill.fillAmount = (float)teamPoints[i]  / (int)GameModeManager.CurrentHandler.Settings["pointsToWinRound"];
				}
			}

			instance.text.color = PlayerSkinBank.GetPlayerSkinColors(PlayerManager.instance.GetPlayersInTeam(winnerTeamID)[0].colorID()).winText;
			instance.text.text = $"POINT TO {(GameModeManager.CurrentHandler.AllowTeams ? "TEAM " : "")}{ExtraPlayerSkins.GetTeamColorName(PlayerManager.instance.GetPlayersInTeam(winnerTeamID)[0].colorID()).ToUpper()}";
		}

        // Overload for the existing DoWinSequence method to support more than two winners, or no winners
        public static IEnumerator DoWinSequence(this PointVisualizer instance, Dictionary<int, int> teamPoints, Dictionary<int, int> teamRounds, int[] winnerTeamIDs)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            SoundManager.Instance.Play(instance.soundWinRound, instance.transform.GetChild(1));

            int teamCount = teamPoints.Count;

            instance.ResetBalls(teamCount);
            instance.bg.SetActive(true);

            instance.transform.GetChild(1).Find("Orange").gameObject.SetActive(true);
            instance.transform.GetChild(1).Find("Blue").gameObject.SetActive(true);

            for (int i = 0; i < teamCount; i++)
            {
                instance.transform.GetChild(1).GetChild(i).gameObject.SetActive(true);
            }

            yield return new WaitForSecondsRealtime(0.2f);

            GamefeelManager.instance.AddUIGameFeelOverTime(10f, 0.1f);

            instance.DoShowPoints(teamPoints, winnerTeamIDs);

            yield return new WaitForSecondsRealtime(0.35f);

            SoundManager.Instance.Play(instance.sound_UI_Arms_Race_A_Ball_Shrink_Go_To_Left_Corner, instance.transform);

            float c = 0f;
            float ballSmallSize = (float) instance.GetFieldValue("ballSmallSize");
            float bigBallScale = (float) instance.GetFieldValue("bigBallScale");

            while (c < instance.timeToScale)
            {
                foreach (int winnerTeamID in winnerTeamIDs)
                {
                    var rt = instance.GetData().teamBall[winnerTeamID].GetComponent<RectTransform>();
                    rt.sizeDelta = Vector2.LerpUnclamped(rt.sizeDelta, Vector2.one * ballSmallSize, instance.scaleCurve.Evaluate(c / instance.timeToScale));
                }
                c += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return new WaitForSecondsRealtime(instance.timeBetween);

            c = 0f;

            while (c < instance.timeToMove)
            {
                foreach (int winnerTeamID in winnerTeamIDs)
                {
                    var trans = instance.GetData().teamBall[winnerTeamID].transform;
                    trans.position = Vector3.LerpUnclamped(trans.position, (Vector3) UIHandler.instance.roundCounterSmall.InvokeMethod("GetPointPos", winnerTeamID), instance.scaleCurve.Evaluate(c / instance.timeToMove));
                }
                c += Time.unscaledDeltaTime;
                yield return null;
            }

            SoundManager.Instance.Play(instance.sound_UI_Arms_Race_B_Ball_Go_Down_Then_Expand, instance.transform);

            foreach (int winnerTeamID in winnerTeamIDs)
            {
                instance.GetData().teamBall[winnerTeamID].transform.position = (Vector3) UIHandler.instance.roundCounterSmall.InvokeMethod("GetPointPos", winnerTeamID);
            }

            yield return new WaitForSecondsRealtime(instance.timeBetween);
            c = 0f;

            while (c < instance.timeToMove)
            {
                for (int i = 0; i < teamCount; i++)
                {
                    if (!winnerTeamIDs.Contains(i))
                    {
                        var trans = instance.GetData().teamBall[i].transform;
                        trans.position = Vector3.LerpUnclamped(trans.position, CardChoiceVisuals.instance.transform.position, instance.scaleCurve.Evaluate(c / instance.timeToMove));
                    }
                }

                c += Time.unscaledDeltaTime;
                yield return null;
            }

            for (int i = 0; i < teamCount; i++)
            {
                if (!winnerTeamIDs.Contains(i))
                {
                    instance.GetData().teamBall[i].transform.position = CardChoiceVisuals.instance.transform.position;
                }
            }

            yield return new WaitForSecondsRealtime(instance.timeBetween);
            c = 0f;

            while (c < instance.timeToScale)
            {
                for (int i = 0; i < teamCount; i++)
                {
                    if (!winnerTeamIDs.Contains(i))
                    {
                        var rt = instance.GetData().teamBall[i].GetComponent<RectTransform>();
                        rt.sizeDelta = Vector2.LerpUnclamped(rt.sizeDelta, Vector2.one * bigBallScale, instance.scaleCurve.Evaluate(c / instance.timeToScale));
                    }
                }

                c += Time.unscaledDeltaTime;
                yield return null;
            }

            SoundManager.Instance.Play(instance.sound_UI_Arms_Race_C_Ball_Pop_Shake, instance.transform);
            GamefeelManager.instance.AddUIGameFeelOverTime(10f, 0.2f);

            // removing this fixes player skin + face desync issues
            /*
			for (int i = 0; i < teamCount; i++) {
				if (i != winnerTeamID) {
					CardChoiceVisuals.instance.Show(i, false);
					break;
				}
			}*/

            UIHandler.instance.roundCounterSmall.UpdateRounds(teamRounds);
            UIHandler.instance.roundCounterSmall.UpdatePoints(teamPoints);

            // Reset fill amounts to prevent visual artifacts when the point visualizer is shown again
            for (int i = 0; i < teamPoints.Count; i++)
            {
                var ball = instance.GetData().teamBall[i];
                var fill = ball.transform.Find("Fill").GetComponent<ProceduralImage>();
                fill.fillAmount = 0f;
            }

            instance.InvokeMethod("Close");
        }

        // Overload for the existing DoSequence method to support more than one winner, or no winners
        public static IEnumerator DoSequence(this PointVisualizer instance, Dictionary<int, int> teamPoints, Dictionary<int, int> teamRounds, int[] winnerTeamIDs)
        {
            yield return new WaitForSecondsRealtime(0.45f);

            SoundManager.Instance.Play(instance.soundWinRound, instance.transform);
            instance.ResetBalls(teamPoints.Count);
            instance.bg.SetActive(true);

            instance.transform.GetChild(1).Find("Orange").gameObject.SetActive(true);
            instance.transform.GetChild(1).Find("Blue").gameObject.SetActive(true);

            for (int i = 0; i < teamPoints.Count; i++)
            {
                instance.transform.GetChild(1).GetChild(i).gameObject.SetActive(true);
            }

            yield return new WaitForSecondsRealtime(0.2f);

            GamefeelManager.instance.AddUIGameFeelOverTime(10f, 0.1f);
            instance.DoShowPoints(teamPoints, winnerTeamIDs);

            yield return new WaitForSecondsRealtime(1.8f);

            for (int i = 0; i < teamPoints.Count; i++)
            {
                instance.GetData().teamBall[i].GetComponent<CurveAnimation>().PlayOut();
            }

            yield return new WaitForSecondsRealtime(0.25f);

            instance.InvokeMethod("Close");
        }

        // Overload for the existing DoShowPoints method to support more than one winner, or no winners
        public static void DoShowPoints(this PointVisualizer instance, Dictionary<int, int> teamPoints, int[] winnerTeamIDs)
        {
            for (int i = 0; i < teamPoints.Count; i++)
            {
                var ball = instance.GetData().teamBall[i];
                var fill = ball.transform.Find("Fill").GetComponent<ProceduralImage>();
                fill.fillMethod = Image.FillMethod.Radial360;

                if (winnerTeamIDs.Contains(i))
                {
                    fill.fillAmount = teamPoints[i] == 0 ? 1f : (float) teamPoints[i] / (int) GameModeManager.CurrentHandler.Settings["pointsToWinRound"];
                }
                else
                {
                    fill.fillAmount = (float) teamPoints[i] / (int) GameModeManager.CurrentHandler.Settings["pointsToWinRound"];
                }
            }

            Color color = new Color32(230, 230, 230, 255);
            string text = "TIE\nNO POINTS";
            if (winnerTeamIDs.Count() > 0)
            {
                text = $"POINT TO{(GameModeManager.CurrentHandler.AllowTeams ? $" TEAM{(winnerTeamIDs.Count() > 1 ? "S" : "")}" : "")}";
                for (int i = 0; i < winnerTeamIDs.Count(); i++)
                {
                    if (winnerTeamIDs.Count() > 1)
                    {
                        if (i == winnerTeamIDs.Count() - 1)
                        {
                            text += "\n<size=50%><voffset=0.5em>AND</voffset></size>";
                        }
                    }
                    int colorID = PlayerManager.instance.GetPlayersInTeam(winnerTeamIDs[i]).First().colorID();
                    text += $"\n<color=#{ColorUtility.ToHtmlStringRGBA(ExtraPlayerSkins.GetPlayerSkinColors(colorID).color)}>{ExtraPlayerSkins.GetTeamColorName(colorID).ToUpper()}</color>";

                }
            }

            float fontSize = winnerTeamIDs.Count() > 2 ? 100f / (winnerTeamIDs.Count() - 1) : 100f;

            instance.text.color = color;
            instance.text.text = text;
            instance.text.fontSize = fontSize;
        }
    }
}
