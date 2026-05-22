using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Controller.Enemy;
using JRogue.Data.Enemy;
using JRogue.Data.Progression;
using JRogue.Manager.Party;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Manager.Progression
{
    /// <summary>
    /// Party-wide XP awards, species journal, and per-member level-ups.
    /// </summary>
    public class PartyExperienceService : MonoBehaviour
    {
        public static PartyExperienceService Instance { get; private set; }

        [SerializeField] ExperienceCurve experienceCurve;

        readonly PartySpeciesJournal _journal = new PartySpeciesJournal();
        readonly object _levelGrowthSource = new object();

        PartyManager _party;

        public PartySpeciesJournal Journal => _journal;

        public ExperienceCurve Curve => experienceCurve;

        /// <summary>Called from <see cref="PartyManager"/> after the component is added at runtime.</summary>
        public void Configure(PartyManager party, ExperienceCurve curve)
        {
            if (party != null)
                _party = party;
            if (curve != null)
                experienceCurve = curve;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsurePartyReference();
            EnsureExperienceCurve();
        }

        void EnsurePartyReference()
        {
            if (_party == null)
                _party = GetComponent<PartyManager>();
            if (_party == null)
                _party = PartyManager.Instance;
        }

        bool EnsureExperienceCurve()
        {
            if (experienceCurve != null)
                return true;

            experienceCurve = Resources.Load<ExperienceCurve>("Progression/DefaultExperienceCurve");
            if (experienceCurve == null)
            {
                Debug.LogError(
                    "[XP] ExperienceCurve is missing. Assign DefaultExperienceCurve on PartyManager " +
                    "or place it at Resources/Progression/DefaultExperienceCurve.");
                return false;
            }

            return true;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void HandleEnemyDeath(EnemyController enemy, GameObject killer)
        {
            if (enemy == null)
                return;

            EnemySpeciesDefinition species = enemy.Species;
            if (species == null || string.IsNullOrEmpty(species.speciesId))
                return;

            string id = species.speciesId;
            if (_journal.HasDefeated(id))
            {
                Debug.Log($"[XP] Repeat kill: {species.displayName} (0 XP)");
                return;
            }

            _journal.TryRegisterFirstKill(id);
            int xp = Mathf.Max(0, species.firstKillExperience);
            Debug.Log($"[XP] First kill: {species.displayName} (+{xp} XP to party)");
            AwardPartyExperience(xp, $"FirstKill:{id}");
        }

        public void AwardPartyExperience(int amount, string source)
        {
            if (amount <= 0)
                return;

            if (!EnsureExperienceCurve())
                return;

            EnsurePartyReference();
            PartyManager party = _party != null ? _party : PartyManager.Instance;
            if (party == null)
            {
                Debug.LogWarning($"[XP] Cannot award {amount} XP ({source}): no PartyManager in scene.");
                return;
            }

            if (party.partyMembers == null || party.partyMembers.Count == 0)
            {
                Debug.LogWarning(
                    $"[XP] Cannot award {amount} XP ({source}): PartyManager on '{party.name}' has no partyMembers.");
                return;
            }

            int applied = 0;
            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null)
                {
                    Debug.LogWarning($"[XP] partyMembers[{i}] is null; skipping XP for {source}.");
                    continue;
                }

                CharacterStats stats = member.stats;
                if (stats == null)
                    stats = member.GetComponent<CharacterStats>();
                if (stats == null)
                {
                    Debug.LogWarning($"[XP] {member.name} has no CharacterStats; skipping XP for {source}.");
                    continue;
                }

                ApplyExperienceGain(stats, amount, source);
                applied++;
            }

            if (applied == 0)
                Debug.LogWarning($"[XP] Awarded 0/{party.partyMembers.Count} members for {source} (+{amount} XP).");
        }

        public void ApplyExperienceGain(CharacterStats stats, int amount, string source)
        {
            if (stats == null || amount <= 0 || experienceCurve == null)
                return;

            if (stats.level >= experienceCurve.MaxLevel)
            {
                stats.experience += amount;
                Debug.Log(
                    $"[XP] {stats.gameObject.name} at max level {experienceCurve.MaxLevel}; " +
                    $"banking +{amount} XP (total banked {stats.experience}, source: {source}).");
                return;
            }

            int levelBefore = stats.level;
            int xpBefore = stats.experience;
            stats.experience += amount;
            int xpToNext = experienceCurve.GetXpRequiredForNextLevel(stats.level);
            Debug.Log(
                $"[XP] {stats.gameObject.name} +{amount} XP from {source}. " +
                $"Level {stats.level}, {stats.experience}/{xpToNext} toward next " +
                $"(was {xpBefore}/{xpToNext} at level {levelBefore}).");

            while (stats.level < experienceCurve.MaxLevel)
            {
                int threshold = experienceCurve.GetXpRequiredForNextLevel(stats.level);
                if (stats.experience < threshold)
                    break;

                stats.experience -= threshold;
                ApplyLevelUp(stats);
            }
        }

        void ApplyLevelUp(CharacterStats stats)
        {
            int oldMaxHp = stats.MaxHP;
            int oldMaxSoul = stats.MaxSoulPower;

            stats.level++;
            stats.Constitution.AddModifier(experienceCurve.constitutionPerLevel, _levelGrowthSource);
            stats.levelSoulPowerBonus += experienceCurve.maxSoulPowerPerLevel;

            int hpGain = stats.MaxHP - oldMaxHp;
            int soulGain = stats.MaxSoulPower - oldMaxSoul;
            stats.currentHP += hpGain;
            stats.currentSoulPower += soulGain;

            int xpToNext = stats.level < experienceCurve.MaxLevel
                ? experienceCurve.GetXpRequiredForNextLevel(stats.level)
                : 0;
            Debug.Log(
                $"[XP] {stats.gameObject.name} leveled up to {stats.level}! " +
                $"CON+{experienceCurve.constitutionPerLevel}, MaxHP+{hpGain}, MaxSoul+{soulGain}. " +
                $"{stats.experience}/{xpToNext} XP toward next level.");
        }

        /// <summary>Resolve killer for kill credit; defaults to party leader when unknown.</summary>
        public static GameObject ResolveKillCredit(HealthComponent victimHealth, PartyManager party)
        {
            if (victimHealth != null && victimHealth.LastDamageSource != null)
                return victimHealth.LastDamageSource;

            if (party != null && party.partyMembers != null && party.partyMembers.Count > 0)
            {
                BaseActor leader = party.partyMembers[0];
                if (leader != null)
                {
                    Debug.Log(
                        "[XP] Ambiguous kill credit; defaulting to party leader.");
                    return leader.gameObject;
                }
            }

            return null;
        }
    }
}
