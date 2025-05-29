using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Prefabs;
using Game.Simulation;
using Game.UI.Editor;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;

namespace StarQWorkflowKit
{
    public partial class EditorCategoryBuilder : GameSystemBase
    {
        private PrefabSystem prefabSystem;

        //private EditorAssetCategorySystem editorAssetCategorySystem;
        //private Dictionary<string, EditorAssetCategory> m_PathMap = new();
        //private List<EditorAssetCategory> m_Categories = new();
        private EntityQuery allAssets;
        private bool catsEnabled;

        protected override void OnCreate()
        {
            base.OnCreate();
            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            //editorAssetCategorySystem = World.GetOrCreateSystemManaged<EditorAssetCategorySystem>();
            allAssets = SystemAPI.QueryBuilder().WithAllRW<PrefabData>().Build();
            catsEnabled = false;
        }

        protected override void OnUpdate()
        {
            if (Mod.m_Setting.ShowEditorCatsTypeBased)
            {
                EnableCats();
            }
            Enabled = false;
        }

        internal void EnableCats()
        {
            if (catsEnabled)
            {
                return;
            }
            var entities = allAssets.ToEntityArray(Allocator.Temp);
            Mod.log.Info("Starting EnableCats");

            foreach (Entity entity in entities)
            {
                prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase);

                string cat = $"WorkflowKit/{prefabBase.prefab.GetType()}"
                    .Replace(".", "/")
                    .Replace("/Game/", "/")
                    .Replace("/Prefabs/", "/");

                EditorAssetCategoryOverride editorAssetCategoryOverride =
                    prefabBase.AddOrGetComponent<EditorAssetCategoryOverride>();

                List<string> cats =
                    editorAssetCategoryOverride.m_IncludeCategories?.ToList() ?? new List<string>();
                cats.Add(cat);
                editorAssetCategoryOverride.m_IncludeCategories = cats.ToArray();
                EntityManager.AddComponent<EditorAssetCategoryOverrideData>(entity);
            }
            Mod.log.Info($"{entities.Length} prefab's category added");
            catsEnabled = true;
        }
    }
}
