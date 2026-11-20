# Art pass: assets to download

Every pack below is **free and CC0** (public domain: free for any use, no credit required).
These sites only allow downloads from a browser, so this part is manual (~10 minutes).

## How

1. Open each link and download the pack:
   - **Kenney:** click **Download**, then **Continue without donating**.
   - **Quaternius:** click **Download** (opens Google Drive), then use the download icon.
2. Put all the `.zip` files in **`~/Downloads/BreachPointAssets/`**. Don't unzip them.
3. Tell Claude they're there. It picks the models, converts them and wires them into the slots below.

## The list (priority order)

| # | Pack | Link | Used for |
|---|---|---|---|
| 1 | Quaternius **Ultimate Spaceships** | https://quaternius.com/packs/ultimatespaceships.html | Player hover-craft (all 3 games) |
| 2 | Quaternius **Sci-Fi Essentials Kit** | https://quaternius.com/packs/scifiessentialskit.html | Server racks, terminals, crates, SOC props |
| 3 | Quaternius **Cyberpunk Game Kit** | https://quaternius.com/packs/cyberpunkgamekit.html | Neon street props, signs, city dressing |
| 4 | Kenney **Racing Kit** | https://kenney.nl/assets/racing-kit | Phish Race track pieces, barriers, signs |
| 5 | Kenney **Sports Pack** | https://kenney.nl/assets/sports-pack | Data Heist Ball goals and stadium bits |
| 6 | Kenney **City Kit (Commercial)** | https://kenney.nl/assets/city-kit-commercial | Real buildings for the skyline |
| 7 | Kenney **Input Prompts** | https://kenney.nl/assets/input-prompts | Xbox / PlayStation button icons on screen |
| 8 | Kenney **Space Kit** | https://kenney.nl/assets/space-kit | Backup vehicle and prop options |

Packs 1–3 matter most. 4–5 are needed for days 3–4.

## Already in the project (downloaded automatically)

Poly Haven (CC0), in `Assets/_BreachPoint/Resources/Art/`:
- **Textures:** `FortPlaster` and `FortStone` (Omani fort), `RoofConcrete` (rooftop), `MetalPlate` (walls, racks, hulls).
- **Sky:** `RooftopNight` light map, used for night-city reflections.

## Model slots (how art reaches the game)

Drop a model at `Assets/_BreachPoint/Resources/Art/Models/<Slot>.fbx` and the game uses it automatically.
If a slot is empty, the game keeps its placeholder shapes, so art can be added one piece at a time.

| Slot | Replaces | Fitted to |
|---|---|---|
| `HoverVehicle` | Player hull, repainted in team colour (installed: Kenney Space Kit `craft_speederB`) | 2.6 × 1.0 × 1.8 m, modelled along X |
| `ServerRack` | The three server racks in the fort | 2 × 3.6 × 2.4 m |

More slots (ball, goals, track pieces, props) are added as days 3–4 are built.
Import rules are applied automatically (`Editor/ArtImport.cs`): normal maps, cubemap sky, no imported cameras or lights.
