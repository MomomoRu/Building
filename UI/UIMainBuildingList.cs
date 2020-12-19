using UnityGameFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GameFramework;
using Game.Hotfix.WorldMap;

namespace Game.Hotfix
{
    public partial class UIMainBuildingList : AnimatorForm
    {
        enum DisplayType 
        {
            None = 0,
            Economy = 1,
            Military = 2,
            Civilization = 3
        }

        private List<UIBuildingCardPreset> dataPreset = new List<UIBuildingCardPreset>();

        private DisplayType displayType = DisplayType.None;
        private DisplayType CurDisplayType
        {
            get { return displayType; }
            set 
            {
                if (displayType != value)
                {
                    displayType = value;
                    UpdateBuildingListData();
                    Refresh();
                }
            }
        }

        private WorldMapSelectMainBuilding.Preset worldMapPreset;
        private int nonUsingCost = 0;
        private int usingMasterTid = 0;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            txtTip.SetText(300065);
            Label_Economy.SetText(300004);
            Label_Military.SetText(300005);
            Label_Civilization.SetText(300006);
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            worldMapPreset = userData as WorldMapSelectMainBuilding.Preset;

            // 計算選擇地格是否有舊建築, 紀錄人口開銷
            var building = GameCore.NetData.GetBuilding(worldMapPreset.posIndex);
            int savePopulationCost = 0;
            usingMasterTid = building != null ? building.MasterTid : 0;
            if (building != null && building.MasterTid > 0)
            {
                var buildingData = GameCore.DataTable.GetData<BuildingData>(building.MasterTid);
                savePopulationCost += buildingData.population_cost;

                if (building.SlaverTid > 0)
                {
                    var slaverBuildingData = GameCore.DataTable.GetData<BuildingData>(building.SlaverTid);
                    savePopulationCost += slaverBuildingData.population_cost;
                }
            }

            // 建物總人口開銷 - 選擇地格建物人口開銷
            int usingPopulation = GameCore.NetData.GetAllBuildingCost() - savePopulationCost;
            // 剩餘人口
            nonUsingCost = GameCore.NetData.Character.Population - usingPopulation;
            // 顯示資訊
            txtPopulation.SetText(Utility.Text.Format("{0}/{1}", usingPopulation.GeneralFormat(), GameCore.NetData.Character.Population.GeneralFormat()));
            Toggle_Economy.onValueChanged.AddListener((on) => { if (on) CurDisplayType = DisplayType.Economy; });
            Toggle_Military.onValueChanged.AddListener((on) => { if (on) CurDisplayType = DisplayType.Military; });
            Toggle_Civilization.onValueChanged.AddListener((on) => { if (on) CurDisplayType = DisplayType.Civilization; });
            ExitButton.onClick.AddListener(() => { GameCore.UI.CloseUIForm(UIForm, true); });
            btnBack.onClick.AddListener(() => { GameCore.UI.CloseUIForm(UIForm, false); });
            Toggle_Economy.isOn = true;
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);
            clearListeners();

            Toggle_Economy.isOn = false;
            displayType = DisplayType.None;
            BuildingList.ClearCells();
        }

        private void UpdateBuildingListData()
        {
            dataPreset.Clear();

            // 建物顯示排序預設為ID遞增排序，但是已解鎖項目會再排在前段，後段為未解鎖
            // 目前使用中建物會顯示在最前面
            var buildData = GameCore.DataTable.GetDataTable<BuildingData>().GetDataRows(delegate (BuildingData building) 
            {
                return (building.ui_type == (int)CurDisplayType);
            });
            buildData = buildData.OrderBy(data => !data.IsUnlocked).ThenBy(data => data.Id != usingMasterTid).ThenBy(data => data.Id).ToArray();

            for (int i = 0; i < buildData.Length; i++)
            {
                UIBuildingCardPreset preset = ReferencePool.Acquire<UIBuildingCardPreset>();
                preset.buildID = buildData[i].Id;
                preset.nonUsingCost = nonUsingCost;
                preset.isUsing = buildData[i].Id == usingMasterTid;
                preset.posIndex = worldMapPreset.posIndex;
                preset.position = worldMapPreset.position;
                dataPreset.Add(preset);
            }
        }

        private void Refresh()
        {
            BuildingList.objectsToFill = dataPreset.ToArray();
            BuildingList.totalCount = dataPreset.Count;
            BuildingList.RefillCells();
        }
    }
}