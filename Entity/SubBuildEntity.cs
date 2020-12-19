using UnityEngine;

namespace Game.Hotfix
{
    public class SubBuildEntity : RenderEntity
    {
        public Texture2D ShadowTexture;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            InternalSetVisible(Visible);
        }
    }
}