using GameFramework.Event;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace Game.Hotfix.WorldMap
{
    public class WorldMapView : WorldMapProcedureBase
    {
        protected override void OnEnter(ProcedureOwner procedureOwner, object userData)
        {
            base.OnEnter(procedureOwner, userData);

            addLogicHandler(new WorldMapCameraLogicHandler());
            addLogicHandler(new WorldMapClickLogicHandler());

            // 登入進入世界地圖, 攝影機移至主堡
            if (userData != null && userData is bool && (bool)userData)
            {
                var data = GameCore.WorldMap.GetLogicHandler<WorldMapSystemLogicHandler>(WorldMapSystemLogicHandler.HandlerID).GetCameraSetting();
                GameCore.Event.Fire(this, FocusWorldPosWithLerpValueEventArgs.Create(GameCore.NetData.SelfCastle.Position, data.focusStartupLerpValue));
            }
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnCloseUIFormComplete(object sender, GameEventArgs e)
        {
            CloseUIFormCompleteEventArgs eventArgs = e as CloseUIFormCompleteEventArgs;

            if (eventArgs.UIFormAssetName.Contains(UIFormId.UICastleInfo.ToString()))
            {
                // 有建設中的建物
                if (GameCore.NetData.ContainsEnchant(NetDataComponent.BuildEnchantID))
                {
                    GameCore.UI.OpenUIForm(UIFormId.UISpeedUpBuildingMsg);
                }
                else
                {
                    // 無建設中的建物
                    if (eventArgs.UserData != null)
                    {
                        bool toSelectBuilding = (bool)eventArgs.UserData;
                        if (toSelectBuilding)
                        {

                            // 進入選擇建設位置狀態
                            ChangeState<WorldMapSelectBuildingGrid>(owner);
                        }
                    }
                }
            }
            else if (eventArgs.UIFormAssetName.Contains(UIFormId.UIMainBuildingInfo.ToString()))
            {
                // 有建設中的建物
                if (GameCore.NetData.ContainsEnchant(NetDataComponent.BuildEnchantID))
                {
                    GameCore.UI.OpenUIForm(UIFormId.UISpeedUpBuildingMsg);
                }
                else
                {
                    // 無建設中的建物
                    if (eventArgs.UserData != null)
                    {
                        var buildingObj = eventArgs.UserData as BuildingObj;
                        // 進入選擇副建築狀態
                        ChangeState<WorldMapSelectSubBuilding>(owner, new WorldMapSelectSubBuilding.Preset() { posIndex = buildingObj.PosIndex, masterTid = buildingObj.MasterTid, position = buildingObj.Position(), isMainBuildingProcess = false });
                    }
                }
            }
        }
    }
}