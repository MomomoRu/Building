using GameFramework;
using GameFramework.Event;
using System.Collections.Generic;
using System.Linq;
using UnityGameFramework.Runtime;
using UnityEngine;

namespace Game.Hotfix
{
	/// <summary>
    /// 網路資料组件。
    /// </summary>
    public sealed partial class NetDataComponent : GameFrameworkComponent
    {
		private void Start()
        {
            // 註冊封包資料 新增/刪除/更新
            subscribeSelfData();
        }
		
		private void subscribeSelfData()
        {
            GameCore.Event.Subscribe((int)client_serial_type.ENeResetBuildingS, onResetBuilding);
        }
		
		private void onResetBuilding(object sender, GameEventArgs e)
        {
            var msg = (e as NetworkS2CPacketEventArgs).data as NE_ResetBuildingS;

            if (msg.Result != EStringId.EStrSuccess)
            {
                WorldSystems.MsgSys.AddPersonalMsg(Utility.Text.Format("{0}:{1}", typeof(NE_ResetBuildingS).Name, msg.Result.ToString()));
            }
            else
            {
                var eventArgs = ReferencePool.Acquire<NetDataEvent.ResetBuildingEvent>();
                eventArgs.PosIndex = msg.PosIndex;
                GameCore.Event.Fire(this, eventArgs);
            }
        }
	}
}