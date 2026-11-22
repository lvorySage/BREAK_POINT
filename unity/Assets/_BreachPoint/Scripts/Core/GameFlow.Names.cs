using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BreachPoint
{
    /// <summary>
    /// Name screen, right after Join: each human player types a name on an on-screen keyboard with their own
    /// controller (both at once). The name is shown in every game and goes on the Most Cyber Aware leaderboard.
    /// </summary>
    public sealed partial class GameFlow
    {
        public const float NameTimeoutSeconds = 75f;

        static readonly string[][] Keys =
        {
            new[] { "A", "B", "C", "D", "E", "F", "G" },
            new[] { "H", "I", "J", "K", "L", "M", "N" },
            new[] { "O", "P", "Q", "R", "S", "T", "U" },
            new[] { "V", "W", "X", "Y", "Z", "-", "." },
            new[] { "0", "1", "2", "3", "4", "5", "6" },
            new[] { "7", "8", "9", "SPACE", "DEL", "DONE" },
        };

        sealed class NamePad
        {
            public PlayerSlot Player;
            public string Text = "";
            public int Row, Col;
            public bool Done;
            public float NavCd;
            public Text Field, Status;
            public Image[,] Cells;
            public Text[,] Labels;
        }

        IEnumerator EnterNames()
        {
            yield return null;
            var pads = new System.Collections.Generic.List<NamePad>();
            for (int i = 0; i < 2; i++)
            {
                var p = Session.Players[i];
                if (p.Controls.IsCpu) { p.PlayerName = "CPU"; continue; }
                pads.Add(new NamePad { Player = p });
            }
            if (pads.Count == 0) yield break;

            State = "names";
            var s = NewScreen("Names");
            Ui.Panel(s, new Color(0.01f, 0.015f, 0.03f, 0.72f), Vector2.zero, Vector2.one);
            Ui.Logo(s, "OmanLNG_Logo", new Vector2(0.06f, 0.89f), 130f);
            Ui.Label(s, "ENTER YOUR NAME", 72, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.85f), new Vector2(1, 0.95f), default, default, Fonts.Title);
            Ui.Label(s, "Your name goes on the MOST CYBER AWARE leaderboard.", 30, Palette.Text, TextAnchor.MiddleCenter, new Vector2(0, 0.8f), new Vector2(1, 0.85f));
            Footer(s, "Stick / D-pad  move   ·   A / ✕  type   ·   B / ○  delete   ·   Y / △ or START  done   (keyboard: your move keys, F / 1 type, G / 2 delete, T / 4 done)");

            for (int k = 0; k < pads.Count; k++)
            {
                var np = pads[k];
                float x = pads.Count == 1 ? 0.5f : (np.Player.Index == 0 ? 0.27f : 0.73f);
                var box = Ui.Box(s, new Vector2(x, 0.43f), new Vector2(820, 700), np.Player.Color, "NamePad" + k);
                var t = box.transform;
                Ui.Label(t, $"PLAYER {np.Player.Index + 1} · {np.Player.Name} TEAM", 32, np.Player.Color, TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(0, -12), Fonts.Title);
                var field = Ui.Panel(t, new Color(0, 0, 0, 0.55f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -150), new Vector2(-40, -72), "Field");
                np.Field = Ui.Fill(field.transform, "", 56, Color.white, TextAnchor.MiddleCenter, Fonts.Mono);
                np.Cells = new Image[Keys.Length, 7];
                np.Labels = new Text[Keys.Length, 7];
                for (int r = 0; r < Keys.Length; r++)
                {
                    float cx = 40;
                    for (int c = 0; c < Keys[r].Length; c++)
                    {
                        float w = Keys[r][c].Length > 1 ? 130 : 98;
                        float y = -172 - r * 78;
                        var cell = Ui.Panel(t, new Color(1, 1, 1, 0.06f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(cx, y - 70), new Vector2(cx + w, y), "Key");
                        np.Cells[r, c] = cell;
                        np.Labels[r, c] = Ui.Fill(cell.transform, Keys[r][c], Keys[r][c].Length > 1 ? 28 : 38, Palette.Text, TextAnchor.MiddleCenter, Fonts.Title);
                        cx += w + 8;
                    }
                }
                np.Status = Ui.Label(t, "", 26, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 10), new Vector2(0, 50));
            }

            float el = 0f;
            while (el < NameTimeoutSeconds)
            {
                el += Time.unscaledDeltaTime;
                bool allDone = true;
                foreach (var np in pads)
                {
                    if (!np.Done && el > 0.35f) HandleNamePad(np);
                    allDone &= np.Done;
                    RefreshNamePad(np, el);
                }
                if (allDone) break;
                yield return null;
            }
            foreach (var np in pads) np.Player.PlayerName = np.Text.Trim();
            yield return new WaitForSecondsRealtime(0.5f);
            ClearScreen();
        }

        void HandleNamePad(NamePad np)
        {
            var c = np.Player.Controls;
            np.NavCd -= Time.unscaledDeltaTime;
            var mv = c.Move;
            if (mv.magnitude < 0.3f) np.NavCd = 0f;
            else if (np.NavCd <= 0f)
            {
                np.NavCd = 0.18f;
                if (Mathf.Abs(mv.x) > Mathf.Abs(mv.y)) np.Col += mv.x > 0 ? 1 : -1;
                else np.Row += mv.y > 0 ? -1 : 1;
                np.Row = (np.Row + Keys.Length) % Keys.Length;
                int len = Keys[np.Row].Length;
                np.Col = (np.Col % len + len) % len;
                Sfx.Play("switch", 0.4f);
            }
            int rowLen = Keys[np.Row].Length;
            if (np.Col >= rowLen) np.Col = rowLen - 1;

            bool done = c.ActionDown(3) || c.PauseDown;
            if (c.ActionDown(1) && np.Text.Length > 0) { np.Text = np.Text.Substring(0, np.Text.Length - 1); Sfx.Play("switch"); }
            if (c.ActionDown(0))
            {
                string key = Keys[np.Row][np.Col];
                if (key == "DONE") done = true;
                else if (key == "DEL") { if (np.Text.Length > 0) np.Text = np.Text.Substring(0, np.Text.Length - 1); Sfx.Play("switch"); }
                else if (np.Text.Length < Leaderboard.MaxNameLength)
                {
                    np.Text += key == "SPACE" ? (np.Text.Length > 0 && !np.Text.EndsWith(" ") ? " " : "") : key;
                    Sfx.Play("confirm", 0.6f);
                    if (np.Text.Length == Leaderboard.MaxNameLength) { np.Row = Keys.Length - 1; np.Col = Keys[np.Row].Length - 1; }
                }
            }
            if (done)
            {
                np.Done = true;
                np.Text = np.Text.Trim();
                Sfx.Play("join");
                Haptics.Pulse(c, 0.3f, 0.6f, 0.2f);
            }
        }

        void RefreshNamePad(NamePad np, float el)
        {
            bool blink = Mathf.Repeat(Time.unscaledTime, 0.9f) < 0.5f;
            np.Field.text = np.Text.Length == 0 && np.Done ? Palette.Paint(np.Player.Name, Palette.Muted)
                          : np.Text + (np.Done ? "" : blink ? "_" : " ");
            for (int r = 0; r < Keys.Length; r++)
            for (int c = 0; c < Keys[r].Length; c++)
            {
                bool sel = !np.Done && r == np.Row && c == np.Col;
                np.Cells[r, c].color = sel ? Palette.A(np.Player.Color, 0.85f) : new Color(1, 1, 1, Keys[r][c] == "DONE" ? 0.14f : 0.06f);
                np.Labels[r, c].color = sel ? Color.white : Keys[r][c] == "DONE" ? Palette.Good : Palette.Text;
            }
            int left = Mathf.CeilToInt(NameTimeoutSeconds - el);
            np.Status.text = np.Done
                ? Palette.Paint($"✔ READY{(np.Text.Length == 0 ? " (no name: shown as " + np.Player.Name + ")" : "")}", Palette.Good)
                : $"{np.Text.Length}/{Leaderboard.MaxNameLength} letters" + (left <= 15 ? Palette.Paint($"   ·   starting in {left}", Palette.Gold) : "");
        }
    }
}
