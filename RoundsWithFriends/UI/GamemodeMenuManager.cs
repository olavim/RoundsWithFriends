using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace RWF.UI
{
    public class GamemodeMenuManager : MonoBehaviour
    {
        public GameObject lobbyMenuObject;
        public Transform ffaContent;
        public Transform teamContent;

        public GameObject topBar;
        public GameObject bottomBar;

        void Start()
        {
            this.gameObject.SetActive(false);
        }

        public void Open()
        {
            this.gameObject.SetActive(true);
            this.transform.Find("BACK(short)").GetComponent<ListMenuButton>().OnPointerEnter(null);
            RWFMod.instance.StartCoroutine(this.Swoop(this.lobbyMenuObject, -1920*2, true));
            RWFMod.instance.StartCoroutine(this.Swoop(this.gameObject, 1920*2, false, () =>
            {
                // Select the current gamemode category
                if (!GameModeManager.CurrentHandler.AllowTeams)
                {
                    this.transform.Find("LeftPanel/Top/FFA(short)").GetComponent<Button>().onClick.Invoke();
                    this.transform.Find("LeftPanel/Bottom/FFA/Scroll View/Viewport/Content/" + GameModeManager.CurrentHandlerID+"(short)").GetComponent<Button>().onClick.Invoke();
                
                }
                else
                {
                    this.transform.Find("LeftPanel/Top/TEAM(short)").GetComponent<Button>().onClick.Invoke();
                    this.transform.Find("LeftPanel/Bottom/TEAM/Scroll View/Viewport/Content/" + GameModeManager.CurrentHandlerID+"(short)").GetComponent<Button>().onClick.Invoke();
                }
                this.topBar.SetActive(true);
                this.bottomBar.SetActive(true);
                this.transform.Find("BACK(short)").GetComponent<ListMenuButton>().OnPointerEnter(null);
            }));
            this.lobbyMenuObject.transform.parent.parent.parent.Find("UIHolder")?.gameObject.SetActive(false);

            this.UpdateInspector();
        }

        public void Init()
        {
            this.transform.Find("BACK(short)").GetComponent<Button>().onClick.AddListener(() =>
            {
                GameObject.Find("Game/UI/UI_Game/Canvas/EscapeMenu/SelectionBar").transform.position = new Vector3(1000, 0, 0);
                
                RWFMod.instance.StartCoroutine(this.Swoop(this.gameObject, 1920*2, true));
                RWFMod.instance.StartCoroutine(this.Swoop(this.lobbyMenuObject, -1920*2, false, () =>
                {
                    this.lobbyMenuObject.transform.Find("ButtonBaseObject(Clone)").GetComponent<ListMenuButton>().OnPointerEnter(null);
                }));
                
                this.topBar.SetActive(false);
                this.bottomBar.SetActive(false);
                
                this.transform.Find("RightPanel/Top/gameModeVideo").GetComponent<VideoPlayer>().Stop();

                this.lobbyMenuObject.transform.parent.parent.parent.Find("UIHolder")?.gameObject.SetActive(true);

                this.gameObject.SetActive(false);
            });

            var gameModeButton =
                this.transform.Find("LeftPanel/Bottom/FFA/Scroll View/Viewport/Content/GamemodeButton (short)").gameObject;

            var ffaGamemodes = GameModeManager.Handlers.Keys
                .Where(k => !GameModeManager.Handlers[k].AllowTeams).OrderByDescending(k => GameModeManager.Handlers[k].Name.ToLower())
                .Where(k => k != GameModeManager.SandBoxID && k != GameModeManager.ArmsRaceID);

            this.ffaContent = this.transform.Find("LeftPanel/Bottom/FFA/Scroll View/Viewport/Content");


            foreach (string gamemode in ffaGamemodes)
            {
                this.CreateGmButton(gamemode,gameModeButton, this.ffaContent);
            }

            var teamGamemodes = GameModeManager.Handlers.Keys
                .Where(k => GameModeManager.Handlers[k].AllowTeams).OrderByDescending(k => GameModeManager.Handlers[k].Name.ToLower())
                .Where(k => k != GameModeManager.SandBoxID && k != GameModeManager.ArmsRaceID);
            
            this.teamContent = this.transform.Find("LeftPanel/Bottom/TEAM/Scroll View/Viewport/Content");
            
            foreach (string gamemode in teamGamemodes)
            {
                this.CreateGmButton(gamemode,gameModeButton, this.teamContent);
            }

            var video = this.transform.Find("RightPanel/Top/gameModeVideo").gameObject;
            var renderTexture = new RenderTexture(1920, 1080, 24);
            video.GetComponent<VideoPlayer>().targetTexture = renderTexture;
            video.GetComponent<RawImage>().texture = renderTexture;

            var mixers = Resources.FindObjectsOfTypeAll<AudioMixerGroup>();
            var mixer = mixers.First(m => m.name == "SFX");
            video.GetComponent<AudioSource>().outputAudioMixerGroup = mixer;

            var barParent = new GameObject("BarParent");
            
            this.topBar = Object.Instantiate(ListMenu.instance.bar, barParent.transform);
            this.topBar.transform.SetZPosition(0);
            
            this.transform.Find("LeftPanel/Top/FFA(short)").GetComponent<Button>().onClick.AddListener(() =>
            {
                this.ExecuteAfterFrames(1, () =>
                {
                    var position = this.transform.Find("LeftPanel/Top/FFA(short)").GetChild(0).position;
                    this.topBar.transform.position = position;
                    this.topBar.transform.localScale = new Vector3(23, 1.85f);
                    
                    if (!GameModeManager.CurrentHandler.AllowTeams)
                    {
                        this.bottomBar.SetActive(true);
                    }
                    else
                    {
                        this.bottomBar.SetActive(false);
                    }
                });
            });
            
            this.transform.Find("LeftPanel/Top/TEAM(short)").GetComponent<Button>().onClick.AddListener(() =>
            {
                this.ExecuteAfterFrames(1, () =>
                {
                    var position = this.transform.Find("LeftPanel/Top/TEAM(short)").GetChild(0).position;
                    this.topBar.transform.position = position;
                    this.topBar.transform.localScale = new Vector3(23, 1.85f);
                    
                    if (!GameModeManager.CurrentHandler.AllowTeams)
                    {
                        this.bottomBar.SetActive(false);
                    }
                    else
                    {
                        this.bottomBar.SetActive(true);
                    }
                });
            });
            
            this.bottomBar = Object.Instantiate(ListMenu.instance.bar, barParent.transform);
            this.bottomBar.transform.SetZPosition(0);
        }

        public void UpdateInspector()
        {
            var curGamemode = GameModeManager.CurrentHandler;
            
            // Set top text
            this.transform.Find("RightPanel/Top/Text").GetComponent<TextMeshProUGUI>().text =
                curGamemode.Name.ToUpper();
            
            // Set video
            this.transform.Find("RightPanel/Top/gameModeVideo").GetComponent<VideoPlayer>().url = curGamemode.Settings.TryGetValue("videoURL", out object url) ? (string) url : "https://media.giphy.com/media/lcngwaPCkqFbfhzrsH/giphy.mp4"; 
            this.transform.Find("RightPanel/Top/gameModeVideo").GetComponent<VideoPlayer>().Play();
            
            // Set description
            var descriptionObj = this.transform.Find("RightPanel/Bottom/Panel/Text").GetComponent<TextMeshProUGUI>();
            descriptionObj.text = curGamemode.Settings.TryGetValue("description", out object description) ? (string) description : "";
            descriptionObj.fontSizeMax = curGamemode.Settings.TryGetValue("descriptionFontSize", out object fontSize) ? (int) fontSize : 30;
            
        }

        public void CreateGmButton(string handlerID, GameObject gameModeButton, Transform parent)
        {
            var curGmButton = Object.Instantiate(gameModeButton, parent);
            curGmButton.name = handlerID+"(short)";
            curGmButton.GetComponentInChildren<TextMeshProUGUI>(true).text = GameModeManager.Handlers[handlerID].Name.ToUpper();
            curGmButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (GameManager.instance.isPlaying || !curGmButton.activeInHierarchy) { return; }
                GameModeManager.SetGameMode(handlerID);
                this.ExecuteAfterFrames(1, () =>
                {
                    this.bottomBar.transform.position = curGmButton.transform.position;
                    this.bottomBar.transform.localScale = new Vector3(23, 3.7f);
                });

                PrivateRoomHandler.instance.UnreadyAllPlayers();
                PrivateRoomHandler.instance.ExecuteAfterGameModeInitialized(handlerID, () =>
                {
                    RWFMod.Log($"SET GAMEMODE: {handlerID}");
                    PrivateRoomHandler.instance.SyncMethod(nameof(PrivateRoomHandler.SetGameSettings), null, GameModeManager.CurrentHandlerID, GameModeManager.CurrentHandler.Settings);
                    PrivateRoomHandler.instance.HandleTeamRules();
                });
                
                this.bottomBar.SetActive(true);
                
                this.UpdateInspector();
            });
            curGmButton.SetActive(true);
        }

        private IEnumerator Swoop(GameObject obj,int moveWidth, bool back, Action onFinished = null)
        {
            var rect = obj.GetComponent<RectTransform>();
            float t = 0;
            var startPos = rect.anchoredPosition;
            var endPos =back ? rect.anchoredPosition + new Vector2(moveWidth, 0) : rect.anchoredPosition - new Vector2(moveWidth, 0);
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t*4);
                yield return null;
            }
            onFinished?.Invoke();
        }
    } 
}