using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.PSI.Environment;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using StarQ.Shared.Extensions;
using Unity.Collections;
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
            if (prefab.TryGet(out EditorAssetCategoryOverride eaco))
            {
                if (eaco.m_IncludeCategories.Length > 0)
                {
                    for (int i = 0; i < eaco.m_IncludeCategories.Length; i++)
                    {
                        if (eaco.m_IncludeCategories[i].StartsWith("WorkflowKit"))
                        {
                            eaco.m_IncludeCategories = eaco
                                .m_IncludeCategories.Where((value, index) => index != i)
                                .ToArray();
                        }
                    }
                    if (
                        eaco.m_IncludeCategories.Length == 0
                        && (
                            eaco.m_ExcludeCategories == null || eaco.m_ExcludeCategories.Length == 0
                        )
                    )
                    {
                        prefab.Remove<EditorAssetCategoryOverride>();
                    }
                }
            }

            if (!noLog)
                LogHelper.SendLog($"Saving {prefab.name}{xt}");
            prefab.asset.Save(ct, false, true);
        }

        public void SaveAsset(string path, int num = 1, bool noLog = false)
        {
            var entities = allAssets.ToEntityArray(Allocator.Temp);
            LogHelper.SendLog($"Checking {entities.Count()} entities.");
            List<string> folderNames = GetValidFolders(path);
            List<string> saved = new();
            foreach (var folderName in folderNames)
            {
                int i = 0;
                foreach (Entity entity in entities)
                {
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
                                i++;
                                //Mod.log.Info($"DoAsset({prefabBase.name}, {num}, {noLog})");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.SendLog($"{ex}");
                        }
                    }
                }
                saved.Add($"Resaved {i} prefabs");
                if (num == 2)
                {
                    CreatePackage(folderName, true);
                    SaveAsset(path, 1, true);
                }
            }
            LogHelper.SendLog("\n" + string.Join("\n", saved) + "\nDone!");
        }

        public void CreatePackage(string path, bool folderValid = false, bool direct = false)
        {
            List<string> validPaths = folderValid
                ? new List<string> { path }
                : GetValidFolders(path);

            if (validPaths == null || validPaths.Count == 0)
            {
                LogHelper.SendLog($"Failed to CreatePackage: No valid folder paths found.");
                return;
            }

            LogHelper.SendLog("Creating Packages...");

            string exportFolder = EnvPath.kUserDataPath + "/StreamingData~/~CreatedPackages";

            if (!Directory.Exists(exportFolder))
                Directory.CreateDirectory(exportFolder);

            foreach (var validPath in validPaths)
            {
                string validPathToUse = validPath.Trim('/');
                if (direct)
                {
                    string folderName = Path.GetFileName(validPathToUse);
                    string outputCokPath = Path.Combine(exportFolder, folderName + ".cok");
                    string outputCidPath = Path.Combine(exportFolder, folderName + ".cok.cid");
                    CreateCokAndCid(outputCokPath, outputCidPath, validPath);
                    continue;
                }

                if (!Directory.EnumerateDirectories(validPathToUse).Any())
                {
                    string folderName = Path.GetFileName(validPathToUse);
                    string outputCokPath = Path.Combine(exportFolder, folderName + ".cok");
                    string outputCidPath = Path.Combine(exportFolder, folderName + ".cok.cid");
                    CreateCokAndCid(outputCokPath, outputCidPath, validPathToUse);
                }
                else
                {
                    foreach (string folderPath in Directory.GetDirectories(validPathToUse))
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
                    string arcname = filePath[(folderPath.Trim('/').Length + 1)..]
                        .Replace("\\", "/");

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
                        LogHelper.SendLog($"Error processing {filePath}: {ex.Message}");
                    }
                }
            }

            string newCid = Colossal.Hash128.CreateGuid(outputCokPath).ToString();
            File.WriteAllText(outputCidPath, newCid);

            LogHelper.SendLog($"Created: {outputCokPath.Replace("\\", "/")}");
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
                        if (EditorAssetCategoryOverride.m_IncludeCategories == null)
                        {
                            Array.Resize(ref EditorAssetCategoryOverride.m_IncludeCategories, 0);
                        }
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
                        if (EditorAssetCategoryOverride.m_IncludeCategories.Length > 0)
                        {
                            foreach (var item in EditorAssetCategoryOverride.m_IncludeCategories)
                            {
                                if (item != null && item.Equals(cat))
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
                        Array.Resize(
                            ref EditorAssetCategoryOverride.m_IncludeCategories,
                            EditorAssetCategoryOverride.m_IncludeCategories.Length + 1
                        );
                        EditorAssetCategoryOverride.m_IncludeCategories[^1] = cat;

                        AssetDataPath adp_main = AssetDataPath.Create(
                            prefabBase.asset.subPath,
                            prefabBase.asset.name,
                            EscapeStrategy.None
                        );
                        AssetDatabase
                            .user.AddAsset(adp_main, prefabBase)
                            .Save(GetPrefabContentType(prefabBase), false, true);
                        LogHelper.SendLog(
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

                        if (EditorAssetCategoryOverride.m_ExcludeCategories == null)
                        {
                            Array.Resize(ref EditorAssetCategoryOverride.m_ExcludeCategories, 0);
                        }
                        if (
                            EditorAssetCategoryOverride.m_ExcludeCategories.Length == 1
                            && (
                                EditorAssetCategoryOverride.m_ExcludeCategories[0] == null
                                || EditorAssetCategoryOverride.m_ExcludeCategories[0] == "null"
                            )
                        )
                        {
                            EditorAssetCategoryOverride.m_ExcludeCategories[0] = cat;
                            break;
                        }
                        if (EditorAssetCategoryOverride.m_ExcludeCategories.Length > 0)
                        {
                            foreach (var item in EditorAssetCategoryOverride.m_ExcludeCategories)
                            {
                                if (item != null && item.Equals(cat))
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
                        Array.Resize(
                            ref EditorAssetCategoryOverride.m_ExcludeCategories,
                            EditorAssetCategoryOverride.m_ExcludeCategories.Length + 1
                        );
                        EditorAssetCategoryOverride.m_ExcludeCategories[^1] = cat;

                        AssetDataPath adp_main = AssetDataPath.Create(
                            prefabBase.asset.subPath,
                            prefabBase.asset.name,
                            EscapeStrategy.None
                        );
                        AssetDatabase
                            .user.AddAsset(adp_main, prefabBase)
                            .Save(GetPrefabContentType(prefabBase), false, true);
                        LogHelper.SendLog(
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
                                LogHelper.SendLog($"Icon being added to {entityName}");
                                prefabSystem.UpdatePrefab(prefabBase);
                            }
                        }
                        else
                        {
                            LogHelper.SendLog($"No icon found for {entityName}");
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
                        LogHelper.SendLog($"PlaceableObject being added to {entityName}");
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
                            LogHelper.SendLog($"{uiGroup} being added to {entityName}");
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
                        LogHelper.SendLog($"Removing ObsoleteIdentifiers from {entityName}");
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
                LogHelper.SendLog($"Testing {allAssetEntities.Length} entities");
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
                        LogHelper.SendLog($"Removing SpawnableObject from {entityName}");
                        prefabSystem.UpdatePrefab(prefabBase);
                    }
                }
            }
        }

        public void AddAssetPack(string path, string pack)
        {
            List<string> folderNames = GetValidFolders(path);
            prefabSystem.TryGetPrefab(
                new PrefabID("AssetPackPrefab", pack),
                out PrefabBase assetPackPrefab
            );
            if (!assetPackPrefab)
            {
                assetPackPrefab = ScriptableObject.CreateInstance<AssetPackPrefab>();
                assetPackPrefab.name = pack;

                AssetDataPath adp_ap = AssetDataPath.Create(
                    $"StreamingData~/{assetPackPrefab.name}",
                    $"{assetPackPrefab.name}.Prefab",
                    EscapeStrategy.None
                );
                AssetDatabase
                    .user.AddAsset(adp_ap, assetPackPrefab)
                    .Save(ContentType.Text, false, true);
                LogHelper.SendLog($"Saving {pack}");
                prefabSystem.UpdatePrefab(assetPackPrefab);
            }

            foreach (var folderName in folderNames)
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
                        AssetPackItem AssetPackItem = prefabBase.AddOrGetComponent<AssetPackItem>();

                        AssetPackItem.m_Packs ??= new AssetPackPrefab[0];
                        Array.Resize(ref AssetPackItem.m_Packs, AssetPackItem.m_Packs.Length + 1);
                        AssetPackItem.m_Packs[^1] = apPrefab;

                        AssetDataPath adp_main = AssetDataPath.Create(
                            prefabBase.asset.subPath,
                            prefabBase.asset.name,
                            EscapeStrategy.None
                        );
                        AssetDatabase
                            .user.AddAsset(adp_main, prefabBase)
                            .Save(GetPrefabContentType(prefabBase), false, true);
                        LogHelper.SendLog($"Adding {pack} to {entityName}");
                        prefabSystem.UpdatePrefab(prefabBase);
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
                        LogHelper.SendLog(text);
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
                LogHelper.SendLog("Folder list is empty");
                return results;
            }

            pattern = pattern.Replace("\\", "/").Trim('/').Replace("\"", "");

            string root;
            string relativePattern;
            if (pattern.StartsWith("mods_subscribed"))
            {
                root = Path.Combine(EnvPath.kCacheDataPath, "Mods");
                const string prefix = "mods_subscribed/";
                root = Path.Combine(root, "mods_subscribed");
                relativePattern = pattern.StartsWith(prefix, StringComparison.Ordinal)
                    ? pattern[prefix.Length..]
                    : pattern;
            }
            else if (pattern.StartsWith("StreamingData~"))
            {
                root = EnvPath.kUserDataPath;
                relativePattern = pattern;
            }
            else if (
                Regex.IsMatch(
                    pattern,
                    "LocalLow[\\\\\\/]*Colossal Order[\\\\\\/]*Cities Skylines II[\\\\\\/]*(.+)"
                )
            )
            {
                var match = Regex.Match(
                    pattern,
                    "LocalLow[\\\\\\/]*Colossal Order[\\\\\\/]*Cities Skylines II[\\\\\\/]*(.+)"
                );
                root = EnvPath.kUserDataPath;
                relativePattern = match.Value[0].ToString();
            }
            else
            {
                root = $"{Path.Combine(EnvPath.kUserDataPath, "StreamingData~")}";
                relativePattern = pattern;
            }

            RecursiveGlob(root, relativePattern, results);

            LogHelper.SendLog($"Folders to scan: {string.Join(", ", results)}");
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
                                $"StreamingData~/CreatedLocalization/" + assetName,
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
            List<string> folderNames = GetValidFolders(path);
            foreach (var folderName in folderNames)
            {
                var allAssetEntities = allAssets.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in allAssetEntities)
                {
                    prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase);
                    if (prefabBase.asset == null)
                        continue;
                    string oldName = prefabSystem.GetPrefabName(entity);
                    string prefabType = prefabBase.GetType().Name;

                    if (prefabBase.asset.path.Contains(folderName))
                    {
                        toLog.Add($"{prefabType};{oldName}");
                    }
                }
            }
            toLog.Sort();
            LogHelper.SendLog($"\nList of prefabs:\n{string.Join('\n', toLog)}");
            Task.Run(() => Process.Start(LogHelper.logPath));
        }

        public void RenamePrefab(string path)
        {
            if (!LoadRenameData())
                return;

            List<string> folderNames = GetValidFolders(path);
            foreach (var folderName in folderNames)
            {
                var allAssetEntities = allAssets.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in allAssetEntities)
                {
                    prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase);
                    if (prefabBase.asset == null)
                        continue;
                    string oldName = prefabSystem.GetPrefabName(entity);
                    string prefabType = prefabBase.GetType().Name;

                    if (!toRename.TryGetValue(prefabType, out var inner))
                        continue;
                    if (!inner.TryGetValue(oldName, out var newName))
                        continue;

                    if (!prefabBase.asset.path.Contains(folderName))
                        continue;

                    var contentType = GetPrefabContentType(prefabBase);
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

                    AssetDataPath adp = AssetDataPath.Create(
                        "StreamingData~/~RenamedPrefabs/" + newName,
                        newName,
                        EscapeStrategy.None
                    );
                    AssetDatabase.user.AddAsset(adp, prefabBase).Save(contentType, false, true);
                    LogHelper.SendLog($"Renaming {prefabType}:{oldName} to {prefabType}:{newName}");
                }
            }
        }
    }
}
