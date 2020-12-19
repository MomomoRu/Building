namespace Game.Hotfix
{
    public class CastleEntityData : GameEntityData
    {
        public string AssetName { get; private set; }

        public CastleEntityData(int entityId, string asseetName) : base(entityId)
        {
            AssetName = GameFramework.Utility.Text.Format("Castle/{0}", asseetName);
        }

        public override string GetAssetName()
        {
            return AssetName;
        }
    }
}