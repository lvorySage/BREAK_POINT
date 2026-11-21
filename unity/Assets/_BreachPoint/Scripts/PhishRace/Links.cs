namespace BreachPoint
{
    /// <summary>Real vs look-alike links painted on the Phish Race fork lanes. The clue says what gave the fake away.</summary>
    public static class Links
    {
        public readonly struct Pair
        {
            public readonly string Real, Fake, Clue;
            public Pair(string real, string fake, string clue) { Real = real; Fake = fake; Clue = clue; }
        }

        /// <summary>Lap 1: obvious fakes (wrong ending, free email, extra words).</summary>
        public static readonly Pair[] Easy =
        {
            new Pair("omanlng.com", "omanlng-login.net", "extra words and the wrong ending"),
            new Pair("it@omanlng.com", "it.helpdesk@gmail.com", "IT never writes from a free Gmail account"),
            new Pair("ceo@omanlng.com", "ceo.omanlng@outlook.com", "managers don't email from personal accounts"),
            new Pair("omanlng.com/benefits", "bit.ly/0mn-bonus", "short links hide where they really go"),
            new Pair("hr@omanlng.com", "hr@omanlng.co", ".co is not .com"),
        };

        /// <summary>Lap 2: look-alikes that need a closer read.</summary>
        public static readonly Pair[] Medium =
        {
            new Pair("mail.omanlng.com", "omanlng.com.verify-mail.co", "the real domain is the part just before the first /"),
            new Pair("https://omanlng.com", "http://0manlng.com", "a zero instead of an O"),
            new Pair("portal.omanlng.com", "omanlng.portal-login.ru", "the real domain is at the end"),
            new Pair("payroll@omanlng.com", "payro11@omanlng.com", "ones instead of Ls"),
            new Pair("teams.microsoft.com", "teams-microsoft.support", "a look-alike brand domain"),
            new Pair("sharepoint.omanlng.com", "omanlng-sharepoint.web.app", "a free hosting site, not OmanLNG"),
        };

        /// <summary>Lap 3: the tricks real attackers use (@ trick, rn for m, extra domain tacked on, one dash).</summary>
        public static readonly Pair[] Hard =
        {
            new Pair("omanlng.com/hr/benefits", "omanlng.com@secure-hr.net", "everything before @ is ignored: the site is secure-hr.net"),
            new Pair("login.microsoftonline.com", "login.microsoftonline.com.sso.ru", "the domain is what's at the end: sso.ru"),
            new Pair("sharepoint.omanlng.com/finance", "sharepoint.omanlng.com.files.io", "it ends in files.io, not omanlng.com"),
            new Pair("omanlng.com/careers", "omanlng.corn/careers", "\"rn\" pretending to be an \"m\""),
            new Pair("vpn.omanlng.com", "vpn-omanlng.com", "one dash makes it a completely different domain"),
            new Pair("omanlng.com/payroll", "omanlng-com.payroll.net", "the real domain is payroll.net"),
            new Pair("docs.omanlng.com/policy.pdf", "docs.omanlng.com.pdf-view.app", "it ends in pdf-view.app"),
        };

        public static readonly Pair[][] Tiers = { Easy, Medium, Hard };
        public static readonly string[] TierNames = { "EASY", "TRICKY", "EXPERT" };

        /// <summary>Lures the phisher drops on the track.</summary>
        public static readonly string[] Bait =
        {
            "FREE GIFT CARD!", "YOU WON A PRIZE!", "PASSWORD EXPIRES TODAY", "URGENT: CLICK NOW",
            "PAYROLL_UPDATE.exe", "PARCEL ON HOLD", "BONUS APPROVED!", "VERIFY YOUR ACCOUNT",
        };
    }
}
