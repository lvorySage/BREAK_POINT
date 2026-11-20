# Breach Point (Unity)

The console-style sequel to the web prototype: local multiplayer with Xbox and PS5 controllers, for the Security Week booth.

| | |
|---|---|
| Engine | **Unity 6.3 LTS** (`6000.3.29f1`) |
| Render pipeline | URP (from Unity's *Universal 3D* template) with post-processing volume |
| Input | Input System only (Xbox, DualSense / DualShock, generic pads; local multiplayer) |
| Packages | Cinemachine, Splines, ProBuilder, VFX Graph, Timeline, AI Navigation, Visual Scripting |
| Build targets | macOS (default) + Windows (Mono) |

## One-time setup

1. Open **Unity Hub** (already installed in `/Applications`) and sign in. A free Unity account gives the Personal licence.
2. **Installs → Install Editor → Archive / "All versions"** → **6000.3.29f1** (Unity 6.3 LTS).
   Tick **Windows Build Support (Mono)** if the event laptops run Windows.
   Or from a terminal, using Unity's CLI (it adds itself to your shell PATH):
   `curl -fsSL https://unity.com/install.sh | bash`, then `unity install 6000.3.29f1`.
3. **Projects → Add → Add project from disk** → choose this `unity/` folder.
   The first open takes a few minutes: Unity downloads the packages and builds `Library/` (which git ignores).
4. Commit the `.meta` files Unity generates on that first open.

## Check the controllers

Menu **Breach Point → Controller Check Scene**, then press ▶ Play.

- Player 1 shows in red and Player 2 in blue. DualSense / DualShock light bars change colour to match.
- Shows live sticks, triggers and every pressed button. **A / Cross** rumbles the pad.
- PS5 DualSense on macOS: pair over Bluetooth (hold **PS + Create** until the light flashes) or use USB-C.
- Xbox Series / One on macOS: Bluetooth (hold the pair button), or USB. On Windows both work over USB with no drivers.

## Folders

```
Assets/
  _BreachPoint/        ← our code and content
    Scripts/           runtime C#
    Editor/            editor tools (menu items)
    Art/Fonts/         Orbitron, Rajdhani, Share Tech Mono (SIL OFL; licence files alongside)
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
