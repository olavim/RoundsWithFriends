using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnboundLib;
using InControl;
using System.Reflection;
using System.Linq;

namespace RWF.UI
{
    class PlayerDisplay : MonoBehaviour
    {
        public static PlayerDisplay instance;

        private const float padding = 100f;

        private static readonly Color disabledTextColor = new Color32(150, 150, 150, 16);
        private static readonly Color enabledTextColor = new Color32(230, 230, 230, 255);

        private static Color defaultColorMax = new Color(0.4434f, 0.2781f, 0.069f, 1f);
        private static Color defaultColorMin = new Color(0.5094f, 0.3371f, 0.0889f, 1f);

        private static Color highlightedColorMax = new Color(0.3204f, 0.3751f, 0.409f, 0.3396f);
        private static Color highlightedColorMin = new Color(0f, 0f, 0f, 0.3396f);

        private static Color selectedColorMax = new Color(0f, 0f, 0.0898f, 0.7925f);
        private static Color selectedColorMin = new Color(0f, 0.0921f, 0.0898f, 0.7925f);

        private static ParticleSystem Particles => UnityEngine.GameObject.Find("Game/UI/UI_MainMenu/Canvas/Particle").GetComponent<ParticleSystem>();

        HorizontalLayoutGroup group;
        PrivateRoomListMenuButton menuButton;
        Button button;
        TextMeshProUGUI text;
        SetBar setBar;
        VersusDisplay versusDisplay;
        LayoutElement layout;
        PrivateRoomHandler PrivateRoom => PrivateRoomHandler.instance;

        bool isHighlighted = false;
        bool selected = false;
        bool playersAdded = false;

        public bool IsSelected => this.selected;
        public bool PlayersHaveBeenAdded => this.playersAdded;

        public void SetHighlighted(bool isHighlighted)
        {
            this.isHighlighted = isHighlighted;
        }

        private void Awake()
        {
            PlayerDisplay.instance = this;
        }
        void Start()
        {
            // add the necessary components
            this.group = this.gameObject.GetOrAddComponent<HorizontalLayoutGroup>();
            this.menuButton = this.gameObject.GetOrAddComponent<PrivateRoomListMenuButton>();
            var textGO = new GameObject("Join");

            textGO.AddComponent<CanvasRenderer>();
            textGO.transform.SetParent(this.transform);
            textGO.transform.SetAsFirstSibling();
            textGO.transform.localScale = Vector3.one;
            this.text = textGO.GetOrAddComponent<TextMeshProUGUI>();
            this.text.color = PlayerDisplay.enabledTextColor;
            this.text.font = RoundsResources.MenuFont;
            this.text.fontSize = 60;
            this.text.fontWeight = FontWeight.Regular;
            this.text.alignment = TextAlignmentOptions.Center;
            this.text.rectTransform.sizeDelta = new Vector2(2050, 92);

            this.setBar = this.gameObject.GetOrAddComponent<SetBar>();
            this.layout = this.gameObject.GetOrAddComponent<LayoutElement>();
            this.versusDisplay = this.gameObject.GetOrAddComponent<VersusDisplay>();
            this.button = this.gameObject.GetOrAddComponent<Button>();

            // set up the highlighting
            this.menuButton.highlightedColorMax = PlayerDisplay.highlightedColorMax;
            this.menuButton.highlightedColorMin = PlayerDisplay.highlightedColorMin;

            // set up the layout
            this.layout.ignoreLayout = false;
            this.layout.minHeight = this.text.rectTransform.sizeDelta.y;

            // set up the horizontal group
            this.group.childAlignment = TextAnchor.MiddleCenter;
            this.group.spacing = 100;

            // set up the menu bar
            this.setBar.heightMult = 1.25f;
            this.setBar.verticalOffset = 0f;
            this.setBar.SetEnabled(false);

            // add text
            this.text.text = "JOIN";

            // add the listener to the button
            this.button.onClick.AddListener(() => this.Select());

            // save the default bar color
            ParticleSystem.MainModule main = PlayerDisplay.Particles.main;
            ParticleSystem.MinMaxGradient startColor = main.startColor;
            PlayerDisplay.defaultColorMax = startColor.colorMax;
            PlayerDisplay.defaultColorMin = startColor.colorMin;
        }

        void Select()
        {
            if (this.selected)
            {
                return;
            }

            this.ExecuteAfterFrames(2, () => this.selected = true);
            this.setBar.Select();
            this.versusDisplay.SetInputEnabled(true);

            // set the bar color
            ParticleSystem.MainModule main = PlayerDisplay.Particles.main;
            ParticleSystem.MinMaxGradient startColor = main.startColor;
            startColor.colorMax = PlayerDisplay.selectedColorMax;
            startColor.colorMin = PlayerDisplay.selectedColorMin;
            main.startColor = startColor;
            PlayerDisplay.Particles.Play();

            // once this has been selected once, hide the text and change the bar height and offset
            this.text.gameObject.SetActive(false);
            this.setBar.SetEnabled(true);
            this.playersAdded = true;

            // disable all other menu items in the same list
            foreach (ListMenuButton listMenuButton in this.transform.parent.gameObject.GetComponentsInChildren<ListMenuButton>(true).Where(lm => lm != this.menuButton && lm.GetComponent<CharacterCreatorPortrait>() == null))
            {
                listMenuButton.enabled = false;
                listMenuButton.GetComponent<Button>().enabled = false;
                foreach(TextMeshProUGUI text in listMenuButton.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    text.color = PlayerDisplay.disabledTextColor;
                }
            }
        }
        void Deselect()
        {
            this.selected = false;
            this.setBar.Deselect();
            this.versusDisplay.SetInputEnabled(false);

            // restore the bar color
            ParticleSystem.MainModule main = PlayerDisplay.Particles.main;
            ParticleSystem.MinMaxGradient startColor = main.startColor;
            startColor.colorMax = PlayerDisplay.highlightedColorMax;
            startColor.colorMin = PlayerDisplay.highlightedColorMin;
            main.startColor = startColor;
            PlayerDisplay.Particles.Play();

            // reenable menu inputs
            foreach (ListMenuButton listMenuButton in this.transform.parent.gameObject.GetComponentsInChildren<ListMenuButton>(true).Where(lm => lm != this.menuButton && lm.GetComponent<CharacterCreatorPortrait>() == null))
            {
                listMenuButton.enabled = true;
                listMenuButton.GetComponent<Button>().enabled = true;
                foreach (TextMeshProUGUI text in listMenuButton.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    text.color = PlayerDisplay.enabledTextColor;
                }
            }
        }

        void Update()
        {
            if (!this.playersAdded) 
            {
                if (VersusDisplay.instance.PlayersHaveBeenAdded)
                {
                    this.playersAdded = true;
                    // once this has been selected once, hide the text and change the bar height and offset
                    this.text.gameObject.SetActive(false);
                    this.setBar.SetEnabled(true);
                }
                else
                {
                    return;
                }
            }
            try
            {
                this.layout.minHeight = 1.25f * this.gameObject.GetComponentsInChildren<RectTransform>(false).Where(r => r != this.GetComponent<RectTransform>()).Select(r => r.sizeDelta.y).Max() + PlayerDisplay.padding;
            }
            catch { }
        }

        void LateUpdate()
        {
            // when this button is highlighted or locked in, check for inputs
            // can't do this with button.OnClick since there's no good way to get which device triggered that
            if (!this.isHighlighted && !this.selected) { return; }

            // check for exit, ready, or join
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1)) // exit with Esc, Q, or RMB
            {
                this.Deselect();
                return;
            }
            else if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButton(0)) // ready or join with Space or LMB
            {
                this.PrivateRoom.StartCoroutine(this.PrivateRoom.ToggleReady(null, !this.selected));
            }

            for (int i = 0; i < InputManager.ActiveDevices.Count; i++)
            {
                InputDevice device = InputManager.ActiveDevices[i];

                if (device.CommandWasPressed || device.Action2.WasPressed) // exit with Start, Select, or B
                {
                    this.Deselect();
                }

                else if (device.Action1.WasPressed || device.Action3.WasPressed || device.Action4.WasPressed)
                {
                    this.PrivateRoom.StartCoroutine(this.PrivateRoom.ToggleReady(device, !this.selected));
                }
            }
        }

    }
    class SetBar : MonoBehaviour
    {
        ListMenuButton menuButton;
        HorizontalLayoutGroup layoutGroup;
        public float heightMult = 1f;
        public float verticalOffset = 0f;
        bool apply = false;

        void Start()
        {
            this.menuButton = this.gameObject.GetOrAddComponent<ListMenuButton>();
            this.layoutGroup = this.gameObject.GetOrAddComponent<HorizontalLayoutGroup>();
        }
        void Update()
        {
            if (!apply) { return; }
            this.menuButton.setBarHeight = this.layoutGroup.preferredHeight * this.heightMult;
            if (ListMenu.instance.selectedButton == this.menuButton)
            {
                ListMenu.instance.bar.transform.position = this.menuButton.transform.position + this.verticalOffset * Vector3.up;
                ListMenu.instance.bar.transform.localScale = new Vector3(ListMenu.instance.bar.transform.localScale.x, this.menuButton.setBarHeight, 1f);
            }
        }
        public void Select()
        {

        }
        public void Deselect()
        {

        }
        public void SetEnabled(bool enabled)
        {
            this.apply = enabled;
        }
    }
}
