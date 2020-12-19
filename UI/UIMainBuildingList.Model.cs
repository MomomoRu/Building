using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Game.Hotfix
{
    public partial class UIMainBuildingList : AnimatorForm
    {
        public CIV_Text txtTip;
        public CIV_Button ExitButton;
        public CIV_Button btnBack;
        public CIV_Text txtPopulation;
        public CIV_Toggle Toggle_Economy;
        public CIV_Text Label_Economy;
        public CIV_Toggle Toggle_Military;
        public CIV_Text Label_Military;
        public CIV_Toggle Toggle_Civilization;
        public CIV_Text Label_Civilization;
        public LoopHorizontalScrollRect BuildingList;
        
        private void clearListeners()
        {
            ExitButton.onClick.RemoveAllListeners();
            btnBack.onClick.RemoveAllListeners();
            Toggle_Economy.onValueChanged.RemoveAllListeners();
            Toggle_Military.onValueChanged.RemoveAllListeners();
            Toggle_Civilization.onValueChanged.RemoveAllListeners();
       }
    }
}