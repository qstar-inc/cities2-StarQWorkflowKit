using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.PSI.Environment;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using Game.UI.Editor;
using Newtonsoft.Json;
using StarQ.Shared.Extensions;
using StarQWorkflowKit.Helper;
using Unity.Entities;
using UnityEngine;

namespace StarQWorkflowKit.Systems
{
    public partial class WorkflowSystem : GameSystemBase
    {
        private PrefabSystem prefabSystem;
        private EntityQuery allAssets;

        protected override void OnCreate()
        {
            base.OnCreate();
            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            allAssets = SystemAPI.QueryBuilder().WithAllRW<PrefabData>().Build();
        }

        protected override void OnUpdate() { }

        public void AddEditorAssetCategoryOverrideInclude(string path, string cat)
        {
            List<PrefabBase> prefabs = PrefabHelper.GetValidPrefabs(path);
            foreach (var prefabBase in prefabs)
            {
                bool alreadyExists = false;

                EditorAssetCategoryOverride EACO =
                    prefabBase.AddOrGetComponent<EditorAssetCategoryOverride>();
                EACO.m_IncludeCategories ??= new string[0];
                if (EACO.m_IncludeCategories == null)
                    Array.Resize(ref EACO.m_IncludeCategories, 0);

                if (
                    EACO.m_IncludeCategories.Length == 1
                    && (
                        EACO.m_IncludeCategories[0] == null || EACO.m_IncludeCategories[0] == "null"
                    )
                )
                    EACO.m_IncludeCategories[0] = cat;
                else if (EACO.m_IncludeCategories != null && EACO.m_IncludeCategories.Length > 0)
                {
                    foreach (var item in EACO.m_IncludeCategories)
                    {
                        if (item != null && item.Equals(cat))
                        {
                            alreadyExists = true;
                            break;
                        }
                    }
                }
                if (alreadyExists)
                    continue;

                Array.Resize(ref EACO.m_IncludeCategories, EACO.m_IncludeCategories.Length + 1);
                EACO.m_IncludeCategories[^1] = cat;

                SaveAndContinue(prefabBase, ChangeType.Adding, $"{cat} (EACO.m_IncludeCategories)");
            }
        }

        public void AddEditorAssetCategoryOverrideExclude(string path, string cat)
        {
            List<PrefabBase> prefabs = PrefabHelper.GetValidPrefabs(path);
            foreach (var prefabBase in prefabs)
            {
                bool alreadyExists = false;

                EditorAssetCategoryOverride EACO =
                    prefabBase.AddOrGetComponent<EditorAssetCategoryOverride>();
                EACO.m_ExcludeCategories ??= new string[0];
                if (EACO.m_ExcludeCategories == null)
                    Array.Resize(ref EACO.m_ExcludeCategories, 0);

                if (
                    EACO.m_ExcludeCategories.Length == 1
                    && (
                        EACO.m_ExcludeCategories[0] == null || EACO.m_ExcludeCategories[0] == "null"
                    )
                )
                    EACO.m_ExcludeCategories[0] = cat;
                else if (EACO.m_ExcludeCategories != null && EACO.m_ExcludeCategories.Length > 0)
                {
                    foreach (var item in EACO.m_ExcludeCategories)
                    {
                        if (item != null && item.Equals(cat))
                        {
                            alreadyExists = true;
                            break;
                        }
                    }
                }
                if (alreadyExists)
                    continue;

                Array.Resize(ref EACO.m_ExcludeCategories, EACO.m_ExcludeCategories.Length + 1);
                EACO.m_ExcludeCategories[^1] = cat;

                SaveAndContinue(
                    prefabBase,
                    ChangeType.Removing,
                    $"{cat} (EACO.m_ExcludeCategories)"
                );
            }
        }

        public void AddUIIcon(string path)
        {
            List<PrefabBase> assets = PrefabHelper.GetValidPrefabs(path);
            List<string> extensions = AssetPicker.kAllThumbnailsFileTypes.ToList();

            foreach (var prefabBase in assets)
            {
                string assetPath = prefabBase.asset.path;
                string iconString = null;

                foreach (var ext in extensions)
                {
                    string iconPath = Path.ChangeExtension(assetPath, ext);

                    if (File.Exists(iconPath))
                        iconString = GetUIIconString(iconPath);
                    break;
                }

                if (!string.IsNullOrEmpty(iconString))
                {
                    UIObject UIObjectComp = prefabBase.AddOrGetComponent<UIObject>();
                    UIObjectComp.m_Icon = $"assetdb://Global/{iconString}";

                    SaveAndContinue(prefabBase, ChangeType.Adding, "UI Icon");
                }
            }
        }

        public string GetUIIconString(string filePath)
        {
            string cidPath = $"{filePath}.cid";

            string cid;
            if (File.Exists(cidPath))
                cid = File.ReadAllText(cidPath);
            else
            {
                string newCid = Colossal.Hash128.CreateGuid(cidPath).ToString();
                File.WriteAllText(cidPath, newCid);
                cid = newCid;
            }
            return $"assetdb://Global/{cid}";
        }

        public void AddPlaceableObject(string path)
        {
            List<PrefabBase> prefabs = PrefabHelper.GetValidPrefabs(path);
            foreach (var prefabBase in prefabs)
            {
                if (prefabBase.Has<PlaceableObject>())
                    continue;
                if (prefabBase.prefab.GetType() != typeof(StaticObjectPrefab))
                    continue;

                PlaceableObject PlaceableObjectComp = prefabBase.AddComponent<PlaceableObject>();

                PlaceableObjectComp.m_ConstructionCost = 0;
                PlaceableObjectComp.m_XPReward = 0;

                SaveAndContinue(prefabBase, ChangeType.Adding, "PlaceableObject");
            }
        }

        public void AddUIGroup(string path, string uiGroup)
        {
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID("UIAssetCategoryPrefab", uiGroup),
                    out PrefabBase assetCat
                )
            )
            {
                List<PrefabBase> prefabs = PrefabHelper.GetValidPrefabs(path);
                foreach (var prefabBase in prefabs)
                {
                    if (prefabBase.prefab.GetType() == typeof(RenderPrefab))
                        continue;

                    UIObject UIObject = prefabBase.AddOrGetComponent<UIObject>();

                    UIObject.m_Group = (UIGroupPrefab)assetCat;

                    SaveAndContinue(prefabBase, ChangeType.Adding, $"{uiGroup} UI Group");
                }
            }
        }

        public void RemoveObsoletes(string path)
        {
            List<PrefabBase> prefabs = PrefabHelper.GetValidPrefabs(path);
            foreach (var prefabBase in prefabs)
            {
                prefabBase.Remove<ObsoleteIdentifiers>();

                SaveAndContinue(prefabBase, ChangeType.Removing, "ObsoleteIdentifiers");
            }
        }

        public void RemoveSpawnables(string path)
        {
            List<PrefabBase> prefabs = PrefabHelper.GetValidPrefabs(path);
            foreach (var prefabBase in prefabs)
            {
                prefabBase.Remove<SpawnableObject>();

                SaveAndContinue(prefabBase, ChangeType.Removing, "SpawnableObject");
            }
        }

        public void AddAssetPack(string path, string pack)
        {
            if (
                !prefabSystem.TryGetPrefab(
                    new PrefabID("AssetPackPrefab", pack),
                    out PrefabBase assetPackPrefab
                )
            )
            {
                assetPackPrefab = ScriptableObject.CreateInstance<AssetPackPrefab>();
                assetPackPrefab.name = pack;

                AssetDataPath adp_ap = AssetDataPath.Create(
                    $"ImportedData/{assetPackPrefab.name}",
                    $"{assetPackPrefab.name}.Prefab",
                    EscapeStrategy.None
                );
                AssetDatabase
                    .user.AddAsset(adp_ap, assetPackPrefab)
                    .Save(ContentType.Text, false, true);
                LogHelper.SendLog($"Saving {pack}");
                prefabSystem.UpdatePrefab(assetPackPrefab);
            }

            List<PrefabBase> prefabs = PrefabHelper.GetValidPrefabs(path);
            foreach (var prefabBase in prefabs)
            {
                if (
                    prefabBase.prefab.GetType() != typeof(ObjectPrefab)
                    && prefabBase.prefab.GetType() != typeof(ZonePrefab)
                    && prefabBase.prefab.GetType() != typeof(NetPrefab)
                    && prefabBase.prefab.GetType() != typeof(AreaPrefab)
                    && prefabBase.prefab.GetType() != typeof(RoutePrefab)
                    && prefabBase.prefab.GetType() != typeof(NetLanePrefab)
                )
                    continue;

                AssetPackPrefab apPrefab = (AssetPackPrefab)assetPackPrefab;
                AssetPackItem AssetPackItem = prefabBase.AddOrGetComponent<AssetPackItem>();

                if (AssetPackItem.m_Packs == null || AssetPackItem.m_Packs.Length == 0)
                {
                    AssetPackItem.m_Packs = new AssetPackPrefab[1];
                    AssetPackItem.m_Packs[0] = apPrefab;
                }
                else
                {
                    bool alreadyExists = false;
                    foreach (var packItem in AssetPackItem.m_Packs)
                    {
                        if (packItem.name == pack)
                        {
                            alreadyExists = true;
                            break;
                        }
                    }
                    if (alreadyExists)
                        continue;

                    Array.Resize(ref AssetPackItem.m_Packs, AssetPackItem.m_Packs.Length + 1);
                    AssetPackItem.m_Packs[^1] = apPrefab;
                }

                SaveAndContinue(prefabBase, ChangeType.Adding, $"AssetPackItem: {pack}");
            }
        }

        public void RemoveAssetPack(string path, string pack)
        {
            List<PrefabBase> prefabs = PrefabHelper.GetValidPrefabs(path);
            foreach (var prefabBase in prefabs)
            {
                if (!prefabBase.Has<AssetPackItem>())
                    continue;

                AssetPackItem assetPackItem = prefabBase.GetComponent<AssetPackItem>();

                if (
                    assetPackItem == null
                    || assetPackItem.m_Packs == null
                    || assetPackItem.m_Packs.Length == 0
                )
                    prefabBase.Remove<AssetPackItem>();

                if (!assetPackItem.m_Packs.Any(p => p.name == pack))
                    continue;

                var packs = assetPackItem.m_Packs.ToList();
                Array.Resize(ref assetPackItem.m_Packs, assetPackItem.m_Packs.Length - 1);
                if (assetPackItem.m_Packs.Length == 0)
                    prefabBase.Remove<AssetPackItem>();
                else
                    assetPackItem.m_Packs = packs.Where(p => p.name != pack).ToArray();

                SaveAndContinue(prefabBase, ChangeType.Removing, $"AssetPackItem: {pack}");
            }
        }

        public void ConvertLocale(string path, string lang)
        {
            LocalizationManager localizationManager = Game.SceneFlow
                .GameManager
                .instance
                .localizationManager;
            if (string.IsNullOrEmpty(path))
            {
                LogHelper.SendLog(
                    $"Failed to convert locale: [ string.IsNullOrEmpty(path) : {string.IsNullOrEmpty(path)} ]"
                );
                return;
            }
            if (!File.Exists(path))
            {
                LogHelper.SendLog($"File ({path}) not found.");
                return;
            }

            Dictionary<string, LocaleData> dictionary = new();

            //string[] supLang = localizationManager.GetSupportedLocales();
            //if (!supLang.Contains(lang))
            //{
            //    LogHelper.SendLog(
            //        $"{lang} is not a supported locale. Try one of these: '{string.Join(",", supLang)}'"
            //    );
            //    return;
            //}
            if (!dictionary.ContainsKey(lang))
            {
                dictionary[lang] = new LocaleData(
                    lang,
                    new Dictionary<string, string>(),
                    new Dictionary<string, int>()
                );

                Dictionary<string, Dictionary<string, string>> groupedByAsset = new();
                try
                {
                    string json = File.ReadAllText(path);
                    Dictionary<string, string> parsed = JsonConvert.DeserializeObject<
                        Dictionary<string, string>
                    >(json);

                    if (parsed == null)
                    {
                        LogHelper.SendLog("JSON is empty or invalid.");
                        return;
                    }

                    Regex bracketRegex = new(@"\[(.*?)\]");

                    foreach (var kvp in parsed)
                    {
                        Match match = bracketRegex.Match(kvp.Key);
                        string assetName;
                        if (match.Success)
                        {
                            assetName = match.Groups[1].Value;
                        }
                        else
                        {
                            assetName = kvp.Key;
                        }

                        if (!groupedByAsset.ContainsKey(assetName))
                            groupedByAsset[assetName] = new Dictionary<string, string>();

                        groupedByAsset[assetName][kvp.Key] = kvp.Value;
                    }

                    LogHelper.SendLog(
                        $"Successfully loaded {parsed.Count} entries for language '{lang}'."
                    );
                }
                catch (JsonException ex)
                {
                    LogHelper.SendLog("Invalid JSON: " + ex.Message);
                }
                catch (Exception ex)
                {
                    LogHelper.SendLog("Unknown Error: " + ex.Message);
                }

                foreach (var assetEntry in groupedByAsset)
                {
                    try
                    {
                        string assetName = assetEntry.Key;
                        Dictionary<string, string> entries = assetEntry.Value;
                        LocaleData localeData = new(lang, entries, new Dictionary<string, int>());

                        LocaleAsset localeAsset = AssetDatabase.user.AddAsset<LocaleAsset>(
                            AssetDataPath.Create(
                                $"ImportedData/CreatedLocalization/" + assetName,
                                assetName + "_" + localeData.localeId,
                                EscapeStrategy.PathAndFilename
                            ),
                            default
                        );
                        localeAsset.SetData(
                            localeData,
                            localizationManager.LocaleIdToSystemLanguage(localeData.localeId),
                            GameManager.instance.localizationManager.GetLocalizedName(
                                localeData.localeId
                            )
                        );
                        localeAsset.Save(true);
                        LogHelper.SendLog($"Saving {assetName} locales");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.SendLog("Unknown Error: " + ex.Message);
                    }
                }
            }
        }

        private readonly Dictionary<string, Dictionary<string, string>> toRename = new();

        private bool LoadRenameData()
        {
            toRename.Clear();

            string dataFile =
                $"{EnvPath.kUserDataPath}/ModsData/{nameof(StarQWorkflowKit)}_PrefabRenamer.txt";
            if (!File.Exists(dataFile))
            {
                LogHelper.SendLog($"PrefabRenamer file not found, creating...");
                File.WriteAllText(
                    dataFile,
                    ""
                        + "# Write the prefab type, old prefab name & new prefab name you want.\n"
                        + "\n"
                        + "# Format:              PrefabType   ; OldPrefabName ;  NewPrefabName\n"
                        + "# Example:           BuildingPrefab ;   School03    ; StarQ School03\n"
                        + "\n"
                        + "# If a line start with '#' or '\\' like this one, it will be ignored.\n"
                        + "# After saving this file, click on the 'Rename Prefab' option again.\n"
                        + "====================================================================\n\n"
                        + "BuildingPrefab;School03;StarQ School03"
                );

                Task.Run(() => Process.Start(dataFile));
                return false;
            }

            foreach (string line in File.ReadLines(dataFile))
            {
                try
                {
                    if (
                        string.IsNullOrWhiteSpace(line)
                        || line.StartsWith("name")
                        || line.StartsWith("#")
                        || line.StartsWith("/")
                        || line.StartsWith("=")
                    )
                        continue;

                    string[] parts = line.Split(';');
                    if (parts.Length != 3)
                    {
                        LogHelper.SendLog($"Invalid line: {line}");
                        continue;
                    }

                    string typeName = parts[0].Trim();
                    string oldName = parts[1].Trim();
                    string newName = parts[2].Trim();

                    if (!toRename.TryGetValue(typeName, out var inner))
                    {
                        inner = new Dictionary<string, string>();
                        toRename[typeName] = inner;
                    }

                    inner[oldName] = newName;
                }
                catch (Exception ex)
                {
                    LogHelper.SendLog(
                        $"Error parsing line while LoadRenameData():\n{line}\n{ex.Message}"
                    );
                    return false;
                }
            }

            return true;
        }

        public void GetListOfPrefabs(string path)
        {
            List<string> toLog = new();
            List<PrefabBase> prefabs = PrefabHelper.GetValidPrefabs(path);
            foreach (var prefabBase in prefabs)
                toLog.Add($"{prefabBase.GetType().Name}:{prefabBase.name}");

            toLog.Sort();
            LogHelper.SendLog($"\nList of prefabs:\n{string.Join('\n', toLog)}");
            Task.Run(() => Process.Start(LogHelper.logPath));
        }

        public void RenamePrefab(string path)
        {
            if (!LoadRenameData())
                return;

            List<PrefabBase> prefabs = PrefabHelper.GetValidPrefabs(path);
            foreach (var prefabBase in prefabs)
            {
                string oldName = prefabBase.name;
                string prefabType = prefabBase.GetType().Name;

                if (!toRename.TryGetValue(prefabType, out var inner))
                    continue;
                if (!inner.TryGetValue(oldName, out var newName))
                    continue;

                ObsoleteIdentifiers ObsoleteIdentifiers =
                    prefabBase.AddOrGetComponent<ObsoleteIdentifiers>();

                ObsoleteIdentifiers.m_PrefabIdentifiers ??= new PrefabIdentifierInfo[0];
                Array.Resize(
                    ref ObsoleteIdentifiers.m_PrefabIdentifiers,
                    ObsoleteIdentifiers.m_PrefabIdentifiers.Length + 1
                );
                ObsoleteIdentifiers.m_PrefabIdentifiers[^1] = new()
                {
                    m_Name = oldName,
                    m_Type = prefabType,
                };
                prefabBase.name = newName;

                SaveAndContinue(prefabBase, ChangeType.Renaming, oldName, newName);
            }
        }

        public enum ChangeType
        {
            Adding,
            Removing,
            Renaming,
        }

        public void SaveAndContinue(
            PrefabBase prefabBase,
            ChangeType changeType,
            string text1,
            string text2 = null
        )
        {
            string logText;
            if (string.IsNullOrEmpty(text2))
                if (changeType == ChangeType.Adding)
                    logText = $"{changeType} {text1} to {prefabBase.name}";
                else
                    logText = $"{changeType} {text1} from {prefabBase.name}";
            else
                logText = $"{changeType} {text1} from {text2} to {prefabBase.name}";

            AssetDataPath adp_main = AssetDataPath.Create(
                prefabBase.asset.subPath,
                prefabBase.asset.name,
                EscapeStrategy.None
            );
            AssetDatabase
                .user.AddAsset(adp_main, prefabBase)
                .Save(PrefabHelper.GetPrefabContentType(prefabBase), false, true);
            LogHelper.SendLog(logText);
            prefabSystem.UpdatePrefab(prefabBase);
        }
    }
}
