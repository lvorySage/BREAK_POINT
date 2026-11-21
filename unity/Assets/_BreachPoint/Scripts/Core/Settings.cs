using UnityEngine;

namespace BreachPoint
{
    /// <summary>
    /// Booth options from the main menu, saved in PlayerPrefs. Mini-games copy these in Run() (not in Init),
    /// so unit tests that build a game directly always get the defaults.
    /// </summary>
    public static class Settings
    {
        public static readonly string[] LengthNames = { "SHORT", "NORMAL", "LONG" };
        public static readonly float[] LengthSeconds = { 45f, 60f, 90f };
        public static readonly string[] CpuNames = { "EASY", "NORMAL", "HARD" };

        public static bool Sound = true;
        public static bool Rumble = true;
        public static int Length = 1;   // index into LengthSeconds
        public static int Cpu = 1;      // 0 easy, 1 normal, 2 hard

        public static float HalfSeconds => LengthSeconds[Mathf.Clamp(Length, 0, LengthSeconds.Length - 1)];

        public static void Load()
        {
            Sound = PlayerPrefs.GetInt("bp.sound", 1) == 1;
            Rumble = PlayerPrefs.GetInt("bp.rumble", 1) == 1;
            Length = Mathf.Clamp(PlayerPrefs.GetInt("bp.length", 1), 0, LengthSeconds.Length - 1);
            Cpu = Mathf.Clamp(PlayerPrefs.GetInt("bp.cpu", 1), 0, CpuNames.Length - 1);
            Apply();
        }

        public static void Save()
        {
            PlayerPrefs.SetInt("bp.sound", Sound ? 1 : 0);
            PlayerPrefs.SetInt("bp.rumble", Rumble ? 1 : 0);
            PlayerPrefs.SetInt("bp.length", Length);
            PlayerPrefs.SetInt("bp.cpu", Cpu);
            PlayerPrefs.Save();
            Apply();
        }

        static void Apply()
        {
            Sfx.Muted = !Sound;
            AudioListener.volume = Sound ? 1f : 0f;
            Haptics.Enabled = Rumble;
        }
    }
}
