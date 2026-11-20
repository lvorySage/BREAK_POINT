/*
 * INCIDENT ATTACK — round content (phishing campaign).
 * Generated from INCIDENT_ATTACK_Developer_Handoff_V5_1.json; safe to edit by hand.
 *
 * Each alert card:
 *   severity  label Blue sees (LOW / MEDIUM / HIGH / CRITICAL). Deliberately NOT a reliable ranking.
 *   priority  hidden true urgency 1–10. Blue scores by ordering alerts from highest to lowest.
 *   correct   hidden truth: "stop" = real threat, "verify" = needs checking, "leave" = harmless/approved.
 *   leak      % of the dataset a "stop" alert can leak if handled late (only counts in rounds 3–4).
 *   heat      attacker progress ("compromise") gained if a "stop" alert is handled late.
 *   clue      short evidence line shown to Blue.
 * All scenarios are fictional.
 */
export const STAGES = [
  {
    "id": "inbox",
    "number": 1,
    "name": "INBOX INFILTRATION",
    "subtitle": "Three suspicious alerts. One is the main phishing attack.",
    "phase": "PLANNING STAGE",
    "theme": "Fake messages arrive across the company.",
    "mainPrompt": "Choose your phishing email",
    "mainHint": "The best-looking bait is not always the loudest one.",
    "main": [
      {
        "id": "password",
        "icon": "🔑",
        "title": "IT password reset",
        "desc": "“Your password expires today.”",
        "severity": "MEDIUM",
        "priority": 5,
        "correct": "stop",
        "leak": 0,
        "heat": 17,
        "clue": "External reset link"
      },
      {
        "id": "invoice",
        "icon": "🧾",
        "title": "Urgent supplier invoice",
        "desc": "“Pay immediately to avoid a delay.”",
        "severity": "HIGH",
        "priority": 7,
        "correct": "stop",
        "leak": 0,
        "heat": 24,
        "clue": "Unknown supplier address"
      },
      {
        "id": "shared",
        "icon": "📄",
        "title": "Shared contract link",
        "desc": "“A contract is waiting for review.”",
        "severity": "MEDIUM",
        "priority": 6,
        "correct": "stop",
        "leak": 0,
        "heat": 21,
        "clue": "Unexpected file-sharing site"
      }
    ],
    "sidePrompt": "Add a distraction",
    "sideHint": "A second alert can be real, misleading, or harmless.",
    "side": [
      {
        "id": "login_noise",
        "icon": "🔐",
        "title": "Login failure spike",
        "desc": "Cause noisy failed sign-in alerts.",
        "severity": "HIGH",
        "priority": 3,
        "correct": "verify",
        "leak": 0,
        "heat": 2,
        "clue": "Failed attempts only; no successful login"
      },
      {
        "id": "manager",
        "icon": "👤",
        "title": "Fake manager message",
        "desc": "Send a second suspicious request.",
        "severity": "MEDIUM",
        "priority": 8,
        "correct": "stop",
        "leak": 0,
        "heat": 12,
        "clue": "Unexpected sender; requests external sign-in"
      },
      {
        "id": "routine_mail",
        "icon": "📣",
        "title": "Noisy IT notice",
        "desc": "Use a legitimate-looking IT announcement.",
        "severity": "HIGH",
        "priority": 1,
        "correct": "leave",
        "leak": 0,
        "heat": 0,
        "clue": "Verified sender; approved IT bulletin"
      }
    ],
    "ambient": [
      {
        "id": "meeting",
        "icon": "📅",
        "title": "Meeting invitation",
        "desc": "Approved internal calendar meeting.",
        "severity": "LOW",
        "priority": 1,
        "correct": "leave",
        "leak": 0,
        "heat": 0,
        "clue": "Verified internal sender"
      },
      {
        "id": "outside_reply",
        "icon": "✉️",
        "title": "Unknown outside reply",
        "desc": "An unsolicited sender asks for a quick reply.",
        "severity": "MEDIUM",
        "priority": 4,
        "correct": "verify",
        "leak": 0,
        "heat": 0,
        "clue": "No link; sender not yet confirmed"
      },
      {
        "id": "quarantine",
        "icon": "📬",
        "title": "Quarantine update",
        "desc": "Gateway already blocked an attachment.",
        "severity": "MEDIUM",
        "priority": 2,
        "correct": "leave",
        "leak": 0,
        "heat": 0,
        "clue": "Mail system confirms block"
      }
    ]
  },
  {
    "id": "signins",
    "number": 2,
    "name": "ACCOUNT TAKEOVER",
    "subtitle": "The attacker keeps pushing even if round one was stopped.",
    "phase": "PLANNING STAGE",
    "theme": "Employees report sign-in prompts and account changes.",
    "mainPrompt": "Choose your sign-in attack",
    "mainHint": "Try to get into an employee account.",
    "main": [
      {
        "id": "stolen",
        "icon": "🔓",
        "title": "Stolen password",
        "desc": "Try a password taken from a fake page.",
        "severity": "HIGH",
        "priority": 6,
        "correct": "stop",
        "leak": 0,
        "heat": 22,
        "clue": "Unrecognized device and location"
      },
      {
        "id": "approval",
        "icon": "📲",
        "title": "Approval spam",
        "desc": "Flood the employee with login prompts.",
        "severity": "HIGH",
        "priority": 7,
        "correct": "stop",
        "leak": 0,
        "heat": 24,
        "clue": "Repeated unwanted approvals"
      },
      {
        "id": "session",
        "icon": "🪪",
        "title": "Stolen login session",
        "desc": "Use an existing session to stay signed in.",
        "severity": "MEDIUM",
        "priority": 9,
        "correct": "stop",
        "leak": 0,
        "heat": 29,
        "clue": "Unfamiliar active session"
      }
    ],
    "sidePrompt": "Add a second signal",
    "sideHint": "Make the defender split attention.",
    "side": [
      {
        "id": "travel",
        "icon": "✈️",
        "title": "Traveler login",
        "desc": "Use a believable travel sign-in alert.",
        "severity": "HIGH",
        "priority": 3,
        "correct": "verify",
        "leak": 0,
        "heat": 0,
        "clue": "Travel note exists; verify with user"
      },
      {
        "id": "otp",
        "icon": "📱",
        "title": "Unexpected code request",
        "desc": "Pressure a second user for a one-time code.",
        "severity": "CRITICAL",
        "priority": 8,
        "correct": "stop",
        "leak": 0,
        "heat": 13,
        "clue": "User denies requesting the code"
      },
      {
        "id": "service",
        "icon": "⚙️",
        "title": "Approved service change",
        "desc": "Generate a scary-looking routine change.",
        "severity": "HIGH",
        "priority": 1,
        "correct": "leave",
        "leak": 0,
        "heat": 0,
        "clue": "Approved change reference"
      }
    ],
    "ambient": [
      {
        "id": "vpn",
        "icon": "🌍",
        "title": "VPN location change",
        "desc": "Employee signs in from a different country.",
        "severity": "HIGH",
        "priority": 4,
        "correct": "verify",
        "leak": 0,
        "heat": 0,
        "clue": "No confirmed compromise; traveler listed"
      },
      {
        "id": "denied",
        "icon": "🚷",
        "title": "Denied login attempt",
        "desc": "Account denied an unfamiliar sign-in.",
        "severity": "MEDIUM",
        "priority": 2,
        "correct": "verify",
        "leak": 0,
        "heat": 0,
        "clue": "Attempt blocked; review surrounding events"
      },
      {
        "id": "maintenance",
        "icon": "🧰",
        "title": "Maintenance sign-in",
        "desc": "Approved overnight maintenance account.",
        "severity": "MEDIUM",
        "priority": 1,
        "correct": "leave",
        "leak": 0,
        "heat": 0,
        "clue": "Scheduled maintenance ticket"
      }
    ]
  },
  {
    "id": "files",
    "number": 3,
    "name": "FIRST DATA THEFT",
    "subtitle": "The attacker attempts to copy sensitive business files.",
    "phase": "ATTACK PHASE",
    "theme": "The SOC sees unusual file and mailbox activity.",
    "mainPrompt": "Choose what to steal first",
    "mainHint": "Copying more files brings more reward — and more alerts.",
    "main": [
      {
        "id": "mailbox",
        "icon": "📨",
        "title": "Employee mailbox",
        "desc": "Copy emails from an employee mailbox.",
        "severity": "MEDIUM",
        "priority": 5,
        "correct": "stop",
        "leak": 8,
        "heat": 11,
        "clue": "Mailbox export from an unfamiliar session"
      },
      {
        "id": "contracts",
        "icon": "📑",
        "title": "Supplier contracts",
        "desc": "Try copying confidential pricing agreements.",
        "severity": "HIGH",
        "priority": 8,
        "correct": "stop",
        "leak": 15,
        "heat": 20,
        "clue": "Restricted files copied by an unknown login"
      },
      {
        "id": "archive",
        "icon": "🗄️",
        "title": "Shared archive",
        "desc": "Copy many documents from a shared folder.",
        "severity": "HIGH",
        "priority": 7,
        "correct": "stop",
        "leak": 21,
        "heat": 23,
        "clue": "Unexpected bulk document downloads"
      }
    ],
    "sidePrompt": "Choose the supporting noise",
    "sideHint": "An unrelated file alert may be the loudest event.",
    "side": [
      {
        "id": "backup",
        "icon": "💾",
        "title": "Scheduled backup",
        "desc": "A routine backup triggers a loud notice.",
        "severity": "CRITICAL",
        "priority": 1,
        "correct": "leave",
        "leak": 0,
        "heat": 0,
        "clue": "Approved job; known backup server"
      },
      {
        "id": "public",
        "icon": "🔗",
        "title": "Public sharing link",
        "desc": "Attempt to expose a second file folder.",
        "severity": "HIGH",
        "priority": 9,
        "correct": "stop",
        "leak": 9,
        "heat": 15,
        "clue": "Public link for restricted folder"
      },
      {
        "id": "role_change",
        "icon": "👥",
        "title": "Permission request",
        "desc": "A user requests unusual access.",
        "severity": "MEDIUM",
        "priority": 4,
        "correct": "verify",
        "leak": 0,
        "heat": 0,
        "clue": "Approval not yet confirmed"
      }
    ],
    "ambient": [
      {
        "id": "audit",
        "icon": "📋",
        "title": "Approved audit scan",
        "desc": "Routine internal file inventory.",
        "severity": "HIGH",
        "priority": 1,
        "correct": "leave",
        "leak": 0,
        "heat": 0,
        "clue": "Valid audit change number"
      },
      {
        "id": "outside",
        "icon": "🕒",
        "title": "Files opened at night",
        "desc": "Repeated file access outside shift hours.",
        "severity": "MEDIUM",
        "priority": 5,
        "correct": "verify",
        "leak": 0,
        "heat": 0,
        "clue": "Needs owner confirmation"
      },
      {
        "id": "folder",
        "icon": "📁",
        "title": "Bulk folder rename",
        "desc": "Multiple folders renamed in one location.",
        "severity": "LOW",
        "priority": 2,
        "correct": "verify",
        "leak": 0,
        "heat": 0,
        "clue": "No copies or exports observed"
      }
    ]
  },
  {
    "id": "transfer",
    "number": 4,
    "name": "FINAL DATA TRANSFER",
    "subtitle": "Last round. Find the real data-loss risk before it is too late.",
    "phase": "ATTACK PHASE",
    "theme": "Outbound activity begins while other alarms compete for attention.",
    "mainPrompt": "How much data will you try to take?",
    "mainHint": "A bigger transfer has a larger reward — and a larger warning.",
    "main": [
      {
        "id": "small",
        "icon": "📩",
        "title": "A few documents",
        "desc": "Send a handful of valuable files outside.",
        "severity": "MEDIUM",
        "priority": 5,
        "correct": "stop",
        "leak": 13,
        "heat": 7,
        "clue": "Unknown external destination"
      },
      {
        "id": "medium",
        "icon": "📦",
        "title": "Several folders",
        "desc": "Move several contract folders.",
        "severity": "HIGH",
        "priority": 8,
        "correct": "stop",
        "leak": 31,
        "heat": 17,
        "clue": "Many files; unfamiliar cloud destination"
      },
      {
        "id": "large",
        "icon": "🌐",
        "title": "Everything accessible",
        "desc": "Move the entire reachable dataset.",
        "severity": "CRITICAL",
        "priority": 10,
        "correct": "stop",
        "leak": 58,
        "heat": 28,
        "clue": "Large outbound spike; sensitive filenames"
      }
    ],
    "sidePrompt": "Launch your final distraction",
    "sideHint": "A loud alert might be harmless — or another theft attempt.",
    "side": [
      {
        "id": "forward",
        "icon": "📨",
        "title": "Secret mail forwarding",
        "desc": "Try sending a second stream of mail outside.",
        "severity": "HIGH",
        "priority": 9,
        "correct": "stop",
        "leak": 16,
        "heat": 10,
        "clue": "New forwarding rule to an outside mailbox"
      },
      {
        "id": "scanner",
        "icon": "🛰️",
        "title": "Loud network scan",
        "desc": "Trigger a scary-looking approved scan.",
        "severity": "CRITICAL",
        "priority": 1,
        "correct": "leave",
        "leak": 0,
        "heat": 0,
        "clue": "Known scanner IP and change ticket"
      },
      {
        "id": "supplier",
        "icon": "🧾",
        "title": "Supplier email follow-up",
        "desc": "Generate a payment-related notification.",
        "severity": "MEDIUM",
        "priority": 3,
        "correct": "verify",
        "leak": 0,
        "heat": 0,
        "clue": "Payment request needs supplier confirmation"
      }
    ],
    "ambient": [
      {
        "id": "sync",
        "icon": "☁️",
        "title": "Cloud backup transfer",
        "desc": "Approved backup sends files to company storage.",
        "severity": "HIGH",
        "priority": 1,
        "correct": "leave",
        "leak": 0,
        "heat": 0,
        "clue": "Approved backup destination"
      },
      {
        "id": "link",
        "icon": "🔗",
        "title": "Public contract link",
        "desc": "Someone published a contract-sharing link.",
        "severity": "HIGH",
        "priority": 7,
        "correct": "stop",
        "leak": 12,
        "heat": 8,
        "clue": "Outside access enabled on restricted file"
      },
      {
        "id": "dlp",
        "icon": "🚫",
        "title": "Transfer already blocked",
        "desc": "Data protection system stopped a file upload.",
        "severity": "MEDIUM",
        "priority": 2,
        "correct": "verify",
        "leak": 0,
        "heat": 0,
        "clue": "Transfer denied; investigate if more occurred"
      }
    ]
  }
];

export const CASE_STUDIES = [
  {
    "incident": "Dropbox 2022 phishing incident",
    "outcome_use": "0%-35% extracted (with different text for 0% vs nonzero)",
    "source": "Dropbox Security Team",
    "url": "https://dropbox.tech/security/a-recent-phishing-campaign-targeting-dropbox",
    "detail": "Attackers accessed a GitHub organization and copied 130 code repositories; illustrates phishing impact. Historical incident differs from game scenario."
  },
  {
    "incident": "MGM Resorts 2023 cyber incident",
    "outcome_use": "over 35% extracted",
    "source": "MGM Resorts SEC Form 8-K",
    "url": "https://www.sec.gov/Archives/edgar/data/789570/000119312523251667/d461062d8k.htm",
    "detail": "Approximately US$100 million negative impact on adjusted property EBITDAR from September 2023 disruption, a distinct real case; NOT a value estimate for fictional LNG theft."
  }
];
