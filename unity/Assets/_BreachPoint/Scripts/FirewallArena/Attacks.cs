using UnityEngine;

namespace BreachPoint
{
    /// <summary>Attack types share their index with the controller action buttons (A/✕ = 0, B/○ = 1, X/□ = 2).</summary>
    public enum AttackType { Phishing = 0, Malware = 1, DDoS = 2 }

    public static class Attacks
    {
        public static readonly AttackType[] All = { AttackType.Phishing, AttackType.Malware, AttackType.DDoS };

        public static string Name(AttackType t) => t switch
        {
            AttackType.Phishing => "PHISHING",
            AttackType.Malware => "MALWARE",
            _ => "DDoS",
        };

        public static string Defense(AttackType t) => t switch
        {
            AttackType.Phishing => "EMAIL FILTER",
            AttackType.Malware => "ANTIVIRUS",
            _ => "RATE LIMITER",
        };

        // Matches the Xbox face-button colours: A green, B red, X blue.
        public static Color Color(AttackType t) => t switch
        {
            AttackType.Phishing => new Color(0.25f, 1f, 0.45f),
            AttackType.Malware => new Color(1f, 0.38f, 0.16f),
            _ => new Color(0.42f, 0.5f, 1f),
        };

        /// <summary>% of the half's data stolen when it reaches a server (DDoS: per pellet, 5 pellets).</summary>
        public static float Value(AttackType t) => t switch
        {
            AttackType.Phishing => 6f,
            AttackType.Malware => 8f,
            _ => 2.5f,
        };

        public static float Speed(AttackType t) => t switch
        {
            AttackType.Phishing => 9f,
            AttackType.Malware => 19f,
            _ => 13f,
        };

        public static float Cooldown(AttackType t) => t switch
        {
            AttackType.Phishing => 1.1f,
            AttackType.Malware => 0.9f,
            _ => 1.6f,
        };

        public static string Lesson(AttackType t) => t switch
        {
            AttackType.Phishing =>
                "Phishing uses fake messages to trick people. Email filters catch a lot of it, but you are the last filter: " +
                "check the sender, hover over links before clicking, and report anything odd.",
            AttackType.Malware =>
                "Malware is harmful software. Antivirus / endpoint protection blocks it: keep it switched on and up to date, " +
                "and never open files or USB sticks you weren't expecting.",
            _ =>
                "A DDoS floods a service with junk traffic to knock it offline. Rate limiting and DDoS protection " +
                "keep the service running for real users.",
        };
    }
}
