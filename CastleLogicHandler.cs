using GameFramework.Entity;
using GameFramework.Event;
using System.Collections.Generic;
using UnityGameFramework.Runtime;

namespace Game.Hotfix
{
    /// <summary>
    /// 主堡邏輯控制器
    /// </summary>
    public class CastleLogicHandler : BaseLogicHandler
    {
        public static readonly int HandlerID = typeof(CastleLogicHandler).GetHashCode();

        public override int Id { get { return HandlerID; } }
        // 採集間隔時間
        private int collectInterval;
        // 採集資源HUDEntityID
        private int? collectSerialId = null;
        // 主堡產生佇列
        private Stack<int> spawnStack = new Stack<int>();
        // 縮放倍率
        private int curLerp = Constant.NetData.FloatConvert;
        // 重複使用的List Entities
        private List<IEntity> entities;

        public override void OnInit()
        {
            entities = new List<IEntity>(256);
        }

        public override void OnEnter()
        {
            // 註冊事件
            GameCore.Event.Subscribe(NetDataEvent.CastleEvent.EventId, onCastleEvent);
            GameCore.Event.Subscribe(NetDataEvent.UpdatePopulationEvent.EventId, onUpdatePopulation);
            GameCore.Event.Subscribe(NetDataEvent.ResetBuildingEvent.EventId, onResetBuilding);
            GameCore.Event.Subscribe(ZoomEventsArgs.EventId, onZoom);

            collectInterval = Paramater.GetCollectInterval();
        }

        public override void OnExit()
        {
            // 註銷事件
            GameCore.Event.Unsubscribe(NetDataEvent.CastleEvent.EventId, onCastleEvent);
            GameCore.Event.Unsubscribe(NetDataEvent.UpdatePopulationEvent.EventId, onUpdatePopulation);
            GameCore.Event.Unsubscribe(NetDataEvent.ResetBuildingEvent.EventId, onResetBuilding);
            GameCore.Event.Unsubscribe(ZoomEventsArgs.EventId, onZoom);

            spawnStack.Clear();
        }

        public override void OnFixedUpdate()
        {
            #region 生成城堡
            if (spawnStack.Count > 0)
            {
                var cameraLogic = GameCore.WorldMap.GetLogicHandler<WorldMapCameraLogicHandler>(WorldMapCameraLogicHandler.HandlerID);
                if (cameraLogic != null && cameraLogic.IsMove() == false)
                {
                    createCastleEntity(spawnStack.Pop());
                }
            }
            #endregion
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            #region 採集資源
            if (collectSerialId.HasValue || CheckCollectCondition() == false)
                return;

            var castle = GameCore.Entity.GetEntity(GameCore.NetData.SelfCastle.Guid);
            if (castle != null)
            {
                collectSerialId = GameCore.Entity.GenerateSerialId();
                GameCore.Entity.ShowHUD(typeof(HUDCollection), new HUDEntityData(collectSerialId.Value, 14, castle.Logic.CachedTransform, castle.Id, new HUDCollection.Preset() { action = CollectResource }));
            }
            #endregion
        }

        public override void OnLateUpdate()
        {
            castleZoomProcess();
        }

        // 是否超過可採集時間間隔
        public bool IsPassCollectIntervalTime()
        {
            return (GameCore.NetData.GetCastleCollectTime() + collectInterval < (int)GameCore.NetData.UTC);
        }

        // 檢測採集條件
        protected bool CheckCollectCondition()
        {
            // 是否達到可採集時間間隔            
            if (!IsPassCollectIntervalTime())
                return false;

            // 是否在2D畫面
            if (Map2DLogicHandler.Is2D())
                return false;

            return true;
        }

        public void CollectResource()
        {
            if (!CheckCollectCondition())
                return;

            var msg = new NE_GetBuildingResourceC() { Serial = client_serial_type.ENeGetBuildingResourceC };
            GameCore.Network.Send(msg.Serial, msg, GameCore.Connect.ProxyChannel);

            // 採集後先將紀錄的採集時間往後推, 待server update正確採集時間, 才不會採集HUD一關閉又於OnUpdate時立刻重開
            GameCore.NetData.SetMaxCastleCollectTime();

            if (collectSerialId.HasValue)
            {
                GameCore.Entity.HideEntity(collectSerialId.Value);
                collectSerialId = null;
            }
        }

        #region event
        private void onCastleEvent(object sender, GameEventArgs e)
        {
            var eventArgs = e as NetDataEvent.CastleEvent;
            Entity tempEntity = null;
            for (int i = 0; i < eventArgs.Guids.Count; i++)
            {
                tempEntity = GameCore.Entity.GetEntity(eventArgs.Guids[i]);
                if (tempEntity != null)
                {
                    // 更新資料
                    tempEntity.Logic.Refresh(null);
                }
                else
                {
                    // 加入生成排程
                    if (eventArgs.Guids[i] == GameCore.NetData.SelfCastle.Guid)
                        createCastleEntity(eventArgs.Guids[i]);
                    else
                        spawnStack.Push(eventArgs.Guids[i]);
                }
            }
        }

        private void onUpdatePopulation(object sender, GameEventArgs e)
        {
            upgradeSelfCastle();
        }

        private void onResetBuilding(object sender, GameEventArgs e)
        {
            var eventArgs = e as NetDataEvent.ResetBuildingEvent;
            if (eventArgs == null)
                return;

            SceneObj sceneObj;
            if (GameCore.NetData.TryGetSceneObjData(GameCore.NetData.SelfCastle.Guid, out sceneObj) == false)
                return;

            CastleObj castle = sceneObj as CastleObj;
            castle.SetResetBuildingFlag(eventArgs.PosIndex);
        }

        private void onZoom(object sender, GameEventArgs e)
        {
            ZoomEventsArgs ne = (ZoomEventsArgs)e;

            // 2D畫面時隱藏泡泡框
            if (ne.ZoomType == WorldMapCameraSetting.ZoomType.ChangeLevel && ne.ZoomLevel == WorldMapCameraSetting.ZoomLevel.LOW_2D)
            {
                if (collectSerialId.HasValue)
                    GameCore.Entity.HideEntity(collectSerialId.Value);

                collectSerialId = null;
            }
        }
        #endregion

        /// <summary>
        /// 根據GUID產生對應的主堡外觀
        /// </summary>
        /// <param name="guid"></param>
        private void createCastleEntity(int guid)
        {
            SceneObj sceneObj;
            if (GameCore.NetData.TryGetSceneObjData(guid, out sceneObj) == false)
                return;

            if (GameCore.Entity.HasEntity(guid))
                return;

            CastleObj castle = sceneObj as CastleObj;
            CastleEntityData data = new CastleEntityData(castle.Guid(), BuildingData.GetCastle(castle.Culture(), castle.Population()).prefab) { Position = castle.Position() };
            GameCore.Entity.ShowCastle(data);
        }

        /// <summary>
        /// 根據提升的人口數改變主城外觀
        /// </summary>
        private void upgradeSelfCastle()
        {
            SceneObj sceneObj;
            if (GameCore.NetData.TryGetSceneObjData(GameCore.NetData.SelfCastle.Guid, out sceneObj) == false)
                return;

            CastleObj castle = sceneObj as CastleObj;

            var newPrefab = BuildingData.GetCastle(castle.Culture(), castle.Population()).prefab;
            int scaffoldSerialID = 0;
            bool haveOldCastle = false;

            if (GameCore.Entity.HasEntity(GameCore.NetData.SelfCastle.Guid))
            {
                var castleEntity = GameCore.Entity.GetEntity(GameCore.NetData.SelfCastle.Guid);

                // 人口提升後仍使用相同主堡外觀
                if (System.IO.Path.GetFileNameWithoutExtension(castleEntity.EntityAssetName).Equals(newPrefab))
                    return;

                haveOldCastle = true;

                // 顯示鷹架
                scaffoldSerialID = GameCore.Entity.GenerateSerialId();
                GameCore.Entity.ShowMisc(new MiscEntityData(scaffoldSerialID, Constant.Resource.Scaffold) { Position = castle.Position() });
            }

            MEC.Timing.CallDelayed(Constant.Resource.ConstructTime, () => 
            {
                // 隱藏舊主堡
                if (haveOldCastle)
                {                    
                    GameCore.Entity.HideEntity(GameCore.NetData.SelfCastle.Guid);
                }

                // 移除鷹架
                GameCore.Entity.HideEntity(scaffoldSerialID);

                // 顯示新主堡
                CastleEntityData data = new CastleEntityData(GameCore.NetData.SelfCastle.Guid, newPrefab) { Position = castle.Position() };
                GameCore.Entity.ShowCastle(data);

                // 播放建設完成特效
                int fxSerialID = GameCore.Entity.GenerateSerialId();
                GameCore.Entity.ShowParticle(new ParticleEntityData(fxSerialID, Constant.Resource.ConstructFX, 0) { Position = castle.Position() });
            });
        }

        /// <summary>
        /// 主堡無極縮放內城處理
        /// </summary>
        private void castleZoomProcess()
        {
            if (curLerp == GameCore.Zoom.Last3DLerp)
                return;

            // 取得縮放值
            curLerp = GameCore.Zoom.Last3DLerp;

            // 取得所有主堡
            GameCore.Entity.GetEntityGroup(Constant.EntityGroup.Castle).GetAllEntities(entities);
            var etor = entities.GetEnumerator();
            Entity entity;
            var calLerp = curLerp * Constant.NetData.FloatRevert;
            while (etor.MoveNext())
            {
                entity = (Entity)etor.Current;
                if (entity.Logic.Visible == false)
                    continue;

                entity.Logic.OnZoom(calLerp, GameCore.Zoom.Scale3DVector);
            }

            // 主堡採集
            if (collectSerialId.HasValue && GameCore.Entity.HasEntity(collectSerialId.Value))
                GameCore.Entity.GetEntity(collectSerialId.Value).Logic.OnZoom(calLerp, GameCore.Zoom.Scale3DVector);
        }

        public bool CheckStackGuid(int guid)
        {
            foreach (var item in spawnStack)
            {
                if (item == guid)
                    return true;
            }

            return false;
        }
    }
}