using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Game.Modding;
using Game.Settings;
using StarQ.Shared.Extensions;
using StarQWorkflowKit.Systems;
using Unity.Entities;

namespace StarQWorkflowKit
{
    [FileLocation("ModsSettings\\StarQ\\" + nameof(StarQWorkflowKit))]
    [SettingsUITabOrder(GeneralTab, AboutTab, LogTab)]
    [SettingsUIGroupOrder(
        Header,
        PrefabSaver,
        //PrefabPackager,
        PrefabModifier,
        LocaleMaker,
        EditorModification
    )]
    [SettingsUIShowGroupName(
        PrefabSaver,
        //PrefabPackager,
        PrefabModifier,
        LocaleMaker,
        EditorModification
    )]
    public class Setting : ModSetting
    {
        public Setting(IMod mod)
            : base(mod) => SetDefaults();

        public const string GeneralTab = "GeneralTab";
        public const string Header = "Header";
        public const string PrefabSaver = "PrefabSaver";

        //public const string PrefabPackager = "PrefabPackager";
        public const string PrefabModifier = "PrefabModifier";
        public const string LocaleMaker = "LocaleMaker";
        public const string EditorModification = "EditorModification";

        public const string AboutTab = "AboutTab";
        public const string InfoGroup = "InfoGroup";
        public const string LogTab = "LogTab";

        private static readonly WorkflowSystem workflowSystem =
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WorkflowSystem>();
        private static readonly PackageSystem packageSystem =
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PackageSystem>();

        [Exclude]
        [SettingsUIHidden]
        public bool IsGameOrEditor => WorldHelper.IsGameOrEditor;

        [SettingsUIMultilineText]
        [SettingsUISection(GeneralTab, Header)]
        public string DisclaimerWarning => string.Empty;

        [SettingsUISection(GeneralTab, PrefabSaver)]
        [SettingsUITextInput]
        public string PrefabPath { get; set; } = string.Empty;

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsGameOrEditor))]
        [SettingsUISection(GeneralTab, PrefabSaver)]
        [SettingsUIButtonGroup("ResavePrefab")]
        public bool ResavePrefab
        {
            set { packageSystem.SaveAsset(PrefabPath, 1); }
        }

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsGameOrEditor))]
        [SettingsUISection(GeneralTab, PrefabSaver)]
        [SettingsUIButtonGroup("ResavePrefab")]
        public bool ResavePrefabT
        {
            set { packageSystem.SaveAsset(PrefabPath, 0); }
        }

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsGameOrEditor))]
        [SettingsUISection(GeneralTab, PrefabSaver)]
        [SettingsUIButtonGroup("ResavePrefab")]
        public bool ResavePrefabB
        {
            set { packageSystem.SaveAsset(PrefabPath, 3); }
        }

        //[SettingsUISection(GeneralTab, PrefabPackager)]
        //[SettingsUITextInput]
        //public string CreatePackagePath { get; set; } = string.Empty;

        //[SettingsUISection(MainTab, PrefabPackager)]
        //[SettingsUIButton]
        //[SettingsUIButtonGroup("CreatePackage")]
        //public bool CreatePackage
        //{
        //    set { prefab_helper.SaveAsset(CreatePackagePath, 2); }
        //}

        [SettingsUISection(GeneralTab, PrefabSaver)]
        public bool UseCustomDeps { get; set; } = false;

        [SettingsUIHideByCondition(typeof(Setting), nameof(UseCustomDeps), true)]
        [SettingsUISection(GeneralTab, PrefabSaver)]
        [SettingsUITextInput]
        public string DepsMod { get; set; } = "";

        [SettingsUIHideByCondition(typeof(Setting), nameof(UseCustomDeps), true)]
        [SettingsUISection(GeneralTab, PrefabSaver)]
        [SettingsUITextInput]
        public string DepsDlc { get; set; } = "";

        //[SettingsUISection(GeneralTab, PrefabPackager)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsGameOrEditor))]
        [SettingsUISection(GeneralTab, PrefabSaver)]
        [SettingsUIButtonGroup("CreatePackage")]
        public bool CreatePackage
        {
            set { packageSystem.CreatePackage(PrefabPath, direct: true); }
        }

        [SettingsUISection(GeneralTab, PrefabSaver)]
        public bool EnableManualUpload { get; set; } = false;

        [SettingsUISection(GeneralTab, PrefabModifier)]
        [SettingsUITextInput]
        public string Path { get; set; } = string.Empty;

        [SettingsUISection(GeneralTab, PrefabModifier)]
        [SettingsUITextInput]
        public string AssetPackToAdd { get; set; } = string.Empty;

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsGameOrEditor))]
        [SettingsUIButtonGroup("AssetPack")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool AddAssetPack
        {
            set { workflowSystem.AddAssetPack(Path, AssetPackToAdd); }
        }

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsGameOrEditor))]
        [SettingsUIButtonGroup("AssetPack")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool RemoveAssetPack
        {
            set { workflowSystem.RemoveAssetPack(Path, AssetPackToAdd); }
        }

        [SettingsUISection(GeneralTab, PrefabModifier)]
        [SettingsUITextInput]
        public string UIGroupToAdd { get; set; } = string.Empty;

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsGameOrEditor))]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool AddUIGroup
        {
            set { workflowSystem.AddUIGroup(Path, UIGroupToAdd); }
        }

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsGameOrEditor))]
        [SettingsUIButtonGroup("RenamePrefab")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool GetListOfPrefabs
        {
            set { workflowSystem.GetListOfPrefabs(Path); }
        }

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsGameOrEditor))]
        [SettingsUIButtonGroup("RenamePrefab")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool RenamePrefab
        {
            set { workflowSystem.RenamePrefab(Path); }
        }

        [SettingsUISection(GeneralTab, PrefabModifier)]
        [SettingsUITextInput]
        public string EditorAssetCategoryOverride { get; set; } = string.Empty;

        //[SettingsUISection(MainTab, PrefabModifier)]
        //[SettingsUITextInput]
        //public string EditorAssetCategoryOverridePath { get; set; } = string.Empty;

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsGameOrEditor))]
        [SettingsUIButtonGroup("EditorAssetCategoryOverride")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool AddEditorAssetCategoryOverrideInclude
        {
            set
            {
                workflowSystem.AddEditorAssetCategoryOverrideInclude(
                    Path,
                    EditorAssetCategoryOverride
                );
            }
        }

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsGameOrEditor))]
        [SettingsUIButtonGroup("EditorAssetCategoryOverride")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool AddEditorAssetCategoryOverrideExclude
        {
            set
            {
                workflowSystem.AddEditorAssetCategoryOverrideExclude(
                    Path,
                    EditorAssetCategoryOverride
                );
            }
        }

        //[SettingsUISection(MainTab, PrefabModifier)]
        //[SettingsUITextInput]
        //public string RemoveObsoletesPath { get; set; } = string.Empty;

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsGameOrEditor))]
        [SettingsUIButtonGroup("PrefabAdd")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool AddUIIcon
        {
            set { workflowSystem.AddUIIcon(Path); }
        }

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsGameOrEditor))]
        [SettingsUIButtonGroup("PrefabAdd")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool AddPlaceableObject
        {
            set { workflowSystem.AddPlaceableObject(Path); }
        }

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsGameOrEditor))]
        [SettingsUIButtonGroup("PrefabRemove")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool RemoveObsoletes
        {
            set { workflowSystem.RemoveObsoletes(Path); }
        }

        //[SettingsUISection(MainTab, PrefabModifier)]
        //[SettingsUITextInput]
        //public string RemoveSpawnablesPath { get; set; } = string.Empty;

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsGameOrEditor))]
        [SettingsUIButtonGroup("PrefabRemove")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool RemoveSpawnables
        {
            set { workflowSystem.RemoveSpawnables(Path); }
        }

        [SettingsUISection(GeneralTab, LocaleMaker)]
        [SettingsUITextInput]
        public string LangPath { get; set; } = string.Empty;

        [SettingsUISection(GeneralTab, LocaleMaker)]
        [SettingsUITextInput]
        public string LangId { get; set; } = "en-US";

        [SettingsUISection(GeneralTab, LocaleMaker)]
        public bool ConvertLocale
        {
            set { workflowSystem.ConvertLocale(LangPath, LangId); }
        }

        //[SettingsUISection(GeneralTab, EditorModification)]
        //public bool ShowEditorCatsTypeBased { get; set; } = false;

        //[SettingsUISection(GeneralTab, EditorModification)]
        //public bool EnableCats
        //{
        //    set
        //    {
        //        World
        //            .DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EditorCategoryBuilder>()
        //            .EnableCats();
        //    }
        //}

        public override void SetDefaults() { }

        [SettingsUISection(AboutTab, InfoGroup)]
        public string NameText => Mod.Name;

        [SettingsUISection(AboutTab, InfoGroup)]
        public string VersionText => VariableHelper.AddDevSuffix(Mod.Version);

        [SettingsUISection(AboutTab, InfoGroup)]
        public string AuthorText => VariableHelper.StarQ;

        [SettingsUIButton]
        [SettingsUIButtonGroup("Social")]
        [SettingsUISection(AboutTab, InfoGroup)]
        public bool BMaCLink
        {
            set => VariableHelper.OpenBMAC();
        }

        [SettingsUIButton]
        [SettingsUIButtonGroup("Social")]
        [SettingsUISection(AboutTab, InfoGroup)]
        public bool Discord
        {
            set => VariableHelper.OpenDiscord("1353366978210824222");
        }

        [SettingsUIMultilineText]
        [SettingsUIDisplayName(typeof(LogHelper), nameof(LogHelper.LogText))]
        [SettingsUISection(LogTab, "")]
        public string LogText => string.Empty;

        [Exclude]
        [SettingsUIHidden]
        public bool IsLogMissing
        {
            get => VariableHelper.CheckLog(Mod.Id);
        }

        [SettingsUIButton]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsLogMissing))]
        [SettingsUISection(LogTab, "")]
        public bool OpenLog
        {
            set => VariableHelper.OpenLog(Mod.Id);
        }
    }
}
