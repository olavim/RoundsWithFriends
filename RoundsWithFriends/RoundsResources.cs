using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sonigon;
using Sonigon.Internal;
using System.Collections.Generic;
using UnboundLib;

namespace RWF
{
    static class RoundsResources
    {
        private static TMP_FontAsset _menuFont;
        private static GameObject _staticTextPrefab;
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

        public static GameObject StaticTextPrefab
        {
            get
            {
                if (!_staticTextPrefab)
                {
                    var go = GameObject.Find("/Game/UI/UI_Game/Canvas/Join");

                    if (go)
                    {
                        _staticTextPrefab = GameObject.Instantiate(go);
                        _staticTextPrefab.name = "Text";

                        var ps = _staticTextPrefab.GetComponentInChildren<GeneralParticleSystem>();
                        ps.loop = false;
                        ps.playOnEnablee = false;
                        ps.playOnAwake = false;
                        ps.StartLooping();
                        ps.gameObject.AddComponent<RemoveExtraObjectPool>();
                        ps.gameObject.AddComponent<InitStaticText>();

                        _staticTextPrefab.GetComponent<Mask>().showMaskGraphic = true;
                    }
                }

                return _staticTextPrefab;
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
                        ps.gameObject.AddComponent<RemoveExtraObjectPool>();

                        _flickeringTextPrefab.GetComponent<Mask>().showMaskGraphic = true;
                    }
                }

                return _flickeringTextPrefab;
            }
        }

        class RemoveExtraObjectPool : MonoBehaviour
        {
            void Update()
            {
                if (this.gameObject.activeInHierarchy && this.gameObject.transform.childCount > 1)
                {
                    Destroy(this.gameObject.transform.GetChild(0).gameObject);
                    Destroy(this);
                }
            }
        }

        internal class InitStaticText : MonoBehaviour
        {
            private const int numParts = 20;
            GeneralParticleSystem particleSystem;
            void Start()
            {
                this.particleSystem = this.gameObject.GetComponent<GeneralParticleSystem>();
                if (this.particleSystem == null) { return; }
                this.particleSystem.loop = false;
                this.particleSystem.playOnEnablee = false;
                this.particleSystem.playOnAwake = false;
            }
            void OnEnable()
            {
                this.particleSystem = this.gameObject.GetComponent<GeneralParticleSystem>();
                if (this.particleSystem == null) { return; }
                this.particleSystem.InvokeMethod("Init", new object[] { });
                for (int i = 0; i < InitStaticText.numParts; i++)
                {
                    InitStaticText.CreateParticleStatic(this.particleSystem, i / this.particleSystem.duration);
                }
            }
            private const float staticRandomRotation = 45f;
            private const float staticRandomXPos = 100f;
            private const float staticRandomYPos = 200f;
            private static void CreateParticleStatic(GeneralParticleSystem instance, float currentAnimationTime)
            {
                GameObject spawned = ((ObjectPool) instance.GetFieldValue("particlePool")).GetObject();
                float counter = UnityEngine.Random.Range(0f, instance.particleSettings.lifetime);
                float t = instance.particleSettings.lifetime;
                Vector3 startSize = spawned.transform.localScale;
                Vector3 modifiedStartSize = spawned.transform.localScale * instance.particleSettings.size * instance.sizeMultiplierOverTime.Evaluate(currentAnimationTime * (float) instance.GetFieldValue("sizeMultiplierOverTimeAnimationCurveLength"));
                Image img = spawned.GetComponent<Image>();
                Color startColor = Color.magenta;
                if (img)
                {
                    startColor = img.color;
                }
                if (img)
                {
                    float value = UnityEngine.Random.value;
                    if (instance.particleSettings.color != Color.magenta)
                    {
                        img.color = instance.particleSettings.color;
                    }
                    if (instance.particleSettings.randomColor != Color.magenta)
                    {
                        img.color = Color.Lerp(img.color, instance.particleSettings.randomColor, value);
                    }
                    if (!instance.particleSettings.singleRandomValueColor)
                    {
                        value = UnityEngine.Random.value;
                    }
                    if (instance.particleSettings.randomAddedColor != Color.black)
                    {
                        img.color += Color.Lerp(Color.black, instance.particleSettings.randomAddedColor, value);
                    }
                    if (!instance.particleSettings.singleRandomValueColor)
                    {
                        value = UnityEngine.Random.value;
                    }
                    if (instance.particleSettings.randomAddedSaturation != 0f || instance.saturationMultiplier != 1f)
                    {
                        float h;
                        float num;
                        float v;
                        Color.RGBToHSV(img.color, out h, out num, out v);
                        num += value * instance.particleSettings.randomAddedSaturation;
                        num *= instance.saturationMultiplier;
                        img.color = Color.HSVToRGB(h, num, v);
                    }
                }
                spawned.transform.Rotate(instance.transform.forward * instance.particleSettings.rotation);
                spawned.transform.Rotate(instance.transform.forward * UnityEngine.Random.Range(-staticRandomRotation, staticRandomRotation));
                spawned.transform.localPosition = Vector3.zero;
                spawned.transform.localPosition += instance.transform.up * UnityEngine.Random.Range(-staticRandomYPos, staticRandomYPos);
                spawned.transform.localPosition += instance.transform.right * UnityEngine.Random.Range(-staticRandomXPos, staticRandomXPos);
                spawned.transform.localPosition += instance.transform.forward * UnityEngine.Random.Range(-0.1f, 0.1f);
                if (instance.particleSettings.sizeOverTime.keys.Length > 1)
                {
                    spawned.transform.localScale = modifiedStartSize * instance.particleSettings.sizeOverTime.Evaluate(counter / t * (float) instance.GetFieldValue("sizeOverTimeAnimationCurveLength"));
                }
                float num2 = instance.particleSettings.alphaOverTime.Evaluate(counter / t * (float) instance.GetFieldValue("alphaOverTimeAnimationCurveLength"));
                if (img && img.color.a != num2)
                {
                    img.color = new Color(img.color.r, img.color.g, img.color.b, num2);
                }
                return;
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
