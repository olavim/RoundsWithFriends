using InControl;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RWF.UI
{
    public class PopUpMenu : MonoBehaviour
    {
        public static PopUpMenu instance;

        private Action<string> callback;
        private int currentChoice;
        private List<string> choices;
        private List<CurveAnimation> choiceAnimations;
        private List<GeneralParticleSystem> choiceParticleSystems;
        private bool isOpen = false;

        public void Awake()
        {
            PopUpMenu.instance = this;

            var layoutGroup = this.gameObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;

            var sizer = this.gameObject.AddComponent<ContentSizeFitter>();
            sizer.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public void Open(List<string> choices, Action<string> callback)
        {
            this.callback = callback;
            this.currentChoice = 0;
            this.choices = choices;
            this.choiceAnimations = new List<CurveAnimation>();
            this.choiceParticleSystems = new List<GeneralParticleSystem>();
            this.isOpen = true;

            while (this.transform.childCount > 0)
            {
                GameObject.DestroyImmediate(this.transform.GetChild(0).gameObject);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(this.gameObject.GetComponent<RectTransform>());

            for (int i = 0; i < this.choices.Count; i++)
            {
                int index = i;
                string choice = this.choices[index];

                var go = GameObject.Instantiate(RoundsResources.PopUpMenuText, this.transform);
                go.name = choice;

                var text = go.GetComponent<TextMeshProUGUI>();
                text.text = choice;
                text.fontSize = 60;

                go.AddComponent<VerticalLayoutGroup>();
                var sizer = go.AddComponent<ContentSizeFitter>();
                var layout = go.AddComponent<LayoutElement>();
                sizer.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                layout.preferredHeight = 92;

                this.choiceAnimations.Add(go.GetComponent<CurveAnimation>());
                this.choiceParticleSystems.Add(go.GetComponentInChildren<GeneralParticleSystem>());
            }

            this.choiceAnimations[0].PlayIn();
        }

        private void Update()
        {
            if (!this.isOpen)
            {
                return;
            }

            bool isUp = false;
            bool isDown = false;
            bool isActionPressed = false;

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                isUp = true;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                isDown = true;
            }

            if (this.currentChoice != -1 && Input.GetKeyDown(KeyCode.Space))
            {
                isActionPressed = true;
            }

            foreach (var input in InputManager.ActiveDevices)
            {
                if (input.Direction.Up.WasPressed)
                {
                    isUp = true;
                }

                if (input.Direction.Down.WasPressed)
                {
                    isDown = true;
                }

                if (this.currentChoice != -1 && input.Action1.IsPressed)
                {
                    isActionPressed = true;
                }
            }

            if (isUp && this.currentChoice > 0)
            {
                this.SetChoice(this.currentChoice - 1);
            }

            if (isDown && this.currentChoice < this.choices.Count - 1)
            {
                this.SetChoice(this.currentChoice + 1);
            }

            if (isActionPressed)
            {
                this.Choose();
            }
        }

        private void SetChoice(int newChoice)
        {
            if (newChoice == this.currentChoice)
            {
                return;
            }

            if (this.currentChoice != -1)
            {
                this.choiceAnimations[this.currentChoice].PlayOut();
            }

            if (newChoice != -1)
            {
                this.choiceAnimations[newChoice].PlayIn();
            }

            this.currentChoice = newChoice;
        }

        private void Choose()
        {
            this.isOpen = false;

            foreach (var anim in this.choiceParticleSystems)
            {
                anim.loop = false;
            }

            this.callback(this.choices[this.currentChoice]);
        }
    }
}
