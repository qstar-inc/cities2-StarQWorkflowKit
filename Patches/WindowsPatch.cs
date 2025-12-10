////using System;
////using System.Collections.Generic;
////using System.Linq;
////using System.Reflection;
////using System.Reflection.Emit;
////using System.Runtime.InteropServices;
////using System.Text;
////using System.Threading.Tasks;
////using HarmonyLib;
////using PDX.SDK.Contracts.Service.Mods;
////using PDX.SDK.Contracts.Service.Mods.Enums;
////using PDX.SDK.Util;

////namespace StarQWorkflowKit.Patches
////{
////    [HarmonyPatch]
////    public static class ValidateStagingDataPatch
////    {
////        static MethodBase TargetMethod()
////        {
////            // load type by name even if internal
////            var type = AccessTools.TypeByName(
////                "PDX.SDK.Internal.Service.Mods.Services.ModsPublishingService"
////            );

////            if (type == null)
////            {
////                Mod.log.Info("ModsPublishingService NOT FOUND");
////                return null;
////            }

////            var nestedType = type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
////                .FirstOrDefault(t =>
////                    t.Name.Contains("ValidateStagingData") && t.Name.Contains("d__")
////                );

////            if (nestedType == null)
////            {
////                Mod.log.Info("Async nested type NOT FOUND");
////                return null;
////            }

////            var moveNext = AccessTools.Method(nestedType, "MoveNext");

////            if (moveNext == null)
////            {
////                Mod.log.Info("MoveNext NOT FOUND");
////                return null;
////            }

////            return moveNext;
////        }

////        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
////        {
////            var codes = new List<CodeInstruction>(instructions);

////            var getDetailsMethod = AccessTools.Method(
////                AccessTools.TypeByName(
////                    "PDX.SDK.Internal.Service.Mods.Interfaces.IModsBackendApiService"
////                ),
////                "GetDetails"
////            );

////            var toModsPlatformStringMethod = AccessTools.Method(
////                typeof(PlatformExtensions),
////                "ToModsPlatformString"
////            );
////            if (toModsPlatformStringMethod == null)
////                Mod.log.Info("toModsPlatformStringMethod IS NULL");

////            var toModsPlatformStringWindows = AccessTools.Method(
////                typeof(ModPlatform),
////                "ToModsPlatformString"
////            );
////            if (toModsPlatformStringWindows == null)
////                Mod.log.Info("toModsPlatformStringWindows IS NULL");

////            for (int i = 0; i < codes.Count; i++)
////            {
////                if (codes[i].Calls(toModsPlatformStringMethod))
////                {
////                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4, (int)ModPlatform.Windows);
////                    codes.Insert(
////                        i + 1,
////                        new CodeInstruction(OpCodes.Call, toModsPlatformStringWindows)
////                    );
////                }
////            }

////            return codes;
////        }
////    }
////}

//using System;
//using System.Linq;
//using System.Reflection;
//using HarmonyLib;

//namespace StarQWorkflowKit.Patches
//{
//    [HarmonyPatch]
//    public static class ValidateStagingData_PrefixPatch
//    {
//        // Target the original async method (the compiler-generated state machine will be created later).
//        static MethodBase TargetMethod()
//        {
//            var svcType = AccessTools.TypeByName(
//                "PDX.SDK.Internal.Service.Mods.Services.ModsPublishingService"
//            );
//            if (svcType == null)
//            {
//                // fallback: try without namespace
//                svcType = AccessTools.TypeByName("ModsPublishingService");
//            }

//            if (svcType == null)
//            {
//                Mod.log?.Info(
//                    "ValidateStagingData_PrefixPatch: ModsPublishingService type NOT FOUND"
//                );
//                return null;
//            }

//            var method = AccessTools.Method(svcType, "ValidateStagingData");
//            if (method == null)
//            {
//                Mod.log?.Info(
//                    "ValidateStagingData_PrefixPatch: ValidateStagingData method NOT FOUND"
//                );
//                return null;
//            }

//            Mod.log?.Info("ValidateStagingData_PrefixPatch: targeting " + method);
//            return method;
//        }

//        // Prefix runs before the async method body (so before MoveNext inside the state machine).
//        // We must accept the stagingData parameter — but the param type is internal, so accept it as object.
//        static void Prefix(object stagingData)
//        {
//            try
//            {
//                if (stagingData == null)
//                {
//                    Mod.log?.Info("ValidateStagingData_PrefixPatch: stagingData is null");
//                    return;
//                }

//                // Get MetaData property (may be a field or property; try property first)
//                var sdType = stagingData.GetType();

//                // Try property "MetaData"
//                var metaProp = sdType.GetProperty(
//                    "MetaData",
//                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
//                );
//                object meta = null;
//                if (metaProp != null)
//                {
//                    meta = metaProp.GetValue(stagingData);
//                }
//                else
//                {
//                    // fallback: field "MetaData"
//                    var metaField = sdType.GetField(
//                        "MetaData",
//                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
//                    );
//                    if (metaField != null)
//                        meta = metaField.GetValue(stagingData);
//                }

//                if (meta == null)
//                {
//                    Mod.log?.Info("ValidateStagingData_PrefixPatch: MetaData is null or not found");
//                    return;
//                }

//                // Find the OperatingSystem member inside MetaData (property preferred)
//                var metaType = meta.GetType();
//                var osProp = metaType.GetProperty(
//                    "OperatingSystem",
//                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
//                );
//                if (osProp != null)
//                {
//                    // Determine the ModPlatform enum Type
//                    var modPlatformType =
//                        AccessTools.TypeByName("ModPlatform")
//                        ?? AccessTools.TypeByName("PDX.SDK.Internal.Service.Mods.ModPlatform")
//                        ?? AccessTools.TypeByName("PDX.SDK.Internal.Common.ModPlatform"); // try common fallbacks

//                    if (modPlatformType == null)
//                    {
//                        // last resort: infer type from property's type
//                        modPlatformType = osProp.PropertyType;
//                    }

//                    // Convert the "Windows" enum value for that enum type
//                    object windowsVal = null;
//                    if (modPlatformType.IsEnum)
//                    {
//                        windowsVal = Enum.Parse(modPlatformType, "Windows");
//                    }
//                    else
//                    {
//                        // if not enum (unexpected), bail
//                        Mod.log?.Info(
//                            "ValidateStagingData_PrefixPatch: modPlatformType is not enum: "
//                                + modPlatformType
//                        );
//                        return;
//                    }

//                    osProp.SetValue(meta, windowsVal);
//                    Mod.log?.Info(
//                        "ValidateStagingData_PrefixPatch: Set MetaData.OperatingSystem = "
//                            + windowsVal
//                    );
//                    return;
//                }

//                // fallback: try field "OperatingSystem"
//                var osField = metaType.GetField(
//                    "OperatingSystem",
//                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
//                );
//                if (osField != null)
//                {
//                    var modPlatformType =
//                        AccessTools.TypeByName("ModPlatform") ?? osField.FieldType;
//                    object windowsVal = Enum.Parse(modPlatformType, "Windows");
//                    osField.SetValue(meta, windowsVal);
//                    Mod.log?.Info(
//                        "ValidateStagingData_PrefixPatch: Set MetaData.OperatingSystem(field) = "
//                            + windowsVal
//                    );
//                    return;
//                }

//                Mod.log?.Info(
//                    "ValidateStagingData_PrefixPatch: OperatingSystem member not found on MetaData"
//                );
//            }
//            catch (Exception ex)
//            {
//                Mod.log?.Info(
//                    "ValidateStagingData_PrefixPatch: exception patching stagingData → " + ex
//                );
//            }
//        }
//    }
//}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Reflection.Emit;
//using System.Runtime.CompilerServices;
//using Game.SceneFlow;
//using HarmonyLib;
//using StarQ.Shared.Extensions;

//[HarmonyPatch]
//public static class Patch_RegisterPdxSdk
//{
//    static MethodBase TargetMethod()
//    {
//        // 1. Get the outer method that you see in dnSpy
//        var outer = typeof(GameManager).GetMethod(
//            "<RegisterPdxSdk>g__RegisterDatabase|104_10",
//            BindingFlags.NonPublic | BindingFlags.Instance
//        );

//        if (outer == null)
//            LogHelper.SendLog("Could not find outer method");

//        // 2. Look for AsyncStateMachineAttribute
//        var attr = outer.GetCustomAttribute<AsyncStateMachineAttribute>();
//        if (attr == null)
//            LogHelper.SendLog("AsyncStateMachineAttribute not found");

//        // 3. This is the real state machine type
//        var stateMachineType = attr.StateMachineType;
//        if (stateMachineType == null)
//            LogHelper.SendLog("State machine type is null");

//        // 4. Patch MoveNext()
//        var moveNext = stateMachineType.GetMethod(
//            "MoveNext",
//            BindingFlags.NonPublic | BindingFlags.Instance
//        );

//        if (moveNext == null)
//            LogHelper.SendLog("MoveNext() not found");

//        LogHelper.SendLog("Patching " + moveNext);
//        return moveNext;
//    }

//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
//    {
//        var list = instructions.ToList();

//        // Look for the "Defer Mods load" string
//        for (int i = 0; i < list.Count; i++)
//        {
//            if (list[i].opcode == OpCodes.Bge || list[i].opcode == OpCodes.Bge_S)
//            {
//                // Replace BGE with BR (always go to PRELOAD)
//                list[i].opcode = OpCodes.Br;
//            }
//            //if (
//            //    list[i].opcode == OpCodes.Ldstr
//            //    && list[i].operand is string s
//            //    && s.Contains("Defer Mods load")
//            //)
//            //{
//            //    LogHelper.SendLog(s);
//            //    // NOP the entire else block or redirect it
//            //    // Easiest: replace the string + call + following code with NOPs
//            //    for (int j = i; j < i + 20 && j < list.Count; j++)
//            //    {
//            //        list[j].opcode = OpCodes.Nop;
//            //        list[j].operand = null;
//            //    }
//            //}
//        }
//        LogHelper.SendLog(list.Count);

//        return list;
//    }
//}
