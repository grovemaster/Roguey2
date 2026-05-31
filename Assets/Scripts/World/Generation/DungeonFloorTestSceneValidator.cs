using JRogue.Input;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.UI.Targeting;
using JRogue.UI.Inventory;
using JRogue.World.Lighting;
using JRogue.World.MapInteract;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Validates that <c>DungeonFloorTest</c> has the GameObjects/components required to play,
    /// and logs explicit <see cref="DungeonGenerationLog"/> entries for anything missing.
    /// </summary>
    public sealed class DungeonFloorTestSceneValidator
    {
        public const string SystemsObjectName = "DungeonTestSystems";
        public const string InputObjectName = "InputSystem";
        public const string PartyObjectName = "Party";

        const string GameControlsAssetPath = "Assets/Controls/GameControls.inputactions";

        public bool AllCriticalPresent { get; private set; }

        public bool ValidateScene(bool tryRepairRuntime)
        {
            DungeonGenerationLog.Info("--- DungeonFloorTest scene validation ---");

            GameObject systems = GameObject.Find(SystemsObjectName);
            GameObject inputRoot = GameObject.Find(InputObjectName);
            GameObject party = GameObject.Find(PartyObjectName);

            DungeonGenerationLog.SceneObject(SystemsObjectName, systems != null,
                systems != null ? "hosts Map/Grid/Turn/Floor manager" : "run JRogue → Dungeon → Fix DungeonFloorTest Scene");
            DungeonGenerationLog.SceneObject(PartyObjectName, party != null,
                "spawn container + DungeonRunBootstrap");

            InputHandler inputHandler = FindAny<InputHandler>();
            PlayerInput playerInput = FindAny<PlayerInput>();
            TargetingReticleView reticle = FindAny<TargetingReticleView>();
            PartyManager partyManager = FindAny<PartyManager>();

            bool hasDedicatedInputRoot = inputRoot != null;
            bool hasInputHandler = inputHandler != null;
            bool hasPlayerInput = playerInput != null;
            bool hasReticle = reticle != null;
            bool hasPartyManager = partyManager != null;

            if (hasDedicatedInputRoot)
            {
                DungeonGenerationLog.SceneObject(InputObjectName, true, "dedicated input root");
            }
            else if (hasInputHandler || hasPlayerInput || hasPartyManager)
            {
                string host = partyManager != null ? partyManager.gameObject.name : "unknown";
                DungeonGenerationLog.Warn(
                    $"[Scene] No '{InputObjectName}' object — input/party on '{host}' (legacy). " +
                    "Run JRogue → Dungeon → Fix DungeonFloorTest Scene to migrate.");
            }
            else
            {
                DungeonGenerationLog.SceneObject(InputObjectName, false,
                    "needs InputHandler + PlayerInput + TargetingReticleView");
            }

            string inputHostName = inputHandler != null ? inputHandler.gameObject.name : InputObjectName;
            DungeonGenerationLog.SceneComponent<InputHandler>(inputHostName, hasInputHandler);
            DungeonGenerationLog.SceneComponent<PlayerInput>(
                playerInput != null ? playerInput.gameObject.name : inputHostName,
                hasPlayerInput);
            DungeonGenerationLog.SceneComponent<TargetingReticleView>(
                reticle != null ? reticle.gameObject.name : inputHostName,
                hasReticle);
            DungeonGenerationLog.SceneComponent<PartyManager>(
                partyManager != null ? partyManager.gameObject.name : "scene",
                hasPartyManager);

            bool hasMap = HasComponent<MapManager>(systems);
            bool hasGrid = HasComponent<GridManager>(systems);
            bool hasTurn = HasComponent<TurnManager>(systems);
            bool hasInteract = HasComponent<AdjacentMapInteractableService>(systems);
            bool hasFloorMgr = HasComponent<DungeonFloorInstanceManager>(systems);
            bool hasTest = HasComponent<DungeonFloorTestController>(systems);

            DungeonGenerationLog.SceneComponent<MapManager>(SystemsObjectName, hasMap);
            DungeonGenerationLog.SceneComponent<GridManager>(SystemsObjectName, hasGrid);
            DungeonGenerationLog.SceneComponent<TurnManager>(SystemsObjectName, hasTurn);
            DungeonGenerationLog.SceneComponent<AdjacentMapInteractableService>(SystemsObjectName, hasInteract);
            DungeonGenerationLog.SceneComponent<DungeonFloorInstanceManager>(SystemsObjectName, hasFloorMgr);
            DungeonGenerationLog.SceneComponent<DungeonFloorTestController>(SystemsObjectName, hasTest);

            bool hasCamera = Camera.main != null;
            DungeonGenerationLog.SceneComponent<Camera>("Main Camera", hasCamera);

            bool hasBootstrap = HasComponent<DungeonRunBootstrap>(party);
            DungeonGenerationLog.SceneComponent<DungeonRunBootstrap>(PartyObjectName, hasBootstrap);

            VisibilityManager visibility = Object.FindAnyObjectByType<VisibilityManager>();
            if (visibility == null)
                DungeonGenerationLog.Warn("[Scene] OPTIONAL MISSING VisibilityManager — fog of war disabled");
            else
                DungeonGenerationLog.Info("[Scene] OK  VisibilityManager (optional)");

            bool hasCanvas = GameObject.Find("Canvas") != null;
            bool hasEventSystem = Object.FindAnyObjectByType<EventSystem>() != null;
            bool hasInventoryUi = Object.FindAnyObjectByType<InventoryUI>() != null;
            DungeonGenerationLog.SceneObject("Canvas", hasCanvas,
                hasCanvas ? "gameplay UI root" : "run JRogue → Dungeon → Fix DungeonFloorTest Scene");
            DungeonGenerationLog.SceneComponent<EventSystem>("scene", hasEventSystem);
            if (hasCanvas && !hasInventoryUi)
                DungeonGenerationLog.Warn("[Scene] Canvas present but InventoryUI missing — UI may be incomplete.");

            LightingService lighting = Object.FindAnyObjectByType<LightingService>();
            if (lighting == null)
                DungeonGenerationLog.Warn("[Scene] OPTIONAL MISSING LightingService — floor tiles stay dark");
            else
                DungeonGenerationLog.Info("[Scene] OK  LightingService (optional)");

            AllCriticalPresent = systems != null && party != null &&
                                 hasMap && hasGrid && hasTurn && hasInteract && hasFloorMgr && hasTest &&
                                 hasInputHandler && hasPlayerInput && hasReticle && hasPartyManager &&
                                 hasCamera && hasBootstrap;

            if (!AllCriticalPresent && tryRepairRuntime)
            {
                DungeonGenerationLog.Warn("Attempting runtime repair of missing scene objects.");
                TryRepairRuntime(systems, party, partyManager);
                return ValidateScene(tryRepairRuntime: false);
            }

            if (AllCriticalPresent)
                DungeonGenerationLog.Info("Scene validation PASSED — all critical objects present.");
            else
                DungeonGenerationLog.Error(
                    "Scene validation FAILED — use JRogue → Dungeon → Fix DungeonFloorTest Scene");

            return AllCriticalPresent;
        }

        static void TryRepairRuntime(GameObject systems, GameObject party, PartyManager existingPartyManager)
        {
            if (systems == null)
            {
                systems = new GameObject(SystemsObjectName);
                systems.AddComponent<GridManager>();
                systems.AddComponent<MapManager>();
                TurnManager turn = systems.AddComponent<TurnManager>();
                turn.currentState = GameState.PLAYER_TURN;
                systems.AddComponent<AdjacentMapInteractableService>();
                systems.AddComponent<VisibilityManager>();
                systems.AddComponent<LightingService>();
                systems.AddComponent<LightingBootstrap>();
                systems.AddComponent<DungeonFloorInstanceManager>();
                systems.AddComponent<DungeonFloorTestController>();
                systems.AddComponent<PortalEntryService>();
                DungeonWorldFeatureServices.EnsureOn(systems);
            }
            else
            {
                if (systems.GetComponent<VisibilityManager>() == null)
                    systems.AddComponent<VisibilityManager>();
                if (systems.GetComponent<LightingService>() == null)
                    systems.AddComponent<LightingService>();
                if (systems.GetComponent<LightingBootstrap>() == null)
                    systems.AddComponent<LightingBootstrap>();
                if (systems.GetComponent<PortalEntryService>() == null)
                    systems.AddComponent<PortalEntryService>();
                DungeonWorldFeatureServices.EnsureOn(systems);
            }

            if (party == null)
            {
                party = new GameObject(PartyObjectName);
                party.AddComponent<DungeonRunBootstrap>();
            }
            else if (party.GetComponent<DungeonRunBootstrap>() == null)
            {
                party.AddComponent<DungeonRunBootstrap>();
            }

            GameObject inputHost = ResolveInputHost(party, existingPartyManager);
            EnsureInputComponents(inputHost);
        }

        /// <summary>
        /// Prefer dedicated InputSystem; otherwise add input components to Party (legacy layout).
        /// </summary>
        static GameObject ResolveInputHost(GameObject party, PartyManager existingPartyManager)
        {
            GameObject dedicated = GameObject.Find(InputObjectName);
            if (dedicated != null)
                return dedicated;

            if (existingPartyManager != null)
                return existingPartyManager.gameObject;

            return party != null ? party : new GameObject(InputObjectName);
        }

        static void EnsureInputComponents(GameObject host)
        {
            if (host == null)
                return;

            if (host.GetComponent<TargetingReticleView>() == null)
                host.AddComponent<TargetingReticleView>();

            if (host.GetComponent<InputHandler>() == null)
                host.AddComponent<InputHandler>();

            PlayerInput playerInput = host.GetComponent<PlayerInput>();
            if (playerInput == null)
                playerInput = host.AddComponent<PlayerInput>();

            WirePlayerInputActions(playerInput);

            if (host.GetComponent<PartyManager>() == null && host.name == PartyObjectName)
            {
                // Legacy: PartyManager already on party from scene — GetComponent above would find it
            }

            if (FindAny<PartyManager>() == null)
                host.AddComponent<PartyManager>();

            DungeonGenerationLog.Info($"Input components ensured on '{host.name}'.");
        }

        static void WirePlayerInputActions(PlayerInput playerInput)
        {
            if (playerInput == null || playerInput.actions != null)
                return;

#if UNITY_EDITOR
            InputActionAsset actions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(GameControlsAssetPath);
            if (actions != null)
            {
                playerInput.actions = actions;
                playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
                DungeonGenerationLog.Info("PlayerInput wired to GameControls (editor).");
                return;
            }
#endif
            DungeonGenerationLog.Warn(
                "PlayerInput has no actions asset — assign Assets/Controls/GameControls.inputactions in the inspector.");
        }

        static bool HasComponent<T>(GameObject go) where T : Component =>
            go != null && go.GetComponent<T>() != null;

        static T FindAny<T>() where T : Component => Object.FindAnyObjectByType<T>();
    }
}
