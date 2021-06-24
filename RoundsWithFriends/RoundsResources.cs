using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sonigon;
using Sonigon.Internal;
using System.Collections.Generic;

namespace RWF
{
    static class RoundsResources
    {
        private static TMP_FontAsset _menuFont;
        private static GameObject _flickeringTextPrefab;
        private static GameObject _popUpMenuTextPrefab;
        private static Dictionary<string, SoundEvent> _soundCache = new Dictionary<string, SoundEvent>();

        public static TMP_FontAsset MenuFont
        {
            get
            {
                if (!_menuFont && MainMenuHandler.instance)
                {
                    var localGo = MainMenuHandler.instance.transform.Find("Canvas").Find("ListSelector").Find("Main").Find("Group").Find("Local").gameObject;
                    _menuFont = localGo.GetComponentInChildren<TextMeshProUGUI>().font;
                }

                return _menuFont;
            }
        }

        public static GameObject FlickeringTextPrefab
        {
            get
            {
                if (!_flickeringTextPrefab)
                {
                    var go = GameObject.Find("/Game/UI/UI_Game/Canvas/Join");

                    if (go)
                    {
                        _flickeringTextPrefab = GameObject.Instantiate(go);
                        _flickeringTextPrefab.name = "Text";

                        var ps = _flickeringTextPrefab.GetComponentInChildren<GeneralParticleSystem>();
                        ps.loop = true;
                        ps.playOnEnablee = true;
                        ps.playOnAwake = true;
                        ps.StartLooping();

                        _flickeringTextPrefab.GetComponent<Mask>().showMaskGraphic = true;
                    }
                }

                return _flickeringTextPrefab;
            }
        }

        public static GameObject PopUpMenuText
        {
            get
            {
                if (!_popUpMenuTextPrefab)
                {
                    var go = GameObject.Find("Game/UI/UI_Game/Canvas/PopUpHandler/Yes");

                    if (go)
                    {
                        _popUpMenuTextPrefab = GameObject.Instantiate(go);
                        _popUpMenuTextPrefab.name = "Text";

                        var ps = _popUpMenuTextPrefab.GetComponentInChildren<GeneralParticleSystem>();
                        ps.loop = true;
                        ps.playOnEnablee = true;
                        ps.playOnAwake = true;
                        ps.StartLooping();
                    }
                }

                return _popUpMenuTextPrefab;
            }
        }

        public static SoundEvent GetSound(string name)
        {
            if (!RoundsResources._soundCache.ContainsKey(name))
            {
                var soundEvent = GameObject.Find("/SonigonSoundEventPool").transform.Find(name).gameObject?.GetComponent<InstanceSoundEvent>().soundEvent;
                RoundsResources._soundCache.Add(name, soundEvent);
            }

            return RoundsResources._soundCache[name];
        }
    }
}
