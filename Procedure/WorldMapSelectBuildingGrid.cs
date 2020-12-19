using GameFramework.Event;
using GameFramework.Input;
using MEC;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace Game.Hotfix.WorldMap
{
    public class WorldMapSelectBuildingGrid : WorldMapProcedureBase
    {
        private EntityRequest<CastleGridHexEntity> selectGridEntity = new EntityRequest<CastleGridHexEntity>();
        private const string gridHexAssetName = "SelectGridHex";
        private int? uiSerialId;

        protected override void OnEnter(ProcedureOwner procedureOwner, object userData)
        {
            base.OnEnter(procedureOwner, userData);

            GameCore.Input.Subscribe(InputEventType.On_SimpleTap, OnSimpleTap);

            addLogicHandler(new CraftCameraLogicHandler());

            Timing.CallDelayed(0.2f, ()=> 
            {
                var data = GameCore.WorldMap.GetLogicHandler<WorldMapSystemLogicHandler>(WorldMapSystemLogicHandler.HandlerID).GetCameraSetting();
                GameCore.Event.Fire(this, FocusWorldPosWithLerpValueEventArgs.Create(GameCore.NetData.SelfCastle.Position, data.focusCastleLerpValue));

                // 產生 preview hex 的 entity:
                selectGridEntity.Release();
                selectGridEntity.LoadEntity(new MiscEntityData(GameCore.Entity.GenerateSerialId(), gridHexAssetName) { Position = GameCore.NetData.SelfCastle.Position },
                (entity) =>
                {
                    entity.OnZoom(0);
                });
            });

            uiSerialId = GameCore.UI.OpenUIForm(UIFormId.UISelectBuildGrid);
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);

            selectGridEntity.Release();

            GameCore.Input.Unsubscribe(InputEventType.On_SimpleTap, OnSimpleTap);

            if (uiSerialId.HasValue && GameCore.UI.HasUIForm(uiSerialId.Value))
            {
                GameCore.UI.CloseUIForm(uiSerialId.Value);
                uiSerialId = null;
            }
                
        }

        protected void OnSimpleTap(object sender, InputEventArgs e)
        {
            if (selectGridEntity.entity == null)
                return;

            int index = -1;
            if (selectGridEntity.entity.TryGetHex(Camera.main.MouseCastPos(WorldAgent.WORLD_HEX_SIZE), out index))
            {
                ChangeState<WorldMapSelectMainBuilding>(owner, new WorldMapSelectMainBuilding.Preset() {  posIndex = (ESceneGridDir)index, position = selectGridEntity.entity.GetTransform((ESceneGridDir)index).position });
            }
        }

        protected override void OnCloseUIFormComplete(object sender, GameEventArgs e)
        {
            CloseUIFormCompleteEventArgs eventArgs = e as CloseUIFormCompleteEventArgs;
            if (eventArgs.SerialId == uiSerialId.Value)
            {
                ChangeStateToWorldMapView();
                uiSerialId = null;
            }
        }

        private void ChangeStateToWorldMapView()
        {
            // 開啟主介面
            GameCore.UI.OpenUIForm(UIFormId.UIWorldMain);
            // 返回世界地圖視角狀態
            ChangeState<WorldMapView>(owner);
        }
    }
}