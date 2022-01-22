using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnboundLib.GameModes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RWF.UI
{
    public static class GamemodeScrollView
    {
        public static GameObject scrollView;
        public static List<GameObject> buttons;
        
        public static void Create(Transform parent)
        {
            GamemodeScrollView.buttons = new List<GameObject>();
            
            GamemodeScrollView.scrollView = new GameObject("ScrollView");
            GamemodeScrollView.scrollView.transform.SetParent(parent);
            var scrollTrans = GamemodeScrollView.scrollView.AddComponent<RectTransform>();
            scrollTrans.anchoredPosition = new Vector2(0, 0);
            var scrollRect = GamemodeScrollView.scrollView.AddComponent<SpecialScrollRect>();
            scrollRect.vertical = false;
            scrollRect.scrollSensitivity = -100;
            scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            var scrollLayout = GamemodeScrollView.scrollView.AddComponent<LayoutElement>();
            scrollLayout.minHeight = 50;
            scrollLayout.minWidth = 5000;
            GamemodeScrollView.scrollView.transform.localScale = Vector3.one;

            var viewPort = new GameObject("ViewPort");
            viewPort.transform.SetParent(GamemodeScrollView.scrollView.transform);
            viewPort.AddComponent<RectTransform>();
            var mask = viewPort.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var img = viewPort.AddComponent<Image>();
            img.color = Color.white;
            img.type = Image.Type.Sliced;
            // img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            // Make sure the viewport is the same size as the scrollview
            GamemodeScrollView.SameAsParent(viewPort.GetComponent<RectTransform>());
            viewPort.transform.localScale = Vector3.one;
            
            var content = new GameObject("Content");
            content.transform.SetParent(viewPort.transform);
            var contentRect = content.AddComponent<RectTransform>();
            var layoutGroup = content.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            var sizeFitter = content.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            GamemodeScrollView.SameAsParent(contentRect);
            content.transform.localScale = Vector3.one;

            string[] gameModes = GameModeManager.Handlers.Keys.Where(k=> k != GameModeManager.SandBoxID && k != GameModeManager.ArmsRaceID).OrderBy(k => GameModeManager.Handlers[k].Name).ToArray();

            for (var i = 0; i < gameModes.Length; i++)
            {
                var gameMode = gameModes[i];
                var buttonObj = new GameObject(gameMode);
                GamemodeScrollView.buttons.Add(buttonObj);
                buttonObj.transform.SetParent(content.transform);
                buttonObj.AddComponent<RectTransform>();
                var width = 400;
                buttonObj.AddComponent<LayoutElement>().minWidth = width;
                var button = buttonObj.AddComponent<Button>();
                var index = i;
                
                var text = PrivateRoomHandler.instance.GetText(GameModeManager.Handlers[gameMode].Name);
                text.transform.SetParent(buttonObj.transform);
                GamemodeScrollView.SameAsParent(text.GetComponent<RectTransform>());
                text.GetComponent<TextMeshProUGUI>().enableAutoSizing = true;
                
                buttonObj.transform.localScale = Vector3.one;
                button.onClick.AddListener(() =>
                {
                    var isEven = gameModes.Length % 2 == 0;
                    if (isEven)
                    {
                        PrivateRoomHandler.instance.StartCoroutine(GamemodeScrollView.MoveContent(contentRect, index * width - (gameModes.Length / 2) * width + width/2));
                    } else
                    {
                        PrivateRoomHandler.instance.StartCoroutine(GamemodeScrollView.MoveContent(contentRect, index * width - (gameModes.Length / 2+1) * width + width));
                    }
                    GameModeManager.SetGameMode(gameMode);
                    PrivateRoomHandler.instance.UnreadyAllPlayers();
                    PrivateRoomHandler.instance.ExecuteAfterGameModeInitialized(gameMode, () =>
                    {
                        PrivateRoomHandler.instance.SyncMethod(nameof(PrivateRoomHandler.SetGameSettings), null, GameModeManager.CurrentHandlerID, GameModeManager.CurrentHandler.Settings);
                        PrivateRoomHandler.instance.HandleTeamRules();
                    });
                });
            }

            scrollRect.content = contentRect;
            scrollRect.viewport = viewPort.GetComponent<RectTransform>();

            scrollRect.horizontalNormalizedPosition = 0;
            GamemodeScrollView.scrollView.SetActive(false);
        }

        public static void SetGameMode(string gameModeName)
        {
            GamemodeScrollView.buttons.First(b => b.name == gameModeName).GetComponent<Button>().onClick.Invoke();
        }

        private static IEnumerator MoveContent(RectTransform content, float xPos)
        {
            var startPos = content.anchoredPosition;
            var endPos = new Vector2(-xPos, 0);
            var time = 0f;
            while (time < 1)
            {
                time += Time.deltaTime*2f;
                content.anchoredPosition = Vector2.Lerp(startPos, endPos, time);
                yield return null;
            }
        } 

        static void SameAsParent(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(0, 0);
            rect.sizeDelta = new Vector2(0, 0);
        }
        
        public class SpecialScrollRect : ScrollRect
        {
            public override void OnBeginDrag(PointerEventData eventData)
            {
            }
    
            public override void OnDrag(PointerEventData eventData)
            {
            }
    
            public override void OnEndDrag(PointerEventData eventData)
            {
            }
        }
    }
    
}