using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BreachPoint
{
    public interface IMiniGame
    {
        string Title { get; }
        /// <summary>Runs the whole mini-game (intro → play → results card) and fills Result.</summary>
        IEnumerator Run(GameFlow flow, Session session);
        MiniGameResult Result { get; }
        void Abort();
    }

    public sealed class Card
    {
        public string Kicker = "";
        public string Title = "";
        public string Body = "";
        public string Footer = "";
        public Color Accent = Palette.Gold;
        public float AutoSeconds = -1f;   // > 0: continues by itself
        public bool RequireBoth = true;   // both players must press confirm
        public float MinSeconds = 0.7f;   // ignore presses that were meant for the previous screen
    }

    /// <summary>
    /// Booth session state machine: Title → Join → each mini-game → Final → Title.
    /// Lives on the single object in Main.unity.
    /// </summary>
    public sealed class GameFlow : MonoBehaviour
    {
        /// <summary>Tests shorten halves etc. through these.</summary>
        public static float HalfSecondsOverride = -1f;
        public static float IdleResetSeconds = 120f;

        /// <summary>The gauntlet. Data Heist Ball and Phish Race are added here as they are built.</summary>
        public static readonly List<Func<IMiniGame>> Games = new List<Func<IMiniGame>>
        {
            () => FirewallArena.Create(),
        };
        public static readonly string[] ComingSoon = { "DATA HEIST BALL", "PHISH RACE" };

        public static GameFlow Instance { get; private set; }
        public Session Session { get; private set; }
        public bool Paused { get; private set; }
        public bool InMatch { get; set; }
        public string State { get; private set; } = "boot";
        public Camera Cam { get; private set; }
        public string ScreenName => _screen != null ? _screen.name : "";

        Canvas _ui, _overlay;
        RectTransform _screen, _pause;
        IMiniGame _current;
        bool _orbit;
        float _orbitAngle, _lastInput;

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Cam = Stage.CreateCamera();
            Stage.Lighting();
            Stage.PostFx();
            City.Build(transform, new Vector2(22, 14));
            _ui = Ui.Canvas("UI", 10);
            _overlay = Ui.Canvas("Overlay", 50);
        }

        void Start() => StartCoroutine(Main());

        void OnDestroy()
        {
            Time.timeScale = 1f;
            Haptics.StopAll();
            if (Instance == this) Instance = null;
        }

        IEnumerator Main()
        {
            while (true)
            {
                Session = new Session();
                yield return Title();
                yield return Join();
                for (int i = 0; i < Games.Count; i++)
                {
                    _current = Games[i]();
                    State = "game:" + _current.Title;
                    yield return _current.Run(this, Session);
                    if (_current.Result != null) Session.Record(_current.Result);
                    _current = null;
                }
                yield return Final();
            }
        }

        // ------------------------------------------------------------------ screens

        public RectTransform NewScreen(string name)
        {
            ClearScreen();
            _screen = Ui.Full(_ui.transform, name);
            return _screen;
        }

        public void ClearScreen()
        {
            if (_screen != null) Destroy(_screen.gameObject);
            _screen = null;
        }

        public void OrbitCamera(bool on) => _orbit = on;

        PlayerControls _starter;

        IEnumerator Title()
        {
            yield return null; // never react to the press that closed the previous screen
            State = "title";
            InMatch = false;
            OrbitCamera(true);
            var s = NewScreen("Title");
            Ui.Panel(s, new Color(0, 0, 0, 0.5f), Vector2.zero, Vector2.one);
            Ui.Label(s, "SECURITY WEEK · RED vs BLUE", 30, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 0.72f), new Vector2(1, 0.78f));
            Ui.Label(s, $"{Palette.Paint("BREACH", Palette.Red)} {Palette.Paint("POINT", Palette.Blue)}", 150, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0, 0.52f), new Vector2(1, 0.72f), default, default, Fonts.Title);
            Ui.Label(s, "Three head-to-head cyber battles. Roles swap every round.", 34, Palette.Text, TextAnchor.MiddleCenter, new Vector2(0, 0.44f), new Vector2(1, 0.5f));
            var press = Ui.Label(s, "PRESS  A / ✕  TO START", 44, Palette.Gold, TextAnchor.MiddleCenter, new Vector2(0, 0.24f), new Vector2(1, 0.31f));
            Ui.Label(s, "Keyboard: Space or Enter", 24, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 0.19f), new Vector2(1, 0.23f));
            Ui.Label(s, "All scenarios are fictional. Simulation only.", 22, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 0.03f), new Vector2(1, 0.07f));

            while ((_starter = ConfirmingDevice()) == null)
            {
                press.color = Palette.A(Palette.Gold, 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f));
                yield return null;
            }
            Sfx.Play("confirm");
        }

        /// <summary>The device that pressed confirm this frame (it becomes Player 1), or null.</summary>
        static PlayerControls ConfirmingDevice()
        {
            foreach (var g in Gamepad.all)
                if (g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame) return new PlayerControls(ControlKind.Gamepad, g);
            var k = Keyboard.current;
            if (k == null) return null;
            if (k.spaceKey.wasPressedThisFrame || k.fKey.wasPressedThisFrame) return new PlayerControls(ControlKind.KeyboardLeft);
            if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) return new PlayerControls(ControlKind.KeyboardRight);
            return null;
        }

        IEnumerator Join()
        {
            yield return null;
            State = "join";
            var s = NewScreen("Join");
            Ui.Panel(s, new Color(0, 0, 0, 0.45f), Vector2.zero, Vector2.one);
            Ui.Label(s, "JOIN THE MATCH", 72, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.8f), new Vector2(1, 0.92f), default, default, Fonts.Title);
            Ui.Label(s, "Player 2: press  A / ✕  on your controller.", 34, Palette.Text, TextAnchor.MiddleCenter, new Vector2(0, 0.73f), new Vector2(1, 0.79f));

            var slotText = new Text[2];
            var slotBox = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                var team = i == 0 ? Team.Red : Team.Blue;
                var c = Palette.Of(team);
                var box = Ui.Box(s, new Vector2(i == 0 ? 0.3f : 0.7f, 0.46f), new Vector2(620, 340), c, "Slot" + i);
                slotBox[i] = box;
                Ui.Label(box.transform, $"PLAYER {i + 1}", 30, Palette.Muted, TextAnchor.UpperCenter, new Vector2(0, 0.78f), new Vector2(1, 0.95f));
                Ui.Label(box.transform, team == Team.Red ? "RED TEAM" : "BLUE TEAM", 64, c, TextAnchor.MiddleCenter, new Vector2(0, 0.5f), new Vector2(1, 0.8f), default, default, Fonts.Title);
                slotText[i] = Ui.Label(box.transform, "", 30, Palette.Text, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.48f));
            }
            Ui.Label(s, "No second player?  Press  Y / △  (keyboard: Tab) to add a CPU opponent.", 28, Palette.Gold, TextAnchor.MiddleCenter, new Vector2(0, 0.14f), new Vector2(1, 0.2f));
            Ui.Label(s, "Keyboard players: Red = Space / F, Blue = Enter / 1", 22, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 0.09f), new Vector2(1, 0.13f));

            // Whoever pressed start on the title screen is Player 1 (Red).
            var slots = new PlayerControls[2];
            if (_starter != null)
            {
                slots[0] = _starter;
                Haptics.Pulse(_starter, 0.3f, 0.6f, 0.2f);
                Haptics.LightBar(_starter, Palette.Red);
                _starter = null;
            }
            float doneAt = -1f;
            while (true)
            {
                PlayerControls joined = null;
                foreach (var g in Gamepad.all)
                {
                    if (!g.buttonSouth.wasPressedThisFrame) continue;
                    if (Array.Exists(slots, p => p != null && p.Pad == g)) continue;
                    joined = new PlayerControls(ControlKind.Gamepad, g);
                    break;
                }
                var k = Keyboard.current;
                if (joined == null && k != null)
                {
                    if ((k.spaceKey.wasPressedThisFrame || k.fKey.wasPressedThisFrame) && !Array.Exists(slots, p => p != null && p.Kind == ControlKind.KeyboardLeft))
                        joined = new PlayerControls(ControlKind.KeyboardLeft);
                    else if ((k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame || k.digit1Key.wasPressedThisFrame)
                             && !Array.Exists(slots, p => p != null && p.Kind == ControlKind.KeyboardRight))
                        joined = new PlayerControls(ControlKind.KeyboardRight);
                }
                if (joined != null)
                {
                    int free = slots[0] == null ? 0 : slots[1] == null ? 1 : -1;
                    if (free >= 0)
                    {
                        slots[free] = joined;
                        Sfx.Play("join");
                        Haptics.Pulse(joined, 0.3f, 0.6f, 0.2f);
                        Haptics.LightBar(joined, free == 0 ? Palette.Red : Palette.Blue);
                    }
                }

                bool addCpu = false;
                foreach (var g in Gamepad.all) if (g.buttonNorth.wasPressedThisFrame) addCpu = true;
                if (k != null && k.tabKey.wasPressedThisFrame) addCpu = true;
                if (addCpu && (slots[0] == null) != (slots[1] == null))
                {
                    slots[slots[0] == null ? 0 : 1] = new PlayerControls(ControlKind.Cpu);
                    Sfx.Play("join");
                }

                for (int i = 0; i < 2; i++)
                {
                    slotText[i].text = slots[i] == null
                        ? Palette.Paint("Press  A / ✕  to join", Palette.Muted)
                        : $"{Palette.Paint("✔ READY", Palette.Good)}\n{slots[i].DeviceLabel}";
                    slotBox[i].color = slots[i] == null ? Palette.Panel : Palette.A(Palette.Of(i == 0 ? Team.Red : Team.Blue) * 0.35f, 0.9f);
                }

                if (slots[0] != null && slots[1] != null)
                {
                    if (doneAt < 0) doneAt = Time.unscaledTime + 1.0f;
                    if (Time.unscaledTime >= doneAt) break;
                }
                yield return null;
            }

            Session.Players[0] = new PlayerSlot(0, slots[0]);
            Session.Players[1] = new PlayerSlot(1, slots[1]);
        }

        /// <summary>Full-screen info card. Waits for confirm presses (or auto-continues).</summary>
        public IEnumerator ShowCard(Card c)
        {
            var s = NewScreen("Card");
            Ui.Panel(s, new Color(0, 0, 0, 0.72f), Vector2.zero, Vector2.one);
            var box = Ui.Box(s, new Vector2(0.5f, 0.5f), new Vector2(1320, 820), c.Accent, "CardBox");
            var t = box.transform;
            Ui.Label(t, c.Kicker, 28, c.Accent, TextAnchor.UpperCenter, new Vector2(0, 0.88f), new Vector2(1, 0.96f));
            Ui.Label(t, c.Title, 70, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.74f), new Vector2(1, 0.88f), default, default, Fonts.Title);
            Ui.Label(t, c.Body, 32, Palette.Text, TextAnchor.UpperCenter, new Vector2(0.06f, 0.2f), new Vector2(0.94f, 0.72f), default, default, Fonts.Medium);
            var foot = Ui.Label(t, c.Footer, 28, Palette.Gold, TextAnchor.MiddleCenter, new Vector2(0, 0.03f), new Vector2(1, 0.15f));

            var ready = new bool[2];
            for (int i = 0; i < 2; i++) ready[i] = Session.Players[i] == null || Session.Players[i].Controls.IsCpu;
            float t0 = Time.unscaledTime;
            while (true)
            {
                float el = Time.unscaledTime - t0;
                if (!Paused && el >= c.MinSeconds)
                    for (int i = 0; i < 2; i++)
                    {
                        var p = Session.Players[i];
                        if (p != null && !ready[i] && p.Controls.ConfirmDown) { ready[i] = true; Sfx.Play("confirm"); Haptics.Pulse(p.Controls, 0.2f, 0.3f, 0.1f); }
                    }

                bool done = c.RequireBoth ? ready[0] && ready[1] : ready[0] || ready[1];
                if (c.AutoSeconds > 0 && el >= c.AutoSeconds) done = true;
                if (done) break;

                string status = c.AutoSeconds > 0
                    ? $"Continuing in {Mathf.CeilToInt(c.AutoSeconds - el)}…"
                    : $"{ReadyTag(0, ready[0])}      {ReadyTag(1, ready[1])}";
                foot.text = string.IsNullOrEmpty(c.Footer) ? status : c.Footer + "\n" + status;
                yield return null;
            }
            ClearScreen();
        }

        string ReadyTag(int i, bool ready)
        {
            var p = Session.Players[i];
            if (p == null) return "";
            return ready ? Palette.Paint($"{p.Name} ✔", p.Color) : Palette.Paint($"{p.Name}: press {p.Controls.ConfirmName}", Palette.A(p.Color, 0.75f));
        }

        IEnumerator Final()
        {
            State = "final";
            InMatch = false;
            OrbitCamera(true);
            int champ = Session.Champion;
            Sfx.Play("win");
            var lines = new System.Text.StringBuilder();
            foreach (var r in Session.Results)
            {
                string w = r.Winner < 0 ? "DRAW" : Session.Players[r.Winner].Tag;
                lines.AppendLine($"{r.Game}:  {w}   ({r.ScoreLabel} {Session.Players[0].Tag} {r.Scores[0]:0}  ·  {Session.Players[1].Tag} {r.Scores[1]:0})");
            }
            lines.AppendLine();
            lines.AppendLine(Palette.Paint("Coming next: " + string.Join(" · ", ComingSoon), Palette.Muted));
            var card = new Card
            {
                Kicker = "GAUNTLET RESULT",
                Title = champ < 0 ? "IT'S A DRAW" : $"{Session.Players[champ].Name} TEAM WINS",
                Accent = champ < 0 ? Palette.Gold : Session.Players[champ].Color,
                Body = lines.ToString(),
                Footer = "Thanks for playing! Stay sharp out there.",
                RequireBoth = false,
                AutoSeconds = 20f,
            };
            if (champ >= 0) Haptics.Pulse(Session.Players[champ].Controls, 0.4f, 0.8f, 0.6f);
            yield return ShowCard(card);
        }

        // ------------------------------------------------------------------ pause / idle

        void Update()
        {
            if (_orbit && Cam != null)
            {
                _orbitAngle += Time.unscaledDeltaTime * 4f;
                var a = _orbitAngle * Mathf.Deg2Rad;
                // Slow aerial orbit above the neon skyline.
                Cam.transform.position = new Vector3(Mathf.Sin(a) * 62f, 62f, -Mathf.Cos(a) * 62f);
                Cam.transform.LookAt(new Vector3(0, -8f, 0));
            }

            if (AnyInput()) _lastInput = Time.unscaledTime;
            if (State != "title" && !InMatch && Time.unscaledTime - _lastInput > IdleResetSeconds) { QuitToTitle(); return; }

            if (Session == null || !Session.Ready || !InMatch) return;
            foreach (var p in Session.Players)
            {
                if (p.Controls.Kind == ControlKind.Gamepad && !p.Controls.Connected && !Paused) { SetPaused(true, $"{p.Name} controller disconnected"); return; }
            }
            if (!Paused)
            {
                foreach (var p in Session.Players)
                    if (p.Controls.PauseDown) { SetPaused(true, $"Paused by {p.Name}"); return; }
            }
            else
            {
                foreach (var p in Session.Players)
                {
                    if (p.Controls.PauseDown || p.Controls.ActionDown(0)) { SetPaused(false, null); return; }
                    if (p.Controls.BackDown) { QuitToTitle(); return; }
                }
            }
        }

        static bool AnyInput()
        {
            foreach (var g in Gamepad.all)
                if (g.wasUpdatedThisFrame && (g.leftStick.ReadValue().sqrMagnitude > 0.1f || g.buttonSouth.isPressed || g.buttonEast.isPressed || g.buttonWest.isPressed || g.startButton.isPressed)) return true;
            var k = Keyboard.current;
            return k != null && k.anyKey.isPressed;
        }

        void SetPaused(bool on, string reason)
        {
            Paused = on;
            Time.timeScale = on ? 0f : 1f;
            Sfx.Play("pause");
            if (on)
            {
                Haptics.StopAll();
                _pause = Ui.Full(_overlay.transform, "Pause");
                Ui.Panel(_pause, new Color(0, 0, 0, 0.7f), Vector2.zero, Vector2.one);
                Ui.Label(_pause, "PAUSED", 110, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.55f), new Vector2(1, 0.7f), default, default, Fonts.Title);
                Ui.Label(_pause, reason ?? "", 32, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 0.48f), new Vector2(1, 0.54f));
                Ui.Label(_pause, "A / ✕  or  Start  resume        B / ○  quit to title", 36, Palette.Gold, TextAnchor.MiddleCenter, new Vector2(0, 0.36f), new Vector2(1, 0.44f));
            }
            else if (_pause != null)
            {
                Destroy(_pause.gameObject);
                _pause = null;
            }
        }

        public void QuitToTitle()
        {
            StopAllCoroutines();
            if (Paused) SetPaused(false, null);
            Time.timeScale = 1f;
            InMatch = false;
            _current?.Abort();
            _current = null;
            ClearScreen();
            Haptics.StopAll();
            _lastInput = Time.unscaledTime;
            StartCoroutine(Main());
        }
    }
}
