#if UNITY_EDITOR
using JRogue.World.Generation;
using JRogue.World.Town;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    /// <summary>East market strip + residential inn building portals on town_residential.</summary>
    public static class ResidentialDistrictPortalsEditor
    {
        public static void EnsureResidentialDistrictPortals()
        {
            var def = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.ResidentialFloorDef);
            if (def == null)
                return;

            int eastStripHeight = MarketResidentialTransition.StripMaxY - MarketResidentialTransition.StripMinY + 1;
            int innDoorCount = 3;
            int portalCount = eastStripHeight + innDoorCount;

            var so = new SerializedObject(def);
            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = portalCount;

            WriteEastStripPortals(
                portals,
                0,
                MarketResidentialTransition.ResidentialToMarketLinkId,
                MarketTownFloorIds.FloorId,
                MarketResidentialTransition.ResidentialEastEdgeX,
                "Market");

            int innIndex = eastStripHeight;

            WriteInnEnterPortal(portals, innIndex, ResidentialInnLayout.EnterWestLinkId, ResidentialInnLayout.ExteriorWestDoorCell);
            WriteInnEnterPortal(portals, innIndex + 1, ResidentialInnLayout.EnterCenterLinkId, ResidentialInnLayout.ExteriorCenterDoorCell);
            WriteInnEnterPortal(portals, innIndex + 2, ResidentialInnLayout.EnterEastLinkId, ResidentialInnLayout.ExteriorEastDoorCell);

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 4;

            SerializedProperty marketArrival = arrivals.GetArrayElementAtIndex(0);
            marketArrival.FindPropertyRelative("portalLinkId").stringValue =
                MarketResidentialTransition.MarketToResidentialLinkId;
            marketArrival.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                MarketResidentialTransition.ResidentialArrivalCell;

            WriteInnExitArrival(arrivals, 1, ResidentialInnLayout.ExitWestLinkId, ResidentialInnLayout.ExteriorWestDoorCell);
            WriteInnExitArrival(arrivals, 2, ResidentialInnLayout.ExitCenterLinkId, ResidentialInnLayout.ExteriorCenterDoorCell);
            WriteInnExitArrival(arrivals, 3, ResidentialInnLayout.ExitEastLinkId, ResidentialInnLayout.ExteriorEastDoorCell);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
        }

        static void WriteInnEnterPortal(SerializedProperty portals, int index, string linkId, Vector3Int cell)
        {
            SerializedProperty portal = portals.GetArrayElementAtIndex(index);
            portal.FindPropertyRelative("portalLinkId").stringValue = linkId;
            portal.FindPropertyRelative("targetFloorId").stringValue = ResidentialInnLayout.InteriorFloorId;
            portal.FindPropertyRelative("portalCell").vector3IntValue = cell;
            portal.FindPropertyRelative("listLabel").stringValue = "Residential Inn";
        }

        static void WriteInnExitArrival(SerializedProperty arrivals, int index, string linkId, Vector3Int cell)
        {
            SerializedProperty arrival = arrivals.GetArrayElementAtIndex(index);
            arrival.FindPropertyRelative("portalLinkId").stringValue = linkId;
            arrival.FindPropertyRelative("arrivalAnchor").vector3IntValue = cell;
        }

        static void WriteEastStripPortals(
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
