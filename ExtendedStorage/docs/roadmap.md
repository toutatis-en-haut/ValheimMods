# ExtendedStorage Roadmap

BepInEx 5 plugin for Valheim that adds tabbed storage cabinets. The mod is built
to host a family of cabinets (wood → iron → black metal → ...) sharing one
storage model; **WoodenCabinet** is the first prefab and ships in v0.1.

Design goals:

- Conserve world entities — one cabinet replaces six chests.
- Keep frame-rate parity for crossplatform players.
- Give players in-place organization (named tabs, fill indicators, content
  preview) without forcing them to scatter chests across a base.

---

## Phase 1: Project scaffolding

**Goal:** Stand up an empty BepInEx plugin that builds, deploys, and loads in
Valheim — no gameplay yet.

- [ ] Create `ExtendedStorage.csproj` modeled on `PersonalGateway.csproj`
      (net462, BepInEx 5.4.21 via NuGet, Jötunn reference, Valheim/Unity refs)
- [ ] `AssemblyName` / `RootNamespace` = `ExtendedStorage`
- [ ] `NuGet.config` matching PersonalGateway
- [ ] `src/ExtendedStoragePlugin.cs` — BepInPlugin entry, Harmony PatchAll, log
- [ ] Empty folder skeleton: `src/Config`, `src/Items`, `src/Patches`,
      `src/Pieces`, `src/Storage`, `src/State`, `src/UI`
- [ ] `assets/` placeholder folder
- [ ] `thunderstore/manifest.json` + `README.md` + icon stub
- [ ] `README.md` at mod root
- [ ] `DeployToBepInEx` target writing to
      `$(BepInExPlugins)/ExtendedStorage`
- [ ] Verify a Release build deploys to BepInEx and the plugin loads
      (Valheim log shows the plugin GUID)

**Deliverable:** `ExtendedStorage.dll` loads in-game, logs `Awake`, does
nothing else.

### Phase Notes

| Files / Areas | Purpose |
| --- | --- |
| `ExtendedStorage.csproj` | Build + auto-deploy mirror of PersonalGateway |
| `src/ExtendedStoragePlugin.cs` | BepInEx entry, Harmony bootstrap |
| `thunderstore/manifest.json` | Thunderstore metadata (deps: BepInEx + Jötunn) |

Key decisions:

- Reuse PersonalGateway's csproj layout verbatim; only names change.
- Plugin GUID `toutatis.extended_storage` (matches the `toutatis.<modname>`
  convention used by PersonalGateway and SpawnPointGateways).

---

## Phase 2: WoodenCabinet piece + recipe

**Goal:** Player can build a WoodenCabinet from the Hammer. Opening it does
nothing yet (or shows a vanilla chest temporarily); destroying it drops
materials.

- [ ] Clone vanilla `piece_chest_wood` prefab via Jötunn `PieceConfig`
- [ ] Register on the Hammer piece table under "Crafting" category
- [ ] Recipe: **100 wood + 20 resin**, station **workbench level 3**
- [ ] Placement: floor piece or flat ground (`m_groundPiece` /
      `m_allowedInDungeons` flags as per chest), forbid water/cliff
- [ ] Durability: **1000 HP**, wood damage type modifiers
- [ ] Decay parity with vanilla wooden chest — mirror its `WearNTear`
      settings exactly (no bespoke decay behavior in v0.1)
- [ ] Respect `PrivateArea` (ward) access checks — inherit from `Container`
- [ ] Hover name + description via Jötunn localization stubs
- [ ] Hammer placement ghost renders with the chest visual

**Deliverable:** WoodenCabinet builds, takes damage, drops 100 wood + 20
resin when destroyed, behaves like a normal chest on every other axis.

### Phase Notes

| Files / Areas | Purpose |
| --- | --- |
| `src/Pieces/WoodenCabinetPiece.cs` | Prefab clone + PieceConfig registration |
| `src/Items/AssetLoader.cs` | Mirrors PersonalGateway pattern for prefab fetching |
| `src/Config/CabinetConfig.cs` | HP, recipe, placement tunables exposed to BepInEx config |

Key decisions:

- Use vanilla wooden-chest prefab as visual base; bespoke art comes later.
- Keep recipe values in `CabinetConfig` so servers can tune without rebuild.

---

## Phase 3: Multi-inventory storage model

**Goal:** Six independent 15-slot inventories backed by the cabinet's ZDO, each
with a tab label. No UI yet — verified via logging and ZDO inspection.

- [ ] `src/Storage/CabinetStorage.cs`: holds six `Inventory` instances
      (1×15 each), all sized `1×15` for stacking parity with chests
- [ ] Component `CabinetContainer : MonoBehaviour` attached to the prefab —
      owns `CabinetStorage`, exposes "active tab" pointer
- [ ] ZDO fields per cabinet:
      - `es_tab{n}_inv` (n = 0..5): inventory blob via
        `Inventory.Save(ZPackage)` style serialization
      - `es_tab{n}_label`: string, default `(n+1).ToString()`
- [ ] Load on `Awake` / when ZDO becomes available, save on every mutation
      (debounced) and on `OnDestroyed`
- [ ] Ownership / sync: only ZDO owner writes, others read; trigger ZDO
      version bumps so remote clients refresh
- [ ] Treat tab 0 as the "compat tab" — its serialized form must also live in
      the vanilla `items` ZDO key so a removed mod still shows tab 1 contents
      via a normal chest
- [ ] Unit-walk: spawn cabinet, drop items into each tab via debug command,
      relog, verify all six tabs survive

**Deliverable:** A console command can push items into any tab and they
round-trip through save/load and across clients.

### Phase Notes

| Files / Areas | Purpose |
| --- | --- |
| `src/Storage/CabinetStorage.cs` | Six-inventory data model + serialization |
| `src/Storage/CabinetContainer.cs` | MonoBehaviour bridging prefab ↔ storage ↔ ZDO |
| `src/Patches/ContainerPatches.cs` | Intercept `Container` lifecycle for our prefab |
| `src/State/DebugCommands.cs` | Hidden console commands for round-trip testing |

Key decisions:

- **Six separate `Inventory` objects**, not one 90-slot inventory. Confirmed:
  cleaner per-tab ops (Take All, shift-click routing), simpler ZDO partitioning.
- Tab 0 inventory is mirrored into the standard chest `items` ZDO key so the
  cabinet degrades into a usable wooden chest if the mod is removed (other
  tabs sleep in ZDO custom fields and resurrect on reinstall).

---

## Phase 4: Tab strip UI

**Goal:** Opening a cabinet shows the inventory window with a tab strip above
it. Clicking a tab switches the active inventory; number keys 1–6 do the same.

- [ ] Patch `InventoryGui.Show(Container)` for our cabinet — render tab strip
      between window header and grid
- [ ] Tab sizing: width = `clamp(text_width + padding, 80, 250)`
- [ ] Row cascade: tabs flow LTR; overflow wraps to a second row above the
      window header (mirroring the Hammer build-menu category bar)
- [ ] Per-tab indicator: `12/15` rendered right-aligned inside the tab; dim
      when `0/15`
- [ ] Visual states: active / hover / inactive / edit-pending
- [ ] Click a tab → switches `CabinetStorage.ActiveTab` and rebinds the
      visible `Inventory` to the grid (no copy — pointer swap)
- [ ] Keys 1..6 switch tabs while the window is open
- [ ] Mouse wheel reserved for content scrolling — do not intercept
- [ ] Shift-click on a player-inventory slot routes to the **active tab
      only** (no overflow to other tabs)
- [ ] "Take All" affects active tab only

**Deliverable:** Six functional tabs, sized to labels, with live fill counts;
items move in/out of the active tab via click and shift-click.

### Phase Notes

| Files / Areas | Purpose |
| --- | --- |
| `src/UI/CabinetTabStrip.cs` | Tab strip layout + row cascade |
| `src/UI/CabinetTab.cs` | Single tab view (label + fill indicator + states) |
| `src/Patches/InventoryGuiPatches.cs` | Hook `Show` / shift-click / Take All |

Key decisions:

- Active-tab-only shift-click — predictability over convenience.
- Tab strip lives above the window header (not inside) so it can wrap to a
  second row without resizing the inventory window itself.

---

## Phase 5: Tab label editing

**Goal:** Players can rename tabs in-place. Default labels are `1`–`6`.
Hovering a tab + pressing **Shift+E** enters edit mode without activating the
tab.

- [ ] Hover detection on tab while inventory window is open
- [ ] **Shift+E** while hovering enters edit mode for that tab (does not
      switch active tab, does not trigger the world-Use action — guard
      against `Player.UseHotkey` triggering on `E`)
- [ ] Edit mode UX:
      - Text cursor visible, blinks
      - Cursor keys move caret; Shift+arrow selects; Ctrl/Cmd+A select all;
        Ctrl/Cmd+C / V / X cut/copy/paste
      - Backspace / Delete erase; printable chars insert
      - 25-character hard cap
      - Width recomputes live as text changes (clamped 80–250 px)
      - Initial state preserves the current label, including a default
        `1`–`6` — user can delete it like normal text
- [ ] **Enter** commits the new label (writes ZDO, syncs to other clients)
- [ ] **Esc** discards, restores previous label
- [ ] Closing the inventory window while editing discards; if the previous
      value was the default number, it stays as default
- [ ] Empty label after commit → revert to default tab number
- [ ] Sync: label ZDO field updates propagate to other clients viewing the
      same cabinet (existing patches refresh tab strip on ZDO version bump)

**Deliverable:** Players name tabs freely with full text-editing affordances;
labels persist and replicate; tab strip relayouts as widths change.

### Phase Notes

| Files / Areas | Purpose |
| --- | --- |
| `src/UI/CabinetTabEditor.cs` | Caret, selection, key handling, clipboard |
| `src/UI/CabinetTabStrip.cs` | Width recompute + row reflow on edit |
| `src/Patches/InputPatches.cs` | Block world-Use while editing a tab |

Key decisions:

- **Shift+E** chosen over plain `E` to avoid colliding with vanilla Use.
- Discard-on-close (rather than auto-save) prevents accidental commits when a
  player closes the window mid-thought.

---

## Phase 6: Shift+hover content panel — COMPLETE

**Goal:** Without opening the cabinet, holding **Shift** while the crosshair is
over a cabinet shows a per-tab icon-grid panel summarizing all six tabs.
Design reference: CDEVx's `ChestItemsHoverDisplay` (approach only — no copied
code; their published source is decompiled and not safely reusable).

- [x] Harmony **postfix** on `Hud.UpdateCrosshair` — detect hover target,
      branch to our panel when it resolves to one of our cabinets
- [x] `PrivateArea.CheckAccess()` gate — if the player can't open the
      cabinet, don't show contents (config toggle to override for solo play)
- [x] Show/hide rule: **only while Shift is held** AND crosshair is on the
      cabinet. Releasing Shift, looking away, or opening the cabinet hides
      the panel and restores the standard `WoodenCabinet — $piece_use` text.
- [x] Panel layout: vertical stack of six **tab sections**, in tab order:
      - Section header: `[label]  N/15`
      - Icon grid: 5 columns × 3 rows (matches the per-tab 15-slot shape),
        sprite + stack count per slot, empty slots dimmed
      - Skip section render entirely if `0/15`? — default **show all six**
        so the layout is stable; revisit if it feels noisy
- [ ] Empty cabinet → render a compact `WoodenCabinet (empty)` card instead
      of six empty grids _(deferred — playtest first; layout stability won)_
- [x] Cell size: default 36 px, exposed via BepInEx config (24–64 range)
- [x] Panel anchored to the cursor area (mirror vanilla hover-text anchor) —
      offset so it doesn't occlude the crosshair
- [x] Refresh cadence: rebuild on ZDO version change, not every frame
      (cache the rendered grid; invalidate on `m_dataRevision` bump)
      _(implemented as a 250 ms throttle — same cache window as Phase 7 plans,
      and avoids fragile direct access to ZDO version internals)_
- [x] Verify it works with our 6-inventory model by reading our own
      `CabinetStorage` directly — do **not** route through the aggregated
      `GetInventory()` shim from Phase 7 (that's for third-party mods)

**Deliverable:** Shift-holding while looking at a cabinet pops a tabbed icon
panel; releasing shift restores the normal hover prompt.

### Phase Notes

| Files / Areas | Purpose |
| --- | --- |
| `src/Patches/HudCrosshairPatch.cs` | `Hud.UpdateCrosshair` postfix → cabinet detection + show/hide |
| `src/UI/CabinetHoverPanel.cs` | Panel root, lifecycle, anchoring, shift-key gating |
| `src/UI/CabinetHoverTabSection.cs` | One tab's header + icon grid |
| `src/Config/CabinetConfig.cs` | `Hover.CellSize`, `Hover.IgnoreWard` toggles added in v0.2.1 |

Key decisions:

- Hook on `Hud.UpdateCrosshair`, **not** `Hoverable.GetHoverText` — gives us
  freedom to render a real panel instead of squeezing into the hover string.
- Bind directly to `CabinetStorage` (not the aggregated `Container` view) so
  the panel can render per-tab sections.
- Show all six sections even when empty for visual stability; revisit after
  playtest.
- Our own panel is the canonical UX — third-party content-display mods are
  best-effort (see Phase 7).

---

## Phase 7: Third-party compatibility + destruction handling

**Goal:** Existing chest-content mods see *something* useful, and breaking the
cabinet does not stutter low-end clients.

- [ ] Patch `Container.GetInventory()` for our prefab to return an
      aggregated **read-only snapshot** that merges all six tabs into one
      virtual inventory
      - Rebuilt lazily on each call with a short cache window (~250 ms)
      - Only used by external read paths; our own UI binds directly to the
        per-tab `Inventory` objects
- [ ] Document the limitation: mods that try to *move* items via this virtual
      inventory will not route correctly (writes are no-ops)
- [ ] Drop-on-destroy stagger:
      - On `Destroy` / `WNT.OnDestroyed`, collect all items across tabs
      - Spawn drops in batches of ~5 items per frame on the next several
        `LateUpdate` ticks, randomized within the cabinet bounds
      - Cancel staggered drops if the player picks up the ZDO area (edge
        case: server shutdown mid-stagger — accept item loss with a
        warning log, since the cabinet ZDO is already gone)
- [ ] Verify ward access path: only PrivateArea-permitted players can open,
      shift-hover content preview still works for the owner

**Deliverable:** Popular content-display mods can list cabinet contents; a
destroyed cabinet drops items across multiple frames instead of one spike.

### Phase Notes

| Files / Areas | Purpose |
| --- | --- |
| `src/Patches/ContainerAggregatePatch.cs` | Read-only virtual inventory for `GetInventory()` |
| `src/Storage/StaggeredDropper.cs` | Frame-spread drop queue |

Key decisions:

- Aggregate view is read-only by design. Writes via `GetInventory()` would be
  ambiguous (which tab?) and silently misroute — better to no-op and document.
- Stagger budget defaults to 5 items / frame; configurable via BepInEx config.

---

## Phase 8: Localization, packaging, release prep

**Goal:** Mod is shippable to Thunderstore.

- [ ] `src/UI/LocalizationLoader.cs` mirroring PersonalGateway, with
      `assets/locales/en.json` covering: piece name, description, recipe,
      hover prompts, tab-edit prompt, empty-tab string
- [ ] Update `thunderstore/manifest.json` with deps, website, version
- [ ] Thunderstore README — feature summary + screenshots
- [ ] `CHANGELOG.md` starting at `0.1.0`
- [ ] GitHub Actions release workflow (mirror PersonalGateway's if present)
- [ ] Manual playtest matrix: solo / dedicated server / two-client sync /
      mod-removed fallback / shift-hover with another content mod loaded

**Deliverable:** Tagged `v0.1.0` release with Thunderstore bundle.

### Phase Notes

| Files / Areas | Purpose |
| --- | --- |
| `assets/locales/en.json` | English strings |
| `thunderstore/` | Release bundle metadata + icon + README |
| `CHANGELOG.md` | Per-version notes |

---

## Decision log

| Date | Decision | Why |
| --- | --- | --- |
| 2026-06-03 | Mod name `ExtendedStorage`, first prefab `WoodenCabinet` | Family of cabinets planned (iron, black metal); umbrella mod avoids one-mod-per-tier sprawl |
| 2026-06-03 | Six independent 15-slot inventories, not one 90-slot | Cleaner per-tab Take All / shift-click; simpler ZDO partitioning per tab |
| 2026-06-03 | Tab 0 mirrored into vanilla `items` ZDO key | Graceful fallback to wooden chest if mod is uninstalled; reinstall resurrects other tabs |
| 2026-06-03 | Shift-click → active tab only | Predictability over convenience |
| 2026-06-03 | Shift+E enters label edit (not plain E) | Avoid collision with vanilla `Use` interact |
| 2026-06-03 | Enter saves label, Esc discards, close-mid-edit discards | Prevents accidental commits |
| 2026-06-03 | Empty label reverts to default tab number `1`–`6` | Tabs always have a visible identifier |
| 2026-06-03 | Number keys 1–6 switch tabs; mouse wheel reserved for content scroll | Conventional + non-conflicting |
| 2026-06-03 | Per-tab fill indicator `N/15` on the tab itself | Removes guesswork before clicking |
| 2026-06-03 | Patch `Container.GetInventory()` as a read-only aggregate | Best-effort compatibility with chest-content mods; writes intentionally no-op |
| 2026-06-03 | Own shift+hover content preview is the canonical UX | Guaranteed to work regardless of third-party mod presence |
| 2026-06-03 | Drop-on-destroy staggered at ~5 items / frame | Avoid frame-spike when 90 items spawn at once |
| 2026-06-03 | Respect `PrivateArea` (ward) access like vanilla chest | Consistent with player expectation |
| 2026-06-03 | Plugin GUID `toutatis.extended_storage` | Matches existing Toutatis mod GUID convention (`toutatis.<modname>`) |
| 2026-06-03 | Decay parity with vanilla wooden chest in v0.1 | Predictable for players; can revisit if cabinets warrant different decay later |
| 2026-06-03 | Phase 6 upgraded from text tooltip to icon-grid panel | CDEVx's `ChestItemsHoverDisplay` showed `Hud.UpdateCrosshair` is the better hook; richer UX for 90-slot cabinets |
| 2026-06-03 | Six tab sections rendered even when empty | Layout stability while shift-hovering; revisit after playtest if too noisy |
| 2026-06-03 | CDEVx mod used as design reference only, no copied code | Their published source is decompiled — licensing is murky; we reimplement the approach cleanly |
