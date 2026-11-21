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

        public static readonly Pair[] All =
        {
            new Pair("omanlng.com", "omanlng-login.net", "extra words and the wrong ending"),
            new Pair("mail.omanlng.com", "omanlng.com.verify-mail.co", "the real domain is the part just before the first /"),
            new Pair("it@omanlng.com", "it.helpdesk@gmail.com", "IT never writes from a free Gmail account"),
            new Pair("https://omanlng.com", "http://0manlng.com", "a zero instead of an O"),
            new Pair("hr@omanlng.com", "hr@omanlng.co", ".co is not .com"),
            new Pair("portal.omanlng.com", "omanlng.portal-login.ru", "the real domain is at the end"),
            new Pair("payroll@omanlng.com", "payro11@omanlng.com", "ones instead of Ls"),
            new Pair("omanlng.com/benefits", "bit.ly/0mn-bonus", "short links hide where they really go"),
            new Pair("teams.microsoft.com", "teams-microsoft.support", "a look-alike brand domain"),
            new Pair("ceo@omanlng.com", "ceo.omanlng@outlook.com", "managers don't email from personal accounts"),
            new Pair("sharepoint.omanlng.com", "omanlng-sharepoint.web.app", "a free hosting site, not OmanLNG"),
            new Pair("omanlng.com/login", "omanlng.com-login.info", "the domain really ends in .info"),
        };

        /// <summary>Lures the phisher drops on the track.</summary>
        public static readonly string[] Bait =
        {
            "FREE GIFT CARD!", "YOU WON A PRIZE!", "PASSWORD EXPIRES TODAY", "URGENT: CLICK NOW",
            "PAYROLL_UPDATE.exe", "PARCEL ON HOLD", "BONUS APPROVED!", "VERIFY YOUR ACCOUNT",
        };
    }
}
