# Extended Storage

A Valheim mod that adds **tabbed storage cabinets** — six labelled drawers
inside a single prefab, so one cabinet replaces six chests.

This is the umbrella mod for a family of cabinets across the tech tree.
**v0.1 ships the Wooden Cabinet**; iron and black-metal tiers are planned.

## Requirements

| Component | Minimum | Recommended | Notes |
|---|---|---|---|
| Valheim | current retail | current retail | Tested against the current Steam build. |
| BepInEx | 5.4.21 | 5.4.23.3 | Required to load the mod. [Get it on Thunderstore](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/). |
| Jötunn | 2.29.0 | 2.29.0+ | Required dependency. [Thunderstore](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) · [GitHub](https://github.com/Valheim-Modding/Jotunn/releases) |
| .NET Framework | 4.6.2 | — | Already shipped with Valheim; nothing to install. |

The mod is client-side. You only need to install it on your own machine — it
works in single-player and on dedicated servers without a server-side install.

## Cabinets

### Wooden Cabinet

| Property | Value |
|---|---|
| Slots | 6 tabs × 15 = 90 |
| Tool | Hammer |
| Station | Workbench (level 3) |
| Recipe | 100 Wood + 20 Resin |
| Durability | 1000 HP |
| Placement | Floor piece or flat ground |
| Decay | Same as a vanilla wooden chest |

## How tabs work

- Each cabinet has **6 tabs**, each holding 15 items.
- Tab labels are editable, up to **25 characters**. Default labels are
  `1`–`6`.
- Tab strip width is bound to the label, clamped to **80–250 px**. Long
  labels cause tabs to wrap onto a second row above the inventory window,
  mirroring the build hammer's category bar.
- Each tab shows a fill indicator (`N/15`).
- Switch tabs with the mouse or the **1**–**6** keys. Mouse wheel scrolls
  content, not tabs.
- **Shift-click** from your inventory routes to the **active tab only**.

## Editing tab labels

1. Hover the tab you want to rename.
2. Press **Shift+E** to enter edit mode (this does not switch tab, and does
   not trigger the world's `E` action).
3. Type / paste / cursor-edit the label. Standard text controls work:
   arrows, Shift+arrows to select, Ctrl/Cmd+A/C/V/X.
4. **Enter** commits the label. **Esc** discards. Closing the window
   mid-edit also discards.
5. An empty label after commit falls back to the default tab number.

## Shift+hover content preview

Hold **Shift** while looking at a cabinet — a small panel pops up next to
the crosshair listing the contents of all six tabs (label, fill count, and
the items themselves). Release Shift to dismiss. The preview respects ward
access: you only see contents you would be allowed to open.

## Compatibility

Existing chest-content mods can read cabinet contents through an aggregated
read-only view. Display works; moving items via that aggregated view is a
no-op — use the cabinet's own tab UI to move items.

If the mod is uninstalled, cabinets degrade to a normal wooden chest that
exposes the **first tab**'s contents. Items in the other five tabs are
preserved in the cabinet's saved data and reappear if the mod is
reinstalled.

## Installation

See [Requirements](#requirements) above for prerequisites.

### Manual Install

1. Download `ExtendedStorage.zip` from the [latest release](https://github.com/toutatis-en-haut/ValheimMods/releases).
2. Extract the zip. You will see an `ExtendedStorage` folder containing
   `ExtendedStorage.dll` and an `assets/` folder.
3. Copy that `ExtendedStorage` folder into your Valheim install at:

   ```
   <Valheim install>/BepInEx/plugins/
   ```

4. Launch the game. On first run, a config file is generated at
   `BepInEx/config/toutatis.extended_storage.cfg`.

### Mod Manager Install

If you use r2modman, Thunderstore Mod Manager, or Vortex, drop the zip into
your manager's import flow — the layout is the standard
`ExtendedStorage/...` structure they expect.

## About the Author

Extended Storage is built and maintained by **William** of **Tribus Studio**.

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
- The shift-hover content panel takes design inspiration from CDEVx's
  `ChestItemsHoverDisplay` Thunderstore mod. The implementation here is
  original.
