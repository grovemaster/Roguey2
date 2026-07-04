using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.World.MapInteract;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Validates the production <c>DungeonFloor</c> scene shell (Phase 1 / Phase 6 acceptance).
    /// </summary>
    public sealed class DungeonProductionSceneValidator
    {
        public bool AllCriticalPresent { get; private set; }

        public bool ValidateScene()
        {
            DungeonGenerationLog.Info("--- DungeonFloor production scene validation ---");

            GameObject systems = GameObject.Find(DungeonFloorTestSceneValidator.SystemsObjectName);
            GameObject party = GameObject.Find(DungeonFloorTestSceneValidator.PartyObjectName);

            DungeonGenerationLog.SceneObject(
                DungeonFloorTestSceneValidator.SystemsObjectName,
                systems != null,
                systems != null
                    ? "hosts Map/Grid/Turn/Floor manager"
                    : "run JRogue → Dungeon → Phase 1 — Setup Production Dungeon");

            bool hasMap = HasComponent<MapManager>(systems);
            bool hasGrid = HasComponent<GridManager>(systems);
            bool hasTurn = HasComponent<TurnManager>(systems);
            bool hasInteract = HasComponent<AdjacentMapInteractableService>(systems);
            bool hasFloorMgr = HasComponent<DungeonFloorInstanceManager>(systems);
            bool hasRuntime = HasComponent<DungeonFloorRuntime>(systems);
            bool hasTestController = HasComponent<DungeonFloorTestController>(systems);

            DungeonGenerationLog.SceneComponent<MapManager>(DungeonFloorTestSceneValidator.SystemsObjectName, hasMap);
            DungeonGenerationLog.SceneComponent<GridManager>(DungeonFloorTestSceneValidator.SystemsObjectName, hasGrid);
            DungeonGenerationLog.SceneComponent<TurnManager>(DungeonFloorTestSceneValidator.SystemsObjectName, hasTurn);
            DungeonGenerationLog.SceneComponent<AdjacentMapInteractableService>(
                DungeonFloorTestSceneValidator.SystemsObjectName,
                hasInteract);
            DungeonGenerationLog.SceneComponent<DungeonFloorInstanceManager>(
                DungeonFloorTestSceneValidator.SystemsObjectName,
                hasFloorMgr);
            DungeonGenerationLog.SceneComponent<DungeonFloorRuntime>(
                DungeonFloorTestSceneValidator.SystemsObjectName,
                hasRuntime);

            if (hasTestController)
            {
                DungeonGenerationLog.Error(
                    "[Scene] DungeonFloorTestController must not be on the production scene — " +
                    "run JRogue → Dungeon → Phase 6 — Validate Production QA.");
            }

            bool hasBootstrap = HasComponent<DungeonRunBootstrap>(party);
            DungeonGenerationLog.SceneComponent<DungeonRunBootstrap>(
                DungeonFloorTestSceneValidator.PartyObjectName,
                hasBootstrap);

            bool hasCamera = Camera.main != null;
            DungeonGenerationLog.SceneComponent<Camera>("Main Camera", hasCamera);

            AllCriticalPresent = systems != null && party != null &&
                                 hasMap && hasGrid && hasTurn && hasInteract && hasFloorMgr &&
                                 hasRuntime && !hasTestController && hasCamera && hasBootstrap;

            if (AllCriticalPresent)
                DungeonGenerationLog.Info("Production scene validation PASSED.");
            else
                DungeonGenerationLog.Error(
                    "Production scene validation FAILED — run JRogue → Dungeon → Phase 6 — Validate Production QA.");

            return AllCriticalPresent;
        }

        static bool HasComponent<T>(GameObject go) where T : Component =>
            go != null && go.GetComponent<T>() != null;
    }
}
