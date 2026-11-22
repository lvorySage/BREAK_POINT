using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BreachPoint
{
    /// <summary>
    /// Booth leaderboard, "most cyber-aware player", stored as JSON on this machine (survives restarts).
    /// Score = Session.AwarenessScore (good security decisions + accuracy bonus).
    /// </summary>
    public static class Leaderboard
    {
        public const int Size = 10;

        [Serializable]
        public class Entry
        {
            public string Name;
            public string Team;
            public int Score;
            public int Accuracy;   // % of security decisions that were right
            public string Date;
        }

        [Serializable]
        class Data
        {
            public List<Entry> Entries = new List<Entry>();
        }

        /// <summary>Tests point this at a temp file.</summary>
        public static string PathOverride;
        static string FilePath => PathOverride ?? Path.Combine(Application.persistentDataPath, "breachpoint_awareness.json");

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

        public const int MaxNameLength = 12;

        /// <summary>
        /// Adds a score; returns its 1-based rank (0 if it didn't make the board).
        /// A player's name appears once: a new score only replaces their entry if it is higher.
        /// </summary>
        public static int Add(string name, Team team, int score, int accuracy = 0)
        {
            name = (name ?? "").Trim().ToUpperInvariant();
            if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength);
            var list = Load();
            if (name.Length > 0)
            {
                var old = list.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (old != null)
                {
                    if (old.Score >= score) return list.IndexOf(old) + 1;
                    list.Remove(old);
                }
            }
            var entry = new Entry
            {
                Name = name,
                Team = team == Team.Red ? "RED" : "BLUE",
                Score = score,
                Accuracy = accuracy,
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
