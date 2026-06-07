using System.Collections.Generic;
using JRogue.UI.Hotbar;
using UnityEngine;

namespace JRogue.UI.Gameplay
{
    public sealed class GameLogService : MonoBehaviour
    {
        public const string LogPrefix = "[GameLog]";

        static GameLogService _instance;

        readonly GameLogSession _session = new GameLogSession();
        readonly Queue<string> _pendingMessages = new Queue<string>();
        readonly object _pendingLock = new object();
        bool _mirrorRegistered;

        public static GameLogService Instance => _instance;

        public GameLogSession Session => _session;

        public static GameLogSession ActiveSession
        {
            get
            {
                EnsureInstance();
                return _instance._session;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void BootstrapOnSceneLoad() => EnsureInstance();

        public static GameLogService EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(GameLogService));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<GameLogService>();
            return _instance;
        }

        public static void ClearSession()
        {
            EnsureInstance();
            _instance._session.ClearSession();
            MessageConsoleUI.Instance?.ResetScrollback();
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
            RegisterMirror();
            MessageConsoleUI.EnsureInstance();
            AbilityHotbarUI.EnsureInstance().RefreshAll();
            PartyControlHudUI.EnsureInstance().RefreshAll();
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                UnregisterMirror();
                _instance = null;
            }
        }

        void Update()
        {
            FlushPendingMessages();
            MessageConsoleUI.Instance?.HandleGameplayInput();
        }

        void RegisterMirror()
        {
            if (_mirrorRegistered)
                return;

            Application.logMessageReceived += OnUnityLogMessage;
            _mirrorRegistered = true;
        }

        void UnregisterMirror()
        {
            if (!_mirrorRegistered)
                return;

            Application.logMessageReceived -= OnUnityLogMessage;
            _mirrorRegistered = false;
        }

        void OnUnityLogMessage(string condition, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(condition))
                return;

            string line = type == LogType.Exception
                ? $"{condition} ({type})"
                : condition;

            lock (_pendingLock)
                _pendingMessages.Enqueue(line);
        }

        void FlushPendingMessages()
        {
            while (true)
            {
                string next;
                lock (_pendingLock)
                {
                    if (_pendingMessages.Count == 0)
                        return;

                    next = _pendingMessages.Dequeue();
                }

                _session.Append(next);
            }
        }
    }
}
