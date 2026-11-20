using NUnit.Framework;
using UnityEngine;

namespace BreachPoint.Tests
{
    /// <summary>Rules of Data Heist Ball, simulated deterministically with Step().</summary>
    public class DataHeistBallTests
    {
        DataHeistBall _g;
        Session _s;

        [SetUp]
        public void SetUp()
        {
            Sfx.Muted = true;
            _s = new Session();
            _s.Players[0] = new PlayerSlot(0, new PlayerControls(ControlKind.Cpu));
            _s.Players[1] = new PlayerSlot(1, new PlayerControls(ControlKind.Cpu));
            _g = DataHeistBall.Create();
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

        /// <summary>Park both ships far from the action so they don't touch the ball.</summary>
        void ParkShips()
        {
            _g.Vehicle(0).Teleport(new Vector3(0, 0, 8.5f));
            _g.Vehicle(1).Teleport(new Vector3(4, 0, 8.5f));
        }

        [Test]
        public void Kick_LaunchesTheBall()
        {
            _g.Vehicle(0).Teleport(new Vector3(2f, 0, 0));
            _g.Vehicle(1).Teleport(new Vector3(0, 0, -8f));
            _g.SetBall(new Vector3(0, 0, 0), Vector3.zero);
            Assert.IsTrue(_g.TryKick(0));
            Assert.Greater(_g.BallVel.magnitude, 10f);
            Assert.Less(_g.BallVel.x, 0f, "kicked away from the kicker");
            Assert.IsFalse(_g.TryKick(0), "cooldown");
        }

        [Test]
        public void Kick_FailsWhenOutOfRange()
        {
            ParkShips();
            _g.SetBall(Vector3.zero, Vector3.zero);
            Assert.IsFalse(_g.TryKick(0));
        }

        [Test]
        public void Exfil_StealsDataAndResetsBall()
        {
            ParkShips();
            _g.SetBall(new Vector3(-15f, 0, 0), new Vector3(-20f, 0, 0));
            Sim(1f);
            Assert.AreEqual(DataHeistBall.ExfilValue, _g.StolenThisHalf, 0.01f);
            Assert.AreEqual(DataHeistBall.ExfilValue, _g.StolenAsAttacker[0], 0.01f);
            Assert.Less(Mathf.Abs(_g.BallPos.x), 1f, "ball back at the centre");
        }

        [Test]
        public void EncryptedData_IsUselessWhenStolen()
        {
            _g.Vehicle(1).Teleport(new Vector3(-10f, 0, 3f));   // defender (Blue) next to the ball
            _g.Vehicle(0).Teleport(new Vector3(0, 0, 8.5f));
            _g.SetBall(new Vector3(-10f, 0, 0), Vector3.zero);
            Assert.IsTrue(_g.Encrypt());
            _g.Vehicle(1).Teleport(new Vector3(4, 0, 8.5f));
            _g.SetBall(new Vector3(-15f, 0, 0), new Vector3(-20f, 0, 0));
            Sim(1f);
            Assert.AreEqual(0f, _g.StolenThisHalf, 0.01f);
            Assert.AreEqual(1, _g.EncryptedSaves[1]);
        }

        [Test]
        public void Encrypt_NeedsTheDefenderCloseToTheData()
        {
            ParkShips();
            _g.SetBall(new Vector3(-12f, 0, 0), Vector3.zero);
            Assert.IsFalse(_g.Encrypt());
            Assert.AreEqual(0f, _g.EncryptCooldownLeft, "no cooldown spent on a miss");
        }

        [Test]
        public void DlpLock_BouncesTheDataBack()
        {
            ParkShips();
            Assert.IsTrue(_g.LockExit());
            Assert.IsFalse(_g.LockExit(), "cooldown");
            _g.SetBall(new Vector3(-15f, 0, 0), new Vector3(-20f, 0, 0));
            Sim(0.4f);
            Assert.AreEqual(0f, _g.StolenThisHalf, 0.01f);
            Assert.AreEqual(1, _g.LockSaves[1]);
            Assert.Greater(_g.BallVel.x, 0f, "bounced back into the field");
        }

        [Test]
        public void BallOutsideTheGoalMouth_BouncesOffTheEndWall()
        {
            ParkShips();
            _g.SetBall(new Vector3(-15f, 0, 5f), new Vector3(-20f, 0, 0)); // flat part of the end wall (not a corner deflector)
            Sim(0.4f);
            Assert.AreEqual(0f, _g.StolenThisHalf, 0.01f);
            Assert.Greater(_g.BallVel.x, 0f);
        }

        [Test]
        public void Vault_RecoveryCountsForTheDefender()
        {
            ParkShips();
            _g.SetBall(new Vector3(15f, 0, 0), new Vector3(20f, 0, 0));
            Sim(1f);
            Assert.AreEqual(1, _g.Recoveries[1]);
            Assert.AreEqual(0f, _g.StolenThisHalf, 0.01f);
        }

        [Test]
        public void RolesSwap_InSecondHalf()
        {
            _g.StartHalf(1);
            Assert.AreEqual(1, _g.AttackerIndex);
            ParkShips();
            _g.SetBall(new Vector3(-15f, 0, 0), new Vector3(-20f, 0, 0));
            Sim(1f);
            Assert.AreEqual(DataHeistBall.ExfilValue, _g.StolenAsAttacker[1], 0.01f, "Blue scores as attacker in half 2");
        }

        [Test]
        public void Result_WinnerAndPoints()
        {
            ParkShips();
            _g.SetBall(new Vector3(-15f, 0, 0), new Vector3(-20f, 0, 0));   // Red exfils 20%
            Sim(1f);
            _g.StartHalf(1);
            ParkShips();
            _g.SetBall(new Vector3(15f, 0, 0), new Vector3(20f, 0, 0));     // Red (defending) secures once
            Sim(1f);
            var r = _g.ComputeResult();
            Assert.AreEqual(0, r.Winner);
            Assert.AreEqual(20 + DataHeistBall.RecoveryPoints, r.Points[0]);
        }

        [Test]
        public void CpuOpponents_PlayAFullHalfWithoutErrors()
        {
            Random.InitState(1234); // deterministic CPU decisions
            _g.AiEnabled = true;
            _g.HalfSeconds = 60f;
            _g.StartHalf(0);
            Sim(61f);
            Assert.IsFalse(_g.Playing);
            Assert.Greater(_g.Kicks[0] + _g.Kicks[1], 0, "CPUs kick the ball");
            float scored = _g.StolenThisHalf + _g.Recoveries[1] + _g.EncryptedSaves[1] + _g.LockSaves[1];
            Assert.That(scored, Is.GreaterThan(0f), "the ball reaches a goal within a half (no deadlocks)");
        }
    }
}
