#if UNITY_EDITOR
using JRogue.World.Generation;
using JRogue.World.Town;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    /// <summary>South strip + all market district building portals on town_market.</summary>
    public static class MarketTownBuildingPortalsEditor
    {
        public static void EnsureMarketBuildingPortals()
        {
            var def = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.MarketFloorDef);
            if (def == null)
                return;

            int stripWidth = DistrictSquareMarketTransition.StripMaxX - DistrictSquareMarketTransition.StripMinX + 1;
            int portalCount = stripWidth + 2 + 1;

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

            int generalStoreIndex = stripWidth;
            SerializedProperty westEnter = portals.GetArrayElementAtIndex(generalStoreIndex);
            westEnter.FindPropertyRelative("portalLinkId").stringValue = MarketGeneralStoreLayout.EnterWestLinkId;
            westEnter.FindPropertyRelative("targetFloorId").stringValue = MarketGeneralStoreLayout.InteriorFloorId;
            westEnter.FindPropertyRelative("portalCell").vector3IntValue = MarketGeneralStoreLayout.ExteriorWestDoorCell;
            westEnter.FindPropertyRelative("listLabel").stringValue = "Market General Store";

            SerializedProperty eastEnter = portals.GetArrayElementAtIndex(generalStoreIndex + 1);
            eastEnter.FindPropertyRelative("portalLinkId").stringValue = MarketGeneralStoreLayout.EnterEastLinkId;
            eastEnter.FindPropertyRelative("targetFloorId").stringValue = MarketGeneralStoreLayout.InteriorFloorId;
            eastEnter.FindPropertyRelative("portalCell").vector3IntValue = MarketGeneralStoreLayout.ExteriorEastDoorCell;
            eastEnter.FindPropertyRelative("listLabel").stringValue = "Market General Store";

            SerializedProperty itemShopEnter = portals.GetArrayElementAtIndex(generalStoreIndex + 2);
            itemShopEnter.FindPropertyRelative("portalLinkId").stringValue = MarketItemShopLayout.EnterLinkId;
            itemShopEnter.FindPropertyRelative("targetFloorId").stringValue = MarketItemShopLayout.InteriorFloorId;
            itemShopEnter.FindPropertyRelative("portalCell").vector3IntValue = MarketItemShopLayout.ExteriorDoorCell;
            itemShopEnter.FindPropertyRelative("listLabel").stringValue = "Market Item Shop";

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 4;

            SerializedProperty squareArrival = arrivals.GetArrayElementAtIndex(0);
            squareArrival.FindPropertyRelative("portalLinkId").stringValue =
                DistrictSquareMarketTransition.SquareToMarketLinkId;
            squareArrival.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                DistrictSquareMarketTransition.MarketArrivalCell;

            SerializedProperty westExitArrival = arrivals.GetArrayElementAtIndex(1);
            westExitArrival.FindPropertyRelative("portalLinkId").stringValue = MarketGeneralStoreLayout.ExitWestLinkId;
            westExitArrival.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                MarketGeneralStoreLayout.ExteriorWestDoorCell;

            SerializedProperty eastExitArrival = arrivals.GetArrayElementAtIndex(2);
            eastExitArrival.FindPropertyRelative("portalLinkId").stringValue = MarketGeneralStoreLayout.ExitEastLinkId;
            eastExitArrival.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                MarketGeneralStoreLayout.ExteriorEastDoorCell;

            SerializedProperty itemShopExitArrival = arrivals.GetArrayElementAtIndex(3);
            itemShopExitArrival.FindPropertyRelative("portalLinkId").stringValue = MarketItemShopLayout.ExitLinkId;
            itemShopExitArrival.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                MarketItemShopLayout.ExteriorDoorCell;

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
    }
}
#endif
