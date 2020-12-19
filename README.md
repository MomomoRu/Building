# Building
建築建造功能

CastleLogicHandler:主城邏輯控制
NetDataComponent:網路資料组件
NetDataEvent:定義網路事件

## 流程管理
使用狀態機控制建造流程的切換
> 資料夾 : Building\Procedure

WorldMapView 大地圖 : 
  -在主堡資訊界面點選"建設"按鈕, 切換至 WorldMapSelectBuildingGrid
  -在主建築資訊界面點選"建設"按鈕, 切換至 WorldMapSelectSubBuilding
WorldMapSelectBuildingGrid 選擇建造地格	: 顯示可選擇之地格, 點選地格後切換至 WorldMapSelectMainBuilding		
WorldMapSelectMainBuilding 選擇主建築 : 開啟主建築選單介面選擇主建物, 選擇欲建設之主建築後切換至 WorldMapSelectSubBuilding	
WorldMapSelectSubBuilding 選擇主建築之附屬建築 : 開啟附屬建築選單介面選擇附屬建物

## UI
建造流程有使用到的UI程式碼
> 資料夾 : Building\UI

## Entity
建築實體:主堡(CastleEntity), 主建築(BuildEntity), 附屬建築(SubBuildEntity)
> 資料夾 : Building\Entity
