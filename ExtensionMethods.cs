using System.Reflection;
using System.Collections;
using System;
using BepInEx.Logging;
using UnityEngine;
using Photon.Realtime;
using Landfall.Network;

namespace RWF
{
    public static class ExtensionMethods
    {
        #region GM_ArmsRace

        public static void SetPlayersNeededToStart(this GM_ArmsRace instance, int num) {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = instance.GetType().GetField("playersNeededToStart", flags);
            field.SetValue(instance, num);
        }

        public static int GetPlayersNeededToStart(this GM_ArmsRace instance) {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = instance.GetType().GetField("playersNeededToStart", flags);
            return (int) field.GetValue(instance);
        }

        #endregion

        #region CardBarHandler

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

            var cardBars = typeof(CardBarHandler).GetField("cardBars", BindingFlags.Instance | BindingFlags.NonPublic);
            cardBars.SetValue(instance, instance.GetComponentsInChildren<CardBar>());
        }

        #endregion

        #region NetworkConnectionHandler

        public static void SetSearchingQuickMatch(this NetworkConnectionHandler instance, bool value) {
            var field = typeof(NetworkConnectionHandler).GetField("m_SearchingQuickMatch", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(instance, value);
        }

        public static bool IsSearchingQuickMatch(this NetworkConnectionHandler instance) {
            var field = typeof(NetworkConnectionHandler).GetField("m_SearchingQuickMatch", BindingFlags.Instance | BindingFlags.NonPublic);
            return (bool) field.GetValue(instance);
        }

        public static void SetSearchingTwitch(this NetworkConnectionHandler instance, bool value) {
            var field = typeof(NetworkConnectionHandler).GetField("m_SearchingTwitch", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(instance, value);
        }

        public static bool IsSearchingTwitch(this NetworkConnectionHandler instance) {
            var field = typeof(NetworkConnectionHandler).GetField("m_SearchingTwitch", BindingFlags.Instance | BindingFlags.NonPublic);
            return (bool) field.GetValue(instance);
        }

        public static void SetForceRegion(this NetworkConnectionHandler instance, bool value) {
            var field = typeof(NetworkConnectionHandler).GetField("m_ForceRegion", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(instance, value);
        }

        public static bool IsSteamConnected(this NetworkConnectionHandler instance) {
            var field = typeof(NetworkConnectionHandler).GetField("m_SteamLobby", BindingFlags.Static | BindingFlags.NonPublic);
            var lobby = (ClientSteamLobby) field.GetValue(null);
            return lobby != null && lobby.IsActive;
        }

        public static void HostPrivate(this NetworkConnectionHandler instance) {
            instance.SetSearchingQuickMatch(false);
            instance.SetSearchingTwitch(false);

            TimeHandler.instance.gameStartTime = 1f;
            RoomOptions options = new RoomOptions();
            options.MaxPlayers = (byte) RWFMod.instance.MaxPlayers;
            options.IsOpen = true;
            options.IsVisible = false;

            var m_DoActionWhenConnected = typeof(NetworkConnectionHandler).GetMethod("DoActionWhenConnected", BindingFlags.Instance | BindingFlags.NonPublic);
            var m_CreateRoom = typeof(NetworkConnectionHandler).GetMethod("CreateRoom", BindingFlags.Instance | BindingFlags.NonPublic);
            Action createRoomFn = () => m_CreateRoom.Invoke(instance, new object[] { options });

            instance.StartCoroutine((IEnumerator) m_DoActionWhenConnected.Invoke(instance, new object[] { createRoomFn }));
        }

        #endregion
    }
}
