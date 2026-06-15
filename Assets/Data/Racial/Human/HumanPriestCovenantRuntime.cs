using System;
using System.Collections.Generic;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    [Serializable]
    public sealed class DivineConductLogEntry
    {
        public string conductId;
        public int pietyDelta;
        public string message;
    }

    [Serializable]
    public sealed class PriestActiveVowState
    {
        public string vowId;
        public bool failed;
        public bool completed;
    }

    /// <summary>Patron, piety, penance, seals, and vow state for a committed Human Priest.</summary>
    [DefaultExecutionOrder(52)]
    public sealed class HumanPriestCovenantRuntime : MonoBehaviour
    {
        [SerializeField] string patronGodId;
        [SerializeField] int piety;
        [SerializeField] int penanceDebt;
        [SerializeField] List<string> earnedSealIds = new();
        [SerializeField] List<PriestActiveVowState> activeVows = new();
        [SerializeField] List<DivineConductLogEntry> recentConduct = new();

        CharacterStats _stats;

        public string PatronGodId => patronGodId;
        public int Piety => piety;
        public int PenanceDebt => penanceDebt;
        public IReadOnlyList<string> EarnedSealIds => earnedSealIds;
        public IReadOnlyList<PriestActiveVowState> ActiveVows => activeVows;
        public IReadOnlyList<DivineConductLogEntry> RecentConduct => recentConduct;

        void Awake() => _stats = GetComponent<CharacterStats>();

        public bool IsCommittedPriest =>
            _stats != null
            && _stats.race == Race.Human
            && _stats.humanClass == HumanClass.Priest
            && !string.IsNullOrWhiteSpace(patronGodId);

        public void InitializeOnCommit(string godId, int startingPiety)
        {
            patronGodId = godId?.Trim() ?? string.Empty;
            piety = Mathf.Max(0, startingPiety);
            penanceDebt = 0;
            earnedSealIds ??= new List<string>();
            earnedSealIds.Clear();
            activeVows ??= new List<PriestActiveVowState>();
            activeVows.Clear();
            recentConduct ??= new List<DivineConductLogEntry>();
            recentConduct.Clear();
        }

        public int AddPiety(int amount, string conductId, string message)
        {
            if (amount <= 0)
                return piety;

            if (penanceDebt > 0)
            {
                int paid = Mathf.Min(penanceDebt, amount);
                penanceDebt -= paid;
                amount -= paid;
                if (amount <= 0)
                {
                    LogConduct(conductId, 0, message + " (penance)");
                    return piety;
                }
            }

            int max = PriestPietyLogic.ResolveMaxPiety();
            piety = Mathf.Min(max, piety + amount);
            LogConduct(conductId, amount, message);
            PriestPietyLogic.ApplyBandPassives(gameObject, this);
            return piety;
        }

        public void ApplyPietyLoss(int amount, string conductId, string message)
        {
            if (amount <= 0)
                return;

            piety = Mathf.Max(0, piety - amount);
            LogConduct(conductId, -amount, message);
            PriestPietyLogic.ApplyBandPassives(gameObject, this);
        }

        public void AddPenance(int debt, string message)
        {
            if (debt <= 0)
                return;

            penanceDebt += debt;
            LogConduct("penance", 0, message);
        }

        public void ClearPenance() => penanceDebt = 0;

        public bool HasSeal(string sealId) =>
            !string.IsNullOrWhiteSpace(sealId)
            && earnedSealIds != null
            && earnedSealIds.Contains(sealId.Trim());

        public void GrantSeal(string sealId)
        {
            if (string.IsNullOrWhiteSpace(sealId))
                return;

            earnedSealIds ??= new List<string>();
            string trimmed = sealId.Trim();
            if (!earnedSealIds.Contains(trimmed))
                earnedSealIds.Add(trimmed);
        }

        public void SetActiveVows(IReadOnlyList<string> vowIds)
        {
            activeVows ??= new List<PriestActiveVowState>();
            activeVows.Clear();
            if (vowIds == null)
                return;

            for (int i = 0; i < vowIds.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(vowIds[i]))
                    continue;

                activeVows.Add(new PriestActiveVowState { vowId = vowIds[i].Trim() });
            }
        }

        public bool TryGetActiveVow(string vowId, out PriestActiveVowState state)
        {
            state = null;
            if (string.IsNullOrWhiteSpace(vowId) || activeVows == null)
                return false;

            string trimmed = vowId.Trim();
            for (int i = 0; i < activeVows.Count; i++)
            {
                PriestActiveVowState entry = activeVows[i];
                if (entry != null && entry.vowId == trimmed)
                {
                    state = entry;
                    return true;
                }
            }

            return false;
        }

        public void ClearActiveVows()
        {
            activeVows?.Clear();
        }

        void LogConduct(string conductId, int delta, string message)
        {
            recentConduct ??= new List<DivineConductLogEntry>();
            recentConduct.Insert(0, new DivineConductLogEntry
            {
                conductId = conductId ?? string.Empty,
                pietyDelta = delta,
                message = message ?? string.Empty,
            });

            const int maxEntries = 5;
            while (recentConduct.Count > maxEntries)
                recentConduct.RemoveAt(recentConduct.Count - 1);

            if (!string.IsNullOrWhiteSpace(message))
                Debug.Log($"[Piety] {message} ({delta:+#;-#;0})");
        }
    }
}
