using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Colossal;
using Colossal.Json;
using Colossal.Localization;
using Game.SceneFlow;

namespace StarQWorkflowKit.Extensions
{
    public class LocaleHelper
    {
        private readonly Dictionary<string, Dictionary<string, string>> _locale;

        public LocaleHelper(string dictionaryResourceName)
        {
            var assembly = GetType().Assembly;

            _locale = new Dictionary<string, Dictionary<string, string>>
            {
                [string.Empty] = GetDictionaryEmbedded(dictionaryResourceName),
            };

            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (
                    name == dictionaryResourceName
                    || !name.Contains(
                        Path.GetFileNameWithoutExtension(dictionaryResourceName) + "."
                    )
                )
                {
                    continue;
                }

                var key = Path.GetFileNameWithoutExtension(name);

                _locale[key[(key.LastIndexOf('.') + 1)..]] = GetDictionaryEmbedded(name);
            }

            //var dllDir = Path.GetDirectoryName(assembly.Location) ?? ".";
            //var localeDir = Path.Combine(dllDir, "Locale");
            //if (Directory.Exists(localeDir))
            //{
            //    foreach (
            //        var file in Directory.GetFiles(
            //            localeDir,
            //            "*.json",
            //            SearchOption.TopDirectoryOnly
            //        )
            //    )
            //    {
            //        var key = Path.GetFileNameWithoutExtension(file);
            //        var userDict = GetDictionaryFile(file);

            //        if (_locale.TryGetValue(key, out var baseDict))
            //        {
            //            foreach (var kv in userDict)
            //            {
            //                baseDict[kv.Key] = kv.Value;
            //            }
            //            _locale[key] = baseDict;
            //        }
            //        else
            //        {
            //            _locale[key] = userDict;
            //        }
            //    }
            //}

            Dictionary<string, string> GetDictionaryEmbedded(string resourceName)
            {
                try
                {
                    using var resourceStream = assembly.GetManifestResourceStream(resourceName);
                    if (resourceStream == null)
                    {
                        return new Dictionary<string, string>();
                    }

                    using var reader = new StreamReader(resourceStream, Encoding.UTF8);
                    JSON.MakeInto<Dictionary<string, string>>(
                        JSON.Load(reader.ReadToEnd()),
                        out var dictionary
                    );

                    return dictionary;
                }
                catch (Exception ex)
                {
                    LogHelper.SendLog($"Failed to load embedded locale '{resourceName}': {ex}");
                    return new Dictionary<string, string>();
                }
            }

            //Dictionary<string, string> GetDictionaryFile(string filePath)
            //{
            //    try
            //    {
            //        var json = File.ReadAllText(filePath, Encoding.UTF8);
            //        JSON.MakeInto<Dictionary<string, string>>(JSON.Load(json), out var dictionary);
            //        return dictionary;
            //    }
            //    catch (Exception ex)
            //    {
            //        LogHelper.SendLog($"Failed to load locale file '{filePath}': {ex}");
            //        return new Dictionary<string, string>();
            //    }
            //}
        }

        public static string Translate(string id, string fallback = null)
        {
            if (
                GameManager.instance.localizationManager.activeDictionary.TryGetValue(
                    id,
                    out var result
                )
            )
            {
                return result;
            }

            return fallback ?? id;
        }

        public IEnumerable<DictionarySource> GetAvailableLanguages()
        {
            foreach (var item in _locale)
            {
                yield return new DictionarySource(item.Key is "" ? "en-US" : item.Key, item.Value);
            }
        }

        public class DictionarySource : IDictionarySource
        {
            private readonly Dictionary<string, string> _dictionary;

            public DictionarySource(string localeId, Dictionary<string, string> dictionary)
            {
                LocaleId = localeId;
                _dictionary = dictionary;
            }

            public string LocaleId { get; }

            public IEnumerable<KeyValuePair<string, string>> ReadEntries(
                IList<IDictionaryEntryError> errors,
                Dictionary<string, int> indexCounts
            )
            {
                return _dictionary;
            }

            public void Unload() { }
        }

        public static void OnActiveDictionaryChanged()
        {
            LocalizationManager lm = GameManager.instance.localizationManager;
            Dictionary<string, string> toUpdate = new();

            Dictionary<string, string> replacements = Mod.localeReplacement;

            Regex regex = new($@"(\{{{Regex.Escape(Mod.Id)}\.[\w.]+\}}+)", RegexOptions.Compiled);

            foreach (var entry in lm.activeDictionary.entries)
            {
                if (!entry.Key.Contains(Mod.Id))
                {
                    continue;
                }
                string newValue = Expand(entry.Value, lm, replacements, regex);
                if (newValue != entry.Value)
                {
                    toUpdate[entry.Key] = newValue;
                }
            }

            foreach (var item in toUpdate)
            {
                try
                {
                    lm.activeDictionary.Add(item.Key, item.Value);
                }
                catch (Exception) { }
            }
        }

        static string Expand(
            string input,
            LocalizationManager lm,
            Dictionary<string, string> replacements,
            Regex regex
        )
        {
            string result = input;
            bool changed;

            do
            {
                changed = false;
                result = regex.Replace(
                    result,
                    match =>
                    {
                        var key = (match.Groups[1].Value).Replace("{", "").Replace("}", "");

                        if (
                            replacements.TryGetValue(
                                key.Replace($"{Mod.Id}.Replacement.", ""),
                                out var replacement
                            )
                        )
                        {
                            changed = true;
                            return replacement;
                        }

                        if (lm.activeDictionary.TryGetValue(key, out var localized))
                        {
                            changed = true;
                            return localized;
                        }

                        return match.Value;
                    }
                );
            } while (changed && regex.IsMatch(result));

            return result;
        }
    }
}
