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
        PrefabSystem prefabSystem =
            World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
        if (prefabSystem == null)
        {
            UnityEngine.Debug.LogError(
                "WorkflowKit EditorAssetCategorySystem Patch failed: PrefabSystem not found."
            );
            return true;
        }

        System.Reflection.FieldInfo m_OverridesField = AccessTools.Field(
            __instance.GetType(),
            "m_Overrides"
        );
        EntityQuery m_Overrides = (EntityQuery)m_OverridesField.GetValue(__instance);
        NativeArray<Entity> nativeArray = m_Overrides.ToEntityArray(Allocator.Temp);

        SortedDictionary<string, List<Entity>> includeDict = new();
        Dictionary<string, HashSet<Entity>> excludeDict = new();

        foreach (Entity entity in nativeArray)
        {
            if (!prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefabBase))
                continue;

            EditorAssetCategoryOverride component =
                prefabBase.GetComponent<EditorAssetCategoryOverride>();
            if (component.m_IncludeCategories != null)
            {
                foreach (var cat in component.m_IncludeCategories)
                {
                    if (!includeDict.TryGetValue(cat, out List<Entity> list))
                        includeDict[cat] = list = new List<Entity>();

                    list.Add(entity);
                }
            }

            if (component.m_ExcludeCategories != null)
            {
                foreach (string cat in component.m_ExcludeCategories)
                {
                    if (!excludeDict.TryGetValue(cat, out HashSet<Entity> set))
                        excludeDict[cat] = set = new HashSet<Entity>();

                    set.Add(entity);
                }
            }
        }

        System.Reflection.FieldInfo m_PathMapField = AccessTools.Field(
            __instance.GetType(),
            "m_PathMap"
        );
        Dictionary<string, EditorAssetCategory> m_PathMap =
            (Dictionary<string, EditorAssetCategory>)m_PathMapField.GetValue(__instance);

        System.Reflection.MethodInfo createCategoryMethod = AccessTools.Method(
            __instance.GetType(),
            "CreateCategory",
            new[] { typeof(string) }
        );

        foreach (KeyValuePair<string, List<Entity>> kvp in includeDict)
        {
            string category = kvp.Key;
            List<Entity> entityList = kvp.Value;

            if (!m_PathMap.TryGetValue(category, out EditorAssetCategory editorCategory))
            {
                editorCategory = (EditorAssetCategory)
                    createCategoryMethod.Invoke(__instance, new object[] { category });
            }

            foreach (Entity entity in entityList)
            {
                if (
                    excludeDict.TryGetValue(category, out HashSet<Entity> excluded)
                    && excluded.Contains(entity)
                )
                    continue;
                editorCategory.AddEntity(entity);
            }
        }

        nativeArray.Dispose();
        return false;
    }
}
