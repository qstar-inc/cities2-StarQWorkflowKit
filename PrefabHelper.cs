using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Environment;
using Game.Prefabs;
using Game;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System.Linq;
using System;
using Unity.Collections;
using Unity.Entities;

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
            allAssets = CreateQuery(all: new[] { ComponentType.ReadWrite<PrefabData>() });
        }

        protected override void OnUpdate()
        {
        }

        public EntityQuery CreateQuery(ComponentType[]? all = null, ComponentType[]? none = null, ComponentType[]? any = null)
        {
            return GetEntityQuery(new EntityQueryDesc
            {
                All = all ?? Array.Empty<ComponentType>(),
                None = none ?? Array.Empty<ComponentType>(),
                Any = any ?? Array.Empty<ComponentType>()
            });
        }

        public void DoAsset(PrefabBase prefab, int num, bool noLog = false)
        {
            ContentType ct = GetPrefabContentType(prefab);
            if (num == 3 || num == 2) ct = ContentType.Binary;
            string xt = ct switch
            {
                ContentType.Text => " (Text)",
                ContentType.Binary => " (Binary)",
                _ => "",
            };
            if (!noLog) Mod.log_timed.Info($"Saving {prefab.name}{xt}");
            prefab.asset.Save(ct, false, true);
        }

        public void SaveAsset(string path, int num = 1, bool noLog = false)
        {
            string folderName = GetValidFolder(path);
            if (string.IsNullOrEmpty(folderName))
            {
                Mod.log.Info($"Failed to save asset: [ string.IsNullOrEmpty(path) : {string.IsNullOrEmpty(path)} ]");
                return;
            }

            var entities = allAssets.ToEntityArray(Allocator.Temp);
            Mod.log.Info($"Checking {entities.Count()} entities.");

            int i = 0;
            foreach (Entity entity in entities)
            {
                i++;
                if (EntityManager.TryGetComponent(entity, out PrefabData prefabData) && prefabSystem.TryGetPrefab(prefabData, out PrefabBase prefabBase))
                {
                    if (prefabBase.builtin)
                    {
                        continue;
                    }
                    try
                    {
                        if (prefabBase.asset != null && prefabBase.asset.path != null && prefabBase.asset.path.Contains(folderName) && prefabBase.asset.path.EndsWith(".Prefab"))
                        {
                            DoAsset(prefabBase, num, noLog);
                        }
                    }
                    catch (Exception ex) { Mod.log.Info(ex); }
                }
            }
            if (num == 2)
            {
                CreatePackage(folderName, true);
                SaveAsset(path, 1, true);
            }
            Mod.log.Info($"Done.");
        }

        public void CreatePackage(string path, bool folderValid = false, bool direct = false)
        {
            if (!folderValid) path = GetValidFolder(path);
            if (string.IsNullOrEmpty(path))
            {
                Mod.log.Info($"Failed to CreatePackage: [ string.IsNullOrEmpty(path) : {string.IsNullOrEmpty(path)} ]");
                return;
            }
            Mod.log.Info("Creating Packages...");

            string exportFolder = EnvPath.kUserDataPath + "/StreamingData~/~CreatedPackages";
            
            if (direct)
            {
                string folderName = Path.GetFileName(path);
                string outputCokPath = Path.Combine(exportFolder, folderName + ".cok");
                string outputCidPath = Path.Combine(exportFolder, folderName + ".cok.cid");
                CreateCokAndCid(outputCokPath, outputCidPath, path);
                return;
            }

            if (!Directory.Exists(exportFolder))
            {
                Directory.CreateDirectory(exportFolder);
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

        public void CreateCokAndCid(string outputCokPath, string outputCidPath, string folderPath)
        {
            using (FileStream zipFile = File.Create(outputCokPath))
            using (ZipOutputStream zipStream = new(zipFile))
            {
                zipStream.SetLevel(0);
                foreach (string filePath in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
                {
                    string arcname = filePath.Substring(folderPath.Length + 1).Replace("\\", "/");

                    try
                    {
                        ZipEntry entry = new(arcname)
                        {
                            CompressionMethod = CompressionMethod.Stored,
                            DateTime = File.GetLastWriteTime(filePath)
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

        public void AddEditorAssetCategoryOverride(string path, string cat)
        {
            string folderName = GetValidFolder(path);
            
            if (string.IsNullOrEmpty(folderName) || string.IsNullOrEmpty(cat))
            {
                Mod.log.Info($"Failed to add EditorAssetCategoryOverride: [ string.IsNullOrEmpty(path) : {string.IsNullOrEmpty(path)}, string.IsNullOrEmpty(cat) : {string.IsNullOrEmpty(cat)} ]");
                return;
            }
            var allAssetEntities = allAssets.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in allAssetEntities)
            {
                string text = "";
                string entityName = prefabSystem.GetPrefabName(entity);
                text += entityName;
                prefabSystem.TryGetPrefab(entity, out PrefabBase bldgPrefab);

                if (bldgPrefab.asset == null) continue;

                string assetPath = bldgPrefab.asset.path;
                if (assetPath.Contains(folderName) && assetPath.EndsWith(".Prefab"))
                {
                    text += $" being EditorAssetCategoryOverride added";
                    EditorAssetCategoryOverride EditorAssetCategoryOverride = bldgPrefab.AddOrGetComponent<EditorAssetCategoryOverride>();
                    EditorAssetCategoryOverride.m_IncludeCategories ??= new string[0];
                    Array.Resize(ref EditorAssetCategoryOverride.m_IncludeCategories, EditorAssetCategoryOverride.m_IncludeCategories.Length + 1);
                    EditorAssetCategoryOverride.m_IncludeCategories[EditorAssetCategoryOverride.m_IncludeCategories.Length - 1] = cat;

                    AssetDataPath adp_main = AssetDataPath.Create(bldgPrefab.asset.subPath, bldgPrefab.asset.name, EscapeStrategy.None);
                    AssetDatabase.user.AddAsset(adp_main, bldgPrefab).Save(GetPrefabContentType(bldgPrefab), false, true);
                    Mod.log.Info(text);
                }
            }
        }

        public void AddUIIcon(string path)
        {
            string folderName = GetValidFolder(path);

            if (string.IsNullOrEmpty(folderName))
            {
                Mod.log.Info($"Failed to add UIIcon: [ string.IsNullOrEmpty(path) : {string.IsNullOrEmpty(path)} ]");
                return;
            }
            var allAssetEntities = allAssets.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in allAssetEntities)
            {
                string text = "";
                string entityName = prefabSystem.GetPrefabName(entity);
                text += entityName;
                prefabSystem.TryGetPrefab(entity, out PrefabBase bldgPrefab);

                if (bldgPrefab.asset == null) continue;

                string assetPath = bldgPrefab.asset.path;
                if (assetPath.Contains(folderName) && assetPath.EndsWith(".Prefab"))
                {
                    text += $" being UI Icon added";
                    UIObject UIObjectComp = bldgPrefab.AddOrGetComponent<UIObject>();

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
                        if (string.IsNullOrEmpty(cid))
                        {
                            UIObjectComp.m_Icon = $"assetdb://Global/{cid}";

                            AssetDataPath adp_main = AssetDataPath.Create(bldgPrefab.asset.subPath, bldgPrefab.asset.name, EscapeStrategy.None);
                            AssetDatabase.user.AddAsset(adp_main, bldgPrefab).Save(GetPrefabContentType(bldgPrefab), false, true);
                            Mod.log.Info(text);
                        }
                    }
                    else
                    {
                        Mod.log.Info($"No icon found for {entityName}");
                    }
                }
            }
        }

        public void RemoveObsoletes(string path)
        {
            string folderName = GetValidFolder(path);
            if (string.IsNullOrEmpty(folderName))
            {
                Mod.log.Info($"Failed to remove ObsoleteIdentifiers: [ string.IsNullOrEmpty(path) : {string.IsNullOrEmpty(path)} ]");
                return;
            }
            var allAssetEntities = allAssets.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in allAssetEntities)
            {
                string text = "";
                string entityName = prefabSystem.GetPrefabName(entity);
                text += entityName;
                prefabSystem.TryGetPrefab(entity, out PrefabBase bldgPrefab);

                if (bldgPrefab.asset == null) continue;

                string assetPath = bldgPrefab.asset.path;
                if (assetPath.Contains(folderName))
                {
                    text += $" being ObsoleteIdentifiers removed";
                    bldgPrefab.Remove<ObsoleteIdentifiers>();

                    AssetDataPath adp_main = AssetDataPath.Create(bldgPrefab.asset.subPath, bldgPrefab.asset.name, EscapeStrategy.None);
                    AssetDatabase.user.AddAsset(adp_main, bldgPrefab).Save(GetPrefabContentType(bldgPrefab), false, true);
                    Mod.log.Info(text);
                }
            }
        }

        public void RemoveSpawnables(string path)
        {
            string folderName = GetValidFolder(path);
            if (string.IsNullOrEmpty(folderName))
            {
                Mod.log.Info($"Failed to remove SpawnableObject: [ string.IsNullOrEmpty(path) : {string.IsNullOrEmpty(path)}]");
                return;
            }
            var allAssetEntities = allAssets.ToEntityArray(Allocator.Temp);
            Mod.log.Info($"Testing {allAssetEntities.Length} entities");
            foreach (Entity entity in allAssetEntities)
            {
                string text = "";
                string entityName = prefabSystem.GetPrefabName(entity);
                text += entityName;
                prefabSystem.TryGetPrefab(entity, out PrefabBase bldgPrefab);

                if (bldgPrefab.asset == null) continue;

                string assetPath = bldgPrefab.asset.path;
                if (assetPath.Contains(folderName))
                {
                    text += $" being SpawnableObject removed";
                    bldgPrefab.Remove<SpawnableObject>();

                    AssetDataPath adp_main = AssetDataPath.Create(bldgPrefab.asset.subPath, bldgPrefab.asset.name, EscapeStrategy.None);
                    AssetDatabase.user.AddAsset(adp_main, bldgPrefab).Save(GetPrefabContentType(bldgPrefab), false, true);
                    Mod.log.Info(text);
                }
            }
        }

        public string GetValidFolder(string path)
        {
            path = path.Replace("\\", "/");
            path = path.Trim('/');
            string folder = path;
            if (Directory.Exists($"{EnvPath.kUserDataPath}/StreamingData~/{path}"))
            {
                return $"{EnvPath.kUserDataPath}/StreamingData~/{path}";
            }
            if (path.StartsWith("mods_subscribed"))
            {
                folder = $"{EnvPath.kCachePathName}/Mods/{path}";
            }
            if (path.StartsWith("StreamingData~"))
            {
                folder = $"{EnvPath.kUserDataPath}/{path}";
            }
            if (Directory.Exists(folder))
            {
                return folder;
            }

            return "";
        }

        public ContentType GetPrefabContentType(PrefabBase prefab)
        {
            using Stream stream = prefab.asset.GetReadStream();
            return stream.ReadByte() != 123 ? ContentType.Binary : ContentType.Text;
        }
    }
}
