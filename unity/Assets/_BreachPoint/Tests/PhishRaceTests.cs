using NUnit.Framework;
using UnityEngine;

namespace BreachPoint.Tests
{
    /// <summary>Rules of Phish Race, simulated deterministically with Step().</summary>
    public class PhishRaceTests
    {
        PhishRace _g;
        Session _s;

        [SetUp]
        public void SetUp()
        {
            Sfx.Muted = true;
            _s = new Session();
            _s.Players[0] = new PlayerSlot(0, new PlayerControls(ControlKind.Cpu));
            _s.Players[1] = new PlayerSlot(1, new PlayerControls(ControlKind.Cpu));
            _g = PhishRace.Create();
            _g.AiEnabled = false;
            _g.ManualStep = true;
            _g.Init(_s);
            _g.StartHalf(0);
        }

        [TearDown]
        public void TearDown()
        {
            if (_g != null) Object.Destroy(_g.gameObject);
            Sfx.Muted = false;
        }

        void Sim(float seconds)
        {
            for (float t = 0; t < seconds; t += 0.02f) _g.Step(0.02f);
        }

        /// <summary>Steer player i along the track at a lane offset (simple autopilot).</summary>
        void Drive(int i, float offset)
        {
            var v = _g.Vehicle(i);
            var me = v.transform.position;
            var target = PhishRace.At(PhishRace.S(me) + 5f, offset) - me;
            _s.Players[i].Controls.CpuMove = new Vector2(target.x, target.z).normalized;
        }

        void SimDriving(float seconds, float off0, float off1, bool p1Drives = true)
        {
            for (float t = 0; t < seconds; t += 0.02f)
            {
                Drive(0, off0);
                if (p1Drives) Drive(1, off1); else _s.Players[1].Controls.CpuMove = Vector2.zero;
                _g.Step(0.02f);
            }
        }

        [Test]
        public void TrackParameterisation_RoundTrips()
        {
            for (float s = 0.5f; s < PhishRace.Perimeter; s += 3.7f)
            {
                var p = PhishRace.At(s, 1.5f);
                Assert.AreEqual(s, PhishRace.S(p), 0.05f, $"s={s}");
                Assert.AreEqual(1.5f, PhishRace.Offset(p), 0.05f);
            }
        }

        [Test]
        public void RealLane_GivesBoostAndPoints()
        {
            _g.SetFork(0, realOuter: true);
            SimDriving(4.5f, 2f, -2f, p1Drives: false);
            Assert.AreEqual(1, _g.SafeLinks[0], "took the real (outer) lane at fork 1");
            Assert.AreEqual(0, _g.Phished[0]);
            Assert.AreEqual(PhishRace.SafePoints, _g.MatchPoints(0));
        }

        [Test]
        public void FakeLane_PhishesAndStalls()
        {
            _g.SetFork(0, realOuter: true);
            float before = -1f;
            for (float t = 0; t < 4.5f; t += 0.02f)
            {
                Drive(0, -2f);  // inner lane = the fake one
                _s.Players[1].Controls.CpuMove = Vector2.zero;
                _g.Step(0.02f);
                if (_g.Phished[0] > 0 && before < 0) before = _g.StallLeft(0);
            }
            Assert.AreEqual(1, _g.Phished[0]);
            Assert.Greater(before, 0f, "stalled right after the fake lane");
            Assert.AreEqual(0, _g.SafeLinks[0]);
        }

        [Test]
        public void ForkLinks_ChangeAfterTheShipPasses()
        {
            _g.SetFork(0, realOuter: true);
            var first = _g.ForkLinks(0).Real;
            SimDriving(4.5f, 2f, 0f, p1Drives: false);
            Assert.AreEqual(1, _g.SafeLinks[0]);
            Assert.AreNotEqual(first, _g.ForkLinks(0).Real, "a fresh pair of links for the next ship / lap");
        }

        [Test]
        public void Divider_KeepsShipInItsLane()
        {
            // Drive player 0 hard across the fork from the inner lane: it must not end up in the outer lane mid-fork.
            _g.SetProgress(0, PhishRace.ForkExitS[0] - 11f);
            _g.Vehicle(0).Teleport(PhishRace.At(PhishRace.ForkExitS[0] - 11f, -2f));
            _g.SetProgress(0, PhishRace.ForkExitS[0] - 11f);
            for (int k = 0; k < 50; k++)
            {
                _s.Players[0].Controls.CpuMove = new Vector2(-0.4f, 1f).normalized; // steer outward (+z on the top straight)
                _g.Step(0.02f);
                var p = _g.Vehicle(0).transform.position;
                if (Mathf.Abs(p.x) < PhishRace.ForkHalf - 1f) Assert.Less(p.z, PhishRace.R, "stayed in the inner lane");
            }
        }

        [Test]
        public void Lure_HooksTheTarget_ReportRemovesIt()
        {
            // Red is the phisher in heat 1. Drop a lure right on Blue.
            var blue = _g.Vehicle(1).transform.position;
            _g.AddLureAt(blue + Vector3.right * 0.5f);
            Sim(0.6f);
            Assert.AreEqual(1, _g.LureHits[0]);
            Assert.AreEqual(PhishRace.LurePoints, _g.MatchPoints(0));
            Assert.Greater(_g.StallLeft(1), 0f);
            Assert.AreEqual(0, _g.LureCount);

            _g.AddLureAt(_g.Vehicle(1).transform.position + Vector3.forward * 5f);
            Assert.IsTrue(_g.Report());
            Assert.AreEqual(1, _g.Reports[1]);
            Assert.AreEqual(0, _g.LureCount);
            Assert.IsFalse(_g.Report(), "cooldown");
        }

        [Test]
        public void DropLure_HasCooldownAndCap()
        {
            Assert.IsTrue(_g.DropLure());
            Assert.IsFalse(_g.DropLure(), "cooldown");
            Assert.AreEqual(1, _g.LureCount);
            Assert.AreEqual(1, _g.LuresDropped[0]);
        }

        [Test]
        public void FinishingFirst_WinsTheHeat()
        {
            _g.SetProgress(0, PhishRace.Laps * PhishRace.Perimeter - 3f);
            SimDriving(1.5f, 0f, 0f, p1Drives: false);
            Assert.IsTrue(_g.Finished(0));
            Assert.AreEqual(0, _g.HeatWinner);
            Assert.AreEqual(1, _g.Place(0));
            Sim(PhishRace.FinishGrace + 0.5f);
            Assert.IsFalse(_g.Playing, "heat ends after the grace period");
            Assert.AreEqual(1, _g.HeatWins[0]);
        }

        [Test]
        public void RolesSwap_AndResult()
        {
            _g.SetProgress(0, PhishRace.Laps * PhishRace.Perimeter - 3f);
            SimDriving(1.5f, 0f, 0f, p1Drives: false);
            Sim(PhishRace.FinishGrace + 0.5f);
            _g.StartHalf(1);
            Assert.AreEqual(1, _g.AttackerIndex);
            Assert.IsTrue(_g.DropLure() && _g.LuresDropped[1] == 1, "Blue drops bait in heat 2");
            var r = _g.ComputeResult();
            Assert.AreEqual(0, r.Winner);
            Assert.AreEqual(PhishRace.HeatWinPoints, r.Points[0]);
        }

        [Test]
        public void CpuOpponents_RaceAFullHeat()
        {
            Random.InitState(4321);
            _g.AiEnabled = true;
            _g.HalfSeconds = 90f;
            _g.StartHalf(0);
            Sim(91f);
            Assert.IsFalse(_g.Playing);
            Assert.IsTrue(_g.Finished(0) || _g.Finished(1), "someone finishes 3 laps inside 90 s");
            Assert.Greater(_g.SafeLinks[0] + _g.SafeLinks[1] + _g.Phished[0] + _g.Phished[1], 4, "forks are taken");
        }
    }
}
