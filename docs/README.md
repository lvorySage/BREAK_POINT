# Breach Point: Red vs Blue

A turn-based cyber battle for OmanLNG Cyber Security Week. Blue Team defends a "money saved" health bar; Red Team fills a "plunder" bar while dodging SOC heat.

## Run it

```bash
npm install        # once
npm run dev        # development, hot reload at http://localhost:5173
npm run build      # produces dist/index.html, a single self-contained file
npm test           # INCIDENT ATTACK engine tests (Node 18+)
```

**At the booth:** copy `dist/index.html` to the display PC and double-click it. It needs no internet and no server. Press **Full screen** on the title screen. Plug in speakers for the sound effects.

## Modes

| Mode | Players | Bar(s) | Ends when |
|---|---|---|---|
| Blue Team | 1 | Money saved (health, drains on mistakes) | 8 turns done |
| Red Team | 1 | Plunder (fills) + SOC heat (100% = busted, loot seized) | 8 turns or caught |
| Versus | 2, same screen | Both | 5 rounds |
| Incident Attack | 2, split screen | Confirmed data extracted (rounds 3–4) | 4 rounds, always |

Turn types: multiple choice, select-all, drag into buckets, drag into order. Drag works with mouse and touch; tap a card, then a box, also works.

Keyboard: `1`–`3` pick a mode on the title screen, `1`–`7` pick answers, `Enter` locks in or continues.

## Incident Attack (key `4`)

Built from `INCIDENT_ATTACK_Developer_Handoff_V5_1.json`: a phishing campaign, played offline on one screen. Red is on the left and Blue on the right, and they share one game state.

- **Red** (30 s): pick 1 main attack + 1 distraction, then launch. Time out → the first card of each is used.
- **Blue** (30 s): three alerts arrive (Red's attack, Red's distraction, one background event). Tap them in order of urgency (#1, #2, #3); tap a numbered card to undo. Time out → handled left to right.
- Rounds 1–2 are the **planning stage**: no data can be stolen. Rounds 3–4 are the **attack phase**: every real threat Blue handles after a less urgent alert leaks data. All 4 rounds are always played.
- The severity label is deliberately unreliable; the hidden urgency (1–10) decides the score.
- Finale: Red's cash settlement and Blue's decrypting report play first; grades are revealed afterwards.

| File | Contents |
|---|---|
| `incident-attack/stages.js` | Round content: choices, alerts, hidden urgency, leak/heat values, case studies |
| `src/incident/engine.js` | Authoritative rules: alert generation, resolution, grades, money report. Screens never change scores. |
| `tests/engine.test.js` | Rule tests (`npm test`) |

All OMR values are illustrative scoring on a fictional OMR 100,000 dataset.

Not built from the handoff: two-laptop LAN play (Node server, room codes, spectator). Everything runs in one browser. Characters are SVG and CSS, not GIFs.

## Editing content (no React knowledge needed)

| File | Contents |
|---|---|
| `blue-team/scenarios.js` | Blue campaign turns |
| `red-team/scenarios.js` | Red campaign turns (`heat` = heat added on a full miss, default 34) |
| `shared-assets/versus-deck.js` | Versus attack cards (each has a `red` and a `blue` question) |
| `src/config.js` | Org name, event name, seconds per turn, idle-reset time |
| `shared-assets/styles.css` | Look and feel |

Turn schema: `type` is `mcq` (`answer` = index), `multi` (`answers` = [indexes]), `sort` (`buckets` + `items[].bucket`) or `order` (`items` listed in the correct order). `value` is in millions of USD. Options are shuffled automatically. `evidence` adds an email, alert or log panel.

After editing, run `npm run build` again and copy the new `dist/index.html`.

## Booth behaviour

- Leaderboard is stored in the browser on that PC (localStorage). Reset it from **Full leaderboard → Reset** (asks for a second click).
- After 2 minutes with no input the game returns to the title screen.
- All scenario names, domains, IPs and amounts are fictional.
