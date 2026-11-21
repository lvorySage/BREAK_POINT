using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.UI;

namespace BreachPoint
{
    /// <summary>Main menu and its sub-screens: Play, Quick Play, How to Play, Options, Controller Test.</summary>
    public sealed partial class GameFlow
    {
        static readonly string[] MainItems = { "PLAY", "QUICK PLAY", "HOW TO PLAY", "OPTIONS", "CONTROLLER TEST" };
        static readonly string[] MainDescs =
        {
            "Red vs Blue · all 3 games · best of 3",
            "Pick a single game",
            "Rules, controls and the lesson in each game",
            "Sound, rumble, match length, CPU difficulty",
            "Check every controller's buttons and rumble",
        };

        /// <summary>Selected main-menu row (tests and attract mode read it).</summary>
        public int MenuSelection { get; private set; }

        static List<int> AllGames()
        {
            var l = new List<int>();
            for (int i = 0; i < Games.Count; i++) l.Add(i);
            return l;
        }

        /// <summary>A vertical list of selectable rows inside a panel.</summary>
        sealed class MenuList
        {
            public int Index;
            readonly Text[] _labels, _descs;
            readonly Image[] _bg, _bar;
            readonly Func<int, string> _label;
            readonly Func<int, string> _desc;

            public int Count => _labels.Length;

            public MenuList(Transform parent, int count, Func<int, string> label, Func<int, string> desc, float top = -30f, float rowH = 96f, int size = 44)
            {
                _label = label;
                _desc = desc;
                _labels = new Text[count];
                _descs = new Text[count];
                _bg = new Image[count];
                _bar = new Image[count];
                for (int i = 0; i < count; i++)
                {
                    float y = top - i * rowH;
                    _bg[i] = Ui.Panel(parent, Color.clear, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, y - rowH + 8), new Vector2(-16, y), "Row" + i);
                    _bar[i] = Ui.Panel(_bg[i].transform, Palette.Gold, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(8, 0), "Bar");
                    _labels[i] = Ui.Label(_bg[i].transform, "", size, Color.white, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(1, 1), new Vector2(34, 0), new Vector2(-20, -6), Fonts.Title);
                    _descs[i] = Ui.Label(_bg[i].transform, "", 24, Palette.Muted, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(1, 1), new Vector2(36, 6), new Vector2(-20, 0));
                }
                Refresh();
            }

            public void Move(int d)
            {
                Index = (Index + d + Count) % Count;
                Sfx.Play("switch", 0.6f);
                Refresh();
            }

            public void Refresh()
            {
                for (int i = 0; i < Count; i++)
                {
                    bool sel = i == Index;
                    _labels[i].text = _label(i);
                    _labels[i].color = sel ? Palette.Gold : Palette.A(Palette.Text, 0.75f);
                    _descs[i].text = _desc?.Invoke(i) ?? "";
                    _descs[i].color = sel ? Palette.Text : Palette.A(Palette.Muted, 0.7f);
                    _bg[i].color = sel ? new Color(1f, 0.75f, 0.14f, 0.12f) : Color.clear;
                    _bar[i].enabled = sel;
                }
            }
        }

        /// <summary>Menu input this frame: -2 nothing, -1 back, otherwise the chosen row (device in <paramref name="by"/>).</summary>
        int PollMenu(MenuList m, out PlayerControls by, Action<int> onHorizontal = null)
        {
            by = null;
            int nav = NavDelta();
            if (nav != 0) m.Move(nav);
            if (onHorizontal != null)
            {
                int h = NavHorizontal();
                if (h != 0) onHorizontal(h);
            }
            if (AnyMenuBack()) { Sfx.Play("pause"); return -1; }
            by = ConfirmingDevice();
            if (by != null) { Sfx.Play("confirm"); return m.Index; }
            return -2;
        }

        float _navHCooldown;

        int NavHorizontal()
        {
            _navHCooldown -= Time.unscaledDeltaTime;
            foreach (var g in Gamepad.all)
            {
                if (g.dpad.left.wasPressedThisFrame) return -1;
                if (g.dpad.right.wasPressedThisFrame) return 1;
            }
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) return -1;
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) return 1;
            }
            float x = 0f;
            foreach (var g in Gamepad.all) { var v = g.leftStick.ReadValue().x; if (Mathf.Abs(v) > Mathf.Abs(x)) x = v; }
            if (Mathf.Abs(x) < 0.3f) { _navHCooldown = 0f; return 0; }
            if (Mathf.Abs(x) > 0.6f && _navHCooldown <= 0f) { _navHCooldown = 0.25f; return x > 0 ? 1 : -1; }
            return 0;
        }

        static bool AnyMenuBack()
        {
            foreach (var g in Gamepad.all) if (g.buttonEast.wasPressedThisFrame) return true;
            var k = Keyboard.current;
            return k != null && (k.escapeKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame);
        }

        void Header(RectTransform s, string title, string sub)
        {
            Ui.Panel(s, new Color(0.01f, 0.015f, 0.03f, 0.74f), Vector2.zero, Vector2.one);
            Ui.Label(s, "OmanLNG  ×  Otech SOC  ·  CYBER SECURITY WEEK", 26, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 0.92f), new Vector2(1, 0.97f));
            Ui.Label(s, title, 76, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.82f), new Vector2(1, 0.92f), default, default, Fonts.Title);
            if (!string.IsNullOrEmpty(sub))
                Ui.Label(s, sub, 28, Palette.Text, TextAnchor.MiddleCenter, new Vector2(0, 0.77f), new Vector2(1, 0.82f));
        }

        static void Footer(RectTransform s, string text)
        {
            Ui.Panel(s, new Color(0, 0, 0, 0.5f), new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 64), "FooterBar");
            Ui.Label(s, text, 26, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 64));
        }

        static string ControllerStatus()
        {
            int n = Gamepad.all.Count;
            return n == 0 ? Palette.Paint("No controllers found · keyboard works too", Palette.Bad)
                 : Palette.Paint($"{n} controller{(n == 1 ? "" : "s")} connected", Palette.Good);
        }

        // ------------------------------------------------------------------ main menu

        IEnumerator MainMenu()
        {
            yield return null; // never react to the press that closed the previous screen
            _plan = null;
            InMatch = false;
            OrbitCamera(true);
            while (true)
            {
                State = "title";
                var s = NewScreen("Title");
                Ui.Panel(s, new Color(0.01f, 0.015f, 0.03f, 0.6f), Vector2.zero, Vector2.one);
                Ui.Label(s, "OmanLNG  ×  Otech SOC  ·  CYBER SECURITY WEEK", 28, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 0.9f), new Vector2(1, 0.95f));
                Ui.Label(s, $"{Palette.Paint("BREACH", Palette.Red)} {Palette.Paint("POINT", Palette.Blue)}", 130, Color.white, TextAnchor.MiddleCenter,
                    new Vector2(0, 0.75f), new Vector2(1, 0.9f), default, default, Fonts.Title);
                Ui.Label(s, "Red vs Blue · three cyber battles · roles swap every round", 30, Palette.Text, TextAnchor.MiddleCenter, new Vector2(0, 0.7f), new Vector2(1, 0.75f));

                var box = Ui.Box(s, new Vector2(0.29f, 0.385f), new Vector2(760, 600), Palette.Gold, "Menu");
                var menu = new MenuList(box.transform, MainItems.Length, i => MainItems[i], i => MainDescs[i], top: -34f, rowH: 110f);
                menu.Index = Mathf.Clamp(MenuSelection, 0, MainItems.Length - 1);
                menu.Refresh();
                BuildLeaderboardPanel(s, new Vector2(0.71f, 0.385f));
                var status = Ui.Label(s, "", 24, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 64), new Vector2(0, 100));
                Footer(s, "↑ ↓  choose   ·   A / ✕  select   ·   B / ○  back   ·   Keyboard: arrows + Enter   ·   Start / Esc pauses a match");

                int chosen;
                PlayerControls by;
                while (true)
                {
                    MenuSelection = menu.Index;
                    status.text = ControllerStatus();
                    chosen = PollMenu(menu, out by);
                    if (chosen >= 0) break;
                    yield return null;
                }
                MenuSelection = chosen;

                switch (chosen)
                {
                    case 0:
                        _plan = AllGames();
                        _starter = by;
                        yield break;
                    case 1:
                    {
                        int pick = -1;
                        yield return QuickPlay(i => pick = i);
                        if (pick >= 0)
                        {
                            _plan = new List<int> { pick };
                            _starter = _quickBy ?? by;
                            yield break;
                        }
                        break;
                    }
                    case 2: yield return HowToPlay(); break;
                    case 3: yield return Options(); break;
                    default: yield return ControllerTest(); break;
                }
            }
        }

        PlayerControls _quickBy;

        IEnumerator QuickPlay(Action<int> picked)
        {
            yield return null;
            State = "menu:quick";
            var s = NewScreen("QuickPlay");
            Header(s, "QUICK PLAY", "One game, Red vs Blue. Roles still swap at half-time.");
            var box = Ui.Box(s, new Vector2(0.5f, 0.45f), new Vector2(980, 480), Palette.Gold, "List");
            var items = new List<string>(GameNames) { "BACK" };
            var descs = new List<string>(GameBlurbs) { "Return to the main menu" };
            var menu = new MenuList(box.transform, items.Count, i => items[i], i => descs[i], top: -30f, rowH: 108f);
            Footer(s, "↑ ↓  choose   ·   A / ✕  play   ·   B / ○  back");
            while (true)
            {
                int r = PollMenu(menu, out var by);
                if (r == -1 || r == items.Count - 1) yield break;
                if (r >= 0) { _quickBy = by; picked(r); yield break; }
                yield return null;
            }
        }

        IEnumerator HowToPlay()
        {
            yield return null;
            State = "menu:howto";
            var s = NewScreen("HowToPlay");
            Header(s, "HOW TO PLAY", "Two players: RED and BLUE. Every game has two halves and the roles swap.");
            string[] titles = GameNames;
            string[] bodies =
            {
                $"{Palette.Paint("ATTACKER", Palette.Red)} fires 4 attack types at the OmanLNG servers.\n\n" +
                $"{Palette.Paint("DEFENDER", Palette.Blue)} picks the matching defense:\n" +
                "A  Email filter ← Phishing\nB  Antivirus ← Malware\nX  Rate limiter ← DDoS\nY  MFA ← Password attack\n\n" +
                Palette.Paint("Lesson: each attack needs the right defense.", Palette.Gold),

                $"{Palette.Paint("ATTACKER", Palette.Red)} pushes the DATA into the EXFIL portal.\n\n" +
                $"{Palette.Paint("DEFENDER", Palette.Blue)} returns it to the vault:\n" +
                "X  Encrypt: stolen data is useless\nY  DLP lock: seal the exit\n\n" +
                "Both: drive into the data, A to kick, RT to boost.\n\n" +
                Palette.Paint("Lesson: encryption and DLP stop data theft.", Palette.Gold),

                "Race 3 laps. At each fork, read the links painted on the lanes.\n\n" +
                $"{Palette.Paint("Real omanlng.com", Palette.Good)} = boost\n{Palette.Paint("Look-alike", Palette.Bad)} = PHISHED, you stall\n\n" +
                $"{Palette.Paint("PHISHER", Palette.Red)}  X drops bait\n{Palette.Paint("TARGET", Palette.Blue)}  X reports bait\n\n" +
                Palette.Paint("Lesson: check the real domain before you click.", Palette.Gold),
            };
            for (int i = 0; i < 3; i++)
            {
                var box = Ui.Box(s, new Vector2(0.18f + i * 0.32f, 0.43f), new Vector2(580, 600), i == 0 ? Palette.Red : i == 1 ? Palette.Gold : Palette.Blue, "Game" + i);
                Ui.Label(box.transform, $"{i + 1}", 30, Palette.Muted, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -60), new Vector2(-20, -16), Fonts.Title);
                Ui.Label(box.transform, titles[i], 36, Color.white, TextAnchor.UpperCenter, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -70), new Vector2(-20, -18), Fonts.Title);
                Ui.Label(box.transform, bodies[i], 27, Palette.Text, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(1, 1), new Vector2(30, 20), new Vector2(-24, -90), Fonts.Medium);
            }
            Footer(s, "Steer: left stick   ·   Boost: RT / R1   ·   Pause: Start / Options   ·   Press A / ✕ or B / ○ to go back");
            float el = 0f;
            while (true)
            {
                el += Time.unscaledDeltaTime;
                if (el > 0.3f && (AnyMenuBack() || ConfirmingDevice() != null)) { Sfx.Play("confirm"); yield break; }
                yield return null;
            }
        }

        IEnumerator Options()
        {
            yield return null;
            State = "menu:options";
            var s = NewScreen("Options");
            Header(s, "OPTIONS", "Saved on this computer.");
            var box = Ui.Box(s, new Vector2(0.5f, 0.44f), new Vector2(980, 560), Palette.Gold, "List");
            string Label(int i) => i switch
            {
                0 => $"SOUND   ◀ {(Settings.Sound ? "ON" : "OFF")} ▶",
                1 => $"RUMBLE   ◀ {(Settings.Rumble ? "ON" : "OFF")} ▶",
                2 => $"MATCH LENGTH   ◀ {Settings.LengthNames[Settings.Length]} ▶",
                3 => $"CPU OPPONENT   ◀ {Settings.CpuNames[Settings.Cpu]} ▶",
                _ => "BACK",
            };
            string Desc(int i) => i switch
            {
                0 => "Music and sound effects",
                1 => "Controller vibration",
                2 => $"{Settings.HalfSeconds:0} seconds per half (each game has 2 halves)",
                3 => Settings.Cpu == 0 ? "Slower and makes more mistakes" : Settings.Cpu == 2 ? "Fast and rarely fooled" : "A fair fight for most visitors",
                _ => "Return to the main menu",
            };
            var menu = new MenuList(box.transform, 5, Label, Desc, top: -30f, rowH: 104f);
            Footer(s, "↑ ↓  choose   ·   ◀ ▶ or A / ✕  change   ·   B / ○  back");

            void Change(int row, int dir)
            {
                switch (row)
                {
                    case 0: Settings.Sound = !Settings.Sound; break;
                    case 1: Settings.Rumble = !Settings.Rumble; break;
                    case 2: Settings.Length = (Settings.Length + dir + Settings.LengthSeconds.Length) % Settings.LengthSeconds.Length; break;
                    case 3: Settings.Cpu = (Settings.Cpu + dir + Settings.CpuNames.Length) % Settings.CpuNames.Length; break;
                    default: return;
                }
                Settings.Save();
                Sfx.Play("switch");
                if (row == 1 && Settings.Rumble) foreach (var g in Gamepad.all) Haptics.Pulse(new PlayerControls(ControlKind.Gamepad, g), 0.3f, 0.6f, 0.25f);
                menu.Refresh();
            }

            while (true)
            {
                int r = PollMenu(menu, out _, h => Change(menu.Index, h));
                if (r == -1 || r == 4) yield break;
                if (r >= 0) Change(r, 1);
                yield return null;
            }
        }

        // ------------------------------------------------------------------ controller test

        IEnumerator ControllerTest()
        {
            yield return null;
            State = "menu:controllers";
            var s = NewScreen("Controllers");
            Header(s, "CONTROLLER TEST", "Press buttons to light them up. A / ✕ rumbles. Hold B / ○ (or press Esc) to go back.");
            var boxes = new Dictionary<Gamepad, Text>();
            var holder = Ui.Rect(s, "Pads", new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.75f));
            var empty = Ui.Label(holder, "No controllers connected.\nPlug in an Xbox or PS5 controller (USB or Bluetooth).", 36, Palette.Muted, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            Footer(s, "Every connected controller shows here · PS5: the light bar cycles colours");
            int lastCount = -1;
            float holdB = 0f, el = 0f;
            var colors = new[] { Palette.Red, Palette.Blue, Palette.Gold, Palette.Good };
            while (true)
            {
                el += Time.unscaledDeltaTime;
                var pads = Gamepad.all;
                if (pads.Count != lastCount)
                {
                    lastCount = pads.Count;
                    foreach (Transform c in holder) if (c.name.StartsWith("Pad")) Destroy(c.gameObject);
                    boxes.Clear();
                    for (int i = 0; i < pads.Count && i < 4; i++)
                    {
                        int shown = Mathf.Min(pads.Count, 4);
                        float w = 1f / Mathf.Max(shown, 2);
                        float x0 = 0.5f - shown * w / 2f + i * w;
                        var box = Ui.Panel(holder, Palette.Panel, new Vector2(x0, 0), new Vector2(x0 + w, 1), new Vector2(10, 0), new Vector2(-10, 0), "Pad" + i);
                        Ui.Panel(box.transform, colors[i], new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -6), Vector2.zero, "Edge");
                        boxes[pads[i]] = Ui.Label(box.transform, "", 28, Palette.Text, TextAnchor.UpperLeft, Vector2.zero, Vector2.one, new Vector2(24, 20), new Vector2(-20, -22), Fonts.Mono);
                        boxes[pads[i]].fontSize = 34;
                    }
                    empty.enabled = pads.Count == 0;
                }

                bool anyB = false;
                int idx = 0;
                foreach (var kv in boxes)
                {
                    var g = kv.Key;
                    var pc = new PlayerControls(ControlKind.Gamepad, g);
                    string B(string name, bool on) => on ? Palette.Paint($"[{name}]", Palette.Gold) : Palette.Paint($" {name} ", Palette.A(Palette.Muted, 0.6f));
                    bool ps = pc.IsPlayStation;
                    var ls = g.leftStick.ReadValue();
                    var rs = g.rightStick.ReadValue();
                    kv.Value.text =
                        $"{Palette.Paint(g.displayName, colors[idx])}\n{Palette.Paint(ps ? "PlayStation layout" : "Xbox layout", Palette.Muted)}\n\n" +
                        $"{B(ps ? "✕" : "A", g.buttonSouth.isPressed)} {B(ps ? "○" : "B", g.buttonEast.isPressed)} {B(ps ? "□" : "X", g.buttonWest.isPressed)} {B(ps ? "△" : "Y", g.buttonNorth.isPressed)}\n" +
                        $"{B(ps ? "L1" : "LB", g.leftShoulder.isPressed)} {B(ps ? "R1" : "RB", g.rightShoulder.isPressed)} {B(ps ? "L2" : "LT", g.leftTrigger.isPressed)} {B(ps ? "R2" : "RT", g.rightTrigger.isPressed)}\n" +
                        $"{B("◀", g.dpad.left.isPressed)} {B("▶", g.dpad.right.isPressed)} {B("▲", g.dpad.up.isPressed)} {B("▼", g.dpad.down.isPressed)}\n" +
                        $"{B(ps ? "OPTIONS" : "START", g.startButton.isPressed)}\n\n" +
                        $"L stick {ls.x,5:0.00} {ls.y,5:0.00}\nR stick {rs.x,5:0.00} {rs.y,5:0.00}\n\n" +
                        (g.buttonEast.isPressed ? Palette.Paint("Hold to go back…", Palette.Gold) : "");
                    if (g.buttonSouth.wasPressedThisFrame) { Haptics.Pulse(pc, 0.5f, 0.9f, 0.3f); Sfx.Play("confirm"); }
                    if (g is DualShockGamepad) Haptics.LightBar(pc, Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.2f + idx * 0.25f, 1f), 1f, 1f));
                    if (g.buttonEast.isPressed) anyB = true;
                    idx++;
                }
                holdB = anyB ? holdB + Time.unscaledDeltaTime : 0f;
                var k = Keyboard.current;
                if (holdB > 1.0f || (el > 0.3f && k != null && (k.escapeKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame)))
                {
                    Sfx.Play("pause");
                    Haptics.StopAll();
                    yield break;
                }
                yield return null;
            }
        }
    }
}
