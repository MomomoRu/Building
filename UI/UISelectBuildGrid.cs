namespace Game.Hotfix
{
    public partial class UISelectBuildGrid : AnimatorForm
    {
        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            ExitBtn.onClick.AddListener(()=> { GameCore.UI.CloseUIForm(this); });
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);
            clearListeners();
        }
    }
}