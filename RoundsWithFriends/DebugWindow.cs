using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnboundLib;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Sonigon;

namespace RWF
{
    class WindowGUIBuilder
    {
        private float x;
        private float y;

        private Rect elementRect;
        private float rowHeight;
        private float rowY;

        public WindowGUIBuilder()
        {
            this.x = 0;
            this.y = GUI.skin.window.border.top;
        }

        public void NextRow(float height, float verticalPadding = 0)
        {
            this.NextRow(height, verticalPadding, verticalPadding);
        }

        public void NextRow(float height, float verticalPaddingTop, float verticalPaddingBottom)
        {
            this.x = 0;
            this.y += verticalPaddingTop;

            this.rowY = this.y;
            this.rowHeight = height - verticalPaddingTop - verticalPaddingBottom;

            this.y += this.rowHeight - verticalPaddingTop;
        }

        public WindowGUIBuilder NextElement(float width, float horizontalPadding = 0)
        {
            return this.NextElement(width, horizontalPadding, horizontalPadding);
        }

        public WindowGUIBuilder NextElement(float width, float horizontalPaddingLeft, float horizontalPaddingRight)
        {
            this.x += horizontalPaddingLeft;
            this.elementRect = new Rect(this.x, this.rowY, width - horizontalPaddingLeft - horizontalPaddingRight, this.rowHeight);
            //GUI.Box(this.elementRect, GUIContent.none);
            this.x += width - horizontalPaddingLeft;
            return this;
        }

        public void Label(string text, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            GUI.skin.label.alignment = alignment;
            var fixedRect = new Rect(this.elementRect.x, this.rowY, this.elementRect.width, this.elementRect.height);
            GUI.Label(fixedRect, text);
        }

        public float HorizontalSlider(float value, float min, float max)
        {
            GUILayout.BeginArea(this.elementRect);
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            float result = GUILayout.HorizontalSlider(value, min, max);

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.EndArea();

            return result;
        }

        public bool Button(string text)
        {
            return GUI.Button(this.elementRect, text);
        }
    }

    public class DebugWindow : MonoBehaviour
    {
        private Rect windowRect = new Rect(10, 10, 260, 130);

        public void OnGUI()
        {
            GUI.skin.label.margin = new RectOffset(0, 0, 0, 0);
            GUI.skin.label.padding = new RectOffset(0, 0, 0, 0);

            this.windowRect = GUI.Window(0, this.windowRect, WindowFunction, "RWF Debug");
        }

        public void WindowFunction(int windowID)
        {
            var builder = new WindowGUIBuilder();

            builder.NextRow(35, 10, 0);
            builder.NextElement(70, 10, 0).Label("points");
            float points = builder.NextElement(150, 10).HorizontalSlider(RWFMod.instance.debugOptions.points, 1f, 5f);
            builder.NextElement(40, 10, 12).Label($"{RWFMod.instance.debugOptions.points}", TextAnchor.MiddleRight);

            builder.NextRow(35, 10, 0);
            builder.NextElement(70, 10, 0).Label("rounds");
            float rounds = builder.NextElement(150, 10).HorizontalSlider(RWFMod.instance.debugOptions.rounds, 1f, 5f);
            builder.NextElement(40, 10, 12).Label($"{RWFMod.instance.debugOptions.rounds}", TextAnchor.MiddleRight);

            builder.NextRow(60, 20, 10);
            bool testerBtn = builder.NextElement(260, 10).Button("Lag Tester");

            if (testerBtn)
            {
                var tester = RWFMod.instance.gameObject.GetComponent<PhotonLagSimulationGui>();
                tester.enabled = !tester.enabled;
            }

            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            {
                if (Mathf.RoundToInt(points) != RWFMod.instance.debugOptions.points)
                {
                    RWFMod.instance.debugOptions.points = Mathf.RoundToInt(points);
                    RWFMod.instance.SyncDebugOptions();
                }

                if (Mathf.RoundToInt(rounds) != RWFMod.instance.debugOptions.rounds)
                {
                    RWFMod.instance.debugOptions.rounds = Mathf.RoundToInt(rounds);
                    RWFMod.instance.SyncDebugOptions();
                }
            }

            GUI.DragWindow(new Rect(0, 0, windowRect.width, windowRect.height));
        }
    }
}
