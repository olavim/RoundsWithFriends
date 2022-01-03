using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnboundLib;
using InControl;
using System.Linq;

namespace RWF.UI
{
    class PlayerDisplay : MonoBehaviour
    {
        public static PlayerDisplay instance;

        private const float barPad = -35f;
        private const float layoutPad = 50f;

        private static readonly Color disabledTextColor = new Color32(150, 150, 150, 16);
        private static readonly Color enabledTextColor = new Color32(230, 230, 230, 255);

        private static Color defaultColorMax = new Color(0.4434f, 0.2781f, 0.069f, 1f);
        private static Color defaultColorMin = new Color(0.5094f, 0.3371f, 0.0889f, 1f);

        private static Color highlightedColorMax = new Color(0.3204f, 0.3751f, 0.409f, 0.3396f);
        private static Color highlightedColorMin = new Color(0f, 0f, 0f, 0.3396f);

        private static Color selectedColorMax = new Color(0f, 0f, 0.0898f, 0.7925f);
        private static Color selectedColorMin = new Color(0f, 0.0921f, 0.0898f, 0.7925f);

        private GameObject _Bar = null;
        public GameObject Bar
        {
            get
            {
                if (this._Bar == null)
                {
                    this._Bar = GameObject.Instantiate(ListMenu.instance.bar, this.gameObject.transform);
                    GameObject.Instantiate(UnityEngine.GameObject.Find("Game/UI/UI_MainMenu/Canvas/Particle"), this._Bar.transform);
                    this._Bar.name = "PlayerDisplayBar";
                    this._Bar.SetActive(false);
                }
                return this._Bar;
            }
        }
        private ParticleSystem Particles => this.Bar.GetComponentInChildren<ParticleSystem>();

        GridLayoutGroup group;
        SetBar setBar;
        LayoutElement layout;
        PrivateRoomHandler PrivateRoom => PrivateRoomHandler.instance;

        bool playersAdded = false;

        public bool PlayersHaveBeenAdded => this.playersAdded;

        private void Awake()
        {
            PlayerDisplay.instance = this;
        }
        void Start()
        {
            // add the necessary components
            this.group = this.gameObject.GetOrAddComponent<GridLayoutGroup>();

            this.setBar = this.gameObject.GetOrAddComponent<SetBar>();
            this.layout = this.gameObject.GetOrAddComponent<LayoutElement>();

            // set the bar sorting layers properly
            this.Bar.GetComponentInChildren<ParticleSystemRenderer>().sortingOrder = 2;
            this.Bar.GetComponent<SpriteMask>().frontSortingOrder = 3;
            this.Bar.GetComponent<SpriteMask>().backSortingOrder = 2;

            // set the bar color
            ParticleSystem.MainModule main = this.Particles.main;
            ParticleSystem.MinMaxGradient startColor = main.startColor;
            startColor.colorMax = PlayerDisplay.selectedColorMax;
            startColor.colorMin = PlayerDisplay.selectedColorMin;
            main.startColor = startColor;
            this.Particles.Play();

            // set up the layout
            this.layout.ignoreLayout = false;

            // set up the horizontal group
            this.group.childAlignment = TextAnchor.MiddleCenter;
            this.group.spacing = new Vector2(100, 60);
            this.group.cellSize = new Vector2(150, 200);
            this.group.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            this.group.constraintCount = 7;

            // set up the menu bar
            this.setBar.heightMult = 1f;
            this.setBar.padding = 65f;
            //this.setBar.verticalOffset = -0.55f;
            this.setBar.SetEnabled(false);
        }

        void Update()
        {
            if (!this.playersAdded) 
            {
                if (VersusDisplay.instance.PlayersHaveBeenAdded)
                {
                    this.playersAdded = true;
                    this.setBar.SetEnabled(true);
                    this.ExecuteAfterFrames(1, () => ListMenu.instance.SelectButton(ListMenu.instance.selectedButton));
                }
                else
                {
                    return;
                }
            }
            try
            {
                this.layout.minHeight = this.gameObject.GetComponentsInChildren<LayoutGroup>(false).Select(c => c.preferredHeight).Max() + PlayerDisplay.layoutPad;
                this.group.cellSize = new Vector2(this.group.cellSize.x, this.gameObject.GetComponentsInChildren<LayoutGroup>(false).Where(c => c != this.group).Select(c => c.preferredHeight).Max() + PlayerDisplay.barPad);
            }
            catch { }
        }

        void LateUpdate()
        {
            // check for exit, ready, or join
            if (Input.GetKeyDown(KeyCode.Escape)) // exit with Esc
            {
                // if the player is ready, toggle their ready status
                // if they are not ready, remove them
                bool? ready = this.PrivateRoom.FindLobbyCharacter(null)?.ready;
                if (ready == null) { return; }
                else if ((bool)ready)
                {
                    this.PrivateRoom.StartCoroutine(this.PrivateRoom.ToggleReady(null, false));
                }
                else
                {
                    // TODO remove players
                }
                return;
            }
            else if (Input.GetKeyDown(KeyCode.Space)) // ready or join with space
            {
                // if the player is ready, do nothing
                // if they are not ready, ready them 
                // if they don't exist, let them join
                bool? ready = this.PrivateRoom.FindLobbyCharacter(null)?.ready;
                if (ready == null || !(bool)ready)
                { 
                    this.PrivateRoom.StartCoroutine(this.PrivateRoom.ToggleReady(null, false));
                }
                return;
            }

            for (int i = 0; i < InputManager.ActiveDevices.Count; i++)
            {
                InputDevice device = InputManager.ActiveDevices[i];

                // enter with start/select
                if (device.CommandWasPressed)
                {
                    // if the player is ready, do nothing
                    // if they are not ready, ready them 
                    // if they don't exist, let them join
                    bool? ready = this.PrivateRoom.FindLobbyCharacter(device)?.ready;
                    if (ready == null || !(bool)ready)
                    { 
                        this.PrivateRoom.StartCoroutine(this.PrivateRoom.ToggleReady(device, false));
                    }
                    return;
                }

                else if (device.Action2.WasPressed) // exit with B
                {
                    // if the player is ready, toggle their ready status
                    // if they are not ready, remove them
                    bool? ready = this.PrivateRoom.FindLobbyCharacter(device)?.ready;
                    if (ready == null) { return; }
                    else if ((bool)ready)
                    {
                        this.PrivateRoom.StartCoroutine(this.PrivateRoom.ToggleReady(device, false));
                    }
                    else
                    {
                        // TODO remove players
                    }
                    return;
                }
            }
        }

    }
    class SetBar : MonoBehaviour
    {
        GridLayoutGroup layoutGroup;
        PlayerDisplay playerDisplay;
        public float heightMult = 1f;
        public float padding = 0f;
        public float verticalOffset = 0f;
        bool apply = false;

        void Start()
        {
            this.layoutGroup = this.gameObject.GetComponent<GridLayoutGroup>();
            this.playerDisplay = this.gameObject.GetComponent<PlayerDisplay>();
            if (this.playerDisplay == null || this.layoutGroup == null) { Destroy(this); }
        }
        void Update()
        {
            if (!apply) { return; }
            this.playerDisplay.Bar.transform.position = this.gameObject.transform.position + this.verticalOffset * Vector3.up;
            this.playerDisplay.Bar.transform.localScale = new Vector3(this.playerDisplay.Bar.transform.localScale.x, this.layoutGroup.preferredHeight * this.heightMult + this.padding, 1f);
        }
        public void SetEnabled(bool enabled)
        {
            this.apply = enabled;
            this.playerDisplay?.Bar.SetActive(true);
        }
    }
}
