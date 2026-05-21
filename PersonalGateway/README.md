# Personal Gateway — The Bifröst Totem

A Valheim mod that grants a single Viking the power to traverse the realms by Heimdall's hand.

## Requirements

| Component | Minimum | Recommended | Notes |
|---|---|---|---|
| Valheim | current retail | current retail | Tested against the current Steam build. |
| BepInEx | 5.4.21 | 5.4.23.3 | Required to load the mod. [Get it on Thunderstore](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/). |
| Jötunn | 2.29.0 | 2.29.0+ | Required dependency. [Thunderstore](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) · [GitHub](https://github.com/Valheim-Modding/Jotunn/releases) |
| .NET Framework | 4.6.2 | — | Already shipped with Valheim; nothing to install. |

The mod is client-side. You only need to install it on your own machine — it works in single-player and on dedicated servers without a server-side install.

## Lore

In Norse myth, **Heimdall** is the watchman of the gods. From his post at the foot of Bifröst — the rainbow bridge between worlds — he can see for a hundred leagues and hear the grass grow on the ground. To a few worthy mortals he has gifted a token of that sight: the **Bifröst Totem**.

While the totem is yours, you may step from any place you have already walked to any other — as long as the bridge holds, and a worthy sacrifice is offered to feed it.

## How It Works

### Crafting the Totem

Build a **Workbench (Level 2)** and gather:

- 10 Greydwarf Eyes
- 10 Thistle
- 10 Dandelion
- 10 Blueberries

The Bifröst Totem appears in the workbench's craft menu as a small golden star. You only need to craft it once — it is never consumed.

### Using the Totem

The full flow is:

1. **Activate the Totem.** Open your inventory and right-click the totem. If you carry no trophy, nothing happens (you need a sacrifice). Otherwise you see *"The totem hums. Choose a trophy to sacrifice."*
2. **Choose a Sacrifice.** Every trophy in your inventory begins pulsing gold. Right-click the trophy you wish to spend. *Any* trophy works — from a Neck tail to a slain forsaken's head. Rarer trophies grant more Bifröst skill XP.
3. **Open the Map.** The world map opens automatically. A light-blue circle marks how far the bridge can carry you at your current Bifröst skill level.
4. **Choose a Destination.** Hold **Ctrl** and **double-click** anywhere on the map inside the circle, on terrain you have already explored. The bridge opens; the trophy is consumed; you arrive.

Clicking outside the circle, on unexplored fog, or with the wrong key combination does nothing — the sacrifice is only spent on a successful crossing.

### The Bifröst Skill

A new skill, **Bifröst**, is added to your character. Each successful teleport awards XP scaled to the trophy's rarity:

| Tier | Examples | XP |
|---|---|---|
| Common | Neck, Boar, Greyling, Deer | 1 |
| Uncommon | Greydwarf, Skeleton, Ghost, Leech | 3 |
| Rare | Troll, Draugr, Fuling, Lox, Serpent | 8 |
| Boss | Eikthyr, The Elder, Bonemass, Moder, Yagluth, Seeker Queen, Fader | 25 |

The higher your Bifröst skill, the wider the light-blue circle. At **level 100** (the default cap), the circle disappears entirely — you may step to anywhere you have ever walked, anywhere in the world.

## Installation

See [Requirements](#requirements) above for prerequisites.

### Manual Install

1. Download `PersonalGateway.zip` from the [latest release](https://github.com/toutatis-en-haut/ValheimMods/releases).
2. Extract the zip. You will see a `PersonalGateway` folder containing `PersonalGateway.dll` and an `assets/` folder.
3. Copy that `PersonalGateway` folder into your Valheim install at:

   ```
   <Valheim install>/BepInEx/plugins/
   ```

   Final layout:

   ```
   BepInEx/plugins/PersonalGateway/
     PersonalGateway.dll
     assets/icons/bifrost_totem.png
   ```

4. Launch the game. On first run, a config file is generated at `BepInEx/config/toutatis.personalgateway.cfg`.

### Mod Manager Install

If you use r2modman, Thunderstore Mod Manager, or Vortex, drop the zip into your manager's import flow — the layout is the standard `PersonalGateway/...` structure they expect.

## Configuration

After your first launch the config file lives at:

```
<Valheim install>/BepInEx/config/toutatis.personalgateway.cfg
```

Open it in any text editor. Changes apply on the next game launch (or use [Configuration Manager](https://thunderstore.io/c/valheim/p/Azumatt/Configuration_Manager/) to edit live).

| Section | Key | Default | Purpose |
|---|---|---|---|
| `Controls` | `TeleportModifierKey` | `LeftControl` | Modifier held while clicking the map to commit a teleport. |
| `Controls` | `TeleportMouseButton` | `0` | Mouse button (0=Left, 1=Right, 2=Middle) used to commit a teleport. |
| `Controls` | `DoubleClickWindowSeconds` | `0.4` | Max seconds between two clicks for a double-click to register. |
| `Skill` | `MaxSkillLevel` | `100` | Max Bifröst skill. The range circle disappears at this level. |
| `Skill` | `MaxTeleportRangeMeters` | `35000` | Range in meters available at max skill. Range at level *L* = `MaxTeleportRangeMeters * (L / MaxSkillLevel)`. |
| `Recipe` | `GreydwarfEye` / `Thistle` / `Dandelion` / `Blueberries` | `10` each | Quantity of each ingredient required to craft the totem. |
| `Recipe` | `WorkbenchLevel` | `2` | Workbench tier required to craft the totem. |
| `UI` | `ShowRangeCircle` | `true` | Show the light-blue range ring on the large map. |
| `UI` | `RangeCircleColor` | `0.5, 0.8, 1.0, 0.45` | RGBA color of the range ring. |
| `XP` | `Common` / `Uncommon` / `Rare` / `Boss` | `1 / 3 / 8 / 25` | Bifröst XP awarded per sacrificed trophy by tier. |
| `XP.Tiers` | `UncommonTrophies` / `RareTrophies` / `BossTrophies` | (see file) | Comma-separated trophy prefab names assigned to each tier. Unlisted trophies default to **Common**. |

To make a trophy worth more XP, move its prefab name to a higher tier list. For example, to treat `TrophyDeer` as Rare instead of Uncommon, remove it from `UncommonTrophies` and add it to `RareTrophies`.

## About the Author

Personal Gateway is built and maintained by **William** of **Tribus Studio**.

- GitHub: <https://github.com/toutatis-en-haut/ValheimMods>
- Email: <william@tribus.studio>

### Reporting Bugs and Suggestions

Please file issues on the [GitHub repository](https://github.com/toutatis-en-haut/ValheimMods/issues). Include:

- Your Valheim version
- Your BepInEx version
- Your Jötunn version
- The contents of `BepInEx/LogOutput.log` from a session where the issue happened

Pull requests are welcome.

## License & Acknowledgements

- Built on top of [BepInEx](https://github.com/BepInEx/BepInEx) and [Jötunn](https://github.com/Valheim-Modding/Jotunn).
- Heimdall, Bifröst, and the world of Yggdrasil belong to the old stories. The rest is fan work, freely given.
