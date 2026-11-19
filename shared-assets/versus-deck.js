/* VERSUS MODE — two players, one screen.
 * Each round = one attack phase. Red picks one attack card, then must answer `red` to execute it.
 * If it executes, Blue gets the matching alert (`blue`) and must respond correctly to block it.
 * value = millions (USD). Blue's health bar starts at the sum of the most valuable card per phase. */
const VERSUS_DECK = {
  phases: [
    {
      name: 'Initial Access',
      brief: 'Get a foothold inside OmanLNG.',
      cards: [
        {
          name: 'Spear-phish the Engineers', value: 15, risk: 'Low noise · relies on humans',
          desc: 'Send a fake "VPN renewal" email to plant engineers.',
          red: {
            prompt: 'Who is the BEST target for this lure?',
            options: [
              'Engineers who have VPN and remote plant access',
              'The public "info@" mailbox on the website',
              'A former employee who left two years ago',
              'The company\'s social-media account',
            ],
            answer: 0,
            explain: 'Pick targets whose access you actually need. A click from someone with remote plant access is worth far more.',
          },
          blue: {
            evidence: { kind: 'alert', title: 'Email gateway', lines: [
              '14 staff received "URGENT: VPN certificate expires" from it-support@omanlng-helpdesk.co',
              '2 recipients clicked the link · 1 user reported it as phishing',
            ] },
            prompt: 'What is your best response?',
            options: [
              'Purge the email from every mailbox, block the domain, reset the 2 clickers\' passwords and review their sign-ins',
              'Send a reminder to the 14 staff to be careful',
              'Wait until someone reports a problem with their account',
              'Disable email for the whole company for the day',
            ],
            answer: 0,
            explain: 'Remove the threat everywhere, cut off the attacker\'s infrastructure, and assume the clickers\' credentials are stolen.',
          },
        },
        {
          name: 'Exploit the VPN', value: 20, risk: 'High reward · noisy',
          desc: 'Hit a freshly announced flaw in the internet-facing VPN appliance.',
          red: {
            prompt: 'Why are VPN and edge appliances such popular targets?',
            options: [
              'They face the internet, connect straight into the network, often lag on patches, and usually can\'t run EDR',
              'They are always switched off at night',
              'They hold no useful credentials',
              'They are hidden behind the company firewall',
            ],
            answer: 0,
            explain: 'Edge devices give direct internal access, and defenders often can\'t see much on them.',
          },
          blue: {
            evidence: { kind: 'alert', title: 'Threat intel + VPN logs', lines: [
              'Vendor advisory: critical VPN vulnerability exploited in the wild',
              'VPN log: malformed requests from 198.51.100.23, 198.51.100.24 … followed by a new admin session',
            ] },
            prompt: 'What is your best response?',
            options: [
              'Apply the vendor fix or mitigation now, kill active sessions, hunt for signs of compromise, and rotate VPN credentials',
              'Wait for next month\'s patch cycle',
              'Block the two IPs and close the ticket',
              'Reboot the VPN appliance and carry on',
            ],
            answer: 0,
            explain: 'Exploitation has already happened, so patching alone isn\'t enough. Hunt for what the attacker did after getting in.',
          },
        },
      ],
    },
    {
      name: 'Execution & Persistence',
      brief: 'Run your tools and make sure you survive a reboot.',
      cards: [
        {
          name: 'Macro Implant', value: 10, risk: 'Classic · well known',
          desc: 'A booby-trapped document drops an implant and a scheduled task.',
          red: {
            prompt: 'Why add a scheduled task or Run key after your implant runs?',
            options: [
              'Persistence: so access survives reboots and logoffs',
              'To make the computer run faster',
              'To delete the antivirus',
              'To change the desktop wallpaper as a calling card',
            ],
            answer: 0,
            explain: 'Without persistence, one reboot ends your operation.',
          },
          blue: {
            evidence: { kind: 'log', title: 'EDR telemetry', lines: [
              'WINWORD.EXE → powershell.exe -enc JABzAD0A...',
              'schtasks /create /tn "AdobeUpdater" /sc hourly /tr C:\\Users\\Public\\upd.exe',
            ] },
            prompt: 'What do these two events tell you?',
            options: [
              'Malicious execution followed by persistence. Isolate the host and remove the task',
              'A normal Adobe update. Close the alert',
              'Reconnaissance only, so there\'s no action needed yet',
              'A printer driver installation',
            ],
            answer: 0,
            explain: 'Office spawning encoded PowerShell, then a fake "updater" task in a public folder, is a textbook compromise.',
          },
        },
        {
          name: 'Rogue Remote-Admin Tool', value: 12, risk: 'Blends in · legit software',
          desc: 'Install a legitimate commercial remote-access tool as your backdoor.',
          red: {
            prompt: 'Why do attackers like legitimate remote-access tools?',
            options: [
              'They are signed and trusted, so they often slip past antivirus and look like normal IT support',
              'They are free of any logging whatsoever',
              'They can only be installed by administrators',
              'Antivirus always deletes them',
            ],
            answer: 0,
            explain: 'Using legitimate tools hides you among real IT activity.',
          },
          blue: {
            evidence: { kind: 'alert', title: 'Software inventory', lines: [
              'New remote-access software installed on FIN-PC-22 (not on the approved list)',
              'Outbound connections to an unknown relay server · user is not in IT',
            ] },
            prompt: 'What is your best response?',
            options: [
              'Isolate the host, check with IT whether it was authorised, and enforce application allow-listing',
              'Ignore it, since the software is legitimate and signed',
              'Quietly uninstall it and close the ticket',
              'Ask the user to stop using it',
            ],
            answer: 0,
            explain: 'Legitimate software in the wrong place is still a backdoor. Investigate how it got there.',
          },
        },
      ],
    },
    {
      name: 'Credential Theft',
      brief: 'Steal the keys to the kingdom.',
      cards: [
        {
          name: 'Dump Credentials', value: 18, risk: 'Big payoff · EDR may notice',
          desc: 'Grab passwords and hashes from a compromised machine\'s memory.',
          red: {
            prompt: 'Which credential is the biggest prize?',
            options: [
              'A Domain Admin account',
              'A guest Wi-Fi password',
              'A user\'s personal email password',
              'A printer\'s default PIN',
            ],
            answer: 0,
            explain: 'Domain Admin controls every Windows machine and account in the company.',
          },
          blue: {
            evidence: { kind: 'alert', title: 'EDR · Critical', lines: [
              'Unknown process accessed LSASS memory on FIN-PC-22',
              '+5 min: admin account "a.alharthy-adm" signs in from FIN-PC-22 to DC01',
            ] },
            prompt: 'What is your best response?',
            options: [
              'Treat it as privileged compromise: isolate the host, disable and reset the admin account, hunt for other domain-controller sign-ins',
              'Restart FIN-PC-22',
              'Ignore it, since antivirus reads LSASS all the time',
              'Change the user\'s email password only',
            ],
            answer: 0,
            explain: 'Theft of credentials followed by a domain-controller sign-in means the attacker is escalating, so act on the account as well as the machine.',
          },
        },
        {
          name: 'Password Spray', value: 14, risk: 'Slow & quiet',
          desc: 'Try one common password against hundreds of accounts.',
          red: {
            prompt: 'Why spray one password across many accounts instead of brute-forcing one?',
            options: [
              'It avoids account-lockout limits, since each account only sees one failed attempt',
              'It is faster to type',
              'It needs admin rights',
              'It only works on weekends',
            ],
            answer: 0,
            explain: 'Lockout policies stop brute force on one account. Spraying stays under the threshold.',
          },
          blue: {
            evidence: { kind: 'log', title: 'Authentication logs', lines: [
              '600 different accounts · 1 failed sign-in each · from 203.0.113.80 · within 10 minutes',
              'Then: 3 successful sign-ins from the same IP',
            ] },
            prompt: 'What is happening, and what should you do?',
            options: [
              'Password spray: block the source, reset the 3 accounts, review what they accessed, and enforce MFA',
              '600 users forgot their passwords. Send a reminder',
              'A brute-force attack on one account. Lock that account',
              'Normal Monday-morning traffic',
            ],
            answer: 0,
            explain: 'Many accounts with one attempt each from one source is the signature of spraying. The 3 successes are compromised.',
          },
        },
      ],
    },
    {
      name: 'Pivot to the Plant',
      brief: 'Cross from IT into the OT network that runs the LNG trains.',
      cards: [
        {
          name: 'Jump-Host Pivot', value: 35, risk: 'Huge leverage · heavily watched',
          desc: 'Hijack the IT/OT DMZ jump host engineers use to reach the plant.',
          red: {
            prompt: 'What is the jump host\'s role in a segmented plant network?',
            options: [
              'It is the controlled bridge between the IT and OT networks',
              'It is the public website server',
              'It is the printer server',
              'It is a backup tape drive',
            ],
            answer: 0,
            explain: 'Jump hosts are the approved crossing, so whoever controls one can reach the plant.',
          },
          blue: {
            evidence: { kind: 'alert', title: 'OT network monitor', lines: [
              '03:12 RDP from jump host 10.20.4.17 → engineering workstation EWS-02',
              'Account "svc_backup2" has never accessed OT before · no change ticket',
            ] },
            prompt: 'What is your best response?',
            options: [
              'Call OT on-call and the control room, end the session and disable the account together with them, and preserve logs',
              'Unplug every OT network cable immediately',
              'Ignore it, because the jump host is a trusted system',
              'Run an aggressive scan of the OT network',
            ],
            answer: 0,
            explain: 'Act fast, but coordinate with operations. Process safety comes first, and fragile OT systems shouldn\'t be scanned aggressively.',
          },
        },
        {
          name: 'Compromised Vendor', value: 30, risk: 'Trusted path · third party',
          desc: 'Breach a maintenance vendor that has standing remote access to the plant.',
          red: {
            prompt: 'Why target a third-party vendor instead of the plant directly?',
            options: [
              'Vendors often have standing remote access and weaker security than the main target',
              'Vendors never have any network access',
              'It is legally allowed',
              'Vendors always use MFA',
            ],
            answer: 0,
            explain: 'Supply-chain and vendor access attacks go around the target\'s strongest defences.',
          },
          blue: {
            evidence: { kind: 'alert', title: 'Remote-access gateway', lines: [
              'Vendor session opened to OT at 02:47, outside the approved window',
              'No change ticket · vendor engineer\'s account is also signed in from another country',
            ] },
            prompt: 'What is your best response?',
            options: [
              'End the session, contact the vendor on a known phone number, review the session recording, and move to just-in-time access',
              'Let it continue, since the vendor is trusted',
              'Email the vendor and wait for a reply',
              'Delete the vendor from the contract',
            ],
            answer: 0,
            explain: 'Out-of-window access with no ticket should be stopped first and verified second.',
          },
        },
      ],
    },
    {
      name: 'Impact',
      brief: 'Cash in. This is where the money moves.',
      cards: [
        {
          name: 'Ransomware Detonation', value: 45, risk: 'Maximum damage · loudest',
          desc: 'Encrypt business servers and threaten to leak stolen data.',
          red: {
            prompt: 'What do ransomware crews usually destroy BEFORE they encrypt?',
            options: [
              'Backups and shadow copies, so the victim can\'t restore without paying',
              'The company website\'s logo',
              'Their own malware',
              'The office Wi-Fi router',
            ],
            answer: 0,
            explain: 'No backups means more pressure to pay.',
          },
          blue: {
            evidence: { kind: 'alert', title: 'EDR · Mass activity', lines: [
              'vssadmin delete shadows /all /quiet  on 37 servers',
              'Mass file renames to *.viper across 120 servers · ransom note README_VIPER.txt',
            ] },
            prompt: 'What is your priority?',
            options: [
              'Isolate affected segments, protect and disconnect the backups, and activate the incident-response plan and crisis team',
              'Pay the ransom quickly to limit downtime',
              'Reboot every server',
              'Wait to see how far it spreads',
            ],
            answer: 0,
            explain: 'Stop the spread, save the backups, and get the plan and people moving.',
          },
        },
        {
          name: 'Cargo Payment Fraud', value: 40, risk: 'Silent · no malware',
          desc: 'Use a hijacked partner mailbox to redirect an LNG cargo payment.',
          red: {
            prompt: 'Which version of this scam is most convincing?',
            options: [
              'A reply inside a real, existing thread from the partner\'s genuine (compromised) mailbox',
              'A new email from a free webmail account',
              'An all-caps email demanding gift cards',
              'A text message from an unknown number',
            ],
            answer: 0,
            explain: 'Hijacking a real thread passes every sender check.',
          },
          blue: {
            evidence: { kind: 'email', title: 'Finance escalation', header: [
              ['From', 'rashid.m@lng-partner-ltd.com (real partner domain)'],
              ['Subject', 'RE: Cargo #2291 — updated bank details, pay today'],
            ], lines: [ 'New IBAN does not match vendor master data · amount $8,200,000' ] },
            prompt: 'What is your best response?',
            options: [
              'Hold the payment and verify by calling the partner on the number already on file',
              'Reply to the email to confirm',
              'Pay it, since it comes from the real partner domain',
              'Pay half now and half later',
            ],
            answer: 0,
            explain: 'A real domain doesn\'t mean a real request. Always verify bank changes out-of-band.',
          },
        },
      ],
    },
  ],
};
export default VERSUS_DECK;
