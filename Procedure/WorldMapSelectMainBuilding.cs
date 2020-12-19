using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;
using GameFramework.Event;
using UnityGameFramework.Runtime;
using UnityEngine;
using MEC;

namespace Game.Hotfix.WorldMap
{
    public class WorldMapSelectMainBuilding : WorldMapProcedureBase
    {
        public class Preset
        {
            public ESceneGridDir posIndex;
            public Vector3 position;
        }

        private int? uiFormSerialId;
        private EntityRequest<MiscEntity> selectGridEntity = new EntityRequest<MiscEntity>();
        private const string gridHexAssetName = "PreviewHex";
        private Preset preset;
        private readonly Vector3 offset = new Vector3(0, 0, -3);

        protected override void OnEnter(ProcedureOwner procedureOwner, object userData)
        {
            base.OnEnter(procedureOwner, userData);

            preset = userData as Preset;

            GameCore.Event.Subscribe(NetDataEvent.LearnTechEvent.EventId, onLearnTech);

            addLogicHandler(new CraftCameraLogicHandler());

            Timing.CallDelayed(0.2f, () =>
            {
                var data = GameCore.WorldMap.GetLogicHandler<WorldMapSystemLogicHandler>(WorldMapSystemLogicHandler.HandlerID).GetCameraSetting();
                GameCore.Event.Fire(this, FocusWorldPosWithLerpValueEventArgs.Create(preset.position + offset, data.focusCastleLerpValue));
            });

            // 產生 preview hex 的 entity:
            selectGridEntity.Release();
            selectGridEntity.LoadEntity(new MiscEntityData(GameCore.Entity.GenerateSerialId(), gridHexAssetName) { Position = preset.position });

            // 開啟建造主建築介面                
            uiFormSerialId = GameCore.UI.OpenUIForm(UIFormId.UIMainBuildingList, preset);
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);

            GameCore.Event.Unsubscribe(NetDataEvent.LearnTechEvent.EventId, onLearnTech);

            selectGridEntity.Release();

            if (GameCore.UI.HasUIForm(uiFormSerialId.Value))
                GameCore.UI.CloseUIForm(uiFormSerialId.Value);
        }

        protected override void OnCloseUIFormComplete(object sender, GameEventArgs e)
        {
            CloseUIFormCompleteEventArgs eventArgs = e as CloseUIFormCompleteEventArgs;

            if (eventArgs.SerialId != uiFormSerialId.Value)
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
                // 返回選擇建築地格
                ChangeState<WorldMapSelectBuildingGrid>(owner);
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