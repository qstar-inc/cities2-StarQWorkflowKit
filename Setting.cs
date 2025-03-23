using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using System;
using Unity.Entities;
using UnityEngine.Device;

namespace StarQWorkflowKit
{
    [FileLocation(nameof(StarQWorkflowKit))]
    [SettingsUITabOrder(MainTab, AboutTab)]
    [SettingsUIGroupOrder(Header, PrefabSaver, PrefabModifier)]
    [SettingsUIShowGroupName(PrefabSaver, PrefabModifier)]
    public class Setting : ModSetting
    {
        public const string MainTab = "Main";
        public const string Header = "Header";
        public const string PrefabSaver = "Prefab Saver";
        public const string PrefabModifier = "Prefab Modifier";

        public const string AboutTab = "About";
        public const string InfoGroup = "Info";

        private static readonly PrefabHelper prefab_helper = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabHelper>();

        public Setting(IMod mod) : base(mod)
        {

        }

        [SettingsUIMultilineText]
        [SettingsUISection(MainTab, Header)]
        public string Disclaimer => string.Empty;

        [SettingsUISection(MainTab, PrefabSaver)]
        [SettingsUITextInput]
        public string ResavePrefabPath { get; set; } = string.Empty;

        [SettingsUISection(MainTab, PrefabSaver)]
        [SettingsUIButton]
        [SettingsUIButtonGroup("ResavePrefab")]
        public bool ResavePrefab { set { prefab_helper.SaveAsset(ResavePrefabPath, 1); } }

        [SettingsUISection(MainTab, PrefabSaver)]
        [SettingsUIButton]
        [SettingsUIButtonGroup("ResavePrefab")]
        public bool ResavePrefabB { set { prefab_helper.SaveAsset(ResavePrefabPath, 3); } }

        [SettingsUISection(MainTab, PrefabSaver)]
        [SettingsUITextInput]
        public string CreatePackagePath { get; set; } = string.Empty;

        [SettingsUISection(MainTab, PrefabSaver)]
        [SettingsUIButton]
        [SettingsUIButtonGroup("CreatePackage")]
        public bool CreatePackage { set { prefab_helper.SaveAsset(CreatePackagePath, 2); } }

        [SettingsUISection(MainTab, PrefabSaver)]
        [SettingsUIButton]
        [SettingsUIButtonGroup("CreatePackage")]
        public bool CreatePackageAny { set { prefab_helper.CreatePackage(CreatePackagePath); } }

        [SettingsUISection(MainTab, PrefabModifier)]
        [SettingsUITextInput]
        public string AddUIIconPath { get; set; } = string.Empty;

        [SettingsUISection(MainTab, PrefabModifier)]
        public bool AddUIIcon { set { prefab_helper.AddUIIcon(AddUIIconPath); } }

        [SettingsUISection(MainTab, PrefabModifier)]
        [SettingsUITextInput]
        public string EditorAssetCategoryOverride { get; set; } = string.Empty;

        [SettingsUISection(MainTab, PrefabModifier)]
        [SettingsUITextInput]
        public string EditorAssetCategoryOverridePath { get; set; } = string.Empty;

        [SettingsUISection(MainTab, PrefabModifier)]
        public bool AddEditorAssetCategoryOverride { set { prefab_helper.AddEditorAssetCategoryOverride(EditorAssetCategoryOverridePath, EditorAssetCategoryOverride); } }

        [SettingsUISection(MainTab, PrefabModifier)]
        [SettingsUITextInput]
        public string RemoveObsoletesPath { get; set; } = string.Empty;

        [SettingsUISection(MainTab, PrefabModifier)]
        public bool RemoveObsoletes { set { prefab_helper.RemoveObsoletes(RemoveObsoletesPath); } }

        [SettingsUISection(MainTab, PrefabModifier)]
        [SettingsUITextInput]
        public string RemoveSpawnablesPath { get; set; } = string.Empty;

        [SettingsUISection(MainTab, PrefabModifier)]
        public bool RemoveSpawnables { set { prefab_helper.RemoveSpawnables(RemoveSpawnablesPath); } }

        public override void SetDefaults()
        {
        }

        [SettingsUISection(AboutTab, InfoGroup)]
        public string NameText => Mod.Name;

        [SettingsUISection(AboutTab, InfoGroup)]
        public string VersionText => Mod.Version;

        [SettingsUISection(AboutTab, InfoGroup)]
        public string AuthorText => "StarQ";

        [SettingsUIButtonGroup("Social")]
        [SettingsUIButton]
        [SettingsUISection(AboutTab, InfoGroup)]
        public bool BMaCLink
        {
            set
            {
                try
                {
                    Application.OpenURL($"https://buymeacoffee.com/starq");
                }
                catch (Exception e)
                {
                    Mod.log.Info(e);
                }
            }
        }
    }
}
