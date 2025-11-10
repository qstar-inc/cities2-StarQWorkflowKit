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
        public Setting(IMod mod)
            : base(mod) => SetDefaults();

        public const string GeneralTab = "GeneralTab";
        public const string Header = "Header";
        public const string PrefabSaver = "PrefabSaver";
        public const string PrefabPackager = "PrefabPackager";
        public const string PrefabModifier = "PrefabModifier";
        public const string LocaleMaker = "LocaleMaker";
        public const string EditorModification = "EditorModification";

        public const string AboutTab = "AboutTab";
        public const string InfoGroup = "InfoGroup";
        public const string LogTab = "LogTab";

        private static readonly WorkflowSystem prefab_helper =
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WorkflowSystem>();

        [SettingsUIMultilineText]
        [SettingsUISection(GeneralTab, Header)]
        public string Disclaimer => string.Empty;

        [SettingsUISection(GeneralTab, PrefabSaver)]
        [SettingsUITextInput]
        public string ResavePrefabPath { get; set; } = string.Empty;

        [SettingsUISection(GeneralTab, PrefabSaver)]
        [SettingsUIButton]
        [SettingsUIButtonGroup("ResavePrefab")]
        public bool ResavePrefab
        {
            set { prefab_helper.SaveAsset(ResavePrefabPath, 1); }
        }

        [SettingsUISection(GeneralTab, PrefabSaver)]
        [SettingsUIButton]
        [SettingsUIButtonGroup("ResavePrefab")]
        public bool ResavePrefabT
        {
            set { prefab_helper.SaveAsset(ResavePrefabPath, 0); }
        }

        [SettingsUISection(GeneralTab, PrefabSaver)]
        [SettingsUIButton]
        [SettingsUIButtonGroup("ResavePrefab")]
        public bool ResavePrefabB
        {
            set { prefab_helper.SaveAsset(ResavePrefabPath, 3); }
        }

        [SettingsUISection(GeneralTab, PrefabPackager)]
        [SettingsUITextInput]
        public string CreatePackagePath { get; set; } = string.Empty;

        //[SettingsUISection(MainTab, PrefabPackager)]
        //[SettingsUIButton]
        //[SettingsUIButtonGroup("CreatePackage")]
        //public bool CreatePackage
        //{
        //    set { prefab_helper.SaveAsset(CreatePackagePath, 2); }
        //}

        [SettingsUISection(GeneralTab, PrefabPackager)]
        [SettingsUIButton]
        [SettingsUIButtonGroup("CreatePackage")]
        public bool CreatePackageAny
        {
            set { prefab_helper.CreatePackage(CreatePackagePath, direct: true); }
        }

        [SettingsUISection(GeneralTab, PrefabModifier)]
        [SettingsUITextInput]
        public string Path { get; set; } = string.Empty;

        [SettingsUISection(GeneralTab, PrefabModifier)]
        [SettingsUITextInput]
        public string AssetPackToAdd { get; set; } = string.Empty;

        [SettingsUIButtonGroup("AssetPack")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool AddAssetPack
        {
            set { prefab_helper.AddAssetPack(Path, AssetPackToAdd); }
        }

        [SettingsUIButtonGroup("AssetPack")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool RemoveAssetPack
        {
            set { prefab_helper.RemoveAssetPacks(Path); }
        }

        [SettingsUISection(GeneralTab, PrefabModifier)]
        [SettingsUITextInput]
        public string UIGroupToAdd { get; set; } = string.Empty;

        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool AddUIGroup
        {
            set { prefab_helper.AddUIGroup(Path, UIGroupToAdd); }
        }

        [SettingsUIButtonGroup("RenamePrefab")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool GetListOfPrefabs
        {
            set { prefab_helper.GetListOfPrefabs(Path); }
        }

        [SettingsUIButtonGroup("RenamePrefab")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool RenamePrefab
        {
            set { prefab_helper.RenamePrefab(Path); }
        }

        [SettingsUISection(GeneralTab, PrefabModifier)]
        [SettingsUITextInput]
        public string EditorAssetCategoryOverride { get; set; } = string.Empty;

        //[SettingsUISection(MainTab, PrefabModifier)]
        //[SettingsUITextInput]
        //public string EditorAssetCategoryOverridePath { get; set; } = string.Empty;

        [SettingsUIButtonGroup("EditorAssetCategoryOverride")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
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
        [SettingsUISection(GeneralTab, PrefabModifier)]
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

        [SettingsUIButtonGroup("PrefabAdd")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool AddUIIcon
        {
            set { prefab_helper.AddUIIcon(Path); }
        }

        [SettingsUIButtonGroup("PrefabAdd")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool AddPlaceableObject
        {
            set { prefab_helper.AddPlaceableObject(Path); }
        }

        [SettingsUIButtonGroup("PrefabRemove")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool RemoveObsoletes
        {
            set { prefab_helper.RemoveObsoletes(Path); }
        }

        //[SettingsUISection(MainTab, PrefabModifier)]
        //[SettingsUITextInput]
        //public string RemoveSpawnablesPath { get; set; } = string.Empty;

        [SettingsUIButtonGroup("PrefabRemove")]
        [SettingsUISection(GeneralTab, PrefabModifier)]
        public bool RemoveSpawnables
        {
            set { prefab_helper.RemoveSpawnables(Path); }
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
            set { prefab_helper.ConvertLocale(LangPath, LangId); }
        }

        [SettingsUISection(GeneralTab, EditorModification)]
        public bool ShowEditorCatsTypeBased { get; set; } = false;

        [SettingsUISection(GeneralTab, EditorModification)]
        public bool EnableCats
        {
            set
            {
                World
                    .DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EditorCategoryBuilder>()
                    .EnableCats();
            }
        }

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
