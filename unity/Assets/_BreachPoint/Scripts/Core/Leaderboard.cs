using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BreachPoint
{
    /// <summary>
    /// Booth leaderboard, stored as JSON on this machine (survives restarts).
    /// Score = leaderboard points across the gauntlet (data stolen + 5 per block).
    /// </summary>
    public static class Leaderboard
    {
        public const int Size = 10;

        [Serializable]
        public class Entry
        {
            public string Initials;
            public string Team;
            public int Score;
            public string Date;
        }

        [Serializable]
        class Data
        {
            public List<Entry> Entries = new List<Entry>();
        }

        /// <summary>Tests point this at a temp file.</summary>
        public static string PathOverride;
        static string FilePath => PathOverride ?? Path.Combine(Application.persistentDataPath, "breachpoint_leaderboard.json");

        public static List<Entry> Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var d = JsonUtility.FromJson<Data>(File.ReadAllText(FilePath));
                    if (d?.Entries != null) return d.Entries;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BreachPoint] Leaderboard unreadable, starting fresh: " + e.Message);
            }
            return new List<Entry>();
        }

        static void Save(List<Entry> entries)
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(new Data { Entries = entries }, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BreachPoint] Could not save leaderboard: " + e.Message);
            }
        }

        public static bool Qualifies(int score)
        {
            if (score <= 0) return false;
            var e = Load();
            return e.Count < Size || score > e[e.Count - 1].Score;
        }

        /// <summary>Adds an entry; returns its 1-based rank (0 if it didn't make the board).</summary>
        public static int Add(string initials, Team team, int score)
        {
            var list = Load();
            var entry = new Entry
            {
                Initials = initials,
                Team = team == Team.Red ? "RED" : "BLUE",
                Score = score,
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            };
            list.Add(entry);
            list = list.OrderByDescending(x => x.Score).ToList();   // stable: on a tie the earlier score stays ahead
            if (list.Count > Size) list.RemoveRange(Size, list.Count - Size);
            Save(list);
            int rank = list.IndexOf(entry);
            return rank < 0 ? 0 : rank + 1;
        }

        public static void Reset() => Save(new List<Entry>());
    }
}
