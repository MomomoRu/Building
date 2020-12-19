using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Game.Hotfix
{
    public partial class UISubBuildingList : AnimatorForm
    {
        public CIV_Text txtTip;
        public CIV_Button ExitButton;
        public CIV_Button btnBack;
        public CIV_Text txt_noBuilds;
        public CIV_Text txtPopulation;
        public LoopHorizontalScrollRect BuildingList;
        public Transform BtnRoot;
        public CIV_Button CancelBtn;
        public CIV_Text TextCancel;
        public CIV_Button OkBtn;
        public CIV_Text TextOK;
        
        private void clearListeners()
        {
            ExitButton.onClick.RemoveAllListeners();
            btnBack.onClick.RemoveAllListeners();
            CancelBtn.onClick.RemoveAllListeners();
            OkBtn.onClick.RemoveAllListeners();
       }
    }
}