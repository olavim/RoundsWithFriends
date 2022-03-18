using System;
using HarmonyLib;
using InControl;
using System.Reflection;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(PlayerActions))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { })]
    class PlayerActionsPatchPlayerActions
    {
        private static void Postfix(PlayerActions __instance)
        {
            __instance.GetAdditionalData().increaseColorID = (PlayerAction) typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                                                BindingFlags.Instance | BindingFlags.InvokeMethod |
                                                                BindingFlags.NonPublic, null, __instance, new object[] { "Increase Team ID" });
            __instance.GetAdditionalData().decreaseColorID = (PlayerAction) typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                                                BindingFlags.Instance | BindingFlags.InvokeMethod |
                                                                BindingFlags.NonPublic, null, __instance, new object[] { "Descrease Team ID" });

        }
    }
    [HarmonyPatch(typeof(PlayerActions), "CreateWithControllerBindings")]
    class PlayerActionsPatchCreateWithControllerBindings
    {
        private static void Postfix(ref PlayerActions __result)
        {
            __result.GetAdditionalData().increaseColorID.AddDefaultBinding(InputControlType.DPadRight);
            __result.GetAdditionalData().decreaseColorID.AddDefaultBinding(InputControlType.DPadLeft);
        }
    }
    [HarmonyPatch(typeof(PlayerActions), "CreateWithKeyboardBindings")]
    class PlayerActionsPatchCreateWithKeyboardBindings
    {
        private static void Postfix(ref PlayerActions __result)
        {
            __result.GetAdditionalData().increaseColorID.AddDefaultBinding(Key.W);
            __result.GetAdditionalData().decreaseColorID.AddDefaultBinding(Key.S);
            __result.GetAdditionalData().increaseColorID.AddDefaultBinding(Key.UpArrow);
            __result.GetAdditionalData().decreaseColorID.AddDefaultBinding(Key.DownArrow);
        }
    }
}
