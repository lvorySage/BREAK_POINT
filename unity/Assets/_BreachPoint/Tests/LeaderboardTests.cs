using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace BreachPoint.Tests
{
    public class LeaderboardTests
    {
        [SetUp]
        public void SetUp()
        {
            Leaderboard.PathOverride = Path.Combine(Application.temporaryCachePath, "bp_test_leaderboard_unit.json");
            Leaderboard.Reset();
        }

        [TearDown]
        public void TearDown() => Leaderboard.PathOverride = null;

        [Test]
        public void Entries_AreSortedAndRanked()
        {
            Assert.AreEqual(1, Leaderboard.Add("AAA", Team.Red, 50));
            Assert.AreEqual(1, Leaderboard.Add("BBB", Team.Blue, 90));
            Assert.AreEqual(3, Leaderboard.Add("CCC", Team.Red, 10));
            Assert.AreEqual(3, Leaderboard.Add("DDD", Team.Blue, 50), "ties keep the earlier score (AAA) ahead");
            var list = Leaderboard.Load();
            CollectionAssert.AreEqual(new[] { "BBB", "AAA", "DDD", "CCC" }, list.ConvertAll(e => e.Initials));
            Assert.AreEqual("BLUE", list[0].Team);
        }

        [Test]
        public void Board_KeepsTopTenAndQualifiesCorrectly()
        {
            for (int i = 1; i <= 10; i++) Leaderboard.Add("P" + i, Team.Red, i * 10);
            Assert.AreEqual(10, Leaderboard.Load().Count);
            Assert.IsFalse(Leaderboard.Qualifies(10), "must beat the lowest score");
            Assert.IsTrue(Leaderboard.Qualifies(11));
            Assert.AreEqual(0, Leaderboard.Add("LOW", Team.Blue, 5), "didn't make the board");
            Assert.AreEqual(10, Leaderboard.Load().Count);
            Assert.IsFalse(Leaderboard.Qualifies(0), "zero never qualifies");
        }

        [Test]
        public void AwarenessScore_IsGoodCallsPlusAccuracyBonus()
        {
            var session = new Session();
            var a = new MiniGameResult();
            a.Aware[0] = 50; a.Right[0] = 5; a.Wrong[0] = 0;   // Red: perfect
            a.Aware[1] = 60; a.Right[1] = 6; a.Wrong[1] = 6;   // Blue: more calls, half wrong
            session.Record(a);
            Assert.AreEqual(100, session.Accuracy(0));
            Assert.AreEqual(50, session.Accuracy(1));
            Assert.AreEqual(150, session.AwarenessScore(0), "careful player wins the awareness board");
            Assert.AreEqual(110, session.AwarenessScore(1));
            Assert.AreEqual(0, new Session().AwarenessScore(0), "no good calls, no score");
        }

        [Test]
        public void Entries_KeepAccuracy()
        {
            Leaderboard.Add("ACC", Team.Blue, 120, 87);
            Assert.AreEqual(87, Leaderboard.Load()[0].Accuracy);
        }
    }
}
