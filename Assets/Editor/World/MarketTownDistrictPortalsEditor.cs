#if UNITY_EDITOR
using JRogue.World.Generation;
using JRogue.World.Town;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    /// <summary>All district portals on town_market: south strip, west residential strip, and building entrances.</summary>
    public static class MarketTownDistrictPortalsEditor
    {
        public static void EnsureMarketDistrictPortals()
        {
            var def = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.MarketFloorDef);
            if (def == null)
                return;

            int southStripWidth = DistrictSquareMarketTransition.StripMaxX - DistrictSquareMarketTransition.StripMinX + 1;
            int westStripHeight = MarketResidentialTransition.StripMaxY - MarketResidentialTransition.StripMinY + 1;
            int buildingPortalCount = 4;
            int portalCount = southStripWidth + westStripHeight + buildingPortalCount;

            var so = new SerializedObject(def);
            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = portalCount;

            WriteSouthStripPortals(
                portals,
                0,
                DistrictSquareMarketTransition.MarketToSquareLinkId,
                DimensionSquareFloorIds.FloorId,
                DistrictSquareMarketTransition.MarketSouthEdgeY,
                "Dimension Square");

            WriteWestStripPortals(
                portals,
                southStripWidth,
                MarketResidentialTransition.MarketToResidentialLinkId,
                ResidentialTownFloorIds.FloorId,
                MarketResidentialTransition.MarketWestEdgeX,
                "Residential");

            int buildingIndex = southStripWidth + westStripHeight;

            SerializedProperty generalStoreWest = portals.GetArrayElementAtIndex(buildingIndex);
            generalStoreWest.FindPropertyRelative("portalLinkId").stringValue = MarketGeneralStoreLayout.EnterWestLinkId;
            generalStoreWest.FindPropertyRelative("targetFloorId").stringValue = MarketGeneralStoreLayout.InteriorFloorId;
            generalStoreWest.FindPropertyRelative("portalCell").vector3IntValue = MarketGeneralStoreLayout.ExteriorWestDoorCell;
            generalStoreWest.FindPropertyRelative("listLabel").stringValue = "Market General Store";

            SerializedProperty generalStoreEast = portals.GetArrayElementAtIndex(buildingIndex + 1);
            generalStoreEast.FindPropertyRelative("portalLinkId").stringValue = MarketGeneralStoreLayout.EnterEastLinkId;
            generalStoreEast.FindPropertyRelative("targetFloorId").stringValue = MarketGeneralStoreLayout.InteriorFloorId;
            generalStoreEast.FindPropertyRelative("portalCell").vector3IntValue = MarketGeneralStoreLayout.ExteriorEastDoorCell;
            generalStoreEast.FindPropertyRelative("listLabel").stringValue = "Market General Store";

            SerializedProperty itemShopEnter = portals.GetArrayElementAtIndex(buildingIndex + 2);
            itemShopEnter.FindPropertyRelative("portalLinkId").stringValue = MarketItemShopLayout.EnterLinkId;
            itemShopEnter.FindPropertyRelative("targetFloorId").stringValue = MarketItemShopLayout.InteriorFloorId;
            itemShopEnter.FindPropertyRelative("portalCell").vector3IntValue = MarketItemShopLayout.ExteriorDoorCell;
            itemShopEnter.FindPropertyRelative("listLabel").stringValue = "Market Item Shop";

            SerializedProperty blacksmithEnter = portals.GetArrayElementAtIndex(buildingIndex + 3);
            blacksmithEnter.FindPropertyRelative("portalLinkId").stringValue = MarketBlacksmithLayout.EnterLinkId;
            blacksmithEnter.FindPropertyRelative("targetFloorId").stringValue = MarketBlacksmithLayout.InteriorFloorId;
            blacksmithEnter.FindPropertyRelative("portalCell").vector3IntValue = MarketBlacksmithLayout.ExteriorDoorCell;
            blacksmithEnter.FindPropertyRelative("listLabel").stringValue = "Market Blacksmith";

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 6;

            SerializedProperty squareArrival = arrivals.GetArrayElementAtIndex(0);
            squareArrival.FindPropertyRelative("portalLinkId").stringValue =
                DistrictSquareMarketTransition.SquareToMarketLinkId;
            squareArrival.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                DistrictSquareMarketTransition.MarketArrivalCell;

            SerializedProperty generalStoreWestExit = arrivals.GetArrayElementAtIndex(1);
            generalStoreWestExit.FindPropertyRelative("portalLinkId").stringValue =
                MarketGeneralStoreLayout.ExitWestLinkId;
            generalStoreWestExit.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                MarketGeneralStoreLayout.ExteriorWestDoorCell;

            SerializedProperty generalStoreEastExit = arrivals.GetArrayElementAtIndex(2);
            generalStoreEastExit.FindPropertyRelative("portalLinkId").stringValue =
                MarketGeneralStoreLayout.ExitEastLinkId;
            generalStoreEastExit.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                MarketGeneralStoreLayout.ExteriorEastDoorCell;

            SerializedProperty itemShopExit = arrivals.GetArrayElementAtIndex(3);
            itemShopExit.FindPropertyRelative("portalLinkId").stringValue = MarketItemShopLayout.ExitLinkId;
            itemShopExit.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                MarketItemShopLayout.ExteriorDoorCell;

            SerializedProperty blacksmithExit = arrivals.GetArrayElementAtIndex(4);
            blacksmithExit.FindPropertyRelative("portalLinkId").stringValue = MarketBlacksmithLayout.ExitLinkId;
            blacksmithExit.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                MarketBlacksmithLayout.ExteriorDoorCell;

            SerializedProperty residentialArrival = arrivals.GetArrayElementAtIndex(5);
            residentialArrival.FindPropertyRelative("portalLinkId").stringValue =
                MarketResidentialTransition.ResidentialToMarketLinkId;
            residentialArrival.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                MarketResidentialTransition.MarketArrivalCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
        }

        static void WriteSouthStripPortals(
            SerializedProperty portals,
            int startIndex,
            string linkId,
            string targetFloorId,
            int y,
            string label)
        {
            int stripWidth = DistrictSquareMarketTransition.StripMaxX - DistrictSquareMarketTransition.StripMinX + 1;
            for (int i = 0; i < stripWidth; i++)
            {
                int x = DistrictSquareMarketTransition.StripMinX + i;
                SerializedProperty portal = portals.GetArrayElementAtIndex(startIndex + i);
                portal.FindPropertyRelative("portalLinkId").stringValue = linkId;
                portal.FindPropertyRelative("targetFloorId").stringValue = targetFloorId;
                portal.FindPropertyRelative("portalCell").vector3IntValue = new Vector3Int(x, y, 0);
                portal.FindPropertyRelative("listLabel").stringValue = label;
            }
        }

        static void WriteWestStripPortals(
            SerializedProperty portals,
            int startIndex,
            string linkId,
            string targetFloorId,
            int x,
            string label)
        {
            for (int i = 0; i <= MarketResidentialTransition.StripMaxY - MarketResidentialTransition.StripMinY; i++)
            {
                int y = MarketResidentialTransition.StripMinY + i;
                SerializedProperty portal = portals.GetArrayElementAtIndex(startIndex + i);
                portal.FindPropertyRelative("portalLinkId").stringValue = linkId;
                portal.FindPropertyRelative("targetFloorId").stringValue = targetFloorId;
                portal.FindPropertyRelative("portalCell").vector3IntValue = new Vector3Int(x, y, 0);
                portal.FindPropertyRelative("listLabel").stringValue = label;
            }
        }
    }
}
#endif
