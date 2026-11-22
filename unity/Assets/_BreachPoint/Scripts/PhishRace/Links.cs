namespace BreachPoint
{
    /// <summary>
    /// Phish Race content. Oman LNG's real domain is omanlng.co.om: every real link or sender uses it
    /// (or a genuine service such as microsoft.com); every fake is a look-alike.
    /// </summary>
    public static class Links
    {
        public const string RealDomain = "omanlng.co.om";

        /// <summary>A fork: the phishing scenario shown above it, the real and fake links on the lanes, and the clue.</summary>
        public readonly struct Pair
        {
            public readonly string Context, Real, Fake, Clue;
            public Pair(string context, string real, string fake, string clue) { Context = context; Real = real; Fake = fake; Clue = clue; }
        }

        /// <summary>Lap 1: obvious fakes (wrong ending, free email, extra words).</summary>
        public static readonly Pair[] Easy =
        {
            new Pair("Visit the company website", "omanlng.co.om", "omanlng-login.net", "extra words and the wrong ending"),
            new Pair("Email from IT support", "it@omanlng.co.om", "it.helpdesk@gmail.com", "IT never writes from a free Gmail account"),
            new Pair("Message from the CEO", "ceo@omanlng.co.om", "ceo.omanlng@outlook.com", "managers don't email from personal accounts"),
            new Pair("Your bonus details inside!", "omanlng.co.om/benefits", "bit.ly/0mn-bonus", "short links hide where they really go"),
            new Pair("HR policy update", "hr@omanlng.co.om", "hr@omanlng.com", "our domain is .co.om, not .com"),
        };

        /// <summary>Lap 2: look-alikes that need a closer read.</summary>
        public static readonly Pair[] Medium =
        {
            new Pair("Your mailbox is full", "mail.omanlng.co.om", "omanlng.co.om.verify-mail.co", "the real domain is the end: verify-mail.co"),
            new Pair("Log in to the staff portal", "https://omanlng.co.om", "http://0manlng.co.om", "a zero instead of an O, and no https"),
            new Pair("Payroll: confirm your bank details", "payroll@omanlng.co.om", "payro11@omanlng.co.om", "ones instead of Ls"),
            new Pair("Teams meeting invite", "teams.microsoft.com", "teams-microsoft.support", "a look-alike brand domain"),
            new Pair("A file was shared with you", "sharepoint.omanlng.co.om", "omanlng-sharepoint.web.app", "a free hosting site, not Oman LNG"),
            new Pair("Security alert: verify now", "portal.omanlng.co.om", "omanlng.co.om.portal-login.ru", "it really ends in .ru"),
            new Pair("Shipping schedule update", "omanlng.co.om/shipping", "omanlng.co/shipping", ".co is missing the .om"),
        };

        /// <summary>Lap 3: the tricks real attackers use (@ trick, rn for m, extra domain tacked on, one dash).</summary>
        public static readonly Pair[] Hard =
        {
            new Pair("Benefits enrolment closes today", "omanlng.co.om/hr/benefits", "omanlng.co.om@secure-hr.net", "everything before @ is ignored: the site is secure-hr.net"),
            new Pair("Microsoft 365: sign in again", "login.microsoftonline.com", "login.microsoftonline.com.sso.ru", "the domain is the end: sso.ru"),
            new Pair("Finance: invoice attached", "sharepoint.omanlng.co.om/finance", "sharepoint.omanlng.co.om.files.io", "it ends in files.io"),
            new Pair("Careers: new vacancy", "omanlng.co.om/careers", "ornanlng.co.om/careers", "\"rn\" pretending to be an \"m\""),
            new Pair("VPN certificate expiring", "vpn.omanlng.co.om", "vpn-omanlng.co.om", "one dash makes it a completely different domain"),
            new Pair("Payroll portal has moved", "omanlng.co.om/payroll", "omanlng-co-om.payroll.net", "the real domain is payroll.net"),
            new Pair("Policy document to sign", "docs.omanlng.co.om/policy.pdf", "docs.omanlng.co.om.pdf-view.app", "it ends in pdf-view.app"),
        };

        public static readonly Pair[][] Tiers = { Easy, Medium, Hard };
        public static readonly string[] TierNames = { "EASY", "TRICKY", "EXPERT" };

        /// <summary>An email floating on the track: drive through a real one for a boost, a phishing one stalls you.</summary>
        public readonly struct Mail
        {
            public readonly string From, Subject, Clue;
            public readonly bool Real;
            public Mail(bool real, string from, string subject, string clue = "")
            { Real = real; From = from; Subject = subject; Clue = clue; }
        }

        public static readonly Mail[] Mails =
        {
            new Mail(true, "it@omanlng.co.om", "Patch tonight: restart your PC"),
            new Mail(true, "security@omanlng.co.om", "Cyber Week quiz: win prizes"),
            new Mail(true, "hr@omanlng.co.om", "New working hours"),
            new Mail(true, "no-reply@microsoft.com", "Your Teams recording is ready"),
            new Mail(true, "ceo@omanlng.co.om", "Thank you, team!"),
            new Mail(true, "training@omanlng.co.om", "Safety course booked"),
            new Mail(false, "it-support@omanlng-co.om", "Password expires in 1 hour!", "omanlng-co.om is not omanlng.co.om"),
            new Mail(false, "hr@omanlng.com", "Salary increase: open file", "our domain is .co.om, not .com"),
            new Mail(false, "delivery@parcel-om.net", "Parcel on hold: pay 1 OMR", "unexpected parcel + payment = scam"),
            new Mail(false, "ceo.omanlng@gmail.com", "Urgent: buy gift cards", "the CEO never asks for gift cards from Gmail"),
            new Mail(false, "microsoft@office365-verify.co", "Unusual sign-in: verify now", "Microsoft doesn't mail from office365-verify.co"),
            new Mail(false, "security@0manlng.co.om", "Account locked: click here", "a zero instead of an O"),
        };

        /// <summary>Lures the phisher drops on the track.</summary>
        public static readonly string[] Bait =
        {
            "FREE GIFT CARD!", "YOU WON A PRIZE!", "PASSWORD EXPIRES TODAY", "URGENT: CLICK NOW",
            "PAYROLL_UPDATE.exe", "PARCEL ON HOLD", "BONUS APPROVED!", "VERIFY YOUR ACCOUNT",
        };
    }
}
