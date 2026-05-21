# Spawn Point Gateways — The Bifrost Charm

A Valheim mod that lets a single Viking step back to any bed they have ever
called home, by spending resin to call upon Heimdall's bridge.

## Requirements

| Component | Minimum | Recommended | Notes |
|---|---|---|---|
| Valheim | current retail | current retail | Tested against the current Steam build. |
| BepInEx | 5.4.21 | 5.4.23.3 | Required to load the mod. [Get it on Thunderstore](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/). |
| Jötunn | 2.29.0 | 2.29.0+ | Required dependency. [Thunderstore](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) · [GitHub](https://github.com/Valheim-Modding/Jotunn/releases) |
| .NET Framework | 4.6.2 | — | Already shipped with Valheim; nothing to install. |

The mod is client-side.

## Lore

Where the Bifröst Totem opens the bridge to any horizon a Viking has seen, the
**Bifröst Charm** keeps a quieter promise. Hung beside a bed, it learns the
shape of the place — and remembers it. Years later, a Viking who carries the
charm and a pouch of resin can light the bridge again and step home to a hearth
long left behind.

## How It Works

### Crafting the Charm

Build a **Workbench (Level 2)** and gather:

- 10 Greydwarf Eyes
- 1 Finewood
- 20 Thistle
- 10 Blueberries

The Bifröst Charm appears in the workbench's craft menu. You only need to
craft it once — it is never consumed.

### Recording Spawn Points

Every time you "Set Spawn" at a bed (the standard Valheim interaction), the
position of that bed is added to your personal list of remembered homes. The
list keeps growing — even when you switch to a new bed, every previous one
remains as a possible destination.

### Using the Charm

The full flow is:

1. **Activate the Charm.** Right-click the charm in your inventory. If you
   have fewer than 20 resin, nothing happens. Otherwise the world map opens.
2. **Pick a Marker.** Every bed you have ever called home appears on the map
   as a small blue circle. The markers stay the same size as you zoom — they
   never grow.
3. **Click a Marker.** Click any blue circle to teleport. The bridge opens;
   20 resin are consumed; you arrive at that bed.

Clicking anywhere off a marker does nothing — only marker clicks teleport.

Right-click the map or press Esc to cancel without spending any resin.
Resin is only consumed on a successful teleport.

## Installation

### Manual Install

1. Download `SpawnPointGateways.zip` from the [latest release](https://github.com/toutatis-en-haut/ValheimMods/releases).
2. Extract the zip. You will see a `SpawnPointGateways` folder containing
   `SpawnPointGateways.dll` and an `assets/` folder.
3. Copy that `SpawnPointGateways` folder into your Valheim install at:

   ```
   <Valheim install>/BepInEx/plugins/
   ```

   Final layout:

   ```
   BepInEx/plugins/SpawnPointGateways/
     SpawnPointGateways.dll
     assets/icons/bifrost_charm.png
   ```

4. Launch the game. On first run, a config file is generated at
   `BepInEx/config/toutatis.spawnpointgateways.cfg`.

### Mod Manager Install

If you use r2modman, Thunderstore Mod Manager, or Vortex, drop the zip into
your manager's import flow.

## Configuration

After your first launch the config file lives at:

```
<Valheim install>/BepInEx/config/toutatis.spawnpointgateways.cfg
```

| Section | Key | Default | Purpose |
|---|---|---|---|
| `Recipe` | `GreydwarfEye` | `10` | Greydwarf Eyes to craft the charm. |
| `Recipe` | `FineWood` | `1` | Fine Wood to craft the charm. |
| `Recipe` | `Thistle` | `20` | Thistle to craft the charm. |
| `Recipe` | `Blueberries` | `10` | Blueberries to craft the charm. |
| `Recipe` | `WorkbenchLevel` | `2` | Workbench level required to craft. |
| `Activation` | `ResinCost` | `20` | Resin consumed each time the charm is activated. |
| `UI` | `MarkerColor` | `0.35, 0.75, 1.0, 0.85` | RGBA color of the spawn-point markers. |
| `UI` | `MarkerRadiusPixels` | `18` | On-screen radius of each spawn-point marker, in pixels. |
| `UI` | `MarkerRingThickness` | `0.35` | Ring thickness as a fraction of marker radius (0.01 = thin line, 0.5 = solid disc). |

## About the Author

Spawn Point Gateways is built and maintained by **William** of **Tribus Studio**.

- GitHub: <https://github.com/toutatis-en-haut/ValheimMods>
- Email: <william@tribus.studio>

## License & Acknowledgements

- Built on top of [BepInEx](https://github.com/BepInEx/BepInEx) and [Jötunn](https://github.com/Valheim-Modding/Jotunn).
- A companion to [Personal Gateway](https://github.com/toutatis-en-haut/ValheimMods/tree/main/PersonalGateway).
