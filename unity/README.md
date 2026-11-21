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
   - The top-10 leaderboard is on the right.
   Whoever selects Play is Player 1 (Red).
2. **Join:** Player 2 presses **A / ✕**. No second player? Press **Y / △** (keyboard **Tab**) for a CPU opponent.
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

**Leaderboard:** top scores show on the title screen (saved on this computer). Points = data stolen + 5 per block.
After a match, players enter 3-letter initials (stick up/down, A next, B back).
Clear it before the event with **Breach Point → Reset Leaderboard**.


### Data Heist Ball (mini-game 2)

The attacker pushes the glowing DATA packet into the **EXFIL** portal (left); the defender returns it to the **OmanLNG DATA VAULT** (right).
Both: drive into the data to push it, **A / ✕** kick, **RT / R1** boost.
Defender tools: **X / □ ENCRYPT** (near the data: anything stolen in the next 5 s is useless) and **Y / △ DLP LOCK** (seals the exit for 3 s).
Each exfil steals 20%. Roles swap at half-time; the winner stole more data. Points: data stolen + 10 per recovery + 5 per save.

### Phish Race (mini-game 3)

Both ships race 3 laps. On each straight the track **forks** into two lanes, each painted with a link or sender.
One is real (really `omanlng.com`) and gives a speed boost; the other is a look-alike (`0manlng.com`, `omanlng-login.net`,
`it.helpdesk@gmail.com`…) and the ship is **PHISHED** and stalls. Links change after every pass, and the clue shows on screen.
Each heat one player is the **PHISHER** (**X / □** drops bait behind them: +10 if the target hits it) and the other is the **TARGET**
(**X / □** reports bait within 10 m: +5). **A / ✕** or **RT / R1** boosts. Points: 1st over the line +20, real link +5.
Roles swap for heat 2; the winner has more points.

## Automated tests

**Window → General → Test Runner → PlayMode → Run All** (39 tests: the rules of each mini-game, leaderboard, pause menu, main menu, and a full two-gamepad playthrough of the gauntlet).
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
