using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace BreachPoint.Tests
{
    /// <summary>
    /// End-to-end: two virtual gamepads play the whole gauntlet from the title screen.
    /// Set BP_SHOTS=/some/dir to also save screenshots of each stage.
    /// </summary>
    public class GameFlowTests
    {
        Gamepad _p1, _p2;
        GameFlow _flow;
        InputSettings.BackgroundBehavior _bg;
        InputSettings.EditorInputBehaviorInPlayMode _editorInput;

        [SetUp]
        public void SetUp()
        {
            _bg = InputSystem.settings.backgroundBehavior;
            _editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _p1 = InputSystem.AddDevice<Gamepad>("TestPad1");
            _p2 = InputSystem.AddDevice<Gamepad>("TestPad2");
            GameFlow.HalfSecondsOverride = 4f;
            GameFlow.IdleResetSeconds = 9999f;
            Leaderboard.PathOverride = Path.Combine(Application.temporaryCachePath, "bp_test_leaderboard.json");
            Leaderboard.Reset();
            Sfx.Muted = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (_flow != null) UnityEngine.Object.Destroy(_flow.gameObject);
            // Remove only what the game creates at the scene root.
            string[] ours = { "Main Camera", "Moonlight", "PostFX", "UI", "Overlay", "FirewallHUD", "FirewallArena", "DataHeistHUD", "DataHeistBall", "PhishRaceHUD", "PhishRace", "Popup" };
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go != null && go.scene.IsValid() && go.transform.parent == null && Array.IndexOf(ours, go.name) >= 0) UnityEngine.Object.Destroy(go);
            InputSystem.RemoveDevice(_p1);
            InputSystem.RemoveDevice(_p2);
            InputSystem.settings.backgroundBehavior = _bg;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
            GameFlow.HalfSecondsOverride = -1f;
            GameFlow.IdleResetSeconds = 120f;
            Leaderboard.PathOverride = null;
            Time.timeScale = 1f;
            AudioListener.volume = 1f;
        }

        static IEnumerator Press(Gamepad pad, GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button));
            yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return null;
        }

        static void Stick(Gamepad pad, Vector2 v) => InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = v });

        IEnumerator WaitFor(Func<bool> cond, float timeout, string what)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!cond())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail($"Timed out waiting for {what} (state={_flow.State}, screen={_flow.ScreenName})");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TwoGamepads_PlayTheWholeGauntlet()
        {
            _flow = new GameObject("BreachPoint").AddComponent<GameFlow>();
            yield return WaitFor(() => _flow.State == "title", 5, "title");
            yield return new WaitForSeconds(0.3f);
            Screenshot.Capture("01-title");

            yield return Press(_p1, GamepadButton.South);
            yield return WaitFor(() => _flow.State == "join", 5, "join");
            yield return Press(_p1, GamepadButton.South);
            yield return Press(_p2, GamepadButton.South);
            yield return null;
            Screenshot.Capture("02-join");
            yield return WaitFor(() => _flow.State.StartsWith("game:") && _flow.ScreenName == "Card", 5, "intro card");
            Assert.AreEqual(ControlKind.Gamepad, _flow.Session.Players[0].Controls.Kind);
            Assert.AreSame(_p1, _flow.Session.Players[0].Controls.Pad, "first pad to join is Red");
            Assert.AreSame(_p2, _flow.Session.Players[1].Controls.Pad);
            yield return new WaitForSeconds(0.9f);
            Screenshot.Capture("03-intro");
            yield return Press(_p1, GamepadButton.South);
            yield return Press(_p2, GamepadButton.South);

            var arena = UnityEngine.Object.FindAnyObjectByType<FirewallArena>();
            Assert.IsNotNull(arena);
            yield return WaitFor(() => arena.Playing, 6, "half 1 to start");

            // Red attacks: move up a lane and fire all three types; Blue holds Email filter.
            Stick(_p1, new Vector2(0.2f, 0.6f));
            yield return new WaitForSeconds(0.4f);
            yield return Press(_p1, GamepadButton.South);
            yield return new WaitForSeconds(0.35f);
            yield return Press(_p1, GamepadButton.East);
            yield return new WaitForSeconds(0.35f);
            yield return Press(_p1, GamepadButton.West);
            yield return new WaitForSeconds(0.45f);
            Screenshot.Capture("04-gameplay");
            yield return Press(_p2, GamepadButton.East);   // Blue switches to Antivirus
            Assert.AreEqual(AttackType.Malware, arena.Shield);
            yield return new WaitForSeconds(0.6f);
            Screenshot.Capture("05-gameplay-hits");

            yield return WaitFor(() => _flow.ScreenName == "Card", 10, "half-time card");
            Screenshot.Capture("06-halftime");
            Assert.AreEqual(1, arena.FiredByType[0] + arena.FiredByType[1] + arena.FiredByType[2] > 0 ? 1 : 0, "Red fired");

            yield return WaitFor(() => arena.Playing && arena.Half == 1, 12, "half 2");
            Assert.AreEqual(1, arena.AttackerIndex, "Blue attacks in half 2");
            yield return Press(_p2, GamepadButton.East);
            yield return new WaitForSeconds(0.5f);
            Screenshot.Capture("07-half2");

            yield return WaitFor(() => _flow.ScreenName == "Card" && !arena.Playing && arena.Half == 1, 10, "result card");
            yield return new WaitForSeconds(0.9f);
            Screenshot.Capture("08-result");
            yield return Press(_p1, GamepadButton.South);
            yield return Press(_p2, GamepadButton.South);

            // ---- Mini-game 2: Data Heist Ball ----
            yield return WaitFor(() => _flow.State == "game:DATA HEIST BALL" && _flow.ScreenName == "Card", 6, "data heist intro");
            yield return new WaitForSeconds(0.9f);
            Screenshot.Capture("08b-heist-intro");
            yield return Press(_p1, GamepadButton.South);
            yield return Press(_p2, GamepadButton.South);
            var heist = UnityEngine.Object.FindAnyObjectByType<DataHeistBall>();
            Assert.IsNotNull(heist);
            yield return WaitFor(() => heist.Playing, 6, "heist half 1");
            Stick(_p1, new Vector2(-1f, 0f));       // Red (attacker) drives at the data
            yield return new WaitForSeconds(0.8f);
            yield return Press(_p1, GamepadButton.South);
            yield return Press(_p2, GamepadButton.North); // Blue tries DLP lock
            yield return new WaitForSeconds(0.3f);
            Screenshot.Capture("08c-heist-play");
            Stick(_p1, Vector2.zero);
            yield return WaitFor(() => _flow.ScreenName == "Card", 10, "heist half-time");
            yield return WaitFor(() => heist.Playing && heist.Half == 1, 12, "heist half 2");
            Assert.AreEqual(1, heist.AttackerIndex);
            yield return WaitFor(() => _flow.ScreenName == "Card" && !heist.Playing && heist.Half == 1, 10, "heist result");
            yield return new WaitForSeconds(0.9f);
            Screenshot.Capture("08d-heist-result");
            yield return Press(_p1, GamepadButton.South);
            yield return Press(_p2, GamepadButton.South);

            // ---- Mini-game 3: Phish Race ----
            yield return WaitFor(() => _flow.State == "game:PHISH RACE" && _flow.ScreenName == "Card", 6, "phish race intro");
            yield return new WaitForSeconds(0.9f);
            Screenshot.Capture("08e-race-intro");
            yield return Press(_p1, GamepadButton.South);
            yield return Press(_p2, GamepadButton.South);
            var race = UnityEngine.Object.FindAnyObjectByType<PhishRace>();
            Assert.IsNotNull(race);
            yield return WaitFor(() => race.Playing, 6, "race heat 1");
            Stick(_p1, new Vector2(0f, 1f));          // both launch off the grid (north, along the right-hand bend)
            Stick(_p2, new Vector2(-0.3f, 1f));
            yield return new WaitForSeconds(0.5f);
            yield return Press(_p1, GamepadButton.West); // Red (phisher) drops bait
            Assert.AreEqual(1, race.LuresDropped[0]);
            yield return Press(_p2, GamepadButton.West); // Blue (target) reports it: a right security call
            Assert.AreEqual(1, race.Reports[1], "Blue reported the bait");
            Stick(_p1, new Vector2(-1f, 0.6f));
            yield return new WaitForSeconds(0.9f);
            Screenshot.Capture("08f-race-play");
            Stick(_p1, Vector2.zero);
            Stick(_p2, Vector2.zero);
            yield return WaitFor(() => _flow.ScreenName == "Card", 10, "race heat swap");
            yield return WaitFor(() => race.Playing && race.Half == 1, 12, "race heat 2");
            Assert.AreEqual(1, race.AttackerIndex);
            yield return WaitFor(() => _flow.ScreenName == "Card" && !race.Playing && race.Half == 1, 10, "race result");
            yield return new WaitForSeconds(0.9f);
            Screenshot.Capture("08g-race-result");
            yield return Press(_p1, GamepadButton.South);
            yield return Press(_p2, GamepadButton.South);

            yield return WaitFor(() => _flow.State == "final", 5, "final");
            Assert.AreEqual(3, _flow.Session.Results.Count, "all three mini-games recorded");
            Assert.Greater(_flow.Session.AwarenessScore(1), 0, "reporting bait counts as cyber awareness");
            yield return new WaitForSeconds(0.9f);
            Screenshot.Capture("09-final");
            yield return Press(_p1, GamepadButton.South);

            // Initials for whoever scored: change the first letter, then confirm three times.
            yield return WaitFor(() => _flow.State == "initials" || _flow.State == "title", 5, "initials");
            if (_flow.State == "initials")
            {
                yield return new WaitForSecondsRealtime(0.4f);
                Screenshot.Capture("10-initials");
                for (int i = 0; i < 3; i++)
                {
                    yield return Press(_p1, GamepadButton.South);
                    yield return Press(_p2, GamepadButton.South);
                }
            }
            yield return WaitFor(() => _flow.State == "title", 6, "back to title");
            yield return new WaitForSeconds(0.5f);
            Screenshot.Capture("11-title-leaderboard");
            Assert.Greater(Leaderboard.Load().Count, 0, "awareness score saved to the leaderboard");
        }

        [UnityTest]
        public IEnumerator PauseAndQuit_ReturnsToTitle()
        {
            _flow = new GameObject("BreachPoint").AddComponent<GameFlow>();
            yield return WaitFor(() => _flow.State == "title", 5, "title");
            yield return Press(_p1, GamepadButton.South);
            yield return WaitFor(() => _flow.State == "join", 5, "join");
            yield return Press(_p1, GamepadButton.South);
            yield return Press(_p1, GamepadButton.North);   // add CPU opponent
            yield return WaitFor(() => _flow.ScreenName == "Card", 5, "intro");
            Assert.IsTrue(_flow.Session.Players[1].Controls.IsCpu);
            yield return new WaitForSeconds(0.9f);
            yield return Press(_p1, GamepadButton.South);   // CPU is auto-ready
            var arena = UnityEngine.Object.FindAnyObjectByType<FirewallArena>();
            yield return WaitFor(() => arena != null && arena.Playing, 6, "play");

            // Global pause works from any controller, even one that isn't playing.
            yield return Press(_p2, GamepadButton.Start);
            Assert.IsTrue(_flow.Paused);
            Assert.AreEqual(0f, Time.timeScale);
            Screenshot.Capture("12-paused");

            // B resumes; the press must not leak into the game.
            yield return Press(_p1, GamepadButton.East);
            Assert.IsFalse(_flow.Paused);
            Assert.AreEqual(1f, Time.timeScale);

            // Restart this game: arena rebuilt, same players.
            yield return Press(_p1, GamepadButton.Start);
            yield return Press(_p1, GamepadButton.DpadDown);
            Assert.AreEqual(1, _flow.PauseSelection);
            yield return Press(_p1, GamepadButton.South);
            yield return WaitFor(() => _flow.ScreenName == "Card", 5, "intro after restart");
            Assert.IsTrue(arena == null, "old arena destroyed");
            Assert.IsTrue(_flow.Session.Players[1].Controls.IsCpu, "same players");
            yield return new WaitForSeconds(0.9f);
            yield return Press(_p1, GamepadButton.South);
            arena = UnityEngine.Object.FindAnyObjectByType<FirewallArena>();
            yield return WaitFor(() => arena != null && arena.Playing, 6, "play after restart");

            // Quit to title: 4th item (wraps from the top going up).
            yield return Press(_p1, GamepadButton.Start);
            yield return Press(_p1, GamepadButton.DpadUp);
            Assert.AreEqual(3, _flow.PauseSelection);
            yield return Press(_p1, GamepadButton.South);
            yield return WaitFor(() => _flow.State == "title", 5, "title after quit");
            Assert.IsFalse(_flow.Paused);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(arena == null, "arena destroyed");
        }
    }

    public class MainMenuTests
    {
        Gamepad _p1;
        GameFlow _flow;
        int _length, _cpu;
        bool _sound, _rumble;

        [SetUp]
        public void SetUp()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _p1 = InputSystem.AddDevice<Gamepad>("MenuPad");
            GameFlow.HalfSecondsOverride = 4f;
            GameFlow.IdleResetSeconds = 9999f;
            Leaderboard.PathOverride = Path.Combine(Application.temporaryCachePath, "bp_test_leaderboard.json");
            Settings.Load();
            _length = Settings.Length; _cpu = Settings.Cpu; _sound = Settings.Sound; _rumble = Settings.Rumble;
        }

        [TearDown]
        public void TearDown()
        {
            if (_flow != null) UnityEngine.Object.Destroy(_flow.gameObject);
            string[] ours = { "Main Camera", "Moonlight", "PostFX", "UI", "Overlay", "PhishRaceHUD", "PhishRace", "DataHeistHUD", "DataHeistBall", "FirewallHUD", "FirewallArena", "Popup" };
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go != null && go.scene.IsValid() && go.transform.parent == null && Array.IndexOf(ours, go.name) >= 0) UnityEngine.Object.Destroy(go);
            InputSystem.RemoveDevice(_p1);
            // Put the visitor-facing options back exactly as they were.
            Settings.Length = _length; Settings.Cpu = _cpu; Settings.Sound = _sound; Settings.Rumble = _rumble;
            Settings.Save();
            GameFlow.HalfSecondsOverride = -1f;
            GameFlow.IdleResetSeconds = 120f;
            Leaderboard.PathOverride = null;
            Time.timeScale = 1f;
        }

        static IEnumerator Press(Gamepad pad, GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button));
            yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return null;
        }

        IEnumerator WaitFor(Func<bool> cond, float timeout, string what)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!cond())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail($"Timed out waiting for {what} (state={_flow.State}, screen={_flow.ScreenName})");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Menu_OptionsHowToAndQuickPlay()
        {
            _flow = new GameObject("BreachPoint").AddComponent<GameFlow>();
            yield return WaitFor(() => _flow.State == "title", 5, "title");
            yield return new WaitForSeconds(0.3f);
            Screenshot.Capture("00-menu");
            Assert.AreEqual(0, _flow.MenuSelection, "PLAY is selected first");

            // Quit needs two presses: arm it, then move away to cancel.
            yield return Press(_p1, GamepadButton.DpadUp);
            Assert.AreEqual(5, _flow.MenuSelection, "QUIT is the last row");
            yield return Press(_p1, GamepadButton.South);
            Assert.IsTrue(_flow.QuitArmed, "first press only asks to confirm");
            Assert.AreEqual("title", _flow.State);
            yield return null;
            Screenshot.Capture("00a-quit-confirm");
            yield return Press(_p1, GamepadButton.DpadDown);
            Assert.IsFalse(_flow.QuitArmed, "moving away cancels");
            Assert.AreEqual(0, _flow.MenuSelection);

            // Options: change CPU difficulty with right, then back.
            for (int i = 0; i < 3; i++) yield return Press(_p1, GamepadButton.DpadDown);
            Assert.AreEqual(3, _flow.MenuSelection);
            yield return Press(_p1, GamepadButton.South);
            yield return WaitFor(() => _flow.State == "menu:options", 3, "options");
            int cpu = Settings.Cpu;
            for (int i = 0; i < 3; i++) yield return Press(_p1, GamepadButton.DpadDown);
            yield return Press(_p1, GamepadButton.DpadRight);
            Assert.AreEqual((cpu + 1) % 3, Settings.Cpu, "CPU difficulty cycles");
            yield return new WaitForSeconds(0.2f);
            Screenshot.Capture("00b-options");
            yield return Press(_p1, GamepadButton.East);
            yield return WaitFor(() => _flow.State == "title", 3, "back to menu");
            Assert.AreEqual(3, _flow.MenuSelection, "menu remembers the row");

            // How to play.
            yield return Press(_p1, GamepadButton.DpadUp);
            yield return Press(_p1, GamepadButton.South);
            yield return WaitFor(() => _flow.State == "menu:howto", 3, "how to play");
            yield return new WaitForSeconds(0.4f);
            Screenshot.Capture("00c-howto");
            yield return Press(_p1, GamepadButton.East);
            yield return WaitFor(() => _flow.State == "title", 3, "back to menu");

            // Controller test.
            yield return Press(_p1, GamepadButton.DpadDown);
            yield return Press(_p1, GamepadButton.DpadDown);
            yield return Press(_p1, GamepadButton.South);
            yield return WaitFor(() => _flow.State == "menu:controllers", 3, "controller test");
            InputSystem.QueueStateEvent(_p1, new GamepadState().WithButton(GamepadButton.North).WithButton(GamepadButton.LeftShoulder));
            yield return new WaitForSeconds(0.3f);
            Screenshot.Capture("00d-controllers");
            InputSystem.QueueStateEvent(_p1, new GamepadState().WithButton(GamepadButton.East));
            yield return new WaitForSeconds(1.3f);
            InputSystem.QueueStateEvent(_p1, new GamepadState());
            yield return WaitFor(() => _flow.State == "title", 3, "hold B leaves the controller test");

            // Quick play: Phish Race against the CPU.
            for (int i = 0; i < 3; i++) yield return Press(_p1, GamepadButton.DpadUp);
            Assert.AreEqual(1, _flow.MenuSelection);
            yield return Press(_p1, GamepadButton.South);
            yield return WaitFor(() => _flow.State == "menu:quick", 3, "quick play");
            yield return Press(_p1, GamepadButton.DpadDown);
            yield return Press(_p1, GamepadButton.DpadDown);
            yield return Press(_p1, GamepadButton.South);
            yield return WaitFor(() => _flow.State == "join", 3, "join");
            yield return Press(_p1, GamepadButton.North);   // CPU opponent
            yield return WaitFor(() => _flow.State == "game:PHISH RACE" && _flow.ScreenName == "Card", 5, "quick play goes straight to Phish Race");
        }
    }

    /// <summary>Renders the main camera plus all overlay UI to a PNG (for automated visual checks).</summary>
    public static class Screenshot
    {
        public static void Capture(string name, int w = 1600, int h = 900)
        {
            var dir = Environment.GetEnvironmentVariable("BP_SHOTS");
            var cam = Camera.main;
            if (string.IsNullOrEmpty(dir) || cam == null) return;
            Directory.CreateDirectory(dir);

            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            var modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                modes[i] = c.renderMode;
                if (!c.gameObject.scene.IsValid() || !c.isRootCanvas || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 0.31f + 0.001f * (100 - c.sortingOrder);
            }
            Canvas.ForceUpdateCanvases();
            cam.Render();

            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = prevActive;

            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] == null || !canvases[i].gameObject.scene.IsValid() || !canvases[i].isRootCanvas) continue;
                canvases[i].renderMode = modes[i];
            }
            cam.targetTexture = prevTarget;
            UnityEngine.Object.Destroy(tex);
            rt.Release();
            UnityEngine.Object.Destroy(rt);
        }
    }
}
