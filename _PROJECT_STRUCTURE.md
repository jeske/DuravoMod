# Terraria Survival Overhaul Mod - Project Structure

## Project Purpose

A tModLoader mod that surgically removes exploits that trivialize Terraria's exploration and combat. The core philosophy: Terraria presents platformer-combat aesthetics but gives players tools that make engagement optional. This mod removes the worst offenders while preserving legitimate building and crafting QoL.

**See [`_DOCS/Terraria-Survival-Mod-SPEC.md`](_DOCS/Terraria-Survival-Mod-SPEC.md) for feature specifications and implementation ordering.**

---

## Development Philosophy: INCREMENTAL ONLY

This means:
- No creating empty folders "for later"
- No creating stub files that aren't being actively coded
- Each file is created ONLY when implementing that feature
- The codebase always represents "what works now"

---

## Project Structure

```
TerrariaSurvivalMod/
├── _PROJECT_STRUCTURE.md        # This file - project overview
├── _TASKS/                      # Current work items
│   └── _TOP_LEVEL_TODO.md
│
├── _DOCS/                       # All design documentation
│   ├── Terraria-Survival-Mod-SPEC.md    # AUTHORITATIVE spec & implementation order
│   ├── WIP-DEFINITELY-IDEAS/            # Features we WILL implement (not yet in main spec)
│   ├── WIP-MAYBE-IDEAS/                 # Brainstorms (probably won't implement)
│   ├── TERRARIA_REFERENCE/              # Research & reference materials
│   └── FUN/                             # Experiments, simulators
│
├── Source/                      # All mod source code (feature-organized)
│   ├── TerrariaSurvivalMod.cs           # Main mod class
│   ├── TerrariaSurvivalModConfig.cs     # Mod configuration
│   ├── ArmorRebalance/                  # Early armor overhaul feature
│   ├── EnemySmartHopping/               # Zombie jump calculation feature
│   ├── PersistentPosition/              # Logout position saving feature
│   └── TetheredMinions/                 # Minion tethering/pathfinding feature
│
├── Localization/                # Internationalization files
│   └── en-US.hjson                      # English strings (primary)
│   # Target: All 9 Terraria languages:
│   # English, French, Italian, German, Spanish-Spain,
│   # Polish, Portuguese-Brazil, Russian, Simplified Chinese
│
├── Properties/                  # .NET project properties
│   └── launchSettings.json
│
├── .vscode/                     # VS Code workspace settings
├── image/                       # Mod assets (icons, sprites)
│
├── build.txt                    # tModLoader mod metadata
├── description.txt              # Mod description for browser
├── icon.png                     # Mod icon
└── TerrariaSurvivalMod.sln      # Visual Studio solution
```

---

## Source Code Organization

Code is organized by **feature**, not by tModLoader hook type. Each feature directory contains ALL files related to that feature.

```
Source/
├── [FeatureName]/
│   ├── SomePlayer.cs        # ModPlayer hooks
│   ├── SomeGlobalNPC.cs     # GlobalNPC hooks
│   ├── SomeGlobalItem.cs    # GlobalItem hooks
│   └── image/               # Screenshots for debugging/docs
```

**Why feature-based:**
- All code for a feature lives together
- Adding/removing features is clean
- Easy to understand what a feature does

---

## Documentation Lifecycle

1. Idea starts in `WIP-MAYBE-IDEAS/` (if uncertain)
2. Promising ideas move to `WIP-DEFINITELY-IDEAS/`
3. When design is solid, content moves to main `Terraria-Survival-Mod-SPEC.md`
4. Main spec is the source of truth for implementation order

---

## Development Environment Setup

tModLoader paths are machine-specific.

1. Copy `.env.example` to `.env`
2. Fill in your local paths
3. Never commit `.env` (it's gitignored)

See [`.env.example`](.env.example) for detailed instructions.

---

## References

- [tModLoader Wiki](https://github.com/tModLoader/tModLoader/wiki)
- [tModLoader Documentation](https://docs.tmodloader.net/)
- Feature Spec & Roadmap: [`_DOCS/Terraria-Survival-Mod-SPEC.md`](_DOCS/Terraria-Survival-Mod-SPEC.md)
