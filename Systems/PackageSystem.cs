using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Colossal;
using Colossal.AssetPipeline.Diagnostic;
using Colossal.Entities;
using Colossal.IO;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Common;
using Colossal.PSI.Environment;
using Game;
using Game.AssetPipeline;
using Game.Prefabs;
using Game.UI.Editor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StarQ.Shared.Extensions;
using StarQWorkflowKit.Helper;
using Unity.Collections;
using Unity.Entities;
using static Colossal.AssetPipeline.Diagnostic.Report;
using AssetData = Colossal.IO.AssetDatabase.AssetData;

namespace StarQWorkflowKit.Systems
{
    public partial class PackageSystem : GameSystemBase
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
            ContentType ct = PrefabHelper.GetPrefabContentType(prefab);
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
            List<string> folderNames = PrefabHelper.GetValidFolders(path);
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
            bool useCustomDeps = Mod.m_Setting.UseCustomDeps;

            List<string> validPaths = folderValid
                ? new List<string> { path }
                : PrefabHelper.GetValidFolders(path);

            if (validPaths == null || validPaths.Count == 0)
            {
                LogHelper.SendLog($"Failed to CreatePackage: No valid folder paths found.");
                return;
            }

            LogHelper.SendLog($"Preparing {validPaths.Count} Packages...");

            //string exportFolder = EnvPath.kUserDataPath + "/ImportedData/~CreatedPackages";

            //if (!Directory.Exists(exportFolder))
            //    Directory.CreateDirectory(exportFolder);
            int i = 1;
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
                    LogHelper.SendLog(
                        $"Creating T1Package {i++} of {validPaths.Count}: {folderName}"
                    );
                    SelectMethod(useCustomDeps, folderName, validPath);
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
                    LogHelper.SendLog(
                        $"Creating T2Package {i++} of {validPaths.Count}: {folderName}"
                    );
                    SelectMethod(useCustomDeps, folderName, validPath);
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
                        LogHelper.SendLog(
                            $"Creating T3Package {i++} of {validPaths.Count}: {folderName}"
                        );
                        SelectMethod(useCustomDeps, folderName, validPath);
                    }
                }
            }
        }

        public void SelectMethod(bool useDeps, string packageName, string folderPath)
        {
            if (!useDeps)
                CreateCokAndCidNew(packageName, folderPath).Wait();
            else
                CreateCokAndCidNew2(packageName, folderPath).Wait();
        }

        public async Task CreateCokAndCidNew(string packageName, string folderPath)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

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
            collector.Clear();

            Dictionary<string, AssetData> assets = PrefabHelper.GetAllAssets(folderPath);
            foreach (var asset in assets)
                collector.AddAsset(asset.Value, false);

            collector.Refresh();

            AssetData[] array = collector.CompileAllAssets();
            PackageDependencies.Data data = new()
            {
                modDependencies = collector.modDependencies.ToArray(),
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
                    PackageAsset pkg = null;
                    Colossal.Core.MainThreadDispatcher.RunOnMainThread(async () =>
                    {
                        object result = method.Invoke(
                            null,
                            new object[] { packageName, collector.mainAsset, array, data2, null }
                        );
                        Task<PackageAsset> task = (Task<PackageAsset>)result;
                        pkg = await task;
                    });
                    stopwatch.Stop();
                    LogHelper.SendLog($"{packageName} build completed in {stopwatch.Elapsed}s");
                    return;
                }
                catch (Exception ex)
                {
                    LogHelper.SendLog($"Error packaging {packageName}: {ex}", LogLevel.Error);
                    return;
                }
            }
        }

        public bool CheckPacakgeDepFromFiles(string folderPath, out string mDep, out string dDep)
        {
            string[] filesToCheck =
            {
                Path.Combine(folderPath, "package.deps"),
                Path.Combine(folderPath, "package.json"),
            };

            foreach (var file in filesToCheck)
            {
                if (!File.Exists(file))
                    continue;

                try
                {
                    var json = File.ReadAllText(file);
                    var root = JToken.Parse(json);

                    mDep = ReadArrayAsStrings(root["modDependencies"]);
                    dDep = ReadArrayAsStrings(root["dlcDependencies"]);

                    return true;
                }
                catch (Exception ex)
                {
                    LogHelper.SendLog(ex.Message, LogLevel.Error);
                }
            }
            mDep = string.Empty;
            dDep = string.Empty;
            return false;
        }

        private static string ReadArrayAsStrings(JToken? token)
        {
            if (token is not JArray array)
                return "";

            //var result = new string[array.Count];

            //for (int i = 0; i < array.Count; i++)
            //    result[i] = array[i]?.ToString() ?? string.Empty;

            return string.Join(",", array);
        }

        public async Task CreateCokAndCidNew2(string packageName, string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return;

            Stopwatch stopwatch = Stopwatch.StartNew();

            Dictionary<string, AssetData> assetDict = PrefabHelper.GetAllAssets(folderPath);
            AssetData[] assets = assetDict.Values.ToArray();

            using (Mod.log.indent.scoped)
                if (assets.Length != 0)
                {
                    bool hasDepData = CheckPacakgeDepFromFiles(
                        folderPath,
                        out string mDep,
                        out string dDep
                    );

                    PackageDependencies.Data data = new()
                    {
                        modDependencies = GetModDep(mDep ?? Mod.m_Setting.DepsMod ?? ""),
                        dlcDependencies = GetDlcDep(dDep ?? Mod.m_Setting.DepsDlc ?? ""),
                        previews = new AssetData[0],
                    };

                    PackageDependencies.Data depsData = data;

                    try
                    {
                        string stagingFolder = string.Concat(
                            new string[]
                            {
                                EnvPath.kTempDataPath,
                                "/",
                                packageName,
                                "_staging_",
                                Guid.NewGuid().ToLowerNoDashString(),
                            }
                        );

                        string backupPath =
                            AssetDatabase.packages.rootPath + "/." + packageName + "_Backup";
                        IOUtils.EmptyFolder(backupPath);
                        IOUtils.CopyFiles(
                            assets
                                .Select(a => a.path)
                                .Concat(assets.Select(a => a.path + ".cid"))
                                .Distinct(StringComparer.OrdinalIgnoreCase),
                            backupPath,
                            EnvPath.kUserDataPath,
                            true
                        );
                        LogHelper.SendLog("Local backup created");

                        using AssetDatabase<Colossal.IO.AssetDatabase.Game> contentDb =
                            AssetDatabase<Colossal.IO.AssetDatabase.Game>.GetInstance(
                                new Colossal.IO.AssetDatabase.Game(stagingFolder)
                            );
                        try
                        {
                            contentDb.MarkForDeletion(false);
                            foreach (AssetData assetData in assets)
                            {
                                SourceMeta meta = assetData.GetMeta();
                                assetData.CopyTo(
                                    contentDb,
                                    AssetDataPath.Create(
                                        meta.fileName + meta.extension,
                                        true,
                                        EscapeStrategy.None
                                    )
                                );
                            }
                            using (contentDb.DisableNotificationsScoped(true))
                            {
                                await AssetDatabase.global.RegisterDatabase(contentDb);
                            }
                            ResavePrefabsBeforePackaging(contentDb);
                            BuildPackageBuildVT(contentDb);
                            PackageDependencies packageDependencies =
                                contentDb.AddAsset<PackageDependencies>(
                                    AssetDataPath.Create("package", EscapeStrategy.Filename),
                                    default
                                );
                            packageDependencies.target = depsData;
                            packageDependencies.Save(false);

                            PackageAsset finalPackage = AssetDatabase.packages.AddAsset(
                                packageName,
                                contentDb
                            );
                            finalPackage.SaveWithTimestamp(false);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.SendLog(ex, LogLevel.Error);
                        }
                        using (AssetDatabase.global.DisableNotificationsScoped(true))
                        {
                            await AssetDatabase.global.UnregisterDatabase(contentDb);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.SendLog($"Error packaging {packageName}: {ex}", LogLevel.Error);
                    }
                }
            stopwatch.Stop();
            LogHelper.SendLog($"{packageName} build completed in {stopwatch.Elapsed}s");
        }

        public void ResavePrefabsBeforePackaging(ILocalAssetDatabase database)
        {
            PrefabAsset[] array = database.GetAssets<PrefabAsset>(default).ToArray();
            int num = array.Length;
            for (int i = 0; i < num; i++)
            {
                PrefabAsset prefabAsset = array[i];
                try
                {
                    float num2 = (i + 1) / (float)num;

                    PrefabBase prefabBase = prefabAsset.Load<PrefabBase>(
                        new IAssetDatabase[] { AssetDatabase.user, AssetDatabase.packages }
                    );
                    ContentType contentType = PrefabHelper.GetPrefabContentType(prefabBase);
                    prefabBase.MarkCurrentPrefabObsolete();
                    prefabAsset.Save(contentType, false, true);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error resaving " + prefabAsset.name, ex);
                }
            }
        }

        public int[] GetModDep(string val)
        {
            int[] ints = val.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.Parse(x.Trim()))
                .ToArray();
            return ints;
        }

        public string[] GetDlcDep(string val)
        {
            var s = val.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (string ss in s)
            {
                if (PlatformManager.instance.GetDlcId(ss) == DlcId.Invalid)
                    s.Remove(ss);
            }
            return s.ToArray();
        }

        //public void CreateCokAndCid(string outputCokPath, string outputCidPath, string folderPath)
        //{
        //    bool hasTextureFiles = Directory
        //        .EnumerateFiles(folderPath, "*.Texture", SearchOption.AllDirectories)
        //        .Any();

        //    if (hasTextureFiles)
        //    {
        //        LogHelper.SendLog(
        //            $"Skipping package creation for '{folderPath}' because it contains non VT texture files."
        //        );
        //        return;
        //    }

        //    string pkgJsonPath = Path.Combine(folderPath, "package.json");
        //    string pkgDepsPath = Path.Combine(folderPath, "package.deps");

        //    JArray modDeps = new();
        //    JArray dlcDeps = new();

        //    if (File.Exists(pkgJsonPath) && !File.Exists(pkgDepsPath))
        //    {
        //        try
        //        {
        //            var pkgJson = File.ReadAllText(pkgJsonPath);
        //            JObject jsonObject = JObject.Parse(pkgJson);

        //            if (jsonObject["modDependencies"] is JArray modArray)
        //            {
        //                foreach (var item in modArray)
        //                {
        //                    if (item.Type == JTokenType.Integer)
        //                        modDeps.Add((int)item);
        //                    else if (
        //                        item.Type == JTokenType.String
        //                        && int.TryParse((string)item, out int parsed)
        //                    )
        //                        modDeps.Add(parsed);
        //                }
        //            }

        //            if (jsonObject["dlcDependencies"] is JArray dlcArray)
        //            {
        //                foreach (var item in dlcArray)
        //                    if (item.Type == JTokenType.String)
        //                        dlcDeps.Add((string)item);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            LogHelper.SendLog($"package.json is invalid\n{ex.Message}");
        //        }

        //        var output = new JObject
        //        {
        //            ["modDependencies"] = modDeps,
        //            ["dlcDependencies"] = dlcDeps,
        //            ["previews"] = new JArray(),
        //        };
        //        File.WriteAllText(
        //            pkgDepsPath,
        //            output.ToString(Newtonsoft.Json.Formatting.Indented)
        //        );
        //    }

        //    string cidPath = pkgDepsPath + ".cid";
        //    if (!File.Exists(cidPath))
        //    {
        //        string newCidPkg = Colossal.Hash128.CreateGuid(cidPath).ToString();
        //        File.WriteAllText(cidPath, newCidPkg);
        //    }

        //    using (FileStream zipFile = File.Create(outputCokPath))
        //    using (ZipOutputStream zipStream = new(zipFile))
        //    {
        //        zipStream.SetLevel(0);
        //        foreach (
        //            string filePath in Directory.GetFiles(
        //                folderPath,
        //                "*",
        //                SearchOption.AllDirectories
        //            )
        //        )
        //        {
        //            string relativePath = Path.GetRelativePath(folderPath, filePath)
        //                .Replace("\\", "/");
        //            string[] parts = relativePath.Split('/');

        //            bool shouldSkip = false;
        //            foreach (string part in parts)
        //            {
        //                if (part.StartsWith(".") || part.StartsWith("~"))
        //                {
        //                    shouldSkip = true;
        //                    break;
        //                }
        //            }

        //            if (shouldSkip)
        //                continue;

        //            string arcname = relativePath;

        //            try
        //            {
        //                ZipEntry entry = new(arcname)
        //                {
        //                    CompressionMethod = CompressionMethod.Stored,
        //                    DateTime = File.GetLastWriteTime(filePath),
        //                };
        //                zipStream.PutNextEntry(entry);

        //                using FileStream fs = File.OpenRead(filePath);
        //                fs.CopyTo(zipStream);

        //                zipStream.CloseEntry();
        //            }
        //            catch (Exception ex)
        //            {
        //                LogHelper.SendLog($"Error processing {filePath}: {ex.Message}");
        //            }
        //        }
        //    }

        //    string newCid = Colossal.Hash128.CreateGuid(outputCokPath).ToString();
        //    File.WriteAllText(outputCidPath, newCid);

        //    LogHelper.SendLog($"Created: {outputCokPath.Replace("\\", "/")}");
        //}

        internal static void BuildPackageAddReferenceTo(
            Dictionary<TextureAsset, List<SurfaceAsset>> references,
            TextureAsset texture,
            SurfaceAsset surface
        )
        {
            if (!references.TryGetValue(texture, out List<SurfaceAsset> list))
            {
                list = new List<SurfaceAsset>();
                references.Add(texture, list);
            }
            list.Add(surface);
        }

        internal void BuildPackageExcludeSourceTextures(
            IEnumerable<SurfaceAsset> surfaces,
            ILocalAssetDatabase database
        )
        {
            Dictionary<TextureAsset, List<SurfaceAsset>> dictionary = new();
            Dictionary<TextureAsset, List<SurfaceAsset>> dictionary2 = new();
            foreach (SurfaceAsset surfaceAsset in surfaces)
            {
                surfaceAsset.LoadProperties(true);
                if (surfaceAsset.isVTMaterial)
                {
                    using (
                        IEnumerator<KeyValuePair<string, TextureAsset>> enumerator2 =
                            surfaceAsset.textures.GetEnumerator()
                    )
                    {
                        while (enumerator2.MoveNext())
                        {
                            KeyValuePair<string, TextureAsset> keyValuePair = enumerator2.Current;
                            if (surfaceAsset.IsHandledByVirtualTexturing(keyValuePair))
                            {
                                BuildPackageAddReferenceTo(
                                    dictionary,
                                    keyValuePair.Value,
                                    surfaceAsset
                                );
                            }
                            else
                            {
                                BuildPackageAddReferenceTo(
                                    dictionary2,
                                    keyValuePair.Value,
                                    surfaceAsset
                                );
                            }
                        }
                        goto unload;
                    }
                }
                goto buildPackRef;
                unload:
                surfaceAsset.UnloadTextures(AssetDatabase.packages, false);
                surfaceAsset.UnloadProperties(false);
                continue;
                buildPackRef:
                foreach (KeyValuePair<string, TextureAsset> keyValuePair2 in surfaceAsset.textures)
                {
                    BuildPackageAddReferenceTo(dictionary2, keyValuePair2.Value, surfaceAsset);
                }
                goto unload;
            }
            List<TextureAsset> list = database.GetAssets<TextureAsset>(default).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                TextureAsset textureAsset = list[i];
                if (dictionary.ContainsKey(textureAsset))
                {
                    if (dictionary2.ContainsKey(textureAsset))
                    {
                        LogHelper.SendLog(
                            string.Format(
                                "Texture {0} is referenced {1} times by VT materials and {2} times by non VT materials. It will be duplicated on disk.",
                                textureAsset,
                                dictionary[textureAsset].Count,
                                dictionary2[textureAsset].Count
                            ),
                            LogLevel.Warn
                        );
                        LogHelper.SendLog(
                            string.Format(
                                "Detail for {0}:\nvt: {1}\nnon vt: {2}",
                                textureAsset,
                                string.Join(", ", dictionary[textureAsset]),
                                string.Join(", ", dictionary2[textureAsset])
                            )
                        );
                    }
                    else
                    {
                        LogHelper.SendLog(
                            string.Format(
                                string.Format("Deleting {0}", textureAsset),
                                Array.Empty<object>()
                            )
                        );
                        textureAsset.Unload(false);
                        textureAsset.Delete();
                    }
                }
            }
        }

        internal void BuildPackageBuildVT(ILocalAssetDatabase database)
        {
            int num = 0;
            Report report = new();
            ImportStep importStep = report.AddImportStep("Convert Selected VT");
            List<SurfaceAsset> list = database.GetAssets<SurfaceAsset>(default).ToList();
            if (list.Count > 0)
            {
                AssetImportPipeline.ConvertSurfacesToVT(
                    list,
                    list,
                    false,
                    512,
                    3,
                    num,
                    false,
                    importStep
                );
                AssetImportPipeline.BuildMidMipsCache(list, 512, 3, database);
                BuildPackageExcludeSourceTextures(list, database);
                report.Log(Mod.log, Severity.Verbose);
            }
        }
    }
}
