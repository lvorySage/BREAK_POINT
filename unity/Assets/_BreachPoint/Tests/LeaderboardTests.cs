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
    }
}
