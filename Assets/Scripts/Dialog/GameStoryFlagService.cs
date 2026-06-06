using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Dialog
{
    /// <summary>Persistent boolean story flags for dialog branches and interactable preconditions.</summary>
    public sealed class GameStoryFlagService : MonoBehaviour
    {
        public const string LogPrefix = "[StoryFlag]";

        static GameStoryFlagService _instance;

        readonly HashSet<string> _flags = new HashSet<string>();

        public static GameStoryFlagService Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindAnyObjectByType<GameStoryFlagService>();
                if (_instance != null)
                    return _instance;

                var go = new GameObject(nameof(GameStoryFlagService));
                _instance = go.AddComponent<GameStoryFlagService>();
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

        public bool IsSet(string flagId)
        {
            if (string.IsNullOrWhiteSpace(flagId))
                return false;

            return _flags.Contains(flagId.Trim());
        }

        public void Set(string flagId, bool value = true)
        {
            if (string.IsNullOrWhiteSpace(flagId))
                return;

            string id = flagId.Trim();
            if (value)
            {
                if (_flags.Add(id))
                    Debug.Log($"{LogPrefix} set '{id}'");
            }
            else if (_flags.Remove(id))
            {
                Debug.Log($"{LogPrefix} cleared '{id}'");
            }
        }

        public bool IsAnySet(IReadOnlyList<string> flagIds)
        {
            if (flagIds == null || flagIds.Count == 0)
                return false;

            for (int i = 0; i < flagIds.Count; i++)
            {
                if (IsSet(flagIds[i]))
                    return true;
            }

            return false;
        }

        public void ClearAll()
        {
            _flags.Clear();
        }
    }
}
