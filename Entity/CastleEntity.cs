using GameFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Game.Hotfix
{
    public class CastleEntity : RenderEntity
    {
        public class BuildInfo : IReference
        {
            public BuildingObject buildingObj;
            public EntityRequest<BuildEntity> master;
            public EntityRequest<SubBuildEntity> slave;
            public EntityRequest<MiscEntity> scaffold;
            public EntityRequest<ParticleEntity> constructFX;
            public long ownerId;
            public Transform parent;
            
            private int constructFxID;

            public BuildInfo()
            {
                master = new EntityRequest<BuildEntity>();
                slave = new EntityRequest<SubBuildEntity>();
                scaffold = new EntityRequest<MiscEntity>();
                constructFX = new EntityRequest<ParticleEntity>();
            }

            public void Clear()
            {
                buildingObj = null;
                parent = null;
                ownerId = 0;
                slave.Release();
                master.Release();
                scaffold.Release();

                constructFxID = 0;
            }

            public void Create(Transform parent)
            {
                this.parent = parent;
                createBuilding();
            }

            private void createBuilding()
            {
                if (buildingObj.IsNew == false)
                    return;

                buildingObj.IsNew = false;

                // 建造流程演出:先顯示鷹架, 帶移除鷹架後才顯示建物&建造特效
                if (buildingObj.IsReset)
                {
                    buildingObj.IsReset = false;

                    // 顯示鷹架
                    scaffold.LoadEntity(new MiscEntityData(GameCore.Entity.GenerateSerialId(), Constant.Resource.Scaffold),
                    (scaffoldEntity) =>
                    {
                        scaffoldEntity.CachedTransform.SetParent(parent);
                        scaffoldEntity.CachedTransform.localPosition = Vector3.zero;
                        scaffoldEntity.CachedTransform.localScale = Vector3.one * 0.1f;
                    });

                    MEC.Timing.CallDelayed(Constant.Resource.ConstructTime, () =>
                    {
                        // 移除鷹架
                        scaffold.Release();                        

                        // 主建築                
                        var masterData = GameCore.DataTable.GetData<BuildingData>(buildingObj.MasterTid);
                        master.LoadEntity(new BuildEntityData(buildingObj.Guid, masterData.prefab, ownerId),
                        (entity) =>
                        {
                            entity.CachedTransform.SetParent(parent);
                            entity.CachedTransform.localPosition = Vector3.zero;
                            entity.CachedTransform.localScale = Vector3.one * 0.1f;

                            if (buildingObj.SlaverTid <= 0)
                            {
                                // 播放建設完成特效
                                constructFxID = GameCore.Entity.GenerateSerialId();
                                GameCore.Entity.ShowAppendParticle(new AppendParticleEntityData(constructFxID, Constant.Resource.ConstructFX, 0, parent, 0.1f) { });

                                // 使特效和建物同時顯示
                                entity.Visible = true;

                                return;
                            }

                            // 副建築
                            var slaveData = GameCore.DataTable.GetData<BuildingData>(buildingObj.SlaverTid);
                            slave.LoadEntity(new BuildEntityData(GameCore.Entity.GenerateSerialId(), slaveData.prefab, ownerId),
                            (subEntity) =>
                            {
                                GameCore.Entity.AttachEntity(subEntity.Id, master.entity.Id);
                                subEntity.CachedTransform.localPosition = Vector3.zero;
                                subEntity.CachedTransform.localScale = Vector3.one;

                                // 播放建設完成特效
                                constructFxID = GameCore.Entity.GenerateSerialId();
                                GameCore.Entity.ShowAppendParticle(new AppendParticleEntityData(constructFxID, Constant.Resource.ConstructFX, 0, parent, 0.1f) { });

                                // 使特效和建物同時顯示
                                entity.Visible = true;
                                subEntity.Visible = true;
                            });
                        });
                    });
                }
                else
                {
                    // 主建築                
                    var masterData = GameCore.DataTable.GetData<BuildingData>(buildingObj.MasterTid);
                    master.LoadEntity(new BuildEntityData(buildingObj.Guid, masterData.prefab, ownerId),
                    (entity) =>
                    {
                        entity.CachedTransform.SetParent(parent);
                        entity.CachedTransform.localPosition = Vector3.zero;
                        entity.CachedTransform.localScale = Vector3.one * 0.1f;

                        if (buildingObj.SlaverTid <= 0)
                            return;

                        // 副建築
                        var slaveData = GameCore.DataTable.GetData<BuildingData>(buildingObj.SlaverTid);
                        slave.LoadEntity(new BuildEntityData(GameCore.Entity.GenerateSerialId(), slaveData.prefab, ownerId),
                        (subEntity) =>
                        {
                            GameCore.Entity.AttachEntity(subEntity.Id, master.entity.Id);
                            subEntity.CachedTransform.localPosition = Vector3.zero;
                            subEntity.CachedTransform.localScale = Vector3.one;                            
                        });
                    });
                }
            }

            public void SetVisible(bool visible)
            {
                master.SetVisible(visible);
                slave.SetVisible(visible);
            }

            public static BuildInfo Create(BuildingObject buildingObj, long ownerId)
            {
                BuildInfo result = ReferencePool.Acquire<BuildInfo>();
                // set properties
                result.buildingObj = buildingObj;
                result.ownerId = ownerId;
                return result;
            }
        }

        private EntityRequest<CastleGridHexEntity> gridHex;
        private const string gridHexAssetName = "CastleGridHex";
        private List<BuildInfo> builds;
        private Transform[] cacheRenderTransforms;

        #region override method
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            if (cacheRenderers.Length > 0)
            {
                cacheRenderTransforms = new Transform[cacheRenderers.Length];
                for (int i = 0; i < cacheRenderers.Length; i++)
                    cacheRenderTransforms[i] = cacheRenderers[i].transform;
            }
                
            Click = true;
            builds = new List<BuildInfo>();
            gridHex = new EntityRequest<CastleGridHexEntity>();
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            // 自己主堡產生時, 檢查是否需要採集資源
            SceneObj sceneObj;
            if (GameCore.NetData.TryGetSceneObjData(Id, out sceneObj) == false)
                return;

            // 設定初始建築資料
            initBuildInfo();

            // 設定是否顯示
            InternalSetVisible(Visible);

            // 通知植披系統不要在此地格顯示地格
            WorldAgent.Vector2i hex = new WorldAgent.Vector2i(sceneObj.Hex().x, sceneObj.Hex().y);
            GameCore.Event.Fire(this, VegetationShownEventArgs.Create(hex, false));
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);
            CachedTransform.localScale = Vector3.one;

            if (isShutdown)
                return;

            var etor = builds.GetEnumerator();
            while (etor.MoveNext())
            {
                ReferencePool.Release(etor.Current);
            }
            builds.Clear();

            gridHex.Release();

            // 通知植披系統恢復顯示
            GameCore.Event.Fire(this, VegetationShownEventArgs.Create(HexUtility.Position2Hex(CachedTransform.position), true));
        }

        public override void OnZoom(float lerp, Vector3 scaleVector)
        {
            CachedTransform.localScale = scaleVector;
            // 修正地板高度
            for (int i = 0; i < cacheRenderTransforms.Length; i++)
                cacheRenderTransforms[i].position = new Vector3(cacheRenderTransforms[i].position.x, 0.6f * scaleVector.y, cacheRenderTransforms[i].position.z);

            // 主副建築
            if (gridHex.entity != null)
            {
                bool visible = gridHex.entity.OnZoom(lerp);
                for (int i = 0; i < builds.Count; i++)
                    builds[i].SetVisible(visible);  
            }
        }

        protected override void showHud()
        {
            if (hudEntReq == null)
                hudEntReq = new CustomEntityRequest(typeof(HUDCastleName), Constant.EntityGroup.UI);

            if (hudEntReq.IsRequested)
                return;

            hudEntReq.LoadEntity(new HUDEntityData(GameCore.Entity.GenerateSerialId(), 15, CachedTransform, Id));
        }

        protected override void InternalSetVisible(bool visible)
        {
            base.InternalSetVisible(visible);

            // 無極縮放
            OnZoom(GameCore.Zoom.Last3DLerp * Constant.NetData.FloatRevert, GameCore.Zoom.Scale3DVector);

            for (int i = 0; i < builds.Count; i++)
                builds[i].SetVisible(visible);
        }
        #endregion

        /// <summary>
        /// 移除舊建築
        /// </summary>
        /// <param name="posIndex"></param>
        public void RemoveBuilding(ESceneGridDir posIndex)
        {
            int index = builds.FindIndex(build => build.buildingObj.PosIndex == posIndex);
            if (index == -1)
                return;

            ReferencePool.Release(builds[index]);
            builds.RemoveAt(index);
        }

        /// <summary>
        /// 伺服器更新主堡資料
        /// </summary>
        public override void Refresh(object userData)
        {
            if (hudEntReq != null && hudEntReq.entity != null)
                hudEntReq.entity.Refresh(userData);

            SceneObj sceneObj;
            if (GameCore.NetData.TryGetSceneObjData(Id, out sceneObj) == false)
                return;

            CastleObj castleObj = sceneObj as CastleObj;
            if (castleObj.GetBuildCount() == 0)
                return;

            // 產生建築資訊
            var etor = castleObj.GetBuilds();
            while (etor.MoveNext())
            {
                if (etor.Current.IsNew == false)
                    continue;

                builds.Add(BuildInfo.Create(etor.Current, sceneObj.OwnerId()));
            }

            createBuilds();
        }

        /// <summary>
        /// 設定主/副建築資料
        /// </summary>
        private void initBuildInfo()
        {
            SceneObj sceneObj;
            if (GameCore.NetData.TryGetSceneObjData(Id, out sceneObj) == false)
                return;

            CastleObj castleObj = sceneObj as CastleObj;
            if (castleObj.OwnerIsSelf() == false && castleObj.GetBuildCount() == 0)
                return;

            // 產生建築資訊
            var etor = castleObj.GetBuilds();
            while (etor.MoveNext())
            {
                builds.Add(BuildInfo.Create(etor.Current, sceneObj.OwnerId()));
            }

            // 產生建築GridRoot
            gridHex.Release();
            gridHex.LoadEntity(new MiscEntityData(GameCore.Entity.GenerateSerialId(), gridHexAssetName),
            (entity) =>
            {
                entity.CachedTransform.position = CachedTransform.position;
                gridHex.entity.OnZoom(GameCore.Zoom.Last3DLerp * Constant.NetData.FloatRevert);
                createBuilds();
            });
        }

        private void createBuilds()
        {
            var entity = gridHex.entity;
            for (int i = 0; i < builds.Count; i++)
                builds[i].Create(entity.GetTransform(builds[i].buildingObj.PosIndex));
        }
    }
}