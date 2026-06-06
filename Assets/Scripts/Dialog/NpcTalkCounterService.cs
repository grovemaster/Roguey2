using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Dialog
{
    /// <summary>Per-NPC talk visit counts for dialog branching.</summary>
    public sealed class NpcTalkCounterService : MonoBehaviour
    {
        static NpcTalkCounterService _instance;

        readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        public static NpcTalkCounterService Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindAnyObjectByType<NpcTalkCounterService>();
                if (_instance != null)
                    return _instance;

                var go = new GameObject(nameof(NpcTalkCounterService));
                _instance = go.AddComponent<NpcTalkCounterService>();
                DontDestroyOnLoad(go);
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public static void EnsureInstance()
        {
            _ = Instance;
        }

        public int GetCount(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return 0;

            return _counts.TryGetValue(npcId.Trim(), out int count) ? count : 0;
        }

        public void Increment(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return;

            string id = npcId.Trim();
            _counts.TryGetValue(id, out int count);
            _counts[id] = count + 1;
        }

        public void ClearAll()
        {
            _counts.Clear();
        }
    }
}
