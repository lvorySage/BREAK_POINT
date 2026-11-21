using NUnit.Framework;
using UnityEngine;

namespace BreachPoint.Tests
{
    /// <summary>Ships must never render upside down or facing the wrong way, in any game or half.</summary>
    public class VehicleTests
    {
        [Test]
        public void Yaw_IsUprightInEveryDirection()
        {
            for (int k = 0; k < 16; k++)
            {
                float a = k * Mathf.PI / 8f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var q = HoverVehicle.Yaw(dir);
                Assert.Greater(Vector3.Dot(q * Vector3.up, Vector3.up), 0.999f, $"upright at {k * 22.5f}°");
                Assert.Greater(Vector3.Dot(q * Vector3.right, dir), 0.999f, $"nose along the direction at {k * 22.5f}°");
            }
        }

        [Test]
        public void FacingLeft_StaysUpright()
        {
            var v = HoverVehicle.Create(null, Team.Blue, "TestShip");
            try
            {
                v.Face(Vector3.left);
                Assert.IsTrue(v.Upright, "snapped upright immediately");
                var zone = Rect.MinMaxRect(-50, -50, 50, 50);
                for (int i = 0; i < 100; i++) { v.Face(Vector3.left); v.Drive(Vector2.zero, false, 0.02f, zone); }
                Assert.IsTrue(v.Upright);
                Assert.Greater(Vector3.Dot(v.Body.right, Vector3.left), 0.9f);
                // Hard reversals at full speed, with boost.
                for (int i = 0; i < 300; i++)
                {
                    var input = (i / 40) % 2 == 0 ? Vector2.left : Vector2.right;
                    v.Drive(input, i % 50 == 0, 0.02f, zone);
                    if (v.Velocity.sqrMagnitude > 1f) v.Face(v.Velocity);
                    Assert.IsTrue(v.Upright, $"upright during reversal, step {i}");
                }
            }
            finally { Object.DestroyImmediate(v.gameObject); }
        }

        static Session CpuSession()
        {
            var s = new Session();
            s.Players[0] = new PlayerSlot(0, new PlayerControls(ControlKind.Cpu));
            s.Players[1] = new PlayerSlot(1, new PlayerControls(ControlKind.Cpu));
            return s;
        }

        static void CheckShips(HoverVehicle a, HoverVehicle b, string when)
        {
            Assert.IsTrue(a.Upright, $"Red upright {when}");
            Assert.IsTrue(b.Upright, $"Blue upright {when}");
        }

        [Test]
        public void FirewallArena_ShipsUprightBothHalves()
        {
            Sfx.Muted = true;
            var g = FirewallArena.Create();
            try
            {
                g.ManualStep = true;
                g.Init(CpuSession());
                for (int h = 0; h < 2; h++)
                {
                    g.StartHalf(h);
                    CheckShips(g.Vehicle(0), g.Vehicle(1), $"at the start of half {h + 1}");
                    var def = g.Vehicle(g.DefenderIndex);
                    Assert.Greater(Vector3.Dot(def.Body.right, Vector3.left), 0.9f, "defender faces the attacker");
                    for (int i = 0; i < 250; i++) { g.Step(0.02f); if (i % 10 == 0) CheckShips(g.Vehicle(0), g.Vehicle(1), $"in half {h + 1}"); }
                }
            }
            finally { Object.DestroyImmediate(g.gameObject); Sfx.Muted = false; }
        }

        [Test]
        public void DataHeistBall_ShipsUprightBothHalves()
        {
            Sfx.Muted = true;
            var g = DataHeistBall.Create();
            try
            {
                g.ManualStep = true;
                g.Init(CpuSession());
                for (int h = 0; h < 2; h++)
                {
                    g.StartHalf(h);
                    CheckShips(g.Vehicle(0), g.Vehicle(1), $"at kick-off of half {h + 1}");
                    Assert.Greater(Vector3.Dot(g.Vehicle(g.AttackerIndex).Body.right, Vector3.left), 0.9f, "attacker faces the exfil side");
                    for (int i = 0; i < 250; i++) { g.Step(0.02f); if (i % 10 == 0) CheckShips(g.Vehicle(0), g.Vehicle(1), $"in half {h + 1}"); }
                }
            }
            finally { Object.DestroyImmediate(g.gameObject); Sfx.Muted = false; }
        }

        [Test]
        public void PhishRace_ShipsUprightBothHeats()
        {
            Sfx.Muted = true;
            var g = PhishRace.Create();
            try
            {
                g.ManualStep = true;
                g.Init(CpuSession());
                for (int h = 0; h < 2; h++)
                {
                    g.StartHalf(h);
                    CheckShips(g.Vehicle(0), g.Vehicle(1), $"on the grid of heat {h + 1}");
                    for (int i = 0; i < 400; i++) { g.Step(0.02f); if (i % 10 == 0) CheckShips(g.Vehicle(0), g.Vehicle(1), $"in heat {h + 1}"); }
                }
            }
            finally { Object.DestroyImmediate(g.gameObject); Sfx.Muted = false; }
        }
    }
}
