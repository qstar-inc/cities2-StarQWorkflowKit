using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.PSI.Environment;
using Game;
using Game.Buildings;
using Game.Prefabs;
using Game.Routes;
using Game.SceneFlow;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Windows;

namespace StarQWorkflowKit
{
    public partial class PrefabHelper : GameSystemBase
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

        public void DoAsset(PrefabBase prefab, int num, bool noLog = false)
        {
            ContentType ct = GetPrefabContentType(prefab);
            if (num == 3 || num == 2)
                ct = ContentType.Binary;
            if (num == 0)
                ct = ContentType.Text;
            string xt = ct switch
            {
                ContentType.Text => " (Text)",
                ContentType.Binary => " (Binary)",
                _ => "",
            };
            if (!noLog)
                Mod.log.Info($"Saving {prefab.name}{xt}");
            prefab.asset.Save(ct, false, true);
        }

        public void SaveAsset(string path, int num = 1, bool noLog = false)
        {
            List<string> folderNames = GetValidFolders(path);
            foreach (var folderName in folderNames)
            {
                var entities = allAssets.ToEntityArray(Allocator.Temp);
                Mod.log.Info($"Checking {entities.Count()} entities.");

                int i = 0;
                foreach (Entity entity in entities)
                {
                    i++;
                    if (
                        EntityManager.TryGetComponent(entity, out PrefabData prefabData)
                        && prefabSystem.TryGetPrefab(prefabData, out PrefabBase prefabBase)
                    )
                    {
                        if (prefabBase.builtin)
                        {
                            continue;
                        }
                        try
                        {
                            if (
                                prefabBase.asset != null
                                && prefabBase.asset.path != null
                                && prefabBase.asset.path.Contains(folderName)
                                && prefabBase.asset.path.EndsWith(".Prefab")
                            )
                            {
                                DoAsset(prefabBase, num, noLog);
                            }
                        }
                        catch (Exception ex)
                        {
                            Mod.log.Info(ex);
                        }
                    }
                }
                if (num == 2)
                {
                    CreatePackage(folderName, true);
                    SaveAsset(path, 1, true);
                }
            }
            Mod.log.Info($"Done.");
        }

        public void CreatePackage(string path, bool folderValid = false, bool direct = false)
        {
            List<string> validPaths = folderValid
                ? new List<string> { path }
                : GetValidFolders(path);

            if (validPaths == null || validPaths.Count == 0)
            {
                Mod.log.Info($"Failed to CreatePackage: No valid folder paths found.");
                return;
            }

            Mod.log.Info("Creating Packages...");

            string exportFolder = EnvPath.kUserDataPath + "/StreamingData~/~CreatedPackages";

            if (!Directory.Exists(exportFolder))
            {
                Directory.CreateDirectory(exportFolder);
            }
            foreach (var validPath in validPaths)
            {
                if (direct)
                {
                    string folderName = Path.GetFileName(path);
                    string outputCokPath = Path.Combine(exportFolder, folderName + ".cok");
                    string outputCidPath = Path.Combine(exportFolder, folderName + ".cok.cid");
                    CreateCokAndCid(outputCokPath, outputCidPath, path);
                    continue;
                }

                if (!Directory.EnumerateDirectories(path).Any())
                {
                    string folderName = Path.GetFileName(path);
                    string outputCokPath = Path.Combine(exportFolder, folderName + ".cok");
                    string outputCidPath = Path.Combine(exportFolder, folderName + ".cok.cid");
                    CreateCokAndCid(outputCokPath, outputCidPath, path);
                }
                else
                {
                    foreach (string folderPath in Directory.GetDirectories(path))
                    {
                        string folderName = Path.GetFileName(folderPath);
                        if (folderName.StartsWith(".") || folderName.StartsWith("~"))
                        {
                            continue;
                        }

                        string outputCokPath = Path.Combine(exportFolder, folderName + ".cok");
                        string outputCidPath = Path.Combine(exportFolder, folderName + ".cok.cid");

                        CreateCokAndCid(outputCokPath, outputCidPath, folderPath);
                    }
                }
            }
        }

        public void CreateCokAndCid(string outputCokPath, string outputCidPath, string folderPath)
        {
            using (FileStream zipFile = File.Create(outputCokPath))
            using (ZipOutputStream zipStream = new(zipFile))
            {
                zipStream.SetLevel(0);
                foreach (
                    string filePath in Directory.GetFiles(
                        folderPath,
                        "*",
                        SearchOption.AllDirectories
                    )
                )
                {
                    string arcname = filePath[(folderPath.Length + 1)..].Replace("\\", "/");

                    try
                    {
                        ZipEntry entry = new(arcname)
                        {
                            CompressionMethod = CompressionMethod.Stored,
                            DateTime = File.GetLastWriteTime(filePath),
                        };
                        zipStream.PutNextEntry(entry);

                        using FileStream fs = File.OpenRead(filePath);
                        fs.CopyTo(zipStream);

                        zipStream.CloseEntry();
                    }
                    catch (Exception ex)
                    {
                        Mod.log.Info($"Error processing {filePath}: {ex.Message}");
                    }
                }
            }

            string newCid = Colossal.Hash128.CreateGuid(outputCokPath).ToString();
            File.WriteAllText(outputCidPath, newCid);

            Mod.log.Info($"Created: {outputCokPath}");
        }

        public void AddEditorAssetCategoryOverrideInclude(string path, string cat)
        {
            List<string> folderNames = GetValidFolders(path);
            foreach (var folderName in folderNames)
            {
                var allAssetEntities = allAssets.ToEntityArray(Allocator.Temp);
                bool breakOuterLoop = false;
                foreach (Entity entity in allAssetEntities)
                {
                    string entityName = prefabSystem.GetPrefabName(entity);
                    prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase);

                    if (prefabBase.asset == null)
                        continue;

                    string assetPath = prefabBase.asset.path;
                    if (assetPath.Contains(folderName) && assetPath.EndsWith(".Prefab"))
                    {
                        EditorAssetCategoryOverride EditorAssetCategoryOverride =
                            prefabBase.AddOrGetComponent<EditorAssetCategoryOverride>();
                        EditorAssetCategoryOverride.m_IncludeCategories ??= new string[0];
                        if (
                            EditorAssetCategoryOverride.m_IncludeCategories.Length == 1
                            && (
                                EditorAssetCategoryOverride.m_IncludeCategories[0] == null
                                || EditorAssetCategoryOverride.m_IncludeCategories[0] == "null"
                            )
                        )
                        {
                            EditorAssetCategoryOverride.m_IncludeCategories[0] = cat;
                            break;
                        }
                        Array.Resize(
                            ref EditorAssetCategoryOverride.m_IncludeCategories,
                            EditorAssetCategoryOverride.m_IncludeCategories.Length + 1
                        );
                        if (EditorAssetCategoryOverride.m_IncludeCategories.Length > 0)
                        {
                            foreach (var item in EditorAssetCategoryOverride.m_IncludeCategories)
                            {
                                if (item.Equals(cat))
                                {
                                    breakOuterLoop = true;
                                    break;
                                }
                            }
                        }
                        if (breakOuterLoop)
                        {
                            break;
                        }
                        EditorAssetCategoryOverride.m_IncludeCategories[^1] = cat;

                        AssetDataPath adp_main = AssetDataPath.Create(
                            prefabBase.asset.subPath,
                            prefabBase.asset.name,
                            EscapeStrategy.None
                        );
                        AssetDatabase
                            .user.AddAsset(adp_main, prefabBase)
                            .Save(GetPrefabContentType(prefabBase), false, true);
                        Mod.log.Info(
                            $"{cat} (EditorAssetCategoryOverride.m_IncludeCategories) added to {entityName}"
                        );
                        prefabSystem.UpdatePrefab(prefabBase);
                    }
                }
            }
        }

        public void AddEditorAssetCategoryOverrideExclude(string path, string cat)
        {
            List<string> folderNames = GetValidFolders(path);
            foreach (var folderName in folderNames)
            {
                var allAssetEntities = allAssets.ToEntityArray(Allocator.Temp);
                bool breakOuterLoop = false;
                foreach (Entity entity in allAssetEntities)
                {
                    string entityName = prefabSystem.GetPrefabName(entity);
                    prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase);

                    if (prefabBase.asset == null)
                        continue;

                    string assetPath = prefabBase.asset.path;
                    if (assetPath.Contains(folderName) && assetPath.EndsWith(".Prefab"))
                    {
                        EditorAssetCategoryOverride EditorAssetCategoryOverride =
                            prefabBase.AddOrGetComponent<EditorAssetCategoryOverride>();
                        EditorAssetCategoryOverride.m_ExcludeCategories ??= new string[0];
                        if (
                            EditorAssetCategoryOverride.m_IncludeCategories.Length == 1
                            && (
                                EditorAssetCategoryOverride.m_ExcludeCategories[0] == null
                                || EditorAssetCategoryOverride.m_ExcludeCategories[0] == "null"
                            )
                        )
                        {
                            EditorAssetCategoryOverride.m_ExcludeCategories[0] = cat;
                            break;
                        }
                        Array.Resize(
                            ref EditorAssetCategoryOverride.m_ExcludeCategories,
                            EditorAssetCategoryOverride.m_ExcludeCategories.Length + 1
                        );
                        if (EditorAssetCategoryOverride.m_ExcludeCategories.Length > 0)
                        {
                            foreach (var item in EditorAssetCategoryOverride.m_ExcludeCategories)
                            {
                                if (item.Equals(cat))
                                {
                                    breakOuterLoop = true;
                                    break;
                                }
                            }
                        }
                        if (breakOuterLoop)
                        {
                            break;
                        }
                        EditorAssetCategoryOverride.m_ExcludeCategories[^1] = cat;

                        AssetDataPath adp_main = AssetDataPath.Create(
                            prefabBase.asset.subPath,
                            prefabBase.asset.name,
                            EscapeStrategy.None
                        );
                        AssetDatabase
                            .user.AddAsset(adp_main, prefabBase)
                            .Save(GetPrefabContentType(prefabBase), false, true);
                        Mod.log.Info(
                            $"{cat} (EditorAssetCategoryOverride.m_ExcludeCategories) added to {entityName}"
                        );
                        prefabSystem.UpdatePrefab(prefabBase);
                    }
                }
            }
        }

        public void AddUIIcon(string path)
        {
            List<string> folderNames = GetValidFolders(path);
            foreach (var folderName in folderNames)
            {
                var allAssetEntities = allAssets.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in allAssetEntities)
                {
                    string entityName = prefabSystem.GetPrefabName(entity);
                    prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase);

                    if (prefabBase.asset == null)
                        continue;

                    string assetPath = prefabBase.asset.path;
                    if (assetPath.Contains(folderName) && assetPath.EndsWith(".Prefab"))
                    {
                        UIObject UIObjectComp = prefabBase.AddOrGetComponent<UIObject>();

                        string pngPath = assetPath.Replace(".Prefab", ".png");
                        string pngCidPath = assetPath.Replace(".Prefab", ".png.cid");
                        if (File.Exists(pngPath))
                        {
                            string cid;
                            if (File.Exists(pngCidPath))
                            {
                                cid = File.ReadAllText(pngCidPath);
                                UIObjectComp.m_Icon = $"assetdb://Global/{cid}";
                            }
                            else
                            {
                                string newCid = Colossal.Hash128.CreateGuid(pngCidPath).ToString();
                                File.WriteAllText(pngCidPath, newCid);
                                cid = newCid;
                            }
                            if (!string.IsNullOrEmpty(cid))
                            {
                                UIObjectComp.m_Icon = $"assetdb://Global/{cid}";

                                AssetDataPath adp_main = AssetDataPath.Create(
                                    prefabBase.asset.subPath,
                                    prefabBase.asset.name,
                                    EscapeStrategy.None
                                );
                                AssetDatabase
                                    .user.AddAsset(adp_main, prefabBase)
                                    .Save(GetPrefabContentType(prefabBase), false, true);
                                Mod.log.Info($"Icon being added to {entityName}");
                                prefabSystem.UpdatePrefab(prefabBase);
                            }
                        }
                        else
                        {
                            Mod.log.Info($"No icon found for {entityName}");
                        }
                    }
                }
            }
        }

        public void AddPlaceableObject(string path)
        {
            List<string> folderNames = GetValidFolders(path);
            foreach (var folderName in folderNames)
            {
                var allAssetEntities = allAssets.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in allAssetEntities)
                {
                    string entityName = prefabSystem.GetPrefabName(entity);
                    prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase);

                    if (prefabBase.asset == null)
                        continue;
                    if (prefabBase.Has<PlaceableObject>())
                        continue;
                    if (prefabBase.prefab.GetType() != typeof(StaticObjectPrefab))
                        continue;

                    string assetPath = prefabBase.asset.path;
                    if (assetPath.Contains(folderName) && assetPath.EndsWith(".Prefab"))
                    {
                        PlaceableObject PlaceableObjectComp =
                            prefabBase.AddComponent<PlaceableObject>();

                        PlaceableObjectComp.m_ConstructionCost = 0;
                        PlaceableObjectComp.m_XPReward = 0;

                        AssetDataPath adp_main = AssetDataPath.Create(
                            prefabBase.asset.subPath,
                            prefabBase.asset.name,
                            EscapeStrategy.None
                        );
                        AssetDatabase
                            .user.AddAsset(adp_main, prefabBase)
                            .Save(GetPrefabContentType(prefabBase), false, true);
                        Mod.log.Info($"PlaceableObject being added to {entityName}");
                        prefabSystem.UpdatePrefab(prefabBase);
                    }
                }
            }
        }

        public void AddUIGroup(string path, string uiGroup)
        {
            List<string> folderNames = GetValidFolders(path);
            foreach (var folderName in folderNames)
            {
                if (
                    prefabSystem.TryGetPrefab(
                        new PrefabID("UIAssetCategoryPrefab", uiGroup),
                        out PrefabBase assetCat
                    )
                )
                {
                    var allAssetEntities = allAssets.ToEntityArray(Allocator.Temp);
                    foreach (Entity entity in allAssetEntities)
                    {
                        string entityName = prefabSystem.GetPrefabName(entity);
                        prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase);

                        if (prefabBase.asset == null)
                            continue;
                        if (prefabBase.prefab.GetType() == typeof(RenderPrefab))
                            continue;

                        if (prefabBase.asset.path.Contains(folderName))
                        {
                            UIObject UIObject = prefabBase.AddOrGetComponent<UIObject>();

                            UIObject.m_Group ??= (UIGroupPrefab)assetCat;

                            AssetDataPath adp_main = AssetDataPath.Create(
                                prefabBase.asset.subPath,
                                prefabBase.asset.name,
                                EscapeStrategy.None
                            );
                            AssetDatabase
                                .user.AddAsset(adp_main, prefabBase)
                                .Save(GetPrefabContentType(prefabBase), false, true);
                            Mod.log.Info($"{uiGroup} being added to {entityName}");
                            prefabSystem.UpdatePrefab(prefabBase);
                        }
                    }
                }
            }
        }

        public void RemoveObsoletes(string path)
        {
            List<string> folderNames = GetValidFolders(path);
            foreach (var folderName in folderNames)
            {
                var allAssetEntities = allAssets.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in allAssetEntities)
                {
                    string entityName = prefabSystem.GetPrefabName(entity);
                    prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase);

                    if (prefabBase.asset == null)
                        continue;

                    string assetPath = prefabBase.asset.path;
                    if (assetPath.Contains(folderName))
                    {
                        prefabBase.Remove<ObsoleteIdentifiers>();

                        AssetDataPath adp_main = AssetDataPath.Create(
                            prefabBase.asset.subPath,
                            prefabBase.asset.name,
                            EscapeStrategy.None
                        );
                        AssetDatabase
                            .user.AddAsset(adp_main, prefabBase)
                            .Save(GetPrefabContentType(prefabBase), false, true);
                        Mod.log.Info($"Removing ObsoleteIdentifiers from {entityName}");
                        prefabSystem.UpdatePrefab(prefabBase);
                    }
                }
            }
        }

        public void RemoveSpawnables(string path)
        {
            List<string> folderNames = GetValidFolders(path);
            foreach (var folderName in folderNames)
            {
                var allAssetEntities = allAssets.ToEntityArray(Allocator.Temp);
                Mod.log.Info($"Testing {allAssetEntities.Length} entities");
                foreach (Entity entity in allAssetEntities)
                {
                    string entityName = prefabSystem.GetPrefabName(entity);
                    prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase);

                    if (prefabBase.asset == null)
                        continue;

                    string assetPath = prefabBase.asset.path;
                    if (assetPath.Contains(folderName))
                    {
                        prefabBase.Remove<SpawnableObject>();

                        AssetDataPath adp_main = AssetDataPath.Create(
                            prefabBase.asset.subPath,
                            prefabBase.asset.name,
                            EscapeStrategy.None
                        );
                        AssetDatabase
                            .user.AddAsset(adp_main, prefabBase)
                            .Save(GetPrefabContentType(prefabBase), false, true);
                        Mod.log.Info($"Removing SpawnableObject from {entityName}");
                        prefabSystem.UpdatePrefab(prefabBase);
                    }
                }
            }
        }

        public void AddAssetPack(string path, string pack)
        {
            List<string> folderNames = GetValidFolders(path);
            foreach (var folderName in folderNames)
            {
                if (
                    prefabSystem.TryGetPrefab(
                        new PrefabID("AssetPackPrefab", pack),
                        out PrefabBase assetPackPrefab
                    )
                )
                {
                    var allAssetEntities = allAssets.ToEntityArray(Allocator.Temp);
                    foreach (Entity entity in allAssetEntities)
                    {
                        string entityName = prefabSystem.GetPrefabName(entity);
                        prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase);

                        if (prefabBase.asset == null)
                            continue;
                        if (prefabBase.prefab.GetType() == typeof(RenderPrefab))
                            continue;
                        if (prefabBase.prefab.GetType() == typeof(AssetPackPrefab))
                            continue;

                        if (prefabBase.asset.path.Contains(folderName))
                        {
                            AssetPackPrefab apPrefab = (AssetPackPrefab)assetPackPrefab;
                            AssetPackItem AssetPackItem =
                                prefabBase.AddOrGetComponent<AssetPackItem>();

                            AssetPackItem.m_Packs ??= new AssetPackPrefab[0];
                            Array.Resize(
                                ref AssetPackItem.m_Packs,
                                AssetPackItem.m_Packs.Length + 1
                            );
                            AssetPackItem.m_Packs[^1] = apPrefab;

                            AssetDataPath adp_main = AssetDataPath.Create(
                                prefabBase.asset.subPath,
                                prefabBase.asset.name,
                                EscapeStrategy.None
                            );
                            AssetDatabase
                                .user.AddAsset(adp_main, prefabBase)
                                .Save(GetPrefabContentType(prefabBase), false, true);
                            Mod.log.Info($"Adding {pack} to {entityName}");
                            prefabSystem.UpdatePrefab(prefabBase);
                        }
                    }
                }
            }
        }

        public void RemoveAssetPacks(string path)
        {
            List<string> folderNames = GetValidFolders(path);
            foreach (var folderName in folderNames)
            {
                var allAssetEntities = allAssets.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in allAssetEntities)
                {
                    string text = "";
                    string entityName = prefabSystem.GetPrefabName(entity);
                    text += entityName;
                    prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase);

                    if (prefabBase.asset == null)
                        continue;

                    string assetPath = prefabBase.asset.path;
                    if (assetPath.Contains(folderName))
                    {
                        text += $" being AssetPackPrefab removed";
                        prefabBase.Remove<AssetPackItem>();

                        AssetDataPath adp_main = AssetDataPath.Create(
                            prefabBase.asset.subPath,
                            prefabBase.asset.name,
                            EscapeStrategy.None
                        );
                        AssetDatabase
                            .user.AddAsset(adp_main, prefabBase)
                            .Save(GetPrefabContentType(prefabBase), false, true);
                        Mod.log.Info(text);
                        prefabSystem.UpdatePrefab(prefabBase);
                    }
                }
            }
        }

        public List<string> GetValidFolders(string pattern)
        {
            var results = new List<string>();
            if (string.IsNullOrEmpty(pattern))
            {
                return results;
            }

            pattern = pattern.Replace("\\", "/").Trim('/');

            string root;
            string relativePattern;
            if (pattern.StartsWith("mods_subscribed"))
            {
                root = Path.Combine(EnvPath.kCachePathName, "Mods");
                relativePattern = pattern["mods_subscribed/".Length..];
            }
            else if (pattern.StartsWith("StreamingData~"))
            {
                root = EnvPath.kUserDataPath;
                relativePattern = pattern;
            }
            else
            {
                root = $"{EnvPath.kUserDataPath}/StreamingData~";
                relativePattern = pattern;
            }

            RecursiveGlob(root, relativePattern, results);

            //Mod.log.Info($"{string.Join(", ", results)}");
            return results;
        }

        private void RecursiveGlob(string currentDir, string pattern, List<string> results)
        {
            if (!pattern.Contains("/"))
            {
                try
                {
                    foreach (var dir in Directory.GetDirectories(currentDir, pattern))
                        results.Add($"{dir.Replace("\\", "/")}/");
                }
                catch { }
                return;
            }

            string[] parts = pattern.Split(new[] { '/' }, 2);
            string currentPattern = parts[0];
            string remainingPattern = parts[1];

            try
            {
                foreach (var match in Directory.GetDirectories(currentDir, currentPattern))
                {
                    RecursiveGlob(match, remainingPattern, results);
                }
            }
            catch { }
        }

        public ContentType GetPrefabContentType(PrefabBase prefab)
        {
            using Stream stream = prefab.asset.GetReadStream();
            return stream.ReadByte() != 123 ? ContentType.Binary : ContentType.Text;
        }

        public void ConvertLocale(string path, string lang)
        {
            LocalizationManager localizationManager = Game.SceneFlow
                .GameManager
                .instance
                .localizationManager;
            if (string.IsNullOrEmpty(path))
            {
                Mod.log.Info(
                    $"Failed to convert locale: [ string.IsNullOrEmpty(path) : {string.IsNullOrEmpty(path)} ]"
                );
                return;
            }
            if (!File.Exists(path))
            {
                Mod.log.Info($"File ({path}) not found.");
                return;
            }

            Dictionary<string, LocaleData> dictionary = new();

            string[] supLang = localizationManager.GetSupportedLocales();
            if (!supLang.Contains(lang))
            {
                Mod.log.Info(
                    $"{lang} is not a supported locale. Try one of these: '{string.Join(",", supLang)}'"
                );
                return;
            }
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
                        Mod.log.Info("JSON is empty or invalid.");
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

                    Mod.log.Info(
                        $"Successfully loaded {parsed.Count} entries for language '{lang}'."
                    );
                }
                catch (JsonException ex)
                {
                    Mod.log.Info("Invalid JSON: " + ex.Message);
                }

                foreach (var assetEntry in groupedByAsset)
                {
                    string assetName = assetEntry.Key;
                    Dictionary<string, string> entries = assetEntry.Value;
                    LocaleData localeData = new(lang, entries, new Dictionary<string, int>());

                    LocaleAsset localeAsset = AssetDatabase.user.AddAsset<LocaleAsset>(
                        AssetDataPath.Create(
                            $"StreamingData~/CreatedLocalization/" + assetName,
                            assetName + "_" + localeData.localeId,
                            EscapeStrategy.Filename
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
                    Mod.log.Info($"Saving {assetName} locales");
                }
            }
        }
    }
}
