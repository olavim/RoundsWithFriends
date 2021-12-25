using System;
using UnityEngine;
using TMPro;
using UnboundLib;

namespace RWF.UI
{
    public static class BetaTextHandler
    {

        private static GameObject _BetaText = null;

        public static GameObject BetaText
        {
            get
            {
                if (_BetaText != null) { return _BetaText; }

                _BetaText = new GameObject("RWF Beta");
                _BetaText.transform.SetParent(MainMenuHandler.instance.transform.Find("Canvas/"));
                //UnityEngine.GameObject.DontDestroyOnLoad(_BetaText);
                // do setup like placement and adding components
                _BetaText.transform.position = MainCam.instance.transform.GetComponent<Camera>().ScreenToWorldPoint(new Vector3(0f, 0f, 0f));
                _BetaText.transform.position += new Vector3(0f, 0f, 100f);
                _BetaText.transform.localPosition += new Vector3(10f, 0f, 0f);
                _BetaText.transform.localScale = Vector3.one;
                TextMeshProUGUI text = _BetaText.AddComponent<TextMeshProUGUI>();

                text.text = $"RWF V{RWFMod.Version} (BETA)";
                text.color = new Color32(230, 230, 230, 64);
                text.font = RoundsResources.MenuFont;
                text.fontSize = 30;
                text.fontWeight = FontWeight.Regular;
                text.alignment = TextAlignmentOptions.Left;
                text.gameObject.GetOrAddComponent<RectTransform>().pivot = Vector2.zero;
                text.gameObject.GetOrAddComponent<RectTransform>().sizeDelta = new Vector2(500, 50);

                return _BetaText;
            }
        }
        public static GameObject ROUNDSModding => BetaText.transform.GetChild(0).gameObject;
        public static GameObject ROUNDSThunderstore => BetaText.transform.GetChild(1).gameObject;

        public static void AddBetaText(bool firstTime)
        {
            RWFMod.instance.ExecuteAfterSeconds(firstTime ? 0.2f : 0f, () =>
            {
                BetaTextHandler.BetaText.SetActive(true);
            });
        }
        public static void HideBetaText()
        {
            BetaTextHandler.BetaText.SetActive(false);
        }
    }
}
