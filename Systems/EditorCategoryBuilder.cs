//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using Colossal.Serialization.Entities;
//using Game;
//using Game.Prefabs;
//using Game.UI.Editor;
//using StarQ.Shared.Extensions;
//using Unity.Collections;
//using Unity.Entities;

//namespace StarQWorkflowKit
//{
//    public partial class EditorCategoryBuilder : GameSystemBase
//    {
//        private PrefabSystem prefabSystem;

//        private EditorAssetCategorySystem editorAssetCategorySystem;

//        //private Dictionary<string, EditorAssetCategory> m_PathMap = new();
//        //private List<EditorAssetCategory> m_Categories = new();
//        private EntityQuery allAssets;
//        private bool catsEnabled;

//        protected override void OnCreate()
//        {
//            base.OnCreate();
//            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
//            editorAssetCategorySystem = World.GetOrCreateSystemManaged<EditorAssetCategorySystem>();
//            allAssets = SystemAPI.QueryBuilder().WithAllRW<PrefabData>().Build();
//            catsEnabled = false;
//        }

//        protected override void OnUpdate()
//        {
//            if (Mod.m_Setting.ShowEditorCatsTypeBased)
//            {
//                EnableCats();
//            }
//        }

//        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
//        {
//            base.OnGameLoadingComplete(purpose, mode);

//            if (Mod.m_Setting.ShowEditorCatsTypeBased)
//            {
//                EnableCats();
//            }
//        }

//        internal void EnableCats()
//        {
//            Type type = typeof(EditorAssetCategorySystem);

//            FieldInfo field = type.GetField(
//                "m_Dirty",
//                BindingFlags.Instance | BindingFlags.NonPublic
//            );

//            bool isDirty = (bool)field.GetValue(editorAssetCategorySystem);

//            if (catsEnabled && !isDirty)
//                return;

//            var entities = allAssets.ToEntityArray(Allocator.Temp);
//            LogHelper.SendLog("Starting EnableCats");

//            foreach (Entity entity in entities)
//            {
//                prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase);

//                string cat = $"WorkflowKit/{prefabBase.prefab.GetType()}"
//                    .Replace(".", "/")
//                    .Replace("/Game/", "/")
//                    .Replace("/Prefabs/", "/");

//                EditorAssetCategoryOverride editorAssetCategoryOverride =
//                    prefabBase.AddOrGetComponent<EditorAssetCategoryOverride>();

//                List<string> cats =
//                    editorAssetCategoryOverride.m_IncludeCategories?.ToList() ?? new List<string>();
//                if (!cats.Contains(cat))
//                {
//                    cats.Add(cat);
//                    editorAssetCategoryOverride.m_IncludeCategories = cats.ToArray();
//                    EntityManager.AddComponent<EditorAssetCategoryOverrideData>(entity);
//                }
//            }
//            editorAssetCategorySystem.Update();

//            MethodInfo method = type.GetMethod(
//                "GenerateCategories",
//                BindingFlags.Instance | BindingFlags.NonPublic
//            );

//            method?.Invoke(editorAssetCategorySystem, null);

//            LogHelper.SendLog($"{entities.Length} prefab's category added");
//            catsEnabled = true;
//        }
//    }
//}
