using UnityEngine;
using UnboundLib;
using UnityEngine.UI;
using TMPro;

namespace RWF.UI
{
    public static class KeybindHints
    {
        private static GameObject _KeybindHintsHolder = null;
        public static GameObject KeybindHintHolder
        {
            get
            {
                if (KeybindHints._KeybindHintsHolder != null) { return KeybindHints._KeybindHintsHolder; }

                KeybindHints._KeybindHintsHolder = new GameObject("Keybinds");
                KeybindHints._KeybindHintsHolder.transform.SetParent(MainMenuHandler.instance.transform.Find("Canvas/"));
                KeybindHints._KeybindHintsHolder.GetOrAddComponent<RectTransform>().pivot = Vector2.zero;
                var fitter = KeybindHints._KeybindHintsHolder.GetOrAddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                var layoutgroup = KeybindHints._KeybindHintsHolder.GetOrAddComponent<VerticalLayoutGroup>();
                layoutgroup.childAlignment = TextAnchor.MiddleLeft;
                layoutgroup.spacing = 10f;
                layoutgroup.childControlWidth = false;
                layoutgroup.childControlHeight = false;
                layoutgroup.childForceExpandWidth = false;
                layoutgroup.childForceExpandHeight = false;
                KeybindHints._KeybindHintsHolder.transform.localScale = Vector3.one;
                KeybindHints._KeybindHintsHolder.transform.position = MainCam.instance.transform.GetComponent<Camera>().ScreenToWorldPoint(new Vector3(0f, 0f, 0f));
                KeybindHints._KeybindHintsHolder.transform.position += new Vector3(0f, 0f, 100f);
                KeybindHints._KeybindHintsHolder.transform.localPosition += new Vector3(10f, 10f, 0f);

                return KeybindHints._KeybindHintsHolder;
            }
        }
        private static GameObject _KeybindPrefab = null;
        public static GameObject KeybindPrefab
        {
            get
            {
                if (KeybindHints._KeybindPrefab != null) { return KeybindHints._KeybindPrefab; }

                KeybindHints._KeybindPrefab = GameObject.Instantiate(RoundsResources.FlickeringTextPrefab);
                KeybindHints._KeybindPrefab.name = "Hint";
                UnityEngine.GameObject.DontDestroyOnLoad(KeybindHints._KeybindPrefab);
                KeybindHints._KeybindPrefab.transform.localScale = Vector3.one;

                KeybindHints._KeybindPrefab.GetOrAddComponent<RectTransform>();
                KeybindHints._KeybindPrefab.GetOrAddComponent<LayoutElement>();
                var sizer = KeybindHints._KeybindPrefab.GetOrAddComponent<ContentSizeFitter>();
                sizer.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                sizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var text = KeybindHints._KeybindPrefab.GetOrAddComponent<TextMeshProUGUI>();
                text.fontSize = 30;
                text.font = RoundsResources.MenuFont;
                text.alignment = TextAlignmentOptions.Left;
                text.overflowMode = TextOverflowModes.Overflow;
                text.enableWordWrapping = true;
                text.color = new Color32(85, 90, 98, 255);
                text.text = "";
                text.fontStyle = FontStyles.Normal;
                text.autoSizeTextContainer = false;

                var particleSystem = text.GetComponentInChildren<GeneralParticleSystem>();
                particleSystem.particleSettings.size = 5;

                KeybindHints._KeybindPrefab.SetActive(false);

                return KeybindHints._KeybindPrefab;
            }
        }
        public static void ClearHints()
        {
            for (int i = KeybindHints.KeybindHintHolder.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.GameObject.Destroy(KeybindHints.KeybindHintHolder.transform.GetChild(i).gameObject);
            }
        }
        public static GameObject AddHint(string action, string hint, Vector2? position = null)
        {
            GameObject hintGO = GameObject.Instantiate(KeybindHints.KeybindPrefab, KeybindHints.KeybindHintHolder.transform);
            TextMeshProUGUI hintText = hintGO.GetOrAddComponent<TextMeshProUGUI>();
            hintGO.GetOrAddComponent<DestroyIfSet>();
            hintGO.SetActive(true);
            hintGO.transform.localScale = Vector3.one;
            if (position != null) 
            { 
                hintGO.transform.position = (Vector2) position;
                hintGO.GetOrAddComponent<LayoutElement>().ignoreLayout = true;
            }
            hintText.text = $"{hint} TO {action}".ToUpper();

            return hintGO;
        }
        public static GameObject AddHint(string action, string[] hints, Vector2? position = null)
        {
            GameObject hintGO = GameObject.Instantiate(KeybindHints.KeybindPrefab, KeybindHints.KeybindHintHolder.transform);
            TextMeshProUGUI hintText = hintGO.GetOrAddComponent<TextMeshProUGUI>();
            hintGO.GetOrAddComponent<DestroyIfSet>();
            hintGO.GetOrAddComponent<CycleHints>().action = action;
            hintGO.GetOrAddComponent<CycleHints>().hints = hints;
            hintGO.SetActive(true);
            hintGO.transform.localScale = Vector3.one;
            if (position != null)
            {
                hintGO.transform.position = (Vector2) position;
                hintGO.GetOrAddComponent<LayoutElement>().ignoreLayout = true;
            }
            hintText.text = $"{hints[0]} TO {action}".ToUpper();

            return hintGO;
        }
        public static void CreateLocalHints()
        {
            KeybindHints.ClearHints();
            if (PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey("ShowKeybinds"), 1) == 0) { return; }
            KeybindHints.AddHint("Join/Ready", "jump");
            KeybindHints.AddHint("Select face", "Left/right");
            KeybindHints.AddHint("select team", new string[] { "[W/S]","[Dpad left/right]" });
        }
        public static void CreateOnlineHints()
        {
            KeybindHints.ClearHints();
            if (PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey("ShowKeybinds"), 1) == 0) { return; }
            KeybindHints.AddHint("Interact", "Select Player Bar");
            KeybindHints.AddHint("Exit Player Menu", new string[] { "[Q/Esc]", "[B/Start]" });
            KeybindHints.AddHint("Join/Ready", "jump");
            KeybindHints.AddHint("Select face", "Left/right");
            KeybindHints.AddHint("select team", new string[] { "[W/S]", "[Dpad left/right]" });
        }

        class DestroyIfSet : MonoBehaviour
        {
            void Start()
            {
                if (PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey("ShowKeybinds"), 1) == 0 && this.gameObject != null) { UnityEngine.GameObject.Destroy(this.gameObject); }
            }
            void Update()
            {
                if (PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey("ShowKeybinds"), 1) == 0 && this.gameObject != null) { UnityEngine.GameObject.Destroy(this.gameObject); }
            }
        }
        class CycleHints : MonoBehaviour
        {
            internal string[] hints;
            internal string action;
            private const float timeToWait = 2f;
            private float time = timeToWait;
            private int i = 0;
            void Start()
            {
                if (this.hints == null || this.action == null || this.hints.Length == 1)
                {
                    UnityEngine.GameObject.Destroy(this);
                }
            }
            void Update()
            {
                this.time -= Time.deltaTime;
                if (this.time < 0f)
                {
                    this.time = CycleHints.timeToWait;
                    this.i = Math.mod(this.i + 1, this.hints.Length);
                    this.gameObject.GetOrAddComponent<TextMeshProUGUI>().text = $"{this.hints[this.i]} TO {this.action}".ToUpper();
                    if (this.gameObject?.transform?.parent?.GetComponent<LayoutGroup>() != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(this.gameObject.GetComponent<RectTransform>());
                    }
                }
            }
        }
    }
}
