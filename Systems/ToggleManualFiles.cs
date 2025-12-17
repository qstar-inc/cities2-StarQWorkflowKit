using System;
using System.Reflection;
using Colossal.Serialization.Entities;
using Game;
using Game.UI.Menu;
using StarQ.Shared.Extensions;
using Unity.Entities;

namespace StarQWorkflowKit.Systems
{
    public partial class ToggleManualFiles : GameSystemBase
    {
        private AssetUploadPanelUISystem assetUploadPanelUISystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            assetUploadPanelUISystem = WorldHelper.GetSystem<AssetUploadPanelUISystem>();
            Mod.m_Setting.onSettingsApplied += OnSettingsChanged;
        }

        protected override void OnUpdate() { }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            ChangeState();
        }

        private void OnSettingsChanged(Game.Settings.Setting setting)
        {
            ChangeState();
        }

        public void ChangeState()
        {
            try
            {
                LogHelper.SendLog(
                    "Setting state to " + Mod.m_Setting.EnableManualUpload,
                    LogLevel.DEVD
                );

                var field = typeof(AssetUploadPanelUISystem).GetField(
                    "m_AllowManualFileCopy",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                field.SetValue(assetUploadPanelUISystem, Mod.m_Setting.EnableManualUpload);
            }
            catch (Exception ex)
            {
                LogHelper.SendLog(ex);
            }
        }
    }
}
