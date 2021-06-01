using UnityEngine;
using UnboundLib;

namespace RWF
{
    public static class CardBarHandlerExtensions
    {
        public static void Rebuild(this CardBarHandler instance) {
            while (instance.transform.childCount > 3) {
                GameObject.DestroyImmediate(instance.transform.GetChild(3).gameObject);
            }

            int numPlayers = PlayerManager.instance.players.Count;
            int extraPlayers = numPlayers - 2;

            var barGo1 = instance.transform.GetChild(0).gameObject;
            var barGo2 = instance.transform.GetChild(1).gameObject;

            var deltaY = -50;

            var teamSize = Mathf.Ceil(numPlayers / 2f);
            barGo2.transform.localPosition = barGo1.transform.localPosition + new Vector3(0, teamSize * deltaY, 0);

            for (int i = 0; i < extraPlayers; i++) {
                // The card viz component has one object we don't care about
                int baseIndex = i >= 2 ? i + 1 : i;

                var baseGo = instance.transform.GetChild(baseIndex).gameObject;

                var barGo = UnityEngine.Object.Instantiate(baseGo);
                barGo.name = "Bar" + (i + 3);
                barGo.transform.SetParent(instance.transform);
                barGo.transform.localScale = Vector3.one;
                barGo.transform.localPosition = baseGo.transform.localPosition + new Vector3(0, deltaY, 0);
            }

            instance.SetFieldValue("cardBars", instance.GetComponentsInChildren<CardBar>());
        }
    }
}
