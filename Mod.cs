using System;
using System.Collections.Generic;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;
using StarQ.Shared.Extensions;

namespace StarQWorkflowKit
{
    public class Mod : IMod
    {
        public static string Id = nameof(StarQWorkflowKit);
        public static string Name = "StarQ's Workflow Kit";
        public static string Version = Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version.ToString(3);
        public static string Author = "StarQ";

        public static string time = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
        public static ILog log = LogManager.GetLogger($"{Id}").SetShowsErrorsInUI(false);
        public static Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            LogHelper.Init(Id, log);
            LocaleHelper.Init(Id, GetReplacements);
            foreach (var item in new LocaleHelper($"{Id}.Locale.json").GetAvailableLanguages())
            {
                GameManager.instance.localizationManager.AddSource(item.LocaleId, item);
            }

            GameManager.instance.localizationManager.onActiveDictionaryChanged +=
                LocaleHelper.OnActiveDictionaryChanged;

            var harmony = new Harmony("StarQ.WorkflowKit");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();

            AssetDatabase.global.LoadSettings(Id, m_Setting, new Setting(this));
            updateSystem.UpdateAfter<EditorCategoryBuilder>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateAfter<EditorCategoryBuilder>(SystemUpdatePhase.UIUpdate);
        }

        public void OnDispose()
        {
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
        }

        public static Dictionary<string, string> GetReplacements()
        {
            return new()
            {
                {
                    "GetSupportedLocales",
                    "- **"
                        + string.Join(
                            "**\n- **",
                            GameManager.instance.localizationManager.GetSupportedLocales()
                        )
                        + "**"
                },
            };
        }
    }
}
