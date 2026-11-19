# Breach Point: Red vs Blue

A turn-based cyber battle for OmanLNG Cyber Security Week. Blue Team defends a "money saved" health bar; Red Team fills a "plunder" bar while dodging SOC heat.

## Run it

```bash
npm install        # once
npm run dev        # development, hot reload at http://localhost:5173
npm run build      # produces dist/index.html, a single self-contained file
```

**At the booth:** copy `dist/index.html` to the display PC and double-click it. It needs no internet and no server. Press **Full screen** on the title screen. Plug in speakers for the sound effects.

## Modes

| Mode | Players | Bar(s) | Ends when |
|---|---|---|---|
| Blue Team | 1 | Money saved (health, drains on mistakes) | 8 turns done |
| Red Team | 1 | Plunder (fills) + SOC heat (100% = busted, loot seized) | 8 turns or caught |
| Versus | 2, same screen | Both | 5 rounds |

Turn types: multiple choice, select-all, drag into buckets, drag into order. Drag works with mouse and touch; tap a card, then a box, also works.

Keyboard: `1`–`3` pick a mode on the title screen, `1`–`7` pick answers, `Enter` locks in or continues.

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
