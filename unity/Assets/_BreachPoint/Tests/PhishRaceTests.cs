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
            _g.MailsEnabled = false;
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
        public void Links_GetHarderEachLap()
        {
            Assert.AreEqual(0, _g.ForkTier(0), "lap 1: easy links");
            float P = PhishRace.Perimeter;
            _g.SetProgress(0, P + PhishRace.ForkExitS[0] - 15f);
            _g.SetProgress(1, P + PhishRace.ForkExitS[0] - 20f);
            _g.RefreshForks();
            Assert.AreEqual(1, _g.ForkTier(0), "lap 2: tricky links");
            Assert.Contains(_g.ForkLinks(0).Fake, System.Array.ConvertAll(Links.Medium, x => x.Fake));
            _g.SetProgress(0, 2 * P + PhishRace.ForkExitS[1] - 15f);
            _g.SetProgress(1, 2 * P + PhishRace.ForkExitS[1] - 20f);
            _g.RefreshForks();
            Assert.AreEqual(2, _g.ForkTier(1), "lap 3: expert links");
            Assert.Contains(_g.ForkLinks(1).Fake, System.Array.ConvertAll(Links.Hard, x => x.Fake));
        }

        [Test]
        public void Awareness_CountsRightAndWrongCalls()
        {
            _g.SetFork(0, realOuter: true);
            SimDriving(4.5f, 2f, 0f, p1Drives: false);    // Red takes the real link (easy tier)
            var r = _g.ComputeResult();
            Assert.AreEqual(1, r.Right[0]);
            Assert.AreEqual(0, r.Wrong[0]);
            Assert.AreEqual(PhishRace.AwareLinkPoints[0], r.Aware[0]);
        }

        [Test]
        public void RealEmail_Boosts_PhishingEmail_Stalls()
        {
            _g.MailsEnabled = true;
            _g.SetMail(0, real: true, offset: 2f);          // first bend, on Red's line
            SimDriving(1.6f, 2f, 0f, p1Drives: false);
            Assert.AreEqual(1, _g.GoodMails[0], "drove through the real email");
            Assert.Greater(_g.SurgeLeft(0), 0f, "boosted");
            Assert.IsFalse(_g.MailActive(0));

            _g.StartHalf(0);
            _g.MailsEnabled = true;
            _g.SetMail(0, real: false, offset: 2f);
            SimDriving(1.6f, 2f, 0f, p1Drives: false);
            Assert.AreEqual(1, _g.BadMails[0], "opened the phishing email");
            Assert.AreEqual(1, _g.Phished[0]);
            Assert.Greater(_g.StallLeft(0), 0f, "stalled");
            var r = _g.ComputeResult();
            Assert.AreEqual(1, r.Wrong[0]);
        }

        [Test]
        public void EveryRealLinkAndSender_UsesTheRealDomain()
        {
            foreach (var tier in Links.Tiers)
            foreach (var p in tier)
            {
                Assert.IsTrue(p.Real.Contains(Links.RealDomain) || p.Real.Contains("microsoft"), $"real: {p.Real}");
                Assert.IsFalse(p.Fake == p.Real);
                Assert.IsFalse(string.IsNullOrEmpty(p.Context));
            }
            foreach (var m in Links.Mails)
                if (m.Real) Assert.IsTrue(m.From.EndsWith("@" + Links.RealDomain) || m.From.EndsWith("@microsoft.com"), m.From);
                else Assert.IsFalse(m.From.EndsWith("@" + Links.RealDomain), m.From);
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
