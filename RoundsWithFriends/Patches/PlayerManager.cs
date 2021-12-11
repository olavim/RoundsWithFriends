using HarmonyLib;
using Photon.Pun;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sonigon;
using System.Linq;
using UnboundLib;
using System;
using System.Reflection.Emit;
using RWF.Algorithms;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(PlayerManager), "PlayerJoined")]
    class PlayerManager_Patch_PlayerJoined
    {
        static void Postfix(Player player)
        {
            if (!PhotonNetwork.OfflineMode)
            {
                PrivateRoomHandler.instance.PlayerJoined(player);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "RemovePlayers")]
    class PlayerManager_Patch_RemovePlayers
    {
        static void Prefix(PlayerManager __instance)
        {
            if (!PhotonNetwork.OfflineMode)
            {
                var players = __instance.players;

                for (int i = players.Count - 1; i >= 0; i--)
                {
                    if (players[i].data.view.AmOwner)
                    {
                        PhotonNetwork.Destroy(players[i].data.view);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "GetOtherPlayer")]
    class PlayerManager_Patch_GetOtherPlayer
    {
        static bool Prefix(PlayerManager __instance, Player asker, ref Player __result)
        {
            __result = __instance.GetClosestPlayerInOtherTeam(asker.transform.position, asker.teamID, false);
            return false;
        }
    }

    [HarmonyPatch]
    class PlayerManager_Patch_Move
    {
        static Type GetNestedMoveType()
        {
            var nestedTypes = typeof(PlayerManager).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic);
            Type nestedType = null;

            foreach (var type in nestedTypes)
            {
                if (type.Name.Contains("Move") && !type.Name.Contains("Player"))
                {
                    nestedType = type;
                    break;
                }
            }

            return nestedType;
        }

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(GetNestedMoveType(), "MoveNext");
        }

        static void TrySetEnabled<T>(GameObject gameObject, bool enabled) where T : MonoBehaviour
        {
            if (gameObject?.GetComponent<T>() != null)
            {
                gameObject.GetComponent<T>().enabled = enabled;
            }
        }

        static void SetCollidersActive(PlayerVelocity player, bool active)
        {
            GameObject col = player.gameObject.transform.Find("ObjectCollider")?.gameObject;

            col?.SetActive(active);

            if (player?.gameObject?.GetComponent<CircleCollider2D>() != null)
            {
                player.gameObject.GetComponent<CircleCollider2D>().enabled = active;
            }

            TrySetEnabled<CollisionChecker>(player?.gameObject, active);
            TrySetEnabled<HealthHandler>(player?.gameObject, active);
            TrySetEnabled<PlayerCollision>(player?.gameObject, active);
            TrySetEnabled<LegRaycasters>(player?.gameObject?.transform?.Find("Limbs/LegStuff")?.gameObject, active);

            // Find the OutOfBoundsHandler for this player
            if (player?.GetComponent<Player>() != null)
            {
                OutOfBoundsHandler[] ooBs = UnityEngine.GameObject.FindObjectsOfType<OutOfBoundsHandler>();
                foreach (OutOfBoundsHandler ooB in ooBs)
                {
                    if (((CharacterData) ooB.GetFieldValue("data")).player.playerID == player.GetComponent<Player>().playerID)
                    {
                        ooB.enabled = active;
                        break;
                    }
                }
            }
        }

        // Patch to disable player's ObjectCollider for the entirety of the move
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            var f_simulated = ExtensionMethods.GetFieldInfo(typeof(PlayerVelocity), "simulated");
            var f_isKinematic = ExtensionMethods.GetFieldInfo(typeof(PlayerVelocity), "isKinematic");
            var f_player = ExtensionMethods.GetFieldInfo(GetNestedMoveType(), "player");

            var m_setCollidersActive = ExtensionMethods.GetMethodInfo(typeof(PlayerManager_Patch_Move), nameof(PlayerManager_Patch_Move.SetCollidersActive));

            int disable_index = -1;
            int enable_index = -1;
            for (int i = 1; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].opcode == OpCodes.Ldfld && codes[i + 1].LoadsField(f_player) && codes[i + 2].opcode == OpCodes.Ldc_I4_0 && codes[i + 3].opcode == OpCodes.Stfld && codes[i + 3].StoresField(f_simulated) && codes[i + 4].opcode == OpCodes.Ldarg_0 && codes[i + 5].opcode == OpCodes.Ldfld && codes[i + 6].opcode == OpCodes.Ldc_I4_1 && codes[i + 7].opcode == OpCodes.Stfld && codes[i + 7].StoresField(f_isKinematic))
                {
                    disable_index = i - 1;
                }
                else if (codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].opcode == OpCodes.Ldfld && codes[i + 1].LoadsField(f_player) && codes[i + 2].opcode == OpCodes.Ldc_I4_1 && codes[i + 3].opcode == OpCodes.Stfld && codes[i + 3].StoresField(f_simulated) && codes[i + 4].opcode == OpCodes.Ldarg_0 && codes[i + 5].opcode == OpCodes.Ldfld && codes[i + 6].opcode == OpCodes.Ldc_I4_0 && codes[i + 7].opcode == OpCodes.Stfld && codes[i + 7].StoresField(f_isKinematic))
                {
                    enable_index = i - 1;
                }
            }
            if (disable_index == -1 || enable_index == -1)
            {
                throw new Exception("[OBJECTCOLLIDER PATCH] INSTRUCTION NOT FOUND");
            }
            else
            {
                codes.Insert(disable_index, new CodeInstruction(OpCodes.Ldarg_0)); // Load the PlayerManager.<Move>d__40 instance onto the stack [PlayerManager.<Move>d__40, ...]
                codes.Insert(disable_index + 1, new CodeInstruction(OpCodes.Ldfld, f_player)); // Load PlayerManager.<Move>d__40::player onto the stack (pops PlayerManager.<Move>d__40 off the stack) [PlayerManager.<Move>d__40::player, ...]
                codes.Insert(disable_index + 2, new CodeInstruction(OpCodes.Ldc_I4_0)); // Load 0 onto the stack [0, PlayerManager.<Move>d__40::player, ...]
                codes.Insert(disable_index + 3, new CodeInstruction(OpCodes.Call, m_setCollidersActive)); // Calls SetObjectColliderActive, taking the parameters off the top of the stack, leaving it how we found it [ ... ]

                codes.Insert(enable_index, new CodeInstruction(OpCodes.Ldarg_0)); // Load the PlayerManager.<Move>d__40 instance onto the stack [PlayerManager.<Move>d__40, ...]
                codes.Insert(enable_index + 1, new CodeInstruction(OpCodes.Ldfld, f_player)); // Load PlayerManager.<Move>d__40::player onto the stack (pops PlayerManager.<Move>d__40 off the stack) [PlayerManager.<Move>d__40::player, ...]
                codes.Insert(enable_index + 2, new CodeInstruction(OpCodes.Ldc_I4_1)); // Load 1 onto the stack [1, PlayerManager.<Move>d__40::player, ...]
                codes.Insert(enable_index + 3, new CodeInstruction(OpCodes.Call, m_setCollidersActive)); // Calls SetObjectColliderActive, taking the parameters off the top of the stack, leaving it how we found it [ ... ]
            }

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "MovePlayers")]
    class PlayerManager_Patch_MovePlayers
    {
        static bool Prefix(PlayerManager __instance, SpawnPoint[] spawnPoints)
        {
            __instance.StartCoroutine(PlayerManager_Patch_MovePlayers.WaitForMapToLoad(__instance, spawnPoints));
            return false;
        }
        static bool MapHasValidGround(Map map)
        {
            if (!(bool) map.GetFieldValue("hasCalledReady")) { return false; }

            foreach (Collider2D collider in map.gameObject.GetComponentsInChildren<Collider2D>())
            {
                if (collider.enabled)
                {
                    return true;
                }
            }
            return false;
        }

        static IEnumerator WaitForMapToLoad(PlayerManager __instance, SpawnPoint[] spawnPoints)
        {
            // Wait until the map has solid ground
            yield return new WaitUntil(() => PlayerManager_Patch_MovePlayers.MapHasValidGround(MapManager.instance.currentMap?.Map));

            // 10 extra frames to make the game happy, this is necessary for dynamic objects to be registered as valid ground
            for (int _ = 0; _ < 10; _++)
            {
                yield return null;
            }

            var spawnDictionary = GeneralizedSpawnPositions.GetSpawnDictionary(__instance.players, spawnPoints);

            for (int i = 0; i < __instance.players.Count; i++)
            {

                __instance.StartCoroutine((IEnumerator) typeof(PlayerManager).InvokeMember("Move",
                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                    BindingFlags.NonPublic, null, __instance, new object[] { __instance.players[i].data.playerVel, (Vector3) spawnDictionary[__instance.players[i]] }));

                int j = i % __instance.soundCharacterSpawn.Length;

                SoundManager.Instance.Play(__instance.soundCharacterSpawn[j], __instance.players[i].transform);
            }
        }
    }
}
