using NUnit.Framework;
using UnityEngine;

namespace BreachPoint.Tests
{
    /// <summary>Rules of Firewall Arena, simulated deterministically with Step().</summary>
    public class FirewallArenaTests
    {
        FirewallArena _g;
        Session _s;

        [SetUp]
        public void SetUp()
        {
            Sfx.Muted = true;
            _s = new Session();
            _s.Players[0] = new PlayerSlot(0, new PlayerControls(ControlKind.Cpu));
            _s.Players[1] = new PlayerSlot(1, new PlayerControls(ControlKind.Cpu));
            _g = FirewallArena.Create();
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

        void PlaceDefender(float z) => _g.Vehicle(_g.DefenderIndex).Teleport(new Vector3(9f, 0, z));

        [Test]
        public void MatchingDefense_BlocksAndStunsAttacker()
        {
            _g.SetShield(AttackType.Malware);
            Assert.IsTrue(_g.TryFire(AttackType.Malware));
            Sim(2.0f);
            Assert.AreEqual(1, _g.Blocks[1], "defender (Blue) blocks");
            Assert.AreEqual(0f, _g.StolenThisHalf, "no data stolen");
            Assert.Greater(_g.Stun, 0f, "reflected shot stuns the attacker");
            Assert.IsFalse(_g.TryFire(AttackType.Phishing), "stunned attacker cannot fire");
        }

        [Test]
        public void WrongDefense_LetsDataThrough()
        {
            _g.SetShield(AttackType.Phishing);
            _g.TryFire(AttackType.Malware);
            Sim(2.0f);
            Assert.AreEqual(1, _g.WrongDefenses[1]);
            Assert.AreEqual(Attacks.Value(AttackType.Malware), _g.StolenThisHalf, 0.01f);
            Assert.AreEqual(_g.StolenThisHalf, _g.StolenAsAttacker[0], 0.01f, "credited to Red as attacker");
        }

        [Test]
        public void EveryAttack_IsStoppedOnlyByItsOwnDefense()
        {
            foreach (var atk in Attacks.All)
            foreach (var shield in Attacks.All)
            {
                _g.StartHalf(0);
                _g.SetShield(shield);
                _g.TryFire(atk);
                Sim(4f); // phishing is deliberately slow (~3 s to cross)
                if (atk == shield) Assert.AreEqual(0f, _g.StolenThisHalf, 0.01f, $"{shield} should stop {atk}");
                else Assert.Greater(_g.StolenThisHalf, 0f, $"{shield} must not stop {atk}");
            }
        }

        [Test]
        public void Cooldowns_PreventSpam()
        {
            Assert.IsTrue(_g.TryFire(AttackType.Phishing));
            Assert.IsFalse(_g.TryFire(AttackType.Phishing), "same type is cooling down");
            Assert.IsFalse(_g.TryFire(AttackType.Malware), "global cooldown");
            Sim(FirewallArena.GlobalCooldown + 0.05f);
            Assert.IsTrue(_g.TryFire(AttackType.Malware));
        }

        [Test]
        public void RolesSwap_InSecondHalf()
        {
            _g.StartHalf(1);
            Assert.AreEqual(1, _g.AttackerIndex);
            Assert.AreEqual(0, _g.DefenderIndex);
            _g.SetShield(AttackType.DDoS);
            _g.TryFire(AttackType.Phishing);
            Sim(4f);
            Assert.Greater(_g.StolenAsAttacker[1], 0f, "Blue scores as attacker in half 2");
            Assert.AreEqual(0f, _g.StolenAsAttacker[0]);
            Assert.AreEqual(1, _g.WrongDefenses[0], "Red is the defender now");
        }

        [Test]
        public void Half_EndsWhenTimeRunsOut()
        {
            _g.HalfSeconds = 1f;
            _g.StartHalf(0);
            Sim(1.1f);
            Assert.IsFalse(_g.Playing);
        }

        [Test]
        public void DataStolen_IsCappedAt100AndEndsTheHalf()
        {
            _g.HalfSeconds = 999f;
            _g.StartHalf(0);
            PlaceDefender(-9f);
            for (int i = 0; i < 30 && _g.Playing; i++)
            {
                _g.TryFire(AttackType.Malware);
                Sim(1.0f);
            }
            Assert.AreEqual(100f, _g.StolenThisHalf, 0.01f);
            Assert.IsFalse(_g.Playing, "servers drained ends the half");
        }

        [Test]
        public void Winner_IsWhoeverStoleMore()
        {
            _g.SetShield(AttackType.Phishing);
            _g.TryFire(AttackType.Malware);
            Sim(2f);
            _g.StartHalf(1);
            _g.SetShield(AttackType.Malware);
            _g.TryFire(AttackType.Malware);
            Sim(2f);
            var r = _g.ComputeResult();
            Assert.AreEqual(0, r.Winner, "Red stole 8%, Blue was blocked");
            StringAssert.Contains("MALWARE", r.Lesson);
        }

        [Test]
        public void CpuOpponents_PlayAFullHalfWithoutErrors()
        {
            _g.AiEnabled = true;
            _g.HalfSeconds = 20f;
            _g.StartHalf(0);
            Sim(21f);
            Assert.IsFalse(_g.Playing);
            int fired = 0;
            foreach (var n in _g.FiredByType) fired += n;
            Assert.Greater(fired, 5, "CPU attacker fires");
            Assert.Greater(_g.Blocks[1], 0, "CPU defender blocks some");
        }
    }
}
