using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.PSI.Common;
using Colossal.PSI.Environment;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using Game.UI.Editor;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                        if (prefabBase.isReadOnly)
                            continue;

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

            //string exportFolder = EnvPath.kUserDataPath + "/ImportedData/~CreatedPackages";

            //if (!Directory.Exists(exportFolder))
            //    Directory.CreateDirectory(exportFolder);

            foreach (var validPath in validPaths)
            {
                string validPathToUse = validPath.Trim('/');
                if (direct)
                {
                    string folderName = Path.GetFileName(validPathToUse);
                    if (folderName.StartsWith(".") || folderName.StartsWith("~"))
                        continue;

                    //string outputCokPath = Path.Combine(exportFolder, folderName + ".cok");
                    //string outputCidPath = Path.Combine(exportFolder, folderName + ".cok.cid");
                    //CreateCokAndCid(outputCokPath, outputCidPath, validPath);
                    CreateCokAndCidNew(folderName, validPath).Wait();
                    continue;
                }

                if (!Directory.EnumerateDirectories(validPathToUse).Any())
                {
                    string folderName = Path.GetFileName(validPathToUse);
                    if (folderName.StartsWith(".") || folderName.StartsWith("~"))
                        continue;

                    //string outputCokPath = Path.Combine(exportFolder, folderName + ".cok");
                    //string outputCidPath = Path.Combine(exportFolder, folderName + ".cok.cid");
                    //CreateCokAndCid(outputCokPath, outputCidPath, validPathToUse);
                    CreateCokAndCidNew(folderName, validPath).Wait();
                }
                else
                {
                    foreach (string folderPath in Directory.GetDirectories(validPathToUse))
                    {
                        string folderName = Path.GetFileName(folderPath);
                        if (folderName.StartsWith(".") || folderName.StartsWith("~"))
                            continue;

                        //string outputCokPath = Path.Combine(exportFolder, folderName + ".cok");
                        //string outputCidPath = Path.Combine(exportFolder, folderName + ".cok.cid");

                        //CreateCokAndCid(outputCokPath, outputCidPath, folderPath);
                        CreateCokAndCidNew(folderName, validPath).Wait();
                    }
                }
            }
        }

        public async Task CreateCokAndCidNew(string packageName, string folderPath)
        {
            Type helperType = typeof(PackageHelper);

            MethodInfo method = helperType.GetMethod(
                "BuildPackage",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            if (method == null)
            {
                Mod.log.Info("BuildPackage not found");
                return;
            }

            DependencyCollector collector = new();

            Dictionary<string, AssetData> assets = GetAllAssets(folderPath);
            foreach (var asset in assets)
            {
                collector.AddAsset(asset.Value, true);
            }

            AssetData[] array = collector.CompileAllAssets();
            PackageDependencies.Data data = new()
            {
                modDependencies = collector.modDependencies.ToArray<int>(),
                dlcDependencies = collector
                    .dlcDependencies.Select(dlc => PlatformManager.instance.GetDlcName(dlc))
                    .ToArray(),
                previews = collector.previews.Keys.ToArray(),
            };

            PackageDependencies.Data data2 = data;
            if (array.Length != 0)
            {
                try
                {
                    Colossal.Core.MainThreadDispatcher.RunOnMainThread(async () =>
                    {
                        object result = method.Invoke(
                            null,
                            new object[] { packageName, collector.mainAsset, array, data2, null }
                        );
                        Task<PackageAsset> task = (Task<PackageAsset>)result;
                        PackageAsset pkg = await task;
                        LogHelper.SendLog("Build completed: " + packageName);
                    });
                }
                catch (Exception ex)
                {
                    LogHelper.SendLog($"Error packaging {packageName}: {ex}", LogLevel.Error);
                }
            }
        }

        public void CreateCokAndCid(string outputCokPath, string outputCidPath, string folderPath)
        {
            bool hasTextureFiles = Directory
                .EnumerateFiles(folderPath, "*.Texture", SearchOption.AllDirectories)
                .Any();

            if (hasTextureFiles)
            {
                LogHelper.SendLog(
                    $"Skipping package creation for '{folderPath}' because it contains non VT texture files."
                );
                return;
            }

            string pkgJsonPath = Path.Combine(folderPath, "package.json");
            string pkgDepsPath = Path.Combine(folderPath, "package.deps");

            JArray modDeps = new();
            JArray dlcDeps = new();

            if (File.Exists(pkgJsonPath) && !File.Exists(pkgDepsPath))
            {
                try
                {
                    var pkgJson = File.ReadAllText(pkgJsonPath);
                    JObject jsonObject = JObject.Parse(pkgJson);

                    if (jsonObject["modDependencies"] is JArray modArray)
                    {
                        foreach (var item in modArray)
                        {
                            if (item.Type == JTokenType.Integer)
                                modDeps.Add((int)item);
                            else if (
                                item.Type == JTokenType.String
                                && int.TryParse((string)item, out int parsed)
                            )
                                modDeps.Add(parsed);
                        }
                    }

                    if (jsonObject["dlcDependencies"] is JArray dlcArray)
                    {
                        foreach (var item in dlcArray)
                            if (item.Type == JTokenType.String)
                                dlcDeps.Add((string)item);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.SendLog($"package.json is invalid\n{ex.Message}");
                }

                var output = new JObject
                {
                    ["modDependencies"] = modDeps,
                    ["dlcDependencies"] = dlcDeps,
                    ["previews"] = new JArray(),
                };
                File.WriteAllText(pkgDepsPath, output.ToString(Formatting.Indented));
            }

            string cidPath = pkgDepsPath + ".cid";
            if (!File.Exists(cidPath))
            {
                string newCidPkg = Colossal.Hash128.CreateGuid(cidPath).ToString();
                File.WriteAllText(cidPath, newCidPkg);
            }

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
                    string relativePath = Path.GetRelativePath(folderPath, filePath)
                        .Replace("\\", "/");
                    string[] parts = relativePath.Split('/');

                    bool shouldSkip = false;
                    foreach (string part in parts)
                    {
                        if (part.StartsWith(".") || part.StartsWith("~"))
                        {
                            shouldSkip = true;
                            break;
                        }
                    }

                    if (shouldSkip)
                        continue;

                    string arcname = relativePath;

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
            List<PrefabBase> prefabs = GetValidPrefabs(path);
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
            List<PrefabBase> prefabs = GetValidPrefabs(path);
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
            List<PrefabBase> assets = GetValidPrefabs(path);
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
            List<PrefabBase> prefabs = GetValidPrefabs(path);
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
                List<PrefabBase> prefabs = GetValidPrefabs(path);
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
            List<PrefabBase> prefabs = GetValidPrefabs(path);
            foreach (var prefabBase in prefabs)
            {
                prefabBase.Remove<ObsoleteIdentifiers>();

                SaveAndContinue(prefabBase, ChangeType.Removing, "ObsoleteIdentifiers");
            }
        }

        public void RemoveSpawnables(string path)
        {
            List<PrefabBase> prefabs = GetValidPrefabs(path);
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

            List<PrefabBase> prefabs = GetValidPrefabs(path);
            foreach (var prefabBase in prefabs)
            {
                if (prefabBase.prefab.GetType() == typeof(RenderPrefab))
                    continue;
                if (prefabBase.prefab.GetType() == typeof(AssetPackPrefab))
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
                }

                Array.Resize(ref AssetPackItem.m_Packs, AssetPackItem.m_Packs.Length + 1);
                AssetPackItem.m_Packs[^1] = apPrefab;

                SaveAndContinue(prefabBase, ChangeType.Adding, $"AssetPackItem: {pack}");
            }
        }

        public void RemoveAssetPack(string path, string pack)
        {
            List<PrefabBase> prefabs = GetValidPrefabs(path);
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

        public Dictionary<string, AssetData> GetAllAssets(string path)
        {
            Dictionary<string, AssetData> assets = new();
            List<string> strings = new();

            foreach (
                AssetData assetData in from asset in AssetDatabase.user.GetAssets<AssetData>(
                    default
                )
                where asset.id.guid.isValid
                select asset
            )
            {
                if (assetData.path.Contains(path))
                {
                    string title = $"{assetData.name} ({assetData.id.guid})";
                    assets[title] = assetData;
                    if (!strings.Contains(title))
                        strings.Add(title);
                }
            }

            LogHelper.SendLog($"Collected {assets.Count} assets:\n{string.Join("\n", strings)}");

            return assets;
        }

        public List<PrefabBase> GetValidPrefabs(string path)
        {
            List<string> folderNames = GetValidFolders(path);
            List<PrefabBase> prefabs = new();
            var pm = AssetDatabase.user.GetAssets<PrefabAsset>();

            foreach (var prefabAsset in pm)
            {
                PrefabBase prefabBase = prefabAsset.GetInstance<PrefabBase>();

                if (prefabBase == null || prefabBase.asset == null)
                    continue;

                foreach (var folderName in folderNames)
                {
                    string assetPath = prefabBase.asset.path;
                    if (!prefabs.Contains(prefabBase) && assetPath.Contains(folderName))
                        prefabs.Add(prefabBase);
                }
            }

            LogHelper.SendLog(prefabs.Count + " prefabs found");
            return prefabs;
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
            else if (pattern.StartsWith("ImportedData") || pattern.StartsWith("StreamingData~"))
            {
                root = EnvPath.kUserDataPath;
                relativePattern = pattern;
            }
            else if (
                Regex.IsMatch(
                    pattern,
                    "LocalLow[\\/]*Colossal Order[\\/]*Cities Skylines II[\\/]*(.+)"
                )
            )
            {
                var match = Regex.Match(
                    pattern,
                    "LocalLow[\\/]*Colossal Order[\\/]*Cities Skylines II[\\/]*(.+)"
                );
                root = EnvPath.kUserDataPath;
                relativePattern = match.Value[0].ToString();
            }
            else
            {
                root = $"{Path.Combine(EnvPath.kUserDataPath, "ImportedData")}";
                relativePattern = pattern;
            }

            RecursiveGlob(root, relativePattern, ref results);

            LogHelper.SendLog($"Folders to scan: {string.Join(", ", results)}");
            return results;
        }

        private void RecursiveGlob(string currentDir, string pattern, ref List<string> results)
        {
            if (!pattern.Contains("/"))
            {
                try
                {
                    foreach (var dir in Directory.GetDirectories(currentDir, pattern))
                    {
                        if (dir.StartsWith(".") || dir.StartsWith("~"))
                            continue;
                        results.Add($"{dir.Replace("\\", "/")}/");
                    }
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
                    RecursiveGlob(match, remainingPattern, ref results);
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
            List<PrefabBase> prefabs = GetValidPrefabs(path);
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

            List<PrefabBase> prefabs = GetValidPrefabs(path);
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
                .Save(GetPrefabContentType(prefabBase), false, true);
            LogHelper.SendLog(logText);
            prefabSystem.UpdatePrefab(prefabBase);
        }
    }
}
