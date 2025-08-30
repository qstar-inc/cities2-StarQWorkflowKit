using System;
using System.Collections.Generic;
using System.Reflection;
using Game.UI.Localization;
using Game.UI.Widgets;
using HarmonyLib;

[HarmonyPatch(typeof(EnumFieldBuilders), "BuildMembers")]
public static class Patch_BuildMembers
{
    static bool Prefix(
        Type memberType,
        Converter<object, ulong> fromObject,
        ref EnumMember[] __result
    )
    {
        bool isFlags = memberType.GetCustomAttribute<FlagsAttribute>() != null;
        FieldInfo[] fields = memberType.GetFields(BindingFlags.Static | BindingFlags.Public);
        List<EnumMember> list = new(fields.Length);

        foreach (FieldInfo fieldInfo in fields)
        {
            object value = fieldInfo.GetValue(null);
            ulong num = fromObject(value);
            if (!isFlags || num != 0UL)
            {
                list.Add(
                    new EnumMember(
                        fromObject(value),
                        LocalizedString.Value($"{fieldInfo.Name} [*H]"),
                        false
                    )
                );
            }
        }

        __result = list.ToArray();
        return false;
    }
}
