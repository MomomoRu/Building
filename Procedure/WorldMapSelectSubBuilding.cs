using GameFramework.Event;
using MEC;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace Game.Hotfix.WorldMap
{
    public class WorldMapSelectSubBuilding : WorldMapProcedureBase
    {
        public class Preset
        {
            public ESceneGridDir posIndex;
            public PreviewBuildingHex buildingHex;
            public int masterTid;
            public Vector3 position;
            public bool isMainBuildingProcess;  // 是:為主建築建造流程所開啟, 否:為副建築建造流程所開啟
        }

        private int? uiFormSerialId;
        private int buildingEntityId;
        private EntityRequest<MiscEntity> selectGridEntity = new EntityRequest<MiscEntity>();
        private const string gridHexAssetName = "PreviewBuildingHex";
        private Preset preset;
        private readonly Vector3 offset = new Vector3(0, 0, -3);
        private SceneObj selfCastle = null;

        protected override void OnEnter(ProcedureOwner procedureOwner, object userData)
        {
            base.OnEnter(procedureOwner, userData);

            preset = userData as Preset;

            GameCore.Event.Subscribe(NetDataEvent.LearnTechEvent.EventId, onLearnTech);

            addLogicHandler(new CraftCameraLogicHandler());

            // 產生 preview hex 的 entity:
            selectGridEntity.Release();
            selectGridEntity.LoadEntity(new MiscEntityData(GameCore.Entity.GenerateSerialId(), gridHexAssetName) { Position = preset.position },
            (Entity) =>
            {
                preset.buildingHex = Entity.GetComponent<PreviewBuildingHex>();
                preset.buildingHex.Show(preset.masterTid);
                uiFormSerialId = GameCore.UI.OpenUIForm(UIFormId.UISubBuildingList, preset);
            });

            // 隱藏主建築
            buildingEntityId = 0;
            if (GameCore.NetData.TryGetSceneObjData(GameCore.NetData.SelfCastle.Guid, out selfCastle))
                (selfCastle as CastleObj).TryGetBuildGuid(preset.posIndex, out buildingEntityId);

            if (buildingEntityId != 0)
            {
                var entity = GameCore.Entity.GetEntity(buildingEntityId);
                entity.Logic.CachedTransform.parent.gameObject.SetActive(false);
            }

            // wait for WorldMapCameraLogicHandler init() finish
            Timing.CallDelayed(0.2f, () =>
            {
                var data = GameCore.WorldMap.GetLogicHandler<WorldMapSystemLogicHandler>(WorldMapSystemLogicHandler.HandlerID).GetCameraSetting();
                GameCore.Event.Fire(this, FocusWorldPosWithLerpValueEventArgs.Create(preset.position + offset, data.focusCastleLerpValue));
            });
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);

            GameCore.Event.Unsubscribe(NetDataEvent.LearnTechEvent.EventId, onLearnTech);

            var data = GameCore.WorldMap.GetLogicHandler<WorldMapSystemLogicHandler>(WorldMapSystemLogicHandler.HandlerID).GetCameraSetting();
            GameCore.Event.Fire(this, FocusWorldPosWithLerpValueEventArgs.Create(selfCastle.Position(), data.focusCastleLerpValue));

            if (preset.buildingHex != null)
                preset.buildingHex.Hide();

            selectGridEntity.Release();
            preset = null;

            if (GameCore.UI.HasUIForm(uiFormSerialId.Value))
                GameCore.UI.CloseUIForm(uiFormSerialId.Value);

            if (buildingEntityId != 0)
            {
                var entity = GameCore.Entity.GetEntity(buildingEntityId);
                if (entity != null)
                    entity.Logic.CachedTransform.parent.gameObject.SetActive(true);
            }
        }

        protected override void OnCloseUIFormComplete(object sender, GameEventArgs e)
        {
            CloseUIFormCompleteEventArgs eventArgs = e as CloseUIFormCompleteEventArgs;

            if (!uiFormSerialId.HasValue || eventArgs.SerialId != uiFormSerialId.Value)
                return;

            bool exitSelectBuilding = (bool)eventArgs.UserData;
            if (exitSelectBuilding)
            {
                // 恢復主介面
                var uiForm = GameCore.UI.GetUIForm(UIFormId.UIWorldMain, Constant.UI.GroupNames[(int)GameFramework.UI.UILevel.Default]);
                GameCore.UI.RefocusUIForm(uiForm.UIForm);
                // 返回世界地圖視角狀態
                ChangeState<WorldMapView>(owner);
            }
            else
            {
                // 返回選擇主建築
                ChangeState<WorldMapSelectMainBuilding>(owner, new WorldMapSelectMainBuilding.Preset() { posIndex = preset.posIndex, position = preset.position });
            }
        }

        private void onLearnTech(object sender, GameEventArgs e)
        {
            var msg = e as NetDataEvent.LearnTechEvent;
            if (msg == null)
                return;

            GameCore.Setting.SetInt(Constant.SettingKey.CultureTreeCollectTechID, msg.TechID);

            // 收取當前研發完成之新科技並前往文明樹頁面檢視
            // 關閉建築清單，前往文明樹介面，並且焦點為對應的文明樹ID
            GameCore.Event.Fire(null, ChangeStateEventArgs.Create(typeof(WorldMapCultureTree), UIBuildingCard.RequireUnlockTechID));
        }
    }
}