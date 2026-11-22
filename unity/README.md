# Breach Point (Unity)

The console-style sequel to the web prototype: local multiplayer with Xbox and PS5 controllers, for the Security Week booth.

| | |
|---|---|
| Engine | **Unity 6.6** (`6000.6.10f1`, Supported release) |
| Render pipeline | URP (from Unity's *Universal 3D* template) with post-processing volume |
| Input | Input System only (Xbox, DualSense / DualShock, generic pads; local multiplayer) |
| Packages | Cinemachine, Splines, ProBuilder, VFX Graph, Timeline, AI Navigation, Visual Scripting |
| Build target | macOS (the event runs on this Mac) |

## One-time setup

1. Open **Unity Hub** (already installed in `/Applications`) and sign in. A free Unity account gives the Personal licence.
2. **Installs → Install Editor** → **6000.6.10f1** (Unity 6.6). Everyone on the team must use this exact version.
3. **Projects → Add → Add project from disk** → choose this `unity/` folder.
   The first open takes a few minutes: Unity downloads the packages and builds `Library/` (which git ignores).

## Play

Menu **Breach Point → Play Game (Main scene)**, or open `Assets/_BreachPoint/Scenes/Main.unity` and press ▶ Play.
For the best view, maximise the Game view (**Shift + Space** over it) at 16:9.

1. **Main menu** (any controller: stick / D-pad to move, **A / ✕** select, **B / ○** back; keyboard: arrows + Enter, Esc back):
   - **Play**: the full gauntlet: Firewall Arena → Data Heist Ball → Phish Race, best of 3.
   - **Quick Play**: one game on its own.
   - **How to Play**: rules, controls and the lesson of each game on one screen.
   - **Options** (saved on this Mac): sound, rumble, match length (45 / 60 / 90 s per half), CPU difficulty (easy / normal / hard).
   - **Controller Test**: every connected pad with live buttons and sticks; **A / ✕** rumbles; hold **B / ○** to leave.
   - **Quit**: press **A / ✕** twice (the first press only asks), so visitors can't close it by accident.
   - The **Most Cyber Aware** top-10 is on the right (see below).
   Whoever selects Play is Player 1 (Red).
2. **Join:** Player 2 presses **A / ✕**. No second player? Press **Y / △** (keyboard **Tab**) for a CPU opponent.
   **Names:** each player types a name (up to 12 letters) on an on-screen keyboard with their own controller:
   stick moves, **A / ✕** types, **B / ○** deletes, **Y / △** or **Start** is done. No name = shown as RED / BLUE.
   The name is shown in every game and used on the leaderboard.
3. Each game has 2 halves; Red and Blue swap roles at half-time. Left alone for 2 minutes, menus go back to the title.

### Firewall Arena (mini-game 1)

| | Gamepad | Keyboard (Red) | Keyboard (Blue) |
|---|---|---|---|
| Move | Left stick / D-pad | W A S D | Arrows |
| Phishing / Email filter | A · ✕ | F | 1 |
| Malware / Antivirus | B · ○ | G | 2 |
| DDoS / Rate limiter | X · □ | H | 3 |
| Password attack / MFA | Y · △ | T | 4 |
| Boost | RT / R1 | Left Shift | Right Shift |
| Aim at a server (optional) | Right stick | (lane follows your position) | |
| Pause (any time, any controller) | Start / Options | Esc or P | Esc or P |

Attacker: line up with a server and fire. Defender: get in front of the shot and pick the matching shield.
Right shield blocks the shot and bounces it back (stunning the attacker); the wrong shield lets data through.
A rate limiter filters a whole DDoS burst, and MFA stops a whole chain of password attempts.
The winner is whoever stole more data while attacking.

**Pause menu:** Resume · Restart this game · Sound on/off · Quit to title.

**Leaderboard: Most Cyber Aware.** Winning the match and being cyber aware are separate. The board ranks each player's
**awareness score**: points for every right security call, plus a bonus equal to their accuracy (% of calls that were right).

| Game | Right calls (awareness points) | Wrong calls |
|---|---|---|
| Firewall Arena | matching defense blocks an attack (10) | wrong defense |
| Data Heist Ball | encryption or DLP save (15), data returned to the vault (10) | data stolen while defending |
| Phish Race | real link taken (10 / 15 / 20 by lap), bait reported (10) | phished (fake link or bait) |

Scores are saved automatically at the end, under the player's name (one entry per name: their best score; CPU players aren't saved).
Clear it before the event with **Breach Point → Reset Leaderboard**.


### Data Heist Ball (mini-game 2)

The attacker pushes the glowing DATA packet into the **EXFIL** portal (left); the defender returns it to the **OmanLNG DATA VAULT** (right).
Both: drive into the data to push it, **A / ✕** kick, **RT / R1** boost.
Defender tools: **X / □ ENCRYPT** (near the data: anything stolen in the next 5 s is useless) and **Y / △ DLP LOCK** (seals the exit for 3 s).
Each exfil steals 20%. Roles swap at half-time; the winner stole more data. Points: data stolen + 10 per recovery + 5 per save.

### Phish Race (mini-game 3)

Both ships race 3 laps. On each straight the track **forks** into two lanes, each painted with a link or sender.
Oman LNG's real domain is **`omanlng.co.om`**. Each fork shows a phishing scenario ("Payroll: confirm your bank details")
and links get harder every lap: lap 1 obvious fakes (`hr@omanlng.com`, Gmail senders), lap 2 look-alikes (`0manlng.co.om`,
`omanlng.co.om.verify-mail.co`), lap 3 expert tricks (`omanlng.co.om@secure-hr.net`, `ornanlng.co.om`, `vpn-omanlng.co.om`).
**Emails** float on the bends: drive through a real sender (`it@omanlng.co.om`) for a boost; a phishing sender
(`it-support@omanlng-co.om`, `ceo.omanlng@gmail.com`) stalls you.
One is real (really `omanlng.co.om`) and gives a speed boost; the other is a look-alike (`0manlng.co.om`, `omanlng-login.net`,
`it.helpdesk@gmail.com`…) and the ship is **PHISHED** and stalls. Links change after every pass, and the clue shows on screen.
Each heat one player is the **PHISHER** (**X / □** drops bait behind them: +10 if the target hits it) and the other is the **TARGET**
(**X / □** reports bait within 10 m: +5). **A / ✕** or **RT / R1** boosts. Points: 1st over the line +20, real link +5.
Roles swap for heat 2; the winner has more points.

## The booth app (no editor needed)

**Build it:** menu **Breach Point → Build Mac App** (about a minute). It writes `unity/Builds/BreachPoint.app`
(Apple silicon, fullscreen, mouse cursor hidden). `Builds/` is not in git; rebuild after pulling changes.

**Run it:** double-click `unity/Builds/BreachPoint.app` in Finder. The first time, macOS may say it can't verify the app:
right-click → **Open** → **Open** (or System Settings → Privacy & Security → **Open Anyway**). After that a double-click works.
Connect the controllers *before* launching (they also work if plugged in later). **Cmd + Q** quits.
For a window instead of fullscreen (testing), run from Terminal:

```bash
open unity/Builds/BreachPoint.app --args -screen-fullscreen 0 -screen-width 1600 -screen-height 900
```

The leaderboard and options are shared between the app and the editor's play mode (same Mac).

## Automated tests

**Window → General → Test Runner → PlayMode → Run All** (51 tests: the rules of each mini-game, leaderboard, pause menu, main menu, and a full two-gamepad playthrough of the gauntlet).
From a terminal, with the editor closed:

```bash
/Applications/Unity/Hub/Editor/6000.6.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath unity \
  -runTests -testPlatform PlayMode -testResults results.xml
```

Set `BP_SHOTS=/some/folder` to also save screenshots of each stage.

## Check the controllers

Main menu → **Controller Test** (in the game), or menu **Breach Point → Controller Check Scene**, then press ▶ Play.

- Player 1 shows in red and Player 2 in blue. DualSense / DualShock light bars change colour to match.
- Shows live sticks, triggers and every pressed button. **A / Cross** rumbles the pad.
- PS5 DualSense on macOS: pair over Bluetooth (hold **PS + Create** until the light flashes) or use USB-C.
- Xbox Series / One on macOS: Bluetooth (hold the pair button), or USB. On Windows both work over USB with no drivers.

## Folders

```
Assets/
  _BreachPoint/        ← our code and content
    Scenes/Main.unity  the game (one object running GameFlow; everything else is built in code)
    Scripts/Core/      game flow, input (gamepad/keyboard/CPU), hover vehicle, city, UI kit, rumble, sounds
    Scripts/FirewallArena/  mini-game 1
    Tests/             PlayMode tests
    Editor/            menu items (setup, play, controller check)
    Resources/         fonts (SIL OFL) and base URP materials loaded at runtime
  Scenes/              template sample scene
  Settings/            URP pipeline + renderer + post-processing profiles (PC and Mobile tiers)
  InputSystem_Actions  default player actions (move, look, attack, jump…) with gamepad bindings
```

Fonts: to use one in UI, open **Window → TextMeshPro → Font Asset Creator** and generate an SDF font asset from the `.ttf`.

## Assets to grab by hand

These sites need a browser or account login, so they can't be scripted. All are free.

| Asset | Where | Why |
|---|---|---|
| Input Prompts (Xbox / PlayStation button icons) | kenney.nl/assets/input-prompts (CC0) | On-screen "press Ⓐ / ✕" prompts that match each player's pad |
| Starter Assets: Character Controllers (URP) | Unity Asset Store, by Unity Technologies | Ready-made third/first-person controller using the Input System |

We'll pick genre-specific art (arena, vehicles, sports) once the three mini-games are chosen.

## Git

Big binaries (textures, models, audio, fonts) go through **Git LFS**; see `../.gitattributes`. Run `git lfs install` once per machine.
GitHub's free LFS quota is limited, so keep source art (`.psd`, `.blend`) small, or store it outside the repo.
