using System.Collections.Generic;
using Game.Prefabs;
using Game.UI.Editor;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;

[HarmonyPatch(typeof(EditorAssetCategorySystem), "AddOverrides")]
public static class Patch_EditorAssetCategorySystem_AddOverrides
{
    static bool Prefix(EditorAssetCategorySystem __instance)
    {
        var prefabSystem =
            World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
        if (prefabSystem == null)
        {
            UnityEngine.Debug.LogError(
                "WorkflowKit EditorAssetCategorySystem Patch failed: PrefabSystem not found."
            );
            return true;
        }

        var m_OverridesField = AccessTools.Field(__instance.GetType(), "m_Overrides");
        var m_Overrides = (EntityQuery)m_OverridesField.GetValue(__instance);
        var nativeArray = m_Overrides.ToEntityArray(Allocator.Temp);

        var includeDict = new SortedDictionary<string, List<Entity>>();
        var excludeDict = new Dictionary<string, HashSet<Entity>>();

        foreach (var entity in nativeArray)
        {
            if (!prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefabBase))
                continue;

            var component = prefabBase.GetComponent<EditorAssetCategoryOverride>();
            if (component.m_IncludeCategories != null)
            {
                foreach (var cat in component.m_IncludeCategories)
                {
                    if (!includeDict.TryGetValue(cat, out var list))
                        includeDict[cat] = list = new List<Entity>();

                    list.Add(entity);
                }
            }

            if (component.m_ExcludeCategories != null)
            {
                foreach (var cat in component.m_ExcludeCategories)
                {
                    if (!excludeDict.TryGetValue(cat, out var set))
                        excludeDict[cat] = set = new HashSet<Entity>();

                    set.Add(entity);
                }
            }
        }

        var m_PathMapField = AccessTools.Field(__instance.GetType(), "m_PathMap");
        var m_PathMap =
            (Dictionary<string, EditorAssetCategory>)m_PathMapField.GetValue(__instance);

        var createCategoryMethod = AccessTools.Method(
            __instance.GetType(),
            "CreateCategory",
            new[] { typeof(string) }
        );

        foreach (var kvp in includeDict)
        {
            string category = kvp.Key;
            var entityList = kvp.Value;

            if (!m_PathMap.TryGetValue(category, out EditorAssetCategory editorCategory))
            {
                editorCategory = (EditorAssetCategory)
                    createCategoryMethod.Invoke(__instance, new object[] { category });
            }

            foreach (var entity in entityList)
            {
                if (
                    excludeDict.TryGetValue(category, out var excluded) && excluded.Contains(entity)
                )
                    continue;
                editorCategory.AddEntity(entity);
            }
        }

        nativeArray.Dispose();
        return false;
    }
}
