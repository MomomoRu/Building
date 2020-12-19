using UnityEngine;
using UnityGameFramework.Runtime;

namespace Game.Hotfix
{
    public class BuildEntity : RenderEntity
    {
        private Material roadMaterial;
        private static readonly string road = "road";
        private static readonly string textureName = "_SubShadow";
        private int? timeHUDSerialID;
        private int scaffoldSerialID;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            Click = true;

            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer.name.ToLower().Contains(road))
                    roadMaterial = renderer.material;
            }
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            // 自己主堡產生時, 檢查是否需要採集資源
            SceneObj sceneObj;
            if (GameCore.NetData.TryGetSceneObjData(Id, out sceneObj) == false)
                return;

            SelfOwner = sceneObj.OwnerIsSelf();

            InternalSetVisible(Visible);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);

            setShadow(null);
        }

        protected override void OnAttached(EntityLogic childEntity, Transform parentTransform, object userData)
        {
            base.OnAttached(childEntity, parentTransform, userData);

            setShadow(childEntity as SubBuildEntity);
        }

        protected override void OnDetached(EntityLogic childEntity, object userData)
        {
            base.OnDetached(childEntity, userData);

            setShadow(null);
        }

        /// <summary>
        /// 新增附屬建築的道路影子貼圖
        /// </summary>
        /// <param name="subBuildEntity"></param>
        protected void setShadow(SubBuildEntity subBuildEntity)
        {
            roadMaterial.SetTexture(textureName, subBuildEntity == null ? null : subBuildEntity.ShadowTexture);
        }
    }
}