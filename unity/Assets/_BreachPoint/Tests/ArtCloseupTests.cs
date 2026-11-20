using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BreachPoint.Tests
{
    /// <summary>Close-up screenshots for the art pass (only writes files when BP_SHOTS is set).</summary>
    public class ArtCloseupTests
    {
        [UnityTest]
        public IEnumerator Closeups()
        {
            Sfx.Muted = true;
            var cam = Stage.CreateCamera();
            Stage.Lighting();
            var fx = Stage.PostFx();
            var s = new Session();
            s.Players[0] = new PlayerSlot(0, new PlayerControls(ControlKind.Cpu));
            s.Players[1] = new PlayerSlot(1, new PlayerControls(ControlKind.Cpu));
            var g = FirewallArena.Create();
            g.ManualStep = true;
            g.AiEnabled = false;
            g.Init(s);
            g.StartHalf(0);
            yield return null;

            void Shot(string name, Vector3 pos, Vector3 look)
            {
                cam.transform.position = pos;
                cam.transform.LookAt(look);
                Screenshot.Capture(name);
            }
            Shot("art-fort", new Vector3(8f, 6f, -14f), new Vector3(16f, 1.5f, -4f));
            Shot("art-vehicles", new Vector3(-3f, 5f, -7f), new Vector3(-8f, 0.5f, 0f));
            Shot("art-roof", new Vector3(-6f, 9f, -18f), new Vector3(0f, 0f, 0f));

            Object.Destroy(g.gameObject);
            Object.Destroy(cam.gameObject);
            Object.Destroy(fx.gameObject);
            foreach (var l in Object.FindObjectsByType<Light>()) if (l.name == "Moonlight") Object.Destroy(l.gameObject);
            Sfx.Muted = false;
            Assert.Pass();
        }
    }
}
