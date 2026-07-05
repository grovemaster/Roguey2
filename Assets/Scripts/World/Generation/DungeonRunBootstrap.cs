using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Manager.Party;
using JRogue.Organizations;
using JRogue.Shop;
using JRogue.World.Town;
using UnityEngine;

namespace JRogue.World.Generation
{
    public sealed class DungeonRunBootstrap : MonoBehaviour
    {
        [SerializeField] bool applyOnAwake = true;
        [SerializeField] GameObject[] partyMemberPrefabs;
        [SerializeField] Transform partyContainer;
        [SerializeField] DungeonFloorInstanceManager floorInstanceManager;

        public Transform PartyContainer => partyContainer;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DevSetPartyMemberPrefabs(GameObject[] prefabs)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RecordObject(this, "Set Party Composition");
#endif
            partyMemberPrefabs = prefabs != null ? (GameObject[])prefabs.Clone() : null;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
#endif

        void Awake()
        {
            if (!applyOnAwake)
                return;

            EnsureDungeonRunObjects();
            // Party is spawned when the dungeon floor is ready (Generate / enter dungeon),
            // not here — instantiating at (0,0,0) causes GridMover registration conflicts.
        }

        public void EnsureDungeonRunObjects()
        {
            if (DungeonRunState.Instance == null)
            {
                var runGo = new GameObject("DungeonRunState");
                runGo.AddComponent<DungeonRunState>();
            }

            if (DungeonTimeService.Instance == null)
                DungeonRunState.Instance.gameObject.AddComponent<DungeonTimeService>();

            TownShopStateService.EnsureRunService();
            InnLodgingService.EnsureRunService();
            TownTimeService.EnsureRunService();
            JRogue.Quest.QuestService.EnsureRunService();

            if (floorInstanceManager == null)
            {
                floorInstanceManager = DungeonFloorInstanceManager.Instance;
                if (floorInstanceManager == null)
                {
                    var managerGo = new GameObject("DungeonFloorInstanceManager");
                    floorInstanceManager = managerGo.AddComponent<DungeonFloorInstanceManager>();
                    if (floorInstanceManager.UseDontDestroyOnLoad)
                        DontDestroyOnLoad(managerGo);
                }
            }
            else if (floorInstanceManager.UseDontDestroyOnLoad)
            {
                DontDestroyOnLoad(floorInstanceManager.gameObject);
            }
        }

        public void EnsurePartyRoster()
        {
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return;

            if (party.partyMembers != null && party.partyMembers.Count > 0)
                return;

            if (RunPartyPersistence.AwaitingTownArrival || RunPartyPersistence.HasLivingParty)
                return;

            if (partyMemberPrefabs == null || partyMemberPrefabs.Length == 0)
            {
#if UNITY_EDITOR
                partyMemberPrefabs = LoadDefaultPartyPrefabsEditor();
#endif
            }

            if (partyMemberPrefabs == null || partyMemberPrefabs.Length == 0)
                return;

            party.partyMembers = new List<BaseActor>();
            Transform parent = partyContainer != null ? partyContainer : party.transform;

            for (int i = 0; i < partyMemberPrefabs.Length; i++)
            {
                GameObject prefab = partyMemberPrefabs[i];
                if (prefab == null)
                    continue;

                GameObject instance = Instantiate(prefab, parent);
                GridMover mover = instance.GetComponent<GridMover>();
                if (mover != null)
                    mover.enabled = false;

                JRogue.View.PlayerRaceWorldSpriteApplier.Apply(instance);

                BaseActor actor = instance.GetComponent<BaseActor>();
                if (actor != null)
                {
                    OrganizationMembershipRuntime.EnsureDefaultGuildMembership(instance);
                    party.partyMembers.Add(actor);
                }
            }
        }

#if UNITY_EDITOR
        static GameObject[] LoadDefaultPartyPrefabsEditor()
        {
            string[] paths =
            {
                "Assets/Prefabs/Actor/Race/BarbarianPlayer.prefab",
                "Assets/Prefabs/Actor/Race/HumanPlayer.prefab",
                "Assets/Prefabs/Actor/Race/ElfPlayer.prefab",
            };

            var loaded = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < paths.Length; i++)
            {
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
                if (prefab != null)
                    loaded.Add(prefab);
            }

            return loaded.Count > 0 ? loaded.ToArray() : null;
        }
#endif
    }
}
