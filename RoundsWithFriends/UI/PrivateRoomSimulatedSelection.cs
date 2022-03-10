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
        }

        new public void Deselect()
        {
            this.hoverEvent.OnPointerExit(null);
            this.button.OnDeselect(null);
        }

        private HoverEvent hoverEvent;

        private Button button;
    }
}
