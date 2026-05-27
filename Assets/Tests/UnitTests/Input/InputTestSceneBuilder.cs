using System;
using System.Collections.Generic;
using System.Reflection;
using JRogue.Actors;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Hazards;
using JRogue.Manager.Turn;
using JRogue.Tests.UnitTests.MockMonoBehavior;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Tests.UnitTests.Input
{
    /// <summary>
    /// Shared map/manager/party construction for input-related unit tests.
    /// </summary>
    public static class InputTestSceneBuilder
    {
        public interface IActorSeed
        {
            Vector3Int GridPosition { get; }
        }

        public static void SetupMapAndManagers(ICollection<GameObject> createdObjects)
        {
            GameObject mapManagerObject = new GameObject("MapManager_Test");
            createdObjects.Add(mapManagerObject);
            MapManager mapManager = mapManagerObject.AddComponent<MapManager>();

            GameObject gridRoot = new GameObject("GridRoot_Test");
            createdObjects.Add(gridRoot);
            gridRoot.AddComponent<Grid>();

            GameObject floorObject = new GameObject("FloorTilemap_Test");
            createdObjects.Add(floorObject);
            floorObject.transform.SetParent(gridRoot.transform);
            Tilemap floorMap = floorObject.AddComponent<Tilemap>();
            floorObject.AddComponent<TilemapRenderer>();

            GameObject wallObject = new GameObject("WallTilemap_Test");
            createdObjects.Add(wallObject);
            wallObject.transform.SetParent(gridRoot.transform);
            Tilemap wallMap = wallObject.AddComponent<Tilemap>();
            wallObject.AddComponent<TilemapRenderer>();

            PopulateWalkableFloor(floorMap, radius: 60);

            SetPrivateField(mapManager, "floorMap", floorMap);
            SetPrivateField(mapManager, "wallMap", wallMap);

            GameObject gridManagerObject = new GameObject("GridManager_Test");
            createdObjects.Add(gridManagerObject);
            gridManagerObject.AddComponent<GridManager>();

            GameObject turnManagerObject = new GameObject("TurnManager_Test");
            createdObjects.Add(turnManagerObject);
            TurnManager turnManager = turnManagerObject.AddComponent<TurnManager>();
            turnManager.currentState = GameState.PLAYER_TURN;

            GameObject hazardServiceObject = new GameObject("HazardService_Test");
            createdObjects.Add(hazardServiceObject);
            hazardServiceObject.AddComponent<HazardService>();

            Assert.IsNotNull(mapManager);
            Assert.IsNotNull(GridManager.Instance);
            Assert.IsNotNull(TurnManager.Instance);
        }

        public static PartyManager CreatePartyWithTestActors(int count, ICollection<GameObject> createdObjects)
        {
            GameObject managerObject = new GameObject("PartyManager_Test");
            createdObjects.Add(managerObject);
            PartyManager partyManager = managerObject.AddComponent<PartyManager>();
            partyManager.partyMembers = new List<BaseActor>();
            List<IActorSeed> actorSeeds = CreateActorSeeds(count);

            for (int i = 0; i < count; i++)
            {
                GameObject actorObject = new GameObject($"InputPartyActor_{i}");
                createdObjects.Add(actorObject);

                actorObject.AddComponent<TestQuietEssenceSlotManager>();

                TestPartyActor actor = actorObject.AddComponent<TestPartyActor>();

                actor.SetGridPosition(actorSeeds[i].GridPosition);
                InitializeActorRuntimeDependencies(actor);
                partyManager.partyMembers.Add(actor);
            }

            Assert.AreEqual(count, partyManager.partyMembers.Count);
            return partyManager;
        }

        /// <summary>
        /// Call after destroying test-owned <see cref="GameObject"/>s. Clears static singleton slots so the next
        /// fixture does not create a second <see cref="GridManager"/> that is immediately destroyed in Awake.
        /// </summary>
        public static void ResetSingletonManagersForTests()
        {
            PartyManager.Instance = null;
            TurnManager.Instance = null;
            ClearPrivateStaticInstanceProperty(typeof(GridManager));
            ClearPrivateStaticInstanceProperty(typeof(MapManager));
            ClearPrivateStaticInstanceProperty(typeof(HazardService));
        }

        private static void ClearPrivateStaticInstanceProperty(Type managerType)
        {
            PropertyInfo instanceProp = managerType.GetProperty(
                "Instance",
                BindingFlags.Public | BindingFlags.Static);
            if (instanceProp != null)
            {
                MethodInfo setter = instanceProp.GetSetMethod(nonPublic: true);
                if (setter != null)
                {
                    setter.Invoke(null, new object[] { null });
                    return;
                }
            }

            // Auto-property private setter is not always visible to reflection; clear compiler backing field.
            FieldInfo backing = managerType.GetField(
                "<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            backing?.SetValue(null, null);
        }

        /// <summary>One history entry per party member at current grid positions.</summary>
        public static List<Vector3Int> BuildTrailHistory(List<BaseActor> members)
        {
            var history = new List<Vector3Int>(members.Count);
            for (int i = 0; i < members.Count; i++)
                history.Add(members[i].GridPosition);

            return history;
        }

        public static void RegisterCurrentPartyOnGrid(List<BaseActor> members)
        {
            foreach (BaseActor member in members)
            {
                GridManager.Instance.RegisterActor(member.GridPosition, member);
            }
        }

        private static List<IActorSeed> CreateActorSeeds(int count)
        {
            var seeds = new List<IActorSeed>(count);
            for (int i = 0; i < count; i++)
            {
                IActorSeed seed = Substitute.For<IActorSeed>();
                seed.GridPosition.Returns(new Vector3Int(0, -i, 0));
                seeds.Add(seed);
            }

            return seeds;
        }

        private static void InitializeActorRuntimeDependencies(BaseActor actor)
        {
            FieldInfo mapManagerField = typeof(BaseActor).GetField("mapManager", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mapManagerField, "Expected protected field 'mapManager' to exist on BaseActor.");
            mapManagerField.SetValue(actor, MapManager.Instance);
        }

        private static void PopulateWalkableFloor(Tilemap floorMap, int radius)
        {
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    floorMap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        public static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }

        public sealed class TestPartyActor : BaseActor
        {
            protected override void Die()
            {
            }
        }
    }
}
