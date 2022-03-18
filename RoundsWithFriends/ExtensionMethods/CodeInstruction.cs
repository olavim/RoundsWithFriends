using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace RWF
{
    public static class CodeInstructionExtensions
    {
        public static bool GetsProperty(this CodeInstruction code, PropertyInfo property)
        {
            if (property is null) throw new ArgumentNullException(nameof(property));

            return code.Calls(property.GetMethod);
        }
        public static bool SetsProperty(this CodeInstruction code, PropertyInfo property)
        {
            if (property is null) throw new ArgumentNullException(nameof(property));

            return code.Calls(property.SetMethod);
        }
    }
}
