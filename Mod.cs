using System;
using System.IO;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.UI.Localization;
using HarmonyLib;

namespace StarQWorkflowKit
{
    public class Mod : IMod
    {
        public static string Name = "StarQ's Workflow Kit";
        public static string Version = Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version.ToString(3);
        public static string Author = "StarQ";

        public static string time = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
        public static ILog log = LogManager
            .GetLogger($"{nameof(StarQWorkflowKit)}")
            .SetShowsErrorsInUI(false);
        public static Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            //log.Info(nameof(OnLoad));

            //if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            //    log.Info($"Current mod asset at {asset.path}");

            var harmony = new Harmony("StarQ.WorkflowKit");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));

            AssetDatabase.global.LoadSettings(
                nameof(StarQWorkflowKit),
                m_Setting,
                new Setting(this)
            );
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EditorCategoryBuilder>();
            updateSystem.UpdateAfter<EditorCategoryBuilder>(SystemUpdatePhase.PrefabUpdate);
        }

        public void OnDispose()
        {
            //log.Info(nameof(OnDispose));
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
        }

        public static LocalizedString LogText => LocalizedString.Id(logText);
        private static string logText = "Nothing logged yet...";

        public static void SendLog(string message, string level = "info")
        {
            switch (level)
            {
                case "verbose":
                    log.Verbose(message);
                    break;
                case "trace":
                    log.Trace(message);
                    break;
                case "debug":
                    log.Debug(message);
                    break;
                case "info":
                    log.Info(message);
                    break;
                case "warn":
                    log.Warn(message);
                    break;
                case "error":
                    log.Error(message);
                    break;
                case "critical":
                    log.Critical(message);
                    break;
                case "fatal":
                    log.Fatal(message);
                    break;
                case "emergency":
                    log.Emergency(message);
                    break;
                default:
                    log.Info(message);
                    break;
            }
            try
            {
                logText = File.ReadAllText(
                    $"{EnvPath.kUserDataPath}/Logs/{nameof(StarQWorkflowKit)}.log"
                );
            }
            catch (Exception e)
            {
                log.Info(e);
            }
        }
    }
}
