using UnityEngine;

namespace BreachPoint
{
    /// <summary>
    /// Attack types share their index with the controller face buttons:
    /// A/✕ = 0, B/○ = 1, X/□ = 2, Y/△ = 3. Colours match the Xbox buttons.
    /// </summary>
    public enum AttackType { Phishing = 0, Malware = 1, DDoS = 2, Password = 3 }

    public static class Attacks
    {
        public const int Count = 4;
        public static readonly AttackType[] All = { AttackType.Phishing, AttackType.Malware, AttackType.DDoS, AttackType.Password };
        public static readonly string[] PadLabels = { "A/✕", "B/○", "X/□", "Y/△" };

        public static string Name(AttackType t) => t switch
        {
            AttackType.Phishing => "PHISHING",
            AttackType.Malware => "MALWARE",
            AttackType.DDoS => "DDoS",
            _ => "PASSWORD ATTACK",
        };

        public static string Defense(AttackType t) => t switch
        {
            AttackType.Phishing => "EMAIL FILTER",
            AttackType.Malware => "ANTIVIRUS",
            AttackType.DDoS => "RATE LIMITER",
            _ => "MFA",
        };

        public static string Behaviour(AttackType t) => t switch
        {
            AttackType.Phishing => "slow, follows its target",
            AttackType.Malware => "fast single shot",
            AttackType.DDoS => "wide burst",
            _ => "rapid chain of login attempts",
        };

        public static Color Color(AttackType t) => t switch
        {
            AttackType.Phishing => new Color(0.25f, 1f, 0.45f),
            AttackType.Malware => new Color(1f, 0.38f, 0.16f),
            AttackType.DDoS => new Color(0.42f, 0.5f, 1f),
            _ => new Color(1f, 0.84f, 0.18f),
        };

        /// <summary>% of the half's data stolen per projectile that reaches a server.</summary>
        public static float Value(AttackType t) => t switch
        {
            AttackType.Phishing => 6f,
            AttackType.Malware => 8f,
            AttackType.DDoS => 2.5f,   // 5 pellets
            _ => 2.5f,                 // 3 attempts
        };

        public static float Speed(AttackType t) => t switch
        {
            AttackType.Phishing => 9f,
            AttackType.Malware => 19f,
            AttackType.DDoS => 13f,
            _ => 15f,
        };

        public static float Cooldown(AttackType t) => t switch
        {
            AttackType.Phishing => 1.1f,
            AttackType.Malware => 0.9f,
            AttackType.DDoS => 1.6f,
            _ => 1.3f,
        };

        public static string Lesson(AttackType t) => t switch
        {
            AttackType.Phishing =>
                "Phishing uses fake messages to trick people. Email filters catch a lot of it, but you are the last filter: " +
                "check the sender, hover over links before clicking, and report anything odd.",
            AttackType.Malware =>
                "Malware is harmful software. Antivirus / endpoint protection blocks it: keep it switched on and up to date, " +
                "and never open files or USB sticks you weren't expecting.",
            AttackType.DDoS =>
                "A DDoS floods a service with junk traffic to knock it offline. Rate limiting and DDoS protection " +
                "keep the service running for real users.",
            _ =>
                "Attackers try leaked or guessed passwords again and again. MFA stops them even when the password is right: " +
                "turn it on everywhere, and never approve a login request you didn't start.",
        };
    }
}
