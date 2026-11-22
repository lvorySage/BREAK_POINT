using System.Collections.Generic;
using UnityEngine;

namespace BreachPoint
{
    public enum Team { Red, Blue }
    public enum Role { Attacker, Defender }

    public static class Palette
    {
        public static readonly Color Red = new Color(0.98f, 0.23f, 0.36f);
        public static readonly Color Blue = new Color(0.22f, 0.74f, 0.97f);
        public static readonly Color Gold = new Color(1f, 0.75f, 0.14f);
        public static readonly Color Ink = new Color(0.02f, 0.03f, 0.06f);
        public static readonly Color Panel = new Color(0.03f, 0.05f, 0.1f, 0.86f);
        public static readonly Color Text = new Color(0.92f, 0.94f, 0.98f);
        public static readonly Color Muted = new Color(0.58f, 0.64f, 0.75f);
        public static readonly Color Good = new Color(0.25f, 0.9f, 0.45f);
        public static readonly Color Bad = new Color(1f, 0.3f, 0.3f);

        public static Color Of(Team t) => t == Team.Red ? Red : Blue;
        public static Color Hdr(Color c, float i) => new Color(c.r * i, c.g * i, c.b * i, c.a);
        public static Color A(Color c, float a) { c.a = a; return c; }
        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
        public static string Paint(string s, Color c) => $"<color={Hex(c)}>{s}</color>";
    }

    /// <summary>One of the two booth players. Team (Red/Blue) is fixed; roles swap inside each mini-game.</summary>
    public sealed class PlayerSlot
    {
        public readonly int Index;
        public readonly Team Team;
        public readonly PlayerControls Controls;

        public PlayerSlot(int index, PlayerControls controls)
        {
            Index = index;
            Team = index == 0 ? Team.Red : Team.Blue;
            Controls = controls;
        }

        /// <summary>Name typed on the name screen (empty = none given).</summary>
        public string PlayerName = "";

        public Color Color => Palette.Of(Team);
        /// <summary>Team name: RED / BLUE.</summary>
        public string Name => Team == Team.Red ? "RED" : "BLUE";
        /// <summary>What players see: their own name, or the team name if they didn't enter one.</summary>
        public string Display => string.IsNullOrEmpty(PlayerName) ? Name : PlayerName;
        public string Tag => Palette.Paint(Display, Color);
    }

    public sealed class MiniGameResult
    {
        public string Game;
        public int Winner = -1;          // player index, -1 = draw
        public float[] Scores = new float[2];
        public string ScoreLabel;        // e.g. "DATA STOLEN"
        public int[] Points = new int[2]; // match points earned in this mini-game (decide who wins the session)
        public string Lesson;

        // ---- cyber awareness (the leaderboard) ----
        /// <summary>Awareness points: earned only by good security decisions (right defense, encrypting, real links, reporting).</summary>
        public int[] Aware = new int[2];
        /// <summary>Security decisions this player got right / wrong (accuracy = right / (right + wrong)).</summary>
        public int[] Right = new int[2];
        public int[] Wrong = new int[2];
    }

    /// <summary>One booth session: two players through the gauntlet.</summary>
    public sealed class Session
    {
        public readonly PlayerSlot[] Players = new PlayerSlot[2];
        public readonly int[] Wins = new int[2];
        public readonly List<MiniGameResult> Results = new List<MiniGameResult>();

        public bool Ready => Players[0] != null && Players[1] != null;

        public void Record(MiniGameResult r)
        {
            Results.Add(r);
            if (r.Winner >= 0) Wins[r.Winner]++;
        }

        public int Champion
        {
            get
            {
                if (Wins[0] != Wins[1]) return Wins[0] > Wins[1] ? 0 : 1;
                int p0 = TotalPoints(0), p1 = TotalPoints(1);
                return p0 == p1 ? -1 : p0 > p1 ? 0 : 1;
            }
        }

        /// <summary>Right security decisions as a percentage of all decisions (0 if none were made).</summary>
        public int Accuracy(int player)
        {
            int right = 0, wrong = 0;
            foreach (var r in Results) { right += r.Right[player]; wrong += r.Wrong[player]; }
            return right + wrong == 0 ? 0 : Mathf.RoundToInt(100f * right / (right + wrong));
        }

        /// <summary>
        /// The leaderboard score: "most cyber-aware player". Awareness points from every game,
        /// plus a bonus equal to the accuracy percentage (so careful players beat button-mashers).
        /// </summary>
        public int AwarenessScore(int player)
        {
            int sum = 0;
            foreach (var r in Results) sum += r.Aware[player];
            return sum > 0 ? sum + Accuracy(player) : 0;
        }

        public int TotalPoints(int player)
        {
            int sum = 0;
            foreach (var r in Results) sum += r.Points[player];
            return sum;
        }
    }

    public static class Fonts
    {
        static Font _bold, _medium, _title, _mono;
        static Font Load(string n) => Resources.Load<Font>("Fonts/" + n) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font Bold => _bold != null ? _bold : (_bold = Load("Rajdhani-Bold"));
        public static Font Medium => _medium != null ? _medium : (_medium = Load("Rajdhani-Medium"));
        public static Font Title => _title != null ? _title : (_title = Load("Orbitron-Variable"));
        public static Font Mono => _mono != null ? _mono : (_mono = Load("ShareTechMono-Regular"));
    }
}
