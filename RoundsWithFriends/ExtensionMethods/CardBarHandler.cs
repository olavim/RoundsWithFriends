using UnityEngine;
using UnityEngine.UI.ProceduralImage;
using UnboundLib;
using System.Collections.Generic;
using RWF.ExtensionMethods;

namespace RWF
{
    public static class CardBarHandlerExtensions
    {
        public static void Rebuild(this CardBarHandler instance) {
            while (instance.transform.childCount > 3) {
                GameObject.DestroyImmediate(instance.transform.GetChild(3).gameObject);
            }

            int numPlayers = PlayerManager.instance.players.Count;
            var barGo = instance.transform.GetChild(0).gameObject;

            var deltaY = -50;
            var cardBars = new List<CardBar>();

            for (int i = 0; i < numPlayers; i++) {
                var newBarGo = GameObject.Instantiate(barGo, instance.transform);
                newBarGo.SetActive(true);
                newBarGo.name = "Bar" + (i + 1);
                newBarGo.transform.localScale = Vector3.one;
                newBarGo.transform.localPosition = barGo.transform.localPosition + new Vector3(0, deltaY * i, 0);

                var player = PlayerManager.instance.players.Find(p => p.playerID == i);
                var teamColor = PlayerSkinBank.GetPlayerSkinColors(player.colorID()).backgroundColor;
                newBarGo.transform.GetChild(0).GetChild(0).gameObject.GetComponent<ProceduralImage>().color = new Color(teamColor.r, teamColor.g, teamColor.b, 0.9f);

                cardBars.Add(newBarGo.GetComponent<CardBar>());
            }

            barGo.SetActive(false);
            instance.transform.GetChild(1).gameObject.SetActive(false);

            instance.SetFieldValue("cardBars", cardBars.ToArray());
        }
    }
}
