using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Game.Hotfix
{
    public partial class UISelectBuildGrid : AnimatorForm
    {
        public CIV_Button ExitBtn;

        
        private void clearListeners()
        {
            ExitBtn.onClick.RemoveAllListeners();
       }
    }
}