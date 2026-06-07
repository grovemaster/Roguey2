using System.Collections.Generic;
using JRogue.UI.Gameplay;
using NUnit.Framework;

namespace JRogue.Tests.UI
{
    [TestFixture]
    public sealed class GameLogSessionTests
    {
        [Test]
        public void Append_AddsLinesAndTrimsToMax()
        {
            var session = new GameLogSession(maxLines: 3);
            session.Append("a");
            session.Append("b");
            session.Append("c");
            session.Append("d");

            Assert.AreEqual(3, session.Count);
            Assert.AreEqual("b", session.Lines[0]);
            Assert.AreEqual("c", session.Lines[1]);
            Assert.AreEqual("d", session.Lines[2]);
        }

        [Test]
        public void ClearSession_EmptiesBufferAndRaisesEvent()
        {
            var session = new GameLogSession();
            session.Append("line");
            int changeCount = 0;
            session.SessionChanged += () => changeCount++;

            session.ClearSession();

            Assert.AreEqual(0, session.Count);
            Assert.AreEqual(1, changeCount);
        }

        [Test]
        public void CopyWindow_ReturnsLatestVisibleLinesWithScrollback()
        {
            var session = new GameLogSession();
            session.Append("one");
            session.Append("two");
            session.Append("three");
            session.Append("four");

            var window = new List<string>();
            session.CopyWindow(scrollbackOffset: 0, visibleLineCount: 2, window);
            Assert.AreEqual(new[] { "three", "four" }, window);

            window.Clear();
            session.CopyWindow(scrollbackOffset: 1, visibleLineCount: 2, window);
            Assert.AreEqual(new[] { "two", "three" }, window);
        }

        [Test]
        public void GetMaxScrollbackOffset_ClampsWhenFewerLinesThanWindow()
        {
            var session = new GameLogSession();
            session.Append("only");

            Assert.AreEqual(0, session.GetMaxScrollbackOffset(visibleLineCount: 5));
            Assert.AreEqual(0, session.GetMaxScrollbackOffset(visibleLineCount: 1));
        }
    }
}
