using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnboundLib;

namespace RWF.UI
{
    public class CharacterSelectionMenuLayoutGroup : MonoBehaviour
    {
        public int maxCols = UnityEngine.Mathf.CeilToInt(RWFMod.instance.MaxPlayers / 4);
        public float maxSize = 3.5f;
        public float minSize = 1f;
        public float maxHSpacing = 50f * 4f / UnityEngine.Mathf.CeilToInt(RWFMod.instance.MaxPlayers / 4);
        public float minHSpacing = 15f * 4f / UnityEngine.Mathf.CeilToInt(RWFMod.instance.MaxPlayers / 4);
        public float maxVSpacing = 20f;
        public float minVSpacing = 2.5f;
        public const float speed = 0.01f;
        public static readonly Vector2 away = new Vector2(1000000f, 0f);

        float scale = 0f;
        float hspace = 0f;
        float vspace = 0f;

        public Vector2 spacing
        {
            get
            {
                return new Vector2(this.hspace, this.vspace);
            }
            set
            {
                this.hspace = value.x;
                this.vspace = value.y;
            }
        }

        int players => PlayerManager.instance.players.Count();

        void Start()
        {
            Init();
        }

        internal void Init()
        {
            this.scale = this.maxSize;
            this.hspace = this.maxHSpacing;
            this.vspace = this.maxVSpacing;
            
            foreach (Transform characterSelectionTransform in this.transform)
            {
                if (characterSelectionTransform.gameObject != null)
                {
                    characterSelectionTransform.transform.position = away;
                    characterSelectionTransform.gameObject.SetActive(false);
                }
            }
        }

        public void PlayerJoined(Player joinedPlayer)
        {
            this.transform.GetChild(joinedPlayer.playerID).transform.position = away;
            this.transform.GetChild(joinedPlayer.playerID).gameObject.SetActive(true);
        }

        List<Vector2> CalculatePositions(int n)
        {
            List<Vector2> pos = new List<Vector2>() { };

            if (n <= 1)
            {
                return new List<Vector2>() { Vector2.zero };
            }

            // basic layout
            for (int i = 0; i < n; i++)
            {
                pos.Add(new Vector2(this.hspace * (i % this.maxCols), -this.vspace * UnityEngine.Mathf.FloorToInt(i/ this.maxCols)));
            }
            // find xcenter of first row
            float xcenter = pos.Take(this.maxCols).Average(p => p.x);
            // find ycenter of first column
            float ycenter = Enumerable.Range(0, n).Where(k => k % this.maxCols == 0).Select(j => pos[j].y).Average();
            // center the layout
            for (int i = 0; i < pos.Count(); i++)
            {
                pos[i] -= new Vector2(xcenter, ycenter);
            }

            return pos;
        }

        void Update()
        {
            // calculate the positions for children in the layout
            List<Vector2> positions = this.CalculatePositions(this.players);

            // disable currently unused portraits
            foreach (Transform characterSelectionInstance in this.transform)
            {
                if (characterSelectionInstance.GetSiblingIndex() >= this.players && characterSelectionInstance.gameObject != null && characterSelectionInstance.gameObject.activeSelf)
                {
                    characterSelectionInstance.transform.localScale = this.maxSize * Vector3.one;
                    characterSelectionInstance.gameObject.SetActive(false);
                }
            }
            // update the members of the layout, one frame delay
            for (int i = 0; i < this.players; i++)
            {
                this.transform.GetChild(i).position = positions[i];
                this.transform.GetChild(i).localScale = this.scale * Vector2.one;
            }

            // update the layout
            float scale = UnityEngine.Mathf.Lerp(this.maxSize, this.minSize, (float) (this.players) / (float) (this.maxCols));
            float Hspacing = UnityEngine.Mathf.Lerp(this.maxHSpacing, this.minHSpacing, (float) (this.players) / (float) (this.maxCols));
            float Vspacing = UnityEngine.Mathf.Lerp(this.maxVSpacing, this.minVSpacing, (float) (float) (UnityEngine.Mathf.FloorToInt((this.players - 1) / this.maxCols)) / (float) (UnityEngine.Mathf.Ceil(RWFMod.instance.MaxPlayers / this.maxCols)));

            if (scale != this.scale || Hspacing != this.hspace || Vspacing != this.vspace)
            {
                ChangeLayout(scale, Hspacing, Vspacing);
            }
        }

        void ChangeLayout(float scale, float HSpacing, float VSpacing)
        {

            this.scale = UnityEngine.Mathf.Clamp(this.scale - CharacterSelectionMenuLayoutGroup.speed * (this.maxSize - this.minSize), scale, this.maxSize);
            this.spacing = new Vector2(UnityEngine.Mathf.Clamp(this.hspace - CharacterSelectionMenuLayoutGroup.speed * (this.maxHSpacing - this.minHSpacing), HSpacing, this.maxHSpacing), UnityEngine.Mathf.Clamp(this.vspace - CharacterSelectionMenuLayoutGroup.speed * (this.maxVSpacing - this.minVSpacing), VSpacing, this.maxVSpacing));

        }

    }

}
