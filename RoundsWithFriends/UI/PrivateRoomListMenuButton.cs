using System;
using SoundImplementation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RWF.UI
{
    public class PrivateRoomListMenuButton : ListMenuButton, IPointerClickHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler, IUpdateSelectedHandler, ISelectHandler, IDeselectHandler
    {
        private static Color defaultColorMax = new Color(0.4434f, 0.2781f, 0.069f, 1f);
        private static Color defaultColorMin = new Color(0.5094f, 0.3371f, 0.0889f, 1f);

        public Color highlightedColorMax = new Color(0.4434f, 0.2781f, 0.069f, 1f);
        public Color highlightedColorMin = new Color(0.5094f, 0.3371f, 0.0889f, 1f);

        private static ParticleSystem Particles => UnityEngine.GameObject.Find("Game/UI/UI_MainMenu/Canvas/Particle").GetComponent<ParticleSystem>();

        private bool inited;

        private PlayerDisplay PlayerDisplay => this.gameObject.GetComponent<PlayerDisplay>();

        private void Awake()
        {
        }

        private void Start()
        {
            this.Init();
            // save the default bar color
            ParticleSystem.MainModule main = PrivateRoomListMenuButton.Particles.main;
            ParticleSystem.MinMaxGradient startColor = main.startColor;
            PrivateRoomListMenuButton.defaultColorMax = startColor.colorMax;
            PrivateRoomListMenuButton.defaultColorMin = startColor.colorMin;
        }

        new public void Nope()
        {
        }

        new public void Deselect()
        {
            if (this.PlayerDisplay.IsSelected || !this.PlayerDisplay.PlayersHaveBeenAdded)
            {
                return;
            }
            // restore the bar color
            ParticleSystem.MainModule main = PrivateRoomListMenuButton.Particles.main;
            ParticleSystem.MinMaxGradient startColor = main.startColor;
            startColor.colorMax = PrivateRoomListMenuButton.defaultColorMax;
            startColor.colorMin = PrivateRoomListMenuButton.defaultColorMin;
            main.startColor = startColor;
            PrivateRoomListMenuButton.Particles.Play();
        }

        new public void Select()
        {
            if (this.PlayerDisplay.IsSelected || !this.PlayerDisplay.PlayersHaveBeenAdded)
            {
                return;
            }
            // set the bar color
            ParticleSystem.MainModule main = PrivateRoomListMenuButton.Particles.main;
            ParticleSystem.MinMaxGradient startColor = main.startColor;
            startColor.colorMax = this.highlightedColorMax;
            startColor.colorMin = this.highlightedColorMin;
            main.startColor = startColor;
            PrivateRoomListMenuButton.Particles.Play();
        }

        new public void OnPointerClick(PointerEventData eventData)
        {
            SoundPlayerStatic.Instance.PlayButtonClick();
        }

        new public void OnPointerEnter(PointerEventData eventData)
        {
            ListMenu.instance.SelectButton(this);
        }

        new public void OnPointerExit(PointerEventData eventData)
        {
        }

        new public void OnUpdateSelected(BaseEventData eventData)
        {
        }

        new public void OnSelect(BaseEventData eventData)
        {
            this.Select();
            this.PlayerDisplay.SetHighlighted(true);
            ListMenu.instance.SelectButton(this);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            this.PlayerDisplay.SetHighlighted(false);
            this.Deselect();
        }

        private void Init()
        {
            if (this.inited)
            {
                return;
            }
            this.inited = true;
        }
    }

}
