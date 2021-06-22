using UnboundLib;
using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Photon.Pun;

namespace RWF
{
    public static class ObjectExtensions
    {
        private static readonly ConditionalWeakTable<MonoBehaviour, HashSet<Tuple<int, string>>> pendingRequests = new ConditionalWeakTable<MonoBehaviour, HashSet<Tuple<int, string>>>();

        /// <summary>
        ///     Executes a method as an UnboundRPC for the specified actors, and marks the actors as waiting for response.
        /// </summary>
        /// <param name="methodName">Method to execute as an UnboundRPC</param>
        /// <param name="actors">Array of actor numbers to execute the method for and mark as waiting for response. Null means all connected actors</param>
        /// <param name="data">Arguments for the UnboundRPC method</param>
        /// <returns></returns>
        public static Coroutine SyncMethod(this MonoBehaviour instance, string methodName, int[] actors, params object[] data) {
            return instance.StartCoroutine(instance.SyncMethodCoroutine(methodName, actors, data));
        }

        public static Coroutine SyncMethod(this MonoBehaviour instance, string methodName, int actor, params object[] data) {
            return instance.SyncMethod(methodName, new int[] { actor }, data);
        }

        private static IEnumerator SyncMethodCoroutine(this MonoBehaviour instance, string methodName, int[] actors, params object[] data) {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom == null) {
                NetworkingManager.RPC(instance.GetType(), methodName, data);
                yield break;
            }

            if (actors == null) {
                actors = PhotonNetwork.CurrentRoom.Players.Values.ToList().Select(p => p.ActorNumber).ToArray();
            }

            foreach (int actor in actors) {
                instance.GetPendingRequests().Add(new Tuple<int, string>(actor, methodName));
            }

            NetworkingManager.RPC(instance.GetType(), methodName, data);

            while (instance.GetPendingRequests().Where(r => r.Item2 == methodName).Any(r => actors.Contains(r.Item1))) {
                yield return null;
            }
        }

        public static HashSet<Tuple<int, string>> GetPendingRequests(this MonoBehaviour instance) {
            return pendingRequests.GetOrCreateValue(instance);
        }

        public static void ClearPendingRequests(this MonoBehaviour instance, int actor) {
            var requests = pendingRequests.GetOrCreateValue(instance);

            foreach (var key in requests.ToList().Where(t => t.Item1 == actor)) {
                requests.Remove(new Tuple<int, string>(actor, key.Item2));
            }
        }

        public static void RemovePendingRequest(this MonoBehaviour instance, int actor, string methodName) {
            var requests = pendingRequests.GetOrCreateValue(instance);
            requests.Remove(new Tuple<int, string>(actor, methodName));
        }
    }
}
