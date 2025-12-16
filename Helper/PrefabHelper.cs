using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Environment;
using Game.Prefabs;
using StarQ.Shared.Extensions;

namespace StarQWorkflowKit.Helper
{
    public class PrefabHelper
    {
        public static ContentType GetPrefabContentType(PrefabBase prefab)
        {
            using Stream stream = prefab.asset.GetReadStream();
            return stream.ReadByte() != 123 ? ContentType.Binary : ContentType.Text;
        }

        public static List<string> GetValidFolders(string pattern)
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

        private static void RecursiveGlob(
            string currentDir,
            string pattern,
            ref List<string> results
        )
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

        public static Dictionary<string, AssetData> GetAllAssets(string path)
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

            LogHelper.SendLog($"Collected {assets.Count} assets");

            return assets;
        }

        public static List<PrefabBase> GetValidPrefabs(string path)
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
    }
}
