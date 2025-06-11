using System.Collections.Generic;
using System.Linq;
using Colossal;
using Colossal.Localization;
using Unity.Entities;

namespace StarQWorkflowKit
{
    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts
        )
        {
            LocalizationManager localizationManager = Game.SceneFlow
                .GameManager
                .instance
                .localizationManager;
            string PathInfo =
                "Supported paths:\r\n- C:\\\\Users\\\\StarQ\\\\AppData\\\\LocalLow\\\\Colossal Order\\\\Cities Skylines II\\\\StreamingData~\\\\Prefab\r\n- StreamingData~\\\\Prefab\r\n- mods_subscribed\\\\12345_0\r\n- Prefab (any StreamingData~ folder)\r\n- StreamingData~ (everything inside StreamingData~, including subfolders  )\r\n- mods_subscribed (everything inside mods_subscribed, including subfolders)\r\nMove all files you want to add to a separate folders in StreamingData~ and input that folder; which you can then take it to it's original folder. Prefabs in this folder, must be existing in the folder before the game is loaded. `*`s are supported for paths like `\\A*\\B` or `\\A\\*\\B`. Will not work for disabled folders (with . or ~ in front). Will not work for folders outside the game's scope.";
            string PathInfoJson =
                "Supported paths:\r\n- C:\\\\Users\\\\StarQ\\\\AppData\\\\LocalLow\\\\Colossal Order\\\\Cities Skylines II\\\\StreamingData~\\\\Prefab\\\\en-US.json\r\n- StreamingData~\\\\Prefab\\\\en-US.json\r\n- F:\\\\Whatever\\\\Folder\\\\Wherever\\\\File.json";
            static string Every(string willBe) =>
                $"Every '.Prefab' files in this folder and any subfolders will be {willBe}.";
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), Mod.Name },
                { m_Setting.GetOptionTabLocaleID(Setting.MainTab), Setting.MainTab },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), Setting.AboutTab },
                { m_Setting.GetOptionGroupLocaleID(Setting.PrefabSaver), Setting.PrefabSaver },
                {
                    m_Setting.GetOptionGroupLocaleID(Setting.PrefabModifier),
                    Setting.PrefabModifier
                },
                { m_Setting.GetOptionGroupLocaleID(Setting.LocaleMaker), Setting.LocaleMaker },
                {
                    m_Setting.GetOptionGroupLocaleID(Setting.EditorModification),
                    Setting.EditorModification
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.Disclaimer)),
                    "DISCLAIMER: This mod modifies prefabs in an irreversible way. Please ensure you have a backup of the StreamingData~ before using it, as changes cannot be undone. The mod author is not responsible for any loss of content resulting from its use.\r\nA game restart is absolutely necessary for the modified assets to be shown up in game.\r\nIf any prefabs from mods_subscribed is modified using any of the methods below, it will change the file integrity of the Prefab thus will show up as \"dirty/modified\" is Simple Mod Checker's mod verification. To restore, simply delete the folder and let the game (or Skyve) redownload it for you."
                },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Disclaimer)), "" },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.CreatePackagePath)),
                    "Path to folders to create packages with"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.CreatePackagePath)),
                    $"Every subfolder in this folder will have one '.cok' file each.\r\n{PathInfo}"
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.CreatePackage)),
                    "Create Binary Package(s)"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.CreatePackage)),
                    $"Create '.cok' packages to 'StreamingData~\\~CreatedPackages' from each subfolders or prefab files.\r\n- If the folder has subfolder, each subfolder will be used to create one cok file, any separate files in the folder will only be reset..\r\n- If the folder only contains files, no folders, then one cok will be created for everything inside the folder.\r\nThe packages won't be available in-game by default, since that will conflict with the original prefab files.\r\nRemove/disable the original prefab folder and move the '.cok' files out of the '~CreatedPackages' folder (or remove the ~ in front of '~CreatedPackages')."
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.CreatePackageAny)),
                    "Create Package as is"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.CreatePackageAny)),
                    $"Create '.cok' package to 'StreamingData~\\~CreatedPackages'\r\n- This will create one package for the whole selected path. No prefab processing will be done, but supports subfolders.\r\nThe packages won't be available in-game by default, since that will conflict with the original prefab files.\r\nRemove/disable the original prefab folder and move the '.cok' files out of the '~CreatedPackages' folder (or remove the ~ in front of '~CreatedPackages')."
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResavePrefabPath)),
                    "Path to folders to resave prefabs from"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.ResavePrefabPath)),
                    $"{Every("resaved")}\r\n{PathInfo}"
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResavePrefabB)),
                    "Resave Prefabs as Binary"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.ResavePrefabB)),
                    $"Resave all prefabs in binary format; can be used in the event of changed prefab structure, resetting id/type after manually editing prefab, or to recreate CID if not found."
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResavePrefabT)),
                    "Resave Prefabs as Text"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.ResavePrefabT)),
                    $"Resave all prefabs in text format; can be used in the event of changed prefab structure, resetting id/type after manually editing prefab, or to recreate CID if not found."
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResavePrefab)),
                    "Resave Prefabs as is"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.ResavePrefab)),
                    $"Resave all prefabs in current format; can be used in the event of changed prefab structure, resetting id/type after manually editing prefab, or to recreate CID if not found."
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.Path)),
                    "Path to folders to process prefabs from"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.Path)),
                    $"{Every("resaved with updated the prefab file replacing the original on the same directory.")}\r\n{PathInfo}"
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.UIGroupToAdd)),
                    "The PrefabID of the UIAssetCategoryPrefab"
                },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UIGroupToAdd)), $"" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AddUIGroup)), "Add UIGroup" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.AddUIGroup)),
                    $"Modifies original prefab.\r\nCannot be added to RenderPrefabs."
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AddUIIcon)), "Add UI Icons" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.AddUIIcon)),
                    $"Add UI Icons from png file with the same name on the same folder. A CID file for the png will be created automatically if not found."
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.EditorAssetCategoryOverride)),
                    "EditorAssetCategoryOverride Category"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.EditorAssetCategoryOverride)),
                    $""
                },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.EditorAssetCategoryOverridePath)), "Path to files to add EditorAssetCategoryOverride to" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.EditorAssetCategoryOverridePath)), $"{Every("resaved with the selected EditorAssetCategoryOverride")}\r\n{PathInfo}" },
                {
                    m_Setting.GetOptionLabelLocaleID(
                        nameof(Setting.AddEditorAssetCategoryOverrideInclude)
                    ),
                    "Add EditorAssetCategoryOverride Include"
                },
                {
                    m_Setting.GetOptionDescLocaleID(
                        nameof(Setting.AddEditorAssetCategoryOverrideInclude)
                    ),
                    $"Modifies original prefab."
                },
                {
                    m_Setting.GetOptionLabelLocaleID(
                        nameof(Setting.AddEditorAssetCategoryOverrideExclude)
                    ),
                    "Add EditorAssetCategoryOverride Exclude"
                },
                {
                    m_Setting.GetOptionDescLocaleID(
                        nameof(Setting.AddEditorAssetCategoryOverrideExclude)
                    ),
                    $"Modifies original prefab."
                },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveObsoletesPath)), "Path to files to remove ObsoleteIdentifiers from" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveObsoletesPath)), $"{Every("resaved with ObsoleteIdentifiers removed")}\r\n{PathInfo}" },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveObsoletes)),
                    "Remove ObsoleteIdentifiers"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveObsoletes)),
                    $"Modifies original prefab."
                },
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveSpawnablesPath)), "Path to files to remove SpawnableObject from" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveSpawnablesPath)), $"{Every("resaved with SpawnableObject removed")}\r\n{PathInfo}" },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveSpawnables)),
                    "Remove SpawnableObject"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveSpawnables)),
                    $"Modifies original prefab."
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.AssetPackToAdd)),
                    "The PrefabID of the AssetPackPrefab"
                },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AssetPackToAdd)), $"" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AddAssetPack)), "Add AssetPack" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.AddAssetPack)),
                    $"Modifies original prefab.\r\nCannot be added to RenderPrefabs."
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveAssetPack)),
                    "Remove AssetPack"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveAssetPack)),
                    $"Modifies original prefab."
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.LangPath)),
                    $"The path to the json file."
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.LangPath)),
                    $"\r\n{PathInfoJson}"
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.LangId)),
                    "The language ID of the json file."
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.LangId)),
                    $"Supported Locales: {string.Join(", ", localizationManager.GetSupportedLocales())}."
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.ConvertLocale)),
                    "Convert Locales"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.ConvertLocale)),
                    $"Save LOC files in CreatedLocalization folder."
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.ShowEditorCatsTypeBased)),
                    "Enable PrefabType based Category on startup"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.ShowEditorCatsTypeBased)),
                    $"Enabling this will add a new prefab category on the Editor/Dev Menu Add Objects panel, based on the prefab type. (Every game start as long as this is active)\r\nIMPORTANT: Saving prefabs with this enable will have the category saved on the prefab file itself. Make sure to remove it manually before saving if you don't want it to be saved."
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableCats)),
                    "Enable PrefabType based Category for this session only"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableCats)),
                    $"Enabling this will add a new prefab category on the Editor/Dev Menu Add Objects panel, based on the prefab type. (Only for this session)\r\nIMPORTANT: Saving prefabs with this enable will have the category saved on the prefab file itself. Make sure to remove it manually before saving if you don't want it to be saved."
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "Mod Name" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "Mod Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AuthorText)), "Author" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AuthorText)), "" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BMaCLink)), "Buy Me a Coffee" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.BMaCLink)),
                    "Support the author."
                },
            };
        }

        public void Unload() { }
    }
}
