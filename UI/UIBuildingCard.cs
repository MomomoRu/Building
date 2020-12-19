using Game.Hotfix.WorldMap;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using GameFramework;

namespace Game.Hotfix
{
    public class UIBuildingCardPreset : IReference
    {
        public int buildID;
        public int totalCost;       // 主建築流程為主+副的開銷, 副建築流程為副建築開銷
        public int nonUsingCost;
        public bool isUsing = false;
        public ESceneGridDir posIndex;
        public Vector3 position;
        public UnityAction<object> selectAction;

        public void Clear()
        {
            buildID = 0;
            nonUsingCost = 0;
            isUsing = false;
            posIndex = ESceneGridDir.ESgdSelf;
            position = Vector3.zero;
            selectAction = null;
        }
    }

    public class UIBuildingCard : UILoopScrollRectItem
    {
        public static int RequireUnlockTechID = -1;

        [System.Serializable]
        public class Attribute
        {
            public CIV_Text title;
            public CIV_Text value;

            public void Refresh(bool enable, int attributeValue = 0, AttributesEffectStr effectStr = null)
            {
                title.enabled = enable;
                value.enabled = enable;

                if (!enable)
                {
                    return;
                }

                title.SetText(effectStr.GetText1ID(attributeValue));
                value.SetText(effectStr.GetTextByType3(attributeValue));
            }
        }

        [System.Serializable]
        public class BuildingCard
        {
            enum DisplayMode
            {
                None = 0,
                Normal,
                Info
            }

            [SerializeField] protected GameObject root;
            [SerializeField] protected CIV_Toggle selectedToggle;
            [SerializeField] protected GameObject normalModeRoot;
            [SerializeField] protected GameObject infoModeRoot;
            [SerializeField] protected GameObject maxRoot;
            [SerializeField] protected Image buildIcon;
            [SerializeField] protected CIV_Button infoBtn;
            [SerializeField] protected CIV_Button infoBackBtn;
            [SerializeField] protected CIV_Text name;
            [SerializeField] protected CIV_Text description;
            [SerializeField] protected Attribute[] attributes;
            [SerializeField] protected CIV_Button selectBtn;

            protected BuildingData buildData;

            private DisplayMode displayMode = DisplayMode.None;
            private DisplayMode curDisplayMode
            {
                set
                {
                    if (displayMode != value)
                    {
                        displayMode = value;
                        normalModeRoot.SetActive(displayMode == DisplayMode.Normal);
                        infoModeRoot.SetActive(displayMode == DisplayMode.Info);
                    }
                }
            }

            public void Init()
            {
                selectedToggle.isOn = false;
            }

            public void Refresh()
            {
                var toggleGroup = root.transform.parent.parent.GetComponent<ToggleGroup>();
                selectedToggle.group = toggleGroup;

                if (buildData != null)
                {
                    curDisplayMode = DisplayMode.Normal;

                    buildIcon.LoadIcon(Constant.IconAtlas.Building, buildData.info_2D);
                    name.SetText(buildData.name_id);

                    description.SetText(buildData.name_id, GameFramework.Localization.StringEnum.Text1);

                    maxRoot.SetActive(buildData.IsMainBuilding);

                    if (buildData.attributes_ID > 0)
                    {
                        var attribute = GameCore.DataTable.GetDataTable<Attributes>().GetDataRow(buildData.attributes_ID);
                        var effectStrDatas = attribute.EffectStrDatas;
                        var values = attribute.Values;
                        for (int i = 0; i < attributes.Length; i++)
                        {
                            if (i < effectStrDatas.Length)
                            {
                                attributes[i].Refresh(true, values[i][0], effectStrDatas[i]);
                            }
                            else
                            {
                                attributes[i].Refresh(false);
                            }
                        }
                    }
                    else
                    {
                        attributes.ForEach(attribute => attribute.Refresh(false));
                    }
                }
            }

            public void RefreshButtons(UIBuildingCardPreset preset)
            {
                infoBtn.onClick.AddListener(() =>
                {
                    selectedToggle.isOn = true;
                    curDisplayMode = DisplayMode.Info;
                });

                infoBackBtn.onClick.AddListener(() =>
                {
                    selectedToggle.isOn = true;
                    curDisplayMode = DisplayMode.Normal;
                });

                // 選擇該建築進行建設
                selectBtn.onClick.AddListener(() =>
                {
                    selectedToggle.isOn = true;

                    if (buildData.IsMainBuilding)
                    {
                        if (CheckBuildCondition(buildData, preset.totalCost, preset.nonUsingCost))
                        {
                            GameCore.Event.Fire(this, ChangeStateEventArgs.Create(typeof(WorldMapSelectSubBuilding), new WorldMapSelectSubBuilding.Preset() {  posIndex = preset.posIndex, position = preset.position, masterTid = buildData.Id, isMainBuildingProcess = true }));
                        }
                    }
                    else if (buildData.IsSubBuilding)
                    {
                        if (preset.selectAction != null)
                            preset.selectAction(buildData);
                    }
                });
            }

            public void OnHide()
            {
                selectedToggle.isOn = false;
                infoBtn.onClick.RemoveAllListeners();
                infoBackBtn.onClick.RemoveAllListeners();
                selectBtn.onClick.RemoveAllListeners();
            }
        }

        [System.Serializable]
        public class UnlockCard : BuildingCard
        {
            [SerializeField] CIV_Text maxTitle;
            [SerializeField] CIV_Text maxNum;
            [SerializeField] GameObject costRoot;            
            [SerializeField] UIIconUnion populationUnion;
            [SerializeField] GameObject unlockTipRoot;
            [SerializeField] CIV_Text unlockTip;

            public int coinTextStyleIdx = 13;
            public int maxTextStyleIdx = 13;

            private int redTextStyleIdx = 2;

            public void Refresh(UIBuildingCardPreset preset)
            {
                buildData = GameCore.DataTable.GetDataTable<BuildingData>().GetDataRow(preset.buildID);
                if (buildData != null)
                {
                    if (buildData.IsUnlocked)
                    {
                        root.SetActive(true);
                    }
                    else
                    {
                        root.SetActive(false);
                        return;
                    }

                    Refresh();
                    RefreshButtons(preset);

                    if (preset.isUsing)
                    {   
                        costRoot.SetActive(false);
                        unlockTipRoot.SetActive(true);
                        unlockTip.SetText(300012);

                        if (buildData.IsSubBuilding)
                        {
                            // 預設為使用中的卡片, 直接觸發被點選的效果
                            selectBtn.onClick.Invoke();
                        }
                        else
                        {
                            selectedToggle.isOn = true;
                        }
                    }
                    else
                    {
                        unlockTipRoot.SetActive(false);
                        costRoot.SetActive(true);

                        maxTitle.SetText(300007);
                        maxNum.text = Utility.Text.Format(GameCore.Localization.GetString(300013), GameCore.NetData.GetBuildingCount(buildData.Id), buildData.max);

                        populationUnion.SetText(buildData.population_cost.GeneralFormat());
                        populationUnion.ChangeStyle(preset.nonUsingCost < buildData.population_cost ? redTextStyleIdx : coinTextStyleIdx);
                    }
                }
            }
        }

        [System.Serializable]
        public class LockCard : BuildingCard
        {
            [SerializeField] CIV_Text unlockTip;

            public void Refresh(UIBuildingCardPreset preset)
            {
                buildData = GameCore.DataTable.GetDataTable<BuildingData>().GetDataRow(preset.buildID);
                if (buildData != null)
                {
                    if (buildData.IsUnlocked)
                    {
                        root.SetActive(false);
                        return;
                    }
                    else
                    {
                        root.SetActive(true);
                    }

                    Refresh();
                    RefreshButtons(preset);

                    if (buildData.culture_tree_id > 0)
                    {
                        var cultureTreeData = GameCore.DataTable.GetDataTable<CultureTreeData>().GetDataRow(buildData.culture_tree_id);
                        unlockTip.SetText(GameFramework.Utility.Text.Format(GameCore.Localization.GetString(300019), GameCore.Localization.GetString(cultureTreeData.string_id)));
                    }
                }
            }
        }

        [SerializeField] UnlockCard unlockCard;
        [SerializeField] LockCard lockCard;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            unlockCard.Init();
            lockCard.Init();
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            var srData = userData as UILoopScrollRectItemData;
            if (srData != null)
            {
                var preset = srData.ItemData as UIBuildingCardPreset;
                if (preset != null)
                {
                    unlockCard.Refresh(preset);
                    lockCard.Refresh(preset);
                }
            }
        }

        /// <summary>
        /// 隱藏。
        /// </summary>
        /// <param name="isShutdown">是否是關閉實體管理器時觸發。</param>
        /// <param name="userData">自定義數據。</param>
        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);

            unlockCard.OnHide();
            lockCard.OnHide();
        }

        // 主建築檢查所需「文明樹、人口、最大數量」
        // 副建築檢查所需「文明樹、人口」
        public static bool CheckBuildCondition(BuildingData buildData, int buildCost, int nonUsingCost)
        {
            if (buildData != null)
            {
                if (!buildData.IsUnlocked)
                {
                    RequireUnlockTechID = buildData.culture_tree_id;
                    var cultrueTreeData = GameCore.DataTable.GetDataTable<CultureTreeData>().GetDataRow(buildData.culture_tree_id);                    
                    var remainTime = GameCore.NetData.GetRemainTime(NetDataComponent.TechEnchantID);
                    if (remainTime == null || remainTime.Value > 0)
                    {
                        // 無待採收科技                        
                        UIComfirm.ShowComfirm(GameCore.Localization.GetString(300014),
                            Utility.Text.Format(GameCore.Localization.GetString(300015), GameCore.Localization.GetString(cultrueTreeData.string_id)),
                            (cultureTreeID) =>
                            {
                                // check remain time again 
                                var remainTime2 = GameCore.NetData.GetRemainTime(NetDataComponent.TechEnchantID);
                                if (remainTime2 == null || remainTime2.Value > 0)
                                {
                                    // 關閉建築清單，前往文明樹介面，並且焦點為對應的文明樹ID
                                    GameCore.Event.Fire(null, ChangeStateEventArgs.Create(typeof(WorldMapCultureTree), cultureTreeID));
                                }
                                else
                                {
                                    // 採收科技
                                    GameCore.NetData.SendLearnTech((int)cultureTreeID, NE_LearnTechC.Types.EOperate.EGet);
                                }
                            },
                            cultrueTreeData.Id, 
                            null, null,
                            GameCore.Localization.GetString(20),
                            GameCore.Localization.GetString(21));
                        return false;
                    }
                    else
                    {
                        // 有已經完成的科技待採收
                        UIComfirm.ShowComfirm(GameCore.Localization.GetString(300014),
                            Utility.Text.Format(GameCore.Localization.GetString(300016), GameCore.Localization.GetString(cultrueTreeData.string_id)),
                            (cultureTreeID) =>
                            {
                                // 採收科技, 收取後前往文明樹頁面檢視
                                GameCore.NetData.SendLearnTech((int)cultureTreeID, NE_LearnTechC.Types.EOperate.EGet);
                            },
                            cultrueTreeData.Id,
                            null, null,
                            GameCore.Localization.GetString(300017),
                            GameCore.Localization.GetString(21));
                        return false;
                    }
                }

                if (buildCost > nonUsingCost)
                {
                    // 人口需求超出上限，文明樹的研發可提昇人口數量
                    WorldSystems.MsgSys.AddPersonalMsg(GameCore.Localization.GetString(300056));
                    return false;
                }

                if (buildData.IsMainBuilding)
                {
                    // check 當前建物建築最大數量
                    if (GameCore.NetData.GetBuildingCount(buildData.Id) >= buildData.max)
                    {
                        // 已達最大可建設數量
                        WorldSystems.MsgSys.AddPersonalMsg(GameCore.Localization.GetString(300010));
                        return false;
                    }
                }
            }
            return true;
        }
    }
}