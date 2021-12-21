using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnboundLib;

namespace RWF.UI
{
    class CharacterSelectButton : MonoBehaviour//, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private LeftRight direction = CharacterSelectButton.LeftRight.Left;
        private CharacterSelectionInstance characterSelectionInstance = null;
        private const float hoverScale = 1.00f;
        private const float clickScale = 0.95f;
        private Vector3 defaultScale;
        private bool inBounds = false;
        private bool pressed = false;
        private TextMeshProUGUI text = null;
        private int currentlySelectedFace = -1;
        private bool isReady = false;
        private static Color disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.25f);
        private static Color enabledColor = Color.white;

        public void SetDirection(CharacterSelectButton.LeftRight direction)
        {
            this.direction = direction;
        }
        public void SetCharacterSelectionInstance(CharacterSelectionInstance characterSelectionInstance)
        {
            this.characterSelectionInstance = characterSelectionInstance;
        }

        void Start()
        {
            this.text = this.gameObject.GetOrAddComponent<TextMeshProUGUI>();

            this.text.text = this.direction == CharacterSelectButton.LeftRight.Left ? "<" : ">";

            this.text.color = CharacterSelectButton.enabledColor;

            this.text.alignment = TextAlignmentOptions.Center;

            this.defaultScale = this.gameObject.transform.localScale;

            this.transform.parent.localPosition = Vector3.zero;
        }
        void Update()
        {
            if (this.characterSelectionInstance == null) { return; }

            if (this.characterSelectionInstance.isReady != this.isReady)
            {
                this.isReady = this.characterSelectionInstance.isReady;
                if (this.isReady)
                {
                    this.text.color = CharacterSelectButton.disabledColor;
                }
                else
                {
                    this.text.color = CharacterSelectButton.enabledColor;
                }
            }

            if (this.currentlySelectedFace == this.characterSelectionInstance.currentlySelectedFace) { return; }

            if (this.characterSelectionInstance.currentlySelectedFace == 0 && this.direction == CharacterSelectButton.LeftRight.Left)
            {
                this.text.color = CharacterSelectButton.disabledColor;
            }
            else if (this.characterSelectionInstance.currentlySelectedFace == ((HoverEvent[]) this.characterSelectionInstance.GetFieldValue("buttons")).Length - 1 && this.direction == CharacterSelectButton.LeftRight.Right)
            {
                this.text.color = CharacterSelectButton.disabledColor;
            }
            else
            {
                this.text.color = CharacterSelectButton.enabledColor;
            }


            if (this.currentlySelectedFace < this.characterSelectionInstance.currentlySelectedFace && this.direction == CharacterSelectButton.LeftRight.Right)
            {
                this.gameObject.transform.localScale = this.defaultScale * CharacterSelectButton.clickScale;
                this.ExecuteAfterSeconds(0.1f, () => this.gameObject.transform.localScale = this.inBounds ? this.defaultScale * CharacterSelectButton.hoverScale : this.defaultScale);
            }
            else if (this.currentlySelectedFace > this.characterSelectionInstance.currentlySelectedFace && this.direction == CharacterSelectButton.LeftRight.Left)
            {
                this.gameObject.transform.localScale = this.defaultScale * CharacterSelectButton.clickScale;
                this.ExecuteAfterSeconds(0.1f, () => this.gameObject.transform.localScale = this.inBounds ? this.defaultScale * CharacterSelectButton.hoverScale : this.defaultScale);
            }
            

            this.currentlySelectedFace = this.characterSelectionInstance.currentlySelectedFace;

        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (this.characterSelectionInstance.currentPlayer.data.input.inputType == GeneralInput.InputType.Controller) { return; }

            if (this.inBounds)
            {
                this.pressed = true;
                this.gameObject.transform.localScale = this.defaultScale * CharacterSelectButton.clickScale;
            }
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            if (this.characterSelectionInstance.currentPlayer.data.input.inputType == GeneralInput.InputType.Controller) { return; }

            if (this.inBounds && this.pressed)
            {
                if (this.characterSelectionInstance != null)
                {
                    if (this.direction == CharacterSelectButton.LeftRight.Left)
                    {
                        this.characterSelectionInstance.currentlySelectedFace--;
                    }
                    else if (this.direction == CharacterSelectButton.LeftRight.Right)
                    {
                        this.characterSelectionInstance.currentlySelectedFace++;
                    }

                    this.characterSelectionInstance.currentlySelectedFace = Mathf.Clamp(this.characterSelectionInstance.currentlySelectedFace, 0, ((HoverEvent[])this.characterSelectionInstance.GetFieldValue("buttons")).Length - 1);
                }

            }
            this.pressed = false;
            if (!this.inBounds)
            {
                this.gameObject.transform.localScale = this.defaultScale;
            }
            else
            {
                this.gameObject.transform.localScale = this.defaultScale * CharacterSelectButton.hoverScale;
            }
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (this.characterSelectionInstance.currentPlayer.data.input.inputType == GeneralInput.InputType.Controller) { return; }

            this.inBounds = true;
            this.gameObject.transform.localScale = this.defaultScale * CharacterSelectButton.hoverScale;
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            if (this.characterSelectionInstance.currentPlayer.data.input.inputType == GeneralInput.InputType.Controller) { return; }

            this.inBounds = false;
            if (!this.pressed)
            {
                this.gameObject.transform.localScale = this.defaultScale;
            }
        }

        public enum LeftRight
        {
            Left,
            Right
        }
        
    }
}
