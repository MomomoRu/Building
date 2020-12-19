namespace Game.Hotfix
{
    public class BuildEntityData : GameEntityData
    {
        public string AssetName { get; private set; }

        public long OwerId { get; private set; }

        public BuildEntityData(int entityId, string asseetName, long ownerId) : base(entityId)
        {
            AssetName = GameFramework.Utility.Text.Format("Building/{0}", asseetName);
            OwerId = ownerId;
        }

        public override string GetAssetName()
        {
            return AssetName;
        }
    }
}