using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Environment;
using Game.Modding;
using Game.Settings;
using Unity.Entities;
using UnityEngine.Device;

namespace StarQWorkflowKit
{
    [FileLocation("ModsSettings/StarQ/ " + nameof(StarQWorkflowKit))]
    [SettingsUITabOrder(MainTab, AboutTab, LogTab)]
    [SettingsUIGroupOrder(
        Header,
        PrefabSaver,
        PrefabPackager,
        PrefabModifier,
        LocaleMaker,
        EditorModification
    )]
    [SettingsUIShowGroupName(
        PrefabSaver,
        PrefabPackager,
        PrefabModifier,
        LocaleMaker,
        EditorModification
    )]
    public class Setting : ModSetting
    {
        public const string MainTab = "Main";
        public const string Header = "Header";
        public const string PrefabSaver = "Prefab Saver";
        public const string PrefabPackager = "Prefab Packager";
        public const string PrefabModifier = "Prefab Modifier";
        public const string LocaleMaker = "Locale Maker";
        public const string EditorModification = "Editor Modification";

        public const string AboutTab = "About";
        public const string InfoGroup = "Info";
        public const string LogTab = "Log";

        private static readonly PrefabHelper prefab_helper =
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabHelper>();
        private static readonly EditorCategoryBuilder editorCategoryBuilder =
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EditorCategoryBuilder>();

        public Setting(IMod mod)
            : base(mod) { }

        [SettingsUIMultilineText]
        [SettingsUISection(MainTab, Header)]
        public string Disclaimer => string.Empty;

        [SettingsUISection(MainTab, PrefabSaver)]
        [SettingsUITextInput]
        public string ResavePrefabPath { get; set; } = string.Empty;

        [SettingsUISection(MainTab, PrefabSaver)]
        [SettingsUIButton]
        [SettingsUIButtonGroup("ResavePrefab")]
        public bool ResavePrefab
        {
            set { prefab_helper.SaveAsset(ResavePrefabPath, 1); }
        }

        [SettingsUISection(MainTab, PrefabSaver)]
        [SettingsUIButton]
        [SettingsUIButtonGroup("ResavePrefab")]
        public bool ResavePrefabT
        {
            set { prefab_helper.SaveAsset(ResavePrefabPath, 0); }
        }

        [SettingsUISection(MainTab, PrefabSaver)]
        [SettingsUIButton]
        [SettingsUIButtonGroup("ResavePrefab")]
        public bool ResavePrefabB
        {
            set { prefab_helper.SaveAsset(ResavePrefabPath, 3); }
        }

        [SettingsUISection(MainTab, PrefabPackager)]
        [SettingsUITextInput]
        public string CreatePackagePath { get; set; } = string.Empty;

        [SettingsUISection(MainTab, PrefabPackager)]
        [SettingsUIButton]
        [SettingsUIButtonGroup("CreatePackage")]
        public bool CreatePackage
        {
            set { prefab_helper.SaveAsset(CreatePackagePath, 2); }
        }

        [SettingsUISection(MainTab, PrefabPackager)]
        [SettingsUIButton]
        [SettingsUIButtonGroup("CreatePackage")]
        public bool CreatePackageAny
        {
            set { prefab_helper.CreatePackage(CreatePackagePath, direct: true); }
        }

        [SettingsUISection(MainTab, PrefabModifier)]
        [SettingsUITextInput]
        public string Path { get; set; } = string.Empty;

        [SettingsUISection(MainTab, PrefabModifier)]
        public bool AddUIIcon
        {
            set { prefab_helper.AddUIIcon(Path); }
        }

        [SettingsUISection(MainTab, PrefabModifier)]
        public bool AddPlaceableObject
        {
            set { prefab_helper.AddPlaceableObject(Path); }
        }

        [SettingsUISection(MainTab, PrefabModifier)]
        [SettingsUITextInput]
        public string UIGroupToAdd { get; set; } = string.Empty;

        [SettingsUISection(MainTab, PrefabModifier)]
        public bool AddUIGroup
        {
            set { prefab_helper.AddUIGroup(Path, UIGroupToAdd); }
        }

        [SettingsUISection(MainTab, PrefabModifier)]
        [SettingsUITextInput]
        public string EditorAssetCategoryOverride { get; set; } = string.Empty;

        //[SettingsUISection(MainTab, PrefabModifier)]
        //[SettingsUITextInput]
        //public string EditorAssetCategoryOverridePath { get; set; } = string.Empty;

        [SettingsUIButtonGroup("EditorAssetCategoryOverride")]
        [SettingsUISection(MainTab, PrefabModifier)]
        public bool AddEditorAssetCategoryOverrideInclude
        {
            set
            {
                prefab_helper.AddEditorAssetCategoryOverrideInclude(
                    Path,
                    EditorAssetCategoryOverride
                );
            }
        }

        [SettingsUIButtonGroup("EditorAssetCategoryOverride")]
        [SettingsUISection(MainTab, PrefabModifier)]
        public bool AddEditorAssetCategoryOverrideExclude
        {
            set
            {
                prefab_helper.AddEditorAssetCategoryOverrideExclude(
                    Path,
                    EditorAssetCategoryOverride
                );
            }
        }

        //[SettingsUISection(MainTab, PrefabModifier)]
        //[SettingsUITextInput]
        //public string RemoveObsoletesPath { get; set; } = string.Empty;

        [SettingsUISection(MainTab, PrefabModifier)]
        public bool RemoveObsoletes
        {
            set { prefab_helper.RemoveObsoletes(Path); }
        }

        //[SettingsUISection(MainTab, PrefabModifier)]
        //[SettingsUITextInput]
        //public string RemoveSpawnablesPath { get; set; } = string.Empty;

        [SettingsUISection(MainTab, PrefabModifier)]
        public bool RemoveSpawnables
        {
            set { prefab_helper.RemoveSpawnables(Path); }
        }

        [SettingsUISection(MainTab, PrefabModifier)]
        [SettingsUITextInput]
        public string AssetPackToAdd { get; set; } = string.Empty;

        [SettingsUISection(MainTab, PrefabModifier)]
        public bool AddAssetPack
        {
            set { prefab_helper.AddAssetPack(Path, AssetPackToAdd); }
        }

        [SettingsUISection(MainTab, PrefabModifier)]
        public bool RemoveAssetPack
        {
            set { prefab_helper.RemoveAssetPacks(Path); }
        }

        [SettingsUISection(MainTab, LocaleMaker)]
        [SettingsUITextInput]
        public string LangPath { get; set; } = string.Empty;

        [SettingsUISection(MainTab, LocaleMaker)]
        [SettingsUITextInput]
        public string LangId { get; set; } = "en-US";

        [SettingsUISection(MainTab, LocaleMaker)]
        public bool ConvertLocale
        {
            set { prefab_helper.ConvertLocale(LangPath, LangId); }
        }

        [SettingsUISection(MainTab, EditorModification)]
        public bool ShowEditorCatsTypeBased { get; set; } = false;

        [SettingsUISection(MainTab, EditorModification)]
        public bool EnableCats
        {
            set { editorCategoryBuilder.EnableCats(); }
        }

        public override void SetDefaults() { }

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
                    Mod.SendLog($"{e}");
                }
            }
        }

        [SettingsUIButtonGroup("Social")]
        [SettingsUIButton]
        [SettingsUISection(AboutTab, InfoGroup)]
        public bool Discord
        {
            set
            {
                try
                {
                    Application.OpenURL(
                        $"https://discord.com/channels/1024242828114673724/1353366978210824222"
                    );
                }
                catch (Exception e)
                {
                    Mod.SendLog($"{e}");
                }
            }
        }

        [SettingsUIMultilineText]
        [SettingsUIDisplayName(typeof(Mod), nameof(Mod.LogText))]
        [SettingsUISection(LogTab, "")]
        public string LogText => string.Empty;

        [SettingsUISection(LogTab, "")]
        public bool OpenLog
        {
            set
            {
                Task.Run(() =>
                    Process.Start($"{EnvPath.kUserDataPath}/Logs/{nameof(StarQWorkflowKit)}.log")
                );
            }
        }
    }
}
