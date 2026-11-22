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
    /// Booth session state machine: Main menu (leaderboard) → Join → Names → each mini-game → Final (scores saved) → menu.
    /// Also owns the global pause menu. Lives on the single object in Main.unity.
    /// </summary>
    public sealed partial class GameFlow : MonoBehaviour
    {
        /// <summary>Tests shorten halves etc. through these.</summary>
        public static float HalfSecondsOverride = -1f;
        public static float IdleResetSeconds = 120f;

        /// <summary>The gauntlet, in play order.</summary>
        public static readonly List<Func<IMiniGame>> Games = new List<Func<IMiniGame>>
        {
            () => FirewallArena.Create(),
            () => DataHeistBall.Create(),
            () => PhishRace.Create(),
        };
        public static readonly string[] GameNames = { "FIREWALL ARENA", "DATA HEIST BALL", "PHISH RACE" };
        public static readonly string[] GameBlurbs =
        {
            "Shooter · match the defense to the attack",
            "Sports · encryption and DLP stop data theft",
            "Racing · spot the fake link, dodge the bait",
        };
        public static int TotalGames => Games.Count;

        /// <summary>Intro-card kicker for a mini-game: its place in this session's plan.</summary>
        public static string Kicker(IMiniGame g)
        {
            var f = Instance;
            if (f == null || f._plan == null) return "MINI-GAME";
            if (f._plan.Count == 1) return "QUICK PLAY";
            return $"MINI-GAME {f._planPos + 1} OF {f._plan.Count}";
        }

        public static GameFlow Instance { get; private set; }
        public Session Session { get; private set; }
        public bool Paused { get; private set; }
        /// <summary>True while paused and on the frame the pause menu closed (that press must not reach the game).</summary>
        public bool InputBlocked => Paused || Time.frameCount <= _resumeFrame;
        public bool InMatch { get; set; }
        public string State { get; private set; } = "boot";
        public Camera Cam { get; private set; }
        public string ScreenName => _screen != null ? _screen.name : "";
        public int PauseSelection => _pauseIndex;

        Canvas _ui, _overlay;
        RectTransform _screen, _pause;
        IMiniGame _current;
        List<int> _plan;
        int _planPos;
        bool _orbit;
        float _orbitAngle, _lastInput;
        PlayerControls _starter;
        int _resumeFrame = -1;

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Settings.Load();
            if (!Application.isEditor) Cursor.visible = false; // booth app: controllers only
            if (!Application.isEditor) UseNativeResolution();
            Cam = Stage.CreateCamera();
            Stage.Lighting();
            Stage.PostFx();
            City.Build(transform, new Vector2(22, 14));
            City.LngPlant(transform, new Vector3(0, -24f, 78f));
            _ui = Ui.Canvas("UI", 10);
            _overlay = Ui.Canvas("Overlay", 50);
        }

        void Start() => StartCoroutine(Main());

        /// <summary>
        /// The Mac player remembers its last window size, so one windowed launch made every later fullscreen launch
        /// upscale 1280×720. Always go full native (Retina) resolution unless a size was given on the command line.
        /// </summary>
        static void UseNativeResolution()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.Exists(args, a => a == "-screen-width" || a == "-screen-fullscreen")) return;
            int w = Display.main.systemWidth, h = Display.main.systemHeight;
            foreach (var r in Screen.resolutions)
                if (r.width * r.height > w * h) { w = r.width; h = r.height; }
            Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
            Debug.Log($"[BreachPoint] Fullscreen at {w}x{h}");
        }

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
                yield return MainMenu();
                yield return Join();
                yield return EnterNames();
                yield return Gauntlet(0);
            }
        }

        /// <summary>Plays mini-games from <paramref name="from"/>, then the final results and initials.</summary>
        IEnumerator Gauntlet(int from)
        {
            _plan ??= AllGames();
            for (int i = from; i < _plan.Count; i++)
            {
                _planPos = i;
                _current = Games[_plan[i]]();
                State = "game:" + _current.Title;
                yield return _current.Run(this, Session);
                if (_current.Result != null) Session.Record(_current.Result);
                _current = null;
            }
            yield return Final();
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

        static void BuildLeaderboardPanel(Transform parent, Vector2 anchor)
        {
            var box = Ui.Box(parent, anchor, new Vector2(760, 640), Palette.Gold, "Leaderboard");
            var t = box.transform;
            Ui.Label(t, "MOST CYBER AWARE", 34, Palette.Gold, TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(0, -12), Fonts.Title);
            Ui.Label(t, "Points for every right security call  +  accuracy bonus", 22, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -88), new Vector2(0, -58));
            Ui.Label(t, "PLAYER", 20, Palette.Muted, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(130, -116), new Vector2(400, -90));
            Ui.Label(t, "ACCURACY", 20, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(0, 1), new Vector2(470, -116), new Vector2(590, -90));
            Ui.Label(t, "SCORE", 20, Palette.Muted, TextAnchor.MiddleRight, new Vector2(0, 1), new Vector2(0, 1), new Vector2(580, -116), new Vector2(720, -90));
            var top = Leaderboard.Load();
            if (top.Count == 0)
            {
                Ui.Label(t, "No scores yet.\nBe the first!", 34, Palette.Muted, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0, -20), new Vector2(0, -60));
                return;
            }
            int rows = Mathf.Min(10, top.Count);
            for (int i = 0; i < rows; i++)
            {
                var e = top[i];
                float y = -116 - i * 50;
                var c = e.Team == "RED" ? Palette.Red : Palette.Blue;
                var rankColor = i == 0 ? Palette.Gold : Palette.Text;
                if (i % 2 == 0) Ui.Panel(t, new Color(1, 1, 1, 0.035f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, y - 46), new Vector2(-16, y), "Row");
                Ui.Label(t, $"{i + 1}.", 30, rankColor, TextAnchor.MiddleRight, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, y - 46), new Vector2(110, y), Fonts.Mono);
                Ui.Panel(t, c, new Vector2(0, 1), new Vector2(0, 1), new Vector2(118, y - 36), new Vector2(124, y - 10), "TeamMark");
                Ui.Label(t, string.IsNullOrEmpty(e.Name) ? "—" : e.Name, 30, rankColor, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(134, y - 46), new Vector2(470, y), Fonts.Mono);
                Ui.Label(t, $"{e.Accuracy}%", 28, Palette.Good, TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(0, 1), new Vector2(470, y - 46), new Vector2(590, y), Fonts.Mono);
                Ui.Label(t, $"{e.Score}", 30, rankColor, TextAnchor.MiddleRight, new Vector2(0, 1), new Vector2(0, 1), new Vector2(590, y - 46), new Vector2(720, y), Fonts.Mono);
            }
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

        /// <summary>Full-screen info card. Waits for confirm presses (or auto-continues). Timers freeze while paused.</summary>
        public IEnumerator ShowCard(Card c)
        {
            var s = NewScreen("Card");
            Ui.Panel(s, new Color(0, 0, 0, 0.72f), Vector2.zero, Vector2.one);
            var box = Ui.Box(s, new Vector2(0.5f, 0.5f), new Vector2(1400, 840), c.Accent, "CardBox");
            var t = box.transform;
            Ui.Label(t, c.Kicker, 28, c.Accent, TextAnchor.UpperCenter, new Vector2(0, 0.88f), new Vector2(1, 0.96f));
            Ui.Label(t, c.Title, 70, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.75f), new Vector2(1, 0.88f), default, default, Fonts.Title);
            Ui.Label(t, c.Body, 31, Palette.Text, TextAnchor.UpperCenter, new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.74f), default, default, Fonts.Medium);
            var foot = Ui.Label(t, c.Footer, 28, Palette.Gold, TextAnchor.MiddleCenter, new Vector2(0, 0.03f), new Vector2(1, 0.15f));

            var ready = new bool[2];
            for (int i = 0; i < 2; i++) ready[i] = Session.Players[i] == null || Session.Players[i].Controls.IsCpu;
            float el = 0f;
            while (true)
            {
                if (!Paused) el += Time.unscaledDeltaTime;
                if (!InputBlocked && el >= c.MinSeconds)
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
            return ready ? Palette.Paint($"{p.Display} ✔", p.Color) : Palette.Paint($"{p.Display}: press {p.Controls.ConfirmName}", Palette.A(p.Color, 0.75f));
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
            lines.AppendLine($"MATCH POINTS   {Session.Players[0].Tag} {Session.TotalPoints(0)}   ·   {Session.Players[1].Tag} {Session.TotalPoints(1)}");
            lines.AppendLine();
            lines.AppendLine(Palette.Paint("CYBER AWARENESS", Palette.Gold) + "  (right security calls + accuracy bonus)");
            for (int i = 0; i < 2; i++)
                lines.AppendLine($"{Session.Players[i].Tag}   {Palette.Paint(Session.AwarenessScore(i).ToString(), Palette.Gold)}   ·   {Session.Accuracy(i)}% of decisions right");
            // Save each human player's awareness score under their name (no initials screen at the end any more).
            for (int i = 0; i < 2; i++)
            {
                var p = Session.Players[i];
                int score = Session.AwarenessScore(i);
                if (p.Controls.IsCpu || score <= 0) continue;
                int rank = Leaderboard.Add(p.Display, p.Team, score, Session.Accuracy(i));
                if (rank > 0) lines.AppendLine(Palette.Paint($"{p.Display} is #{rank} on the Most Cyber Aware board!", Palette.Good));
            }
            int aware0 = Session.AwarenessScore(0), aware1 = Session.AwarenessScore(1);
            if (aware0 != aware1)
            {
                var top = Session.Players[aware0 > aware1 ? 0 : 1];
                lines.AppendLine(Palette.Paint($"MOST CYBER AWARE: {top.Display}", top.Color));
            }
            var card = new Card
            {
                Kicker = "GAUNTLET RESULT",
                Title = champ < 0 ? "IT'S A DRAW" : $"{Session.Players[champ].Display} WINS",
                Accent = champ < 0 ? Palette.Gold : Session.Players[champ].Color,
                Body = lines.ToString(),
                Footer = "Thanks for playing! Stay sharp out there.",
                RequireBoth = false,
                AutoSeconds = 20f,
            };
            if (champ >= 0) Haptics.Pulse(Session.Players[champ].Controls, 0.4f, 0.8f, 0.6f);
            yield return ShowCard(card);
        }

        // ------------------------------------------------------------------ global pause

        static readonly string[] PauseItems = { "RESUME", "RESTART THIS GAME", "SOUND", "QUIT TO TITLE" };
        int _pauseIndex;
        Text[] _pauseText;
        float _navCooldown;

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
            if (State != "title" && !InMatch && !Paused && Time.unscaledTime - _lastInput > IdleResetSeconds) { QuitToTitle(); return; }

            bool pausable = InMatch && Session != null && Session.Ready;
            if (!pausable) return;

            if (!Paused)
            {
                foreach (var p in Session.Players)
                    if (p.Controls.Kind == ControlKind.Gamepad && !p.Controls.Connected) { SetPaused(true, $"{p.Display} controller disconnected. Reconnect it, then resume."); return; }
                if (AnyPausePressed()) SetPaused(true, null);
                return;
            }

            // Pause menu navigation (any controller or keyboard).
            int nav = NavDelta();
            if (nav != 0)
            {
                _pauseIndex = (_pauseIndex + nav + PauseItems.Length) % PauseItems.Length;
                Sfx.Play("switch", 0.6f);
                RefreshPauseMenu();
            }
            if (AnyPausePressed() || AnyBack()) { SetPaused(false, null); return; }
            if (AnyMenuConfirm())
            {
                switch (_pauseIndex)
                {
                    case 0: SetPaused(false, null); break;
                    case 1: RestartCurrentGame(); break;
                    case 2:
                        Sfx.Muted = !Sfx.Muted;
                        AudioListener.volume = Sfx.Muted ? 0f : 1f;
                        Sfx.Play("confirm");
                        RefreshPauseMenu();
                        break;
                    default: QuitToTitle(); break;
                }
            }
        }

        static bool AnyPausePressed()
        {
            foreach (var g in Gamepad.all) if (g.startButton.wasPressedThisFrame) return true;
            var k = Keyboard.current;
            return k != null && (k.escapeKey.wasPressedThisFrame || k.pKey.wasPressedThisFrame);
        }

        static bool AnyBack()
        {
            foreach (var g in Gamepad.all) if (g.buttonEast.wasPressedThisFrame) return true;
            return false;
        }

        static bool AnyMenuConfirm()
        {
            foreach (var g in Gamepad.all) if (g.buttonSouth.wasPressedThisFrame) return true;
            var k = Keyboard.current;
            return k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.fKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
        }

        int NavDelta()
        {
            _navCooldown -= Time.unscaledDeltaTime;
            if (_navCooldown < -1f) _navCooldown = -1f;
            foreach (var g in Gamepad.all)
            {
                if (g.dpad.up.wasPressedThisFrame) return -1;
                if (g.dpad.down.wasPressedThisFrame) return 1;
            }
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) return -1;
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) return 1;
            }
            float y = 0f;
            foreach (var g in Gamepad.all) { var v = g.leftStick.ReadValue().y; if (Mathf.Abs(v) > Mathf.Abs(y)) y = v; }
            if (Mathf.Abs(y) < 0.3f) { _navCooldown = 0f; return 0; }
            if (Mathf.Abs(y) > 0.6f && _navCooldown <= 0f) { _navCooldown = 0.25f; return y > 0 ? -1 : 1; }
            return 0;
        }

        static bool AnyInput()
        {
            foreach (var g in Gamepad.all)
                if (g.wasUpdatedThisFrame && (g.leftStick.ReadValue().sqrMagnitude > 0.1f || g.buttonSouth.isPressed || g.buttonEast.isPressed || g.buttonWest.isPressed || g.buttonNorth.isPressed || g.startButton.isPressed)) return true;
            var k = Keyboard.current;
            return k != null && k.anyKey.isPressed;
        }

        void SetPaused(bool on, string reason)
        {
            if (Paused == on) return;
            Paused = on;
            Time.timeScale = on ? 0f : 1f;
            Sfx.Play("pause");
            if (on)
            {
                Haptics.StopAll();
                _pauseIndex = 0;
                _pause = Ui.Full(_overlay.transform, "Pause");
                Ui.Panel(_pause, new Color(0, 0, 0, 0.75f), Vector2.zero, Vector2.one);
                Ui.Label(_pause, "PAUSED", 110, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.7f), new Vector2(1, 0.85f), default, default, Fonts.Title);
                Ui.Label(_pause, reason ?? "", 30, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 0.64f), new Vector2(1, 0.69f));
                _pauseText = new Text[PauseItems.Length];
                for (int i = 0; i < PauseItems.Length; i++)
                {
                    float y = 0.55f - i * 0.085f;
                    _pauseText[i] = Ui.Label(_pause, "", 46, Color.white, TextAnchor.MiddleCenter, new Vector2(0, y - 0.04f), new Vector2(1, y + 0.04f), default, default, Fonts.Title);
                }
                Ui.Label(_pause, "Up / down to choose  ·  A / ✕ select  ·  B / ○ or Start resume", 26, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 0.08f), new Vector2(1, 0.13f));
                RefreshPauseMenu();
            }
            else
            {
                _resumeFrame = Time.frameCount;
                if (_pause != null) Destroy(_pause.gameObject);
                _pause = null;
            }
        }

        void RefreshPauseMenu()
        {
            if (_pauseText == null) return;
            for (int i = 0; i < PauseItems.Length; i++)
            {
                if (_pauseText[i] == null) continue;
                string label = i == 2 ? $"SOUND: {(Sfx.Muted ? "OFF" : "ON")}" : PauseItems[i];
                bool sel = i == _pauseIndex;
                _pauseText[i].text = sel ? $"▶  {label}  ◀" : label;
                _pauseText[i].color = sel ? Palette.Gold : Palette.A(Palette.Text, 0.7f);
            }
        }

        /// <summary>Restarts the current mini-game with the same players (results so far are kept).</summary>
        public void RestartCurrentGame()
        {
            StopAllCoroutines();
            if (Paused) SetPaused(false, null);
            _current?.Abort();
            _current = null;
            ClearScreen();
            Haptics.StopAll();
            StartCoroutine(RestartFrom(_planPos));
        }

        IEnumerator RestartFrom(int index)
        {
            yield return null;
            yield return Gauntlet(index);
            StartCoroutine(Main());
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
