using System;
using System.Collections.Generic;

namespace JRogue.UI.Gameplay
{
    public sealed class GameLogSession
    {
        public const int DefaultMaxLines = 500;

        readonly List<string> _lines = new List<string>();
        readonly int _maxLines;

        public GameLogSession(int maxLines = DefaultMaxLines)
        {
            _maxLines = maxLines < 1 ? DefaultMaxLines : maxLines;
        }

        public event Action SessionChanged;

        public IReadOnlyList<string> Lines => _lines;

        public int Count => _lines.Count;

        public void Append(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            _lines.Add(message);
            TrimToMax();
            SessionChanged?.Invoke();
        }

        public void ClearSession()
        {
            if (_lines.Count == 0)
                return;

            _lines.Clear();
            SessionChanged?.Invoke();
        }

        public int GetMaxScrollbackOffset(int visibleLineCount)
        {
            if (_lines.Count <= visibleLineCount)
                return 0;

            return _lines.Count - visibleLineCount;
        }

        public void CopyWindow(int scrollbackOffset, int visibleLineCount, List<string> destination)
        {
            destination.Clear();
            if (_lines.Count == 0 || visibleLineCount <= 0)
                return;

            int clampedOffset = scrollbackOffset < 0 ? 0 : scrollbackOffset;
            int maxOffset = GetMaxScrollbackOffset(visibleLineCount);
            if (clampedOffset > maxOffset)
                clampedOffset = maxOffset;

            int endExclusive = _lines.Count - clampedOffset;
            int start = endExclusive - visibleLineCount;
            if (start < 0)
                start = 0;

            for (int i = start; i < endExclusive; i++)
                destination.Add(_lines[i]);
        }

        void TrimToMax()
        {
            int overflow = _lines.Count - _maxLines;
            if (overflow <= 0)
                return;

            _lines.RemoveRange(0, overflow);
        }
    }
}
