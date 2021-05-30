using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RWF
{
    static class RoundsResources
    {
        private static TMP_FontAsset _menuFont;
        private static GameObject _flickeringTextPrefab;

        public static TMP_FontAsset MenuFont {
            get {
                if (!_menuFont && MainMenuHandler.instance) {
                    var localGo = MainMenuHandler.instance.transform.Find("Canvas").Find("ListSelector").Find("Main").Find("Group").Find("Local").gameObject;
                    _menuFont = localGo.GetComponentInChildren<TextMeshProUGUI>().font;
                }

                return _menuFont;
            }
        }

        public static GameObject FlickeringTextPrefab {
            get {
                if (!_flickeringTextPrefab) {
                    var go = GameObject.Find("/Game/UI/UI_Game/Canvas/Join");

                    if (go) {
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
    }
}
