using System.Collections.Generic;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    [DefaultExecutionOrder(51)]
    public sealed class BeastmanSoulBeastRuntime : MonoBehaviour
    {
        [SerializeField] string soulBeastId;
        [Min(0)] [SerializeField] int soulBeastLevel;
        [SerializeField] bool requireBeastmanSoulBeastSubsystem = true;

        readonly Dictionary<int, SoulBeastModifierSource> _sourcesByLevel =
            new Dictionary<int, SoulBeastModifierSource>();

        CharacterStats _stats;

        public string SoulBeastId => soulBeastId;
        public int SoulBeastLevel => soulBeastLevel;
        public bool IsBonded => !string.IsNullOrEmpty(soulBeastId);

        void Awake() => _stats = GetComponent<CharacterStats>();

        void Start() => ReapplyPayloads();

        void OnDestroy() => ClearAllPayloads();

        public bool TryFormContract(SoulBeastDefinition beast, out string failureReason)
        {
            failureReason = null;

            if (!ValidateBeastmanActor(out failureReason))
                return false;

            if (IsBonded)
            {
                failureReason = "Already bound to a Soul Beast.";
                return false;
            }

            if (beast == null || string.IsNullOrEmpty(beast.soulBeastId))
            {
                failureReason = "Invalid Soul Beast.";
                return false;
            }

            soulBeastId = beast.soulBeastId;
            soulBeastLevel = 1;
            ReapplyPayloads();
            return true;
        }

        public bool TryIncrementLevel(out string failureReason)
        {
            failureReason = null;

            if (!ValidateBeastmanActor(out failureReason))
                return false;

            if (!IsBonded)
            {
                failureReason = "No Soul Beast contract.";
                return false;
            }

            if (!TryResolveBondedDefinition(out SoulBeastDefinition beast))
            {
                failureReason = "Unknown Soul Beast.";
                return false;
            }

            int cap = SoulBeastProgressionLogic.GetEffectiveLevelCap(_stats, beast);
            if (soulBeastLevel >= cap)
            {
                failureReason = $"Soul Beast level cannot exceed {cap}.";
                return false;
            }

            soulBeastLevel = Mathf.Min(soulBeastLevel + 1, beast.maxLevel);
            ReapplyPayloads();
            return true;
        }

        public bool TryResolveBondedDefinition(out SoulBeastDefinition beast) =>
            SoulBeastRegistryService.TryGetDefinition(soulBeastId, out beast);

        public void ReapplyPayloads()
        {
            ClearAllPayloads();

            if (!IsBonded || _stats == null)
                return;

            if (!TryResolveBondedDefinition(out SoulBeastDefinition beast))
            {
                Debug.LogWarning($"[SoulBeast] Unknown soulBeastId '{soulBeastId}' on {name}.");
                return;
            }

            int level = Mathf.Clamp(soulBeastLevel, 1, beast.maxLevel);
            for (int rowLevel = 1; rowLevel <= level; rowLevel++)
            {
                if (!beast.TryGetLevelRow(rowLevel, out SoulBeastLevelData row))
                    continue;

                if (!_sourcesByLevel.TryGetValue(rowLevel, out SoulBeastModifierSource src))
                {
                    src = new SoulBeastModifierSource(soulBeastId, rowLevel);
                    _sourcesByLevel[rowLevel] = src;
                }

                RacialProgressionPayloadApplicator.Apply(gameObject, _stats, src, row);
            }
        }

        public void RefreshPassives()
        {
            if (!IsBonded || !TryResolveBondedDefinition(out SoulBeastDefinition beast))
                return;

            int level = Mathf.Clamp(soulBeastLevel, 1, beast.maxLevel);
            for (int rowLevel = 1; rowLevel <= level; rowLevel++)
            {
                if (beast.TryGetLevelRow(rowLevel, out SoulBeastLevelData row))
                    RacialProgressionPayloadApplicator.RefreshPassives(gameObject, row);
            }
        }

        public void NotifyPassivesTurnStart()
        {
            if (!IsBonded || !TryResolveBondedDefinition(out SoulBeastDefinition beast))
                return;

            int level = Mathf.Clamp(soulBeastLevel, 1, beast.maxLevel);
            for (int rowLevel = 1; rowLevel <= level; rowLevel++)
            {
                if (beast.TryGetLevelRow(rowLevel, out SoulBeastLevelData row))
                    RacialProgressionPayloadApplicator.NotifyPassivesTurnStart(gameObject, row);
            }
        }

        void ClearAllPayloads()
        {
            if (!TryResolveBondedDefinition(out SoulBeastDefinition beast))
            {
                _sourcesByLevel.Clear();
                return;
            }

            int level = soulBeastLevel > 0 ? Mathf.Clamp(soulBeastLevel, 1, beast.maxLevel) : beast.maxLevel;
            for (int rowLevel = 1; rowLevel <= level; rowLevel++)
            {
                if (!beast.TryGetLevelRow(rowLevel, out SoulBeastLevelData row))
                    continue;

                if (_sourcesByLevel.TryGetValue(rowLevel, out SoulBeastModifierSource src))
                    RacialProgressionPayloadApplicator.Remove(gameObject, _stats, src, row);
            }

            _sourcesByLevel.Clear();
        }

        bool ValidateBeastmanActor(out string failureReason)
        {
            failureReason = null;
            if (_stats == null)
            {
                failureReason = "No CharacterStats.";
                return false;
            }

            if (_stats.race != Race.Beastman)
            {
                failureReason = "Not a Beastman.";
                return false;
            }

            if (requireBeastmanSoulBeastSubsystem
                && _stats.racialSubsystem != RacialSubsystemKind.BeastmanSoulBeast)
            {
                failureReason = "Racial subsystem is not BeastmanSoulBeast.";
                return false;
            }

            return true;
        }
    }
}
