using System;
using HarmonyLib;
using InControl;
using System.Reflection;
using RWF.ExtensionMethods;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(PlayerActions))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { })]
    class PlayerActionsPatchPlayerActions
    {
        private static void Postfix(PlayerActions __instance)
        {
            __instance.GetAdditionalData().increaseTeamID = (PlayerAction) typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                    BindingFlags.NonPublic, null, __instance, new object[] { "Increase Team ID" });
            __instance.GetAdditionalData().decreaseTeamID = (PlayerAction) typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                        BindingFlags.Instance | BindingFlags.InvokeMethod |
                        BindingFlags.NonPublic, null, __instance, new object[] { "Descrease Team ID" });

        }
    }
    [HarmonyPatch(typeof(PlayerActions), "CreateWithControllerBindings")]
    class PlayerActionsPatchCreateWithControllerBindings
    {
        private static void Postfix(ref PlayerActions __result)
        {
            __result.GetAdditionalData().increaseTeamID.AddDefaultBinding(InputControlType.DPadRight);
            __result.GetAdditionalData().decreaseTeamID.AddDefaultBinding(InputControlType.DPadLeft);
        }
    }
    [HarmonyPatch(typeof(PlayerActions), "CreateWithKeyboardBindings")]
    class PlayerActionsPatchCreateWithKeyboardBindings
    {
        private static void Postfix(ref PlayerActions __result)
        {
            __result.GetAdditionalData().increaseTeamID.AddDefaultBinding(Key.W);
            __result.GetAdditionalData().decreaseTeamID.AddDefaultBinding(Key.S);
            __result.GetAdditionalData().increaseTeamID.AddDefaultBinding(Key.UpArrow);
            __result.GetAdditionalData().decreaseTeamID.AddDefaultBinding(Key.DownArrow);
        }
    }
}
