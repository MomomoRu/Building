using Game.Hotfix.WorldMap;
using GameFramework;
using System.Collections.Generic;
using System.Linq;

namespace Game.Hotfix
{
    public class UISubBuildingListPreset : IReference
    {
        public BuildingObj masterBuildingObj;
        public PreviewBuildingHex previewBuildingHex;

        public void Clear()
        {
            masterBuildingObj = null;
            previewBuildingHex = null;
        }
    }

    public partial class UISubBuildingList : AnimatorForm
    {
        private BuildingData masterData;
        private BuildingData selectData;
        private int slaverTid;
        private int preSlaverTid;
        private int usingPopulation;
        private int buildCost = 0;
        private int nonUsingCost = 0;
        private List<UIBuildingCardPreset> dataPreset = new List<UIBuildingCardPreset>();
        private WorldMapSelectSubBuilding.Preset subBuildingPreset;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            txtTip.SetText(300066);
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            subBuildingPreset = userData as WorldMapSelectSubBuilding.Preset;

            btnBack.gameObject.SetActive(subBuildingPreset.isMainBuildingProcess);
            btnBack.onClick.AddListener(() => { GameCore.UI.CloseUIForm(UIForm, false); });
            CancelBtn.onClick.AddListener(() => { GameCore.UI.CloseUIForm(UIForm, true); });
            ExitButton.onClick.AddListener(() => { GameCore.UI.CloseUIForm(UIForm, true); });            
            OkBtn.onClick.AddListener(()=> 
            {
                if (selectData == null)
                {
                    var msg = new NE_ResetBuildingC() { Serial = client_serial_type.ENeResetBuildingC, Building = new BuildingObject() { PosIndex = subBuildingPreset.posIndex, MasterTid = subBuildingPreset.masterTid, SlaverTid = 0 } };
                    GameCore.Network.Send(msg.Serial, msg, GameCore.Connect.ProxyChannel);
                    GameCore.UI.CloseUIForm(UIForm, true);
                }
                else if (UIBuildingCard.CheckBuildCondition(selectData, buildCost, nonUsingCost))
                {
                    var msg = new NE_ResetBuildingC() { Serial = client_serial_type.ENeResetBuildingC, Building = new BuildingObject() { PosIndex = subBuildingPreset.posIndex, MasterTid = subBuildingPreset.masterTid, SlaverTid = selectData.Id } };
                    GameCore.Network.Send(msg.Serial, msg, GameCore.Connect.ProxyChannel);
                    GameCore.UI.CloseUIForm(UIForm, true);
                }
            });

            masterData = GameCore.DataTable.GetData<BuildingData>(subBuildingPreset.masterTid);

            // 計算選擇地格是否有舊建築, 紀錄人口開銷
            var building = GameCore.NetData.GetBuilding(subBuildingPreset.posIndex);
            int savePopulationCost = 0;
            slaverTid = masterData.default_sub;
            preSlaverTid = 0;
            if (building != null && building.MasterTid > 0)
            {
                var buildingData = GameCore.DataTable.GetData<BuildingData>(building.MasterTid);
                if (subBuildingPreset.isMainBuildingProcess)
                    savePopulationCost += buildingData.population_cost;

                if (building.SlaverTid > 0)
                {
                    var slaverBuildingData = GameCore.DataTable.GetData<BuildingData>(building.SlaverTid);
                    savePopulationCost += slaverBuildingData.population_cost;

                    // 為相同建物的副建物進行置換, 原始設定才算是目前使用中建物
                    if (building.MasterTid == masterData.Id)
                    {
                        preSlaverTid = building.SlaverTid;
                        slaverTid = building.SlaverTid;
                    }
                }
            }

            // 建物總人口開銷 - 選擇地格建物人口開銷
            usingPopulation = GameCore.NetData.GetAllBuildingCost() - savePopulationCost;
            // 剩餘人口
            nonUsingCost = GameCore.NetData.Character.Population - usingPopulation;

            RefreshPopulation();
            UpdateBuildingListData();
            Refresh();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);
            clearListeners();
            subBuildingPreset = null;
        }

        private void UpdateBuildingListData()
        {
            dataPreset.Clear();

            // 建物顯示排序預設為ID遞增排序，當前的項目會顯示在最前面
            var buildDatas = GameCore.DataTable.GetDataTable<BuildingData>().GetDataRows(delegate (BuildingData building)
            {
                return (building.type == masterData.sub_type);
            });
            buildDatas = buildDatas.OrderBy(data => !data.IsUnlocked).ThenBy(data => data.Id != slaverTid).ThenBy(data => data.Id).ToArray();

            for (int i = 0; i < buildDatas.Length; i++)
            {
                UIBuildingCardPreset preset = ReferencePool.Acquire<UIBuildingCardPreset>();
                preset.buildID = buildDatas[i].Id;
                preset.totalCost = (subBuildingPreset.isMainBuildingProcess) ? masterData.population_cost + buildDatas[i].population_cost : buildDatas[i].population_cost;
                preset.nonUsingCost = nonUsingCost;
                preset.isUsing = buildDatas[i].Id == slaverTid;
                preset.selectAction = OnSelectBuilding;
                dataPreset.Add(preset);
            }

            txt_noBuilds.gameObject.SetActive(buildDatas.Length == 0);
            BtnRoot.gameObject.SetActive(buildDatas.Length == 0);
        }

        private void Refresh()
        {
            BuildingList.objectsToFill = dataPreset.ToArray();
            BuildingList.totalCount = dataPreset.Count;
            BuildingList.RefillCells();
        }

        private void RefreshPopulation(BuildingData selectSubBuildingData = null)
        {
            buildCost = (selectSubBuildingData == null) ? 0 : selectSubBuildingData.population_cost;
            if (subBuildingPreset.isMainBuildingProcess)
                buildCost += masterData.population_cost;

            // 顯示資訊
            txtPopulation.SetText(Utility.Text.Format("{0}/{1}", (usingPopulation + buildCost).GeneralFormat(), GameCore.NetData.Character.Population.GeneralFormat()));
        }

        private void OnSelectBuilding(object data)
        {
            selectData = data as BuildingData;
            subBuildingPreset.buildingHex.SelectSlave(selectData.Id);
            BtnRoot.gameObject.SetActive(preSlaverTid == 0 || selectData.Id != slaverTid);

            RefreshPopulation(selectData);
        }
    }
}