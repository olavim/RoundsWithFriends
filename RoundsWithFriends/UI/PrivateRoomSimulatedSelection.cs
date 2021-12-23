using UnityEngine.UI;
using UnityEngine;

namespace RWF.UI
{
    public class PrivateRoomSimulatedSelection : SimulatedSelection
    {
        private void Start()
        {
            this.hoverEvent = base.GetComponent<HoverEvent>();
            this.button = base.GetComponent<Button>();
        }

        private void OnDisable()
        {
            this.Deselect();
        }

        new public void Select()
        {
            this.hoverEvent.OnPointerEnter(null);
            //this.button.targetGraphic.color = this.button.colors.highlightedColor;
        }

        new public void Deselect()
        {
            this.hoverEvent.OnPointerExit(null);
            this.button.OnDeselect(null);
            //this.button.targetGraphic.color = this.button.colors.normalColor;
        }

        private HoverEvent hoverEvent;

        private Button button;
    }
}
