using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using Photon.Pun;
using UnboundLib;

namespace RWF
{
    public static class SyncMethodStatic
    {
        private static readonly ConditionalWeakTable<Type, HashSet<Tuple<int, string>>> pendingRequests = new ConditionalWeakTable<Type, HashSet<Tuple<int, string>>>();

        /// <summary>
        ///     Executes a method as an UnboundRPC for the specified actors, and marks the actors as waiting for response.
        /// </summary>
        /// <param name="methodName">Method to execute as an UnboundRPC</param>
        /// <param name="actors">Array of actor numbers to execute the method for and mark as waiting for response. Null means all connected actors</param>
        /// <param name="data">Arguments for the UnboundRPC method</param>
        /// <returns></returns>
        public static Coroutine SyncMethod(Type staticType, string methodName, int[] actors, params object[] data)
        {
            return RWFMod.instance.StartCoroutine(SyncMethodStatic.SyncMethodCoroutine(staticType, methodName, actors, data));
        }

        public static Coroutine SyncMethod(Type staticType, string methodName, int actor, params object[] data)
        {
            return SyncMethodStatic.SyncMethod(staticType, methodName, new int[] { actor }, data);
        }

        private static IEnumerator SyncMethodCoroutine(Type staticType, string methodName, int[] actors, params object[] data)
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom == null)
            {
                NetworkingManager.RPC(staticType, methodName, data);
                yield break;
            }

            if (actors == null)
            {
                actors = PhotonNetwork.CurrentRoom.Players.Values.ToList().Select(p => p.ActorNumber).ToArray();
            }

            foreach (int actor in actors)
            {
                SyncMethodStatic.GetPendingRequests(staticType).Add(new Tuple<int, string>(actor, methodName));
            }

            NetworkingManager.RPC(staticType, methodName, data);

            while (SyncMethodStatic.GetPendingRequests(staticType).Where(r => r.Item2 == methodName).Any(r => actors.Contains(r.Item1)))
            {
                yield return null;
            }
        }

        public static HashSet<Tuple<int, string>> GetPendingRequests(Type staticType)
        {
            return pendingRequests.GetOrCreateValue(staticType);
        }

        public static void ClearPendingRequests(Type staticType, int actor)
        {
            var requests = pendingRequests.GetOrCreateValue(staticType);

            foreach (var key in requests.ToList().Where(t => t.Item1 == actor))
            {
                requests.Remove(new Tuple<int, string>(actor, key.Item2));
            }
        }

        public static void RemovePendingRequest(Type staticType, int actor, string methodName)
        {
            var requests = pendingRequests.GetOrCreateValue(staticType);
            requests.Remove(new Tuple<int, string>(actor, methodName));
        }
    }
}
