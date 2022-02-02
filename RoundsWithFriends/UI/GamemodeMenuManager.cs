using RWF.GameModes;
using System;
using System.Linq;
using TMPro;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
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

        public void Open()
        {
            this.lobbyMenuObject.SetActive(false);
            this.gameObject.SetActive(true);
            this.lobbyMenuObject.transform.parent.parent.parent.Find("UIHolder")?.gameObject.SetActive(false);
            this.topBar.SetActive(true);
            this.bottomBar.SetActive(true);

            this.transform.Find("BACK(short)").GetComponent<ListMenuButton>().OnPointerEnter(null);
            
            // Select the current gamemode category
            if (GameModeManager.CurrentHandler.Settings.TryGetValue("allowTeams", out object allowTeams) && !(bool) allowTeams)
            {
                this.transform.Find("LeftPanel/Top/FFA(short)").GetComponent<Button>().onClick.Invoke();
                this.transform.Find("LeftPanel/Bottom/FFA/Scroll View/Viewport/Content/" + GameModeManager.CurrentHandler.Name+"(short)").GetComponent<Button>().onClick.Invoke();
                
            }
            else
            {
                this.transform.Find("LeftPanel/Top/TEAM(short)").GetComponent<Button>().onClick.Invoke();
                this.transform.Find("LeftPanel/Bottom/TEAM/Scroll View/Viewport/Content/" + GameModeManager.CurrentHandler.Name+"(short)").GetComponent<Button>().onClick.Invoke();
            }
            
            this.UpdateInspector();
        }

        public void Init()
        {
            this.transform.Find("BACK(short)").GetComponent<Button>().onClick.AddListener(() =>
            {
                this.lobbyMenuObject.SetActive(true);
                this.gameObject.SetActive(false);
                this.lobbyMenuObject.transform.Find("ButtonBaseObject(Clone)").GetComponent<ListMenuButton>().OnPointerEnter(null);
                this.topBar.SetActive(false);
                this.bottomBar.SetActive(false);
                this.lobbyMenuObject.transform.parent.parent.parent.Find("UIHolder")?.gameObject.SetActive(true);
            });

            var gameModeButton =
                this.transform.Find("LeftPanel/Bottom/FFA/Scroll View/Viewport/Content/GamemodeButton (short)").gameObject;

            var ffaGamemodes = GameModeManager.Handlers.Keys
                .Where(k => GameModeManager.Handlers[k].Settings.TryGetValue("allowTeams", out object allowTeams) &&
                            !(bool) allowTeams).OrderByDescending(k => GameModeManager.Handlers[k].Name.ToLower())
                .Where(k => k != GameModeManager.SandBoxID && k != GameModeManager.ArmsRaceID);

            this.ffaContent = this.transform.Find("LeftPanel/Bottom/FFA/Scroll View/Viewport/Content");


            foreach (string gamemode in ffaGamemodes)
            {
                this.CreateGmButton(gamemode,gameModeButton, this.ffaContent);
            }

            var teamGamemodes = GameModeManager.Handlers.Keys
                .Where(k => !GameModeManager.Handlers[k].Settings.TryGetValue("allowTeams", out object allowTeams) ||
                            (bool) allowTeams).OrderByDescending(k => GameModeManager.Handlers[k].Name.ToLower())
                .Where(k => k != GameModeManager.SandBoxID && k != GameModeManager.ArmsRaceID);
            
            this.teamContent = this.transform.Find("LeftPanel/Bottom/TEAM/Scroll View/Viewport/Content");
            
            foreach (string gamemode in teamGamemodes)
            {
                this.CreateGmButton(gamemode,gameModeButton, this.teamContent);
            }

            var video = this.transform.Find("RightPanel/Top/gameModeVideo").gameObject;
            var renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            video.GetComponent<VideoPlayer>().targetTexture = renderTexture;
            video.GetComponent<RawImage>().texture = renderTexture;

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
                    
                    if(GameModeManager.CurrentHandler.Settings.TryGetValue("allowTeams", out object allowTeams) && !(bool) allowTeams)
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
                    
                    if(GameModeManager.CurrentHandler.Settings.TryGetValue("allowTeams", out object allowTeams) || (bool) allowTeams)
                    {
                        this.bottomBar.SetActive(true);
                    }
                    else
                    {
                        this.bottomBar.SetActive(false);
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
            this.transform.Find("RightPanel/Top/gameModeVideo").GetComponent<VideoPlayer>().url = curGamemode.Settings.TryGetValue("videoURL", out object url) ? (string) url : "https://media.giphy.com/media/50dtBlALJ5jIgmnasA/giphy.mp4"; 
            this.transform.Find("RightPanel/Top/gameModeVideo").GetComponent<VideoPlayer>().Play();
            
            // Set description
            var descriptionObj = this.transform.Find("RightPanel/Bottom/Panel/Text").GetComponent<TextMeshProUGUI>();
            descriptionObj.text = curGamemode.Settings.TryGetValue("description", out object description) ? (string) description : "";
            descriptionObj.fontSizeMax = curGamemode.Settings.TryGetValue("descriptionFontSize", out object fontSize) ? (int) fontSize : 30;
            
        }

        public void CreateGmButton(string gamemode, GameObject gameModeButton, Transform parent)
        {
            var curGmButton = Object.Instantiate(gameModeButton, parent);
            curGmButton.name = gamemode+"(short)";
            curGmButton.GetComponentInChildren<TextMeshProUGUI>(true).text = GameModeManager.Handlers[gamemode].Name.ToUpper();
            curGmButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                GameModeManager.SetGameMode(gamemode);
                this.ExecuteAfterFrames(1, () =>
                {
                    this.bottomBar.transform.position = curGmButton.transform.position;
                    this.bottomBar.transform.localScale = new Vector3(23, 3.7f);
                });

                PrivateRoomHandler.instance.UnreadyAllPlayers();
                PrivateRoomHandler.instance.ExecuteAfterGameModeInitialized(gamemode, () =>
                {
                    PrivateRoomHandler.instance.SyncMethod(nameof(PrivateRoomHandler.SetGameSettings), null, GameModeManager.CurrentHandlerID, GameModeManager.CurrentHandler.Settings);
                    PrivateRoomHandler.instance.HandleTeamRules();
                });
                
                this.bottomBar.SetActive(true);
                
                this.UpdateInspector();
            });
            curGmButton.SetActive(true);
        }
    } 
}