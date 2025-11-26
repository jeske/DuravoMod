# Terraria Survival Overhaul Mod - Project Structure

## Project Purpose

A tModLoader mod that surgically removes exploits that trivialize Terraria's exploration and combat. The core philosophy: Terraria presents platformer-combat aesthetics but gives players tools that make engagement optional. This mod removes the worst offenders while preserving legitimate building and crafting QoL.

**Key Goals:**
- Force engagement with the game's combat systems
- Make exploration committal and risky
- Depth = danger (deeper = harder)
- Remove instant-escape mechanics
- Make underground enemies actually threatening

---

## Development Philosophy: INCREMENTAL ONLY

**CRITICAL RULE: We do NOT pre-scaffold the project.**

This means:
- No creating empty folders "for later"
- No creating stub files that aren't being actively coded
- No placeholder code
- Each file is created ONLY when we're implementing that feature

**Why this approach:**
1. Reduces confusion about what's actually implemented vs. planned
2. Avoids stale/outdated scaffolding that drifts from reality
3. Forces us to think about dependencies in order
4. Makes the codebase always represent "what works now"

**What IS allowed:**
- This document can contain a *planned* directory structure
- DOCS folder can contain specifications and ideas
- We create source files only when actively implementing them

---

## Collaborative Testing Workflow

**Important:** The AI assistant cannot run or test Terraria/tModLoader directly. 

**How we develop:**
1. AI writes code based on specifications and tModLoader documentation
2. Human builds and loads the mod in tModLoader
3. Human tests in-game and reports results back
4. AI adjusts code based on feedback
5. Repeat until feature works correctly

**This means:**
- First priority is getting ANY mod working (build → load → test cycle)
- Start with simplest possible feature to validate the pipeline
- Each feature must be testable in isolation before moving on

---

## Implementation Order (Simplest → Complex)

### Phase 0: Project Setup & Pipeline Validation
**Goal:** Create minimal mod that builds, loads, and does something observable

Planned first feature: **Persistent Player Position**
- Why: Simple to implement (just save/load position)
- Why: Easy to test (logout in cave, reload, check if still in cave)
- Why: No complex interactions with other systems
- Why: Uses basic tModLoader APIs (ModPlayer, SaveData/LoadData)

### Phase 1: Foundational Systems
1. **Persistent Player Position** - Logout anywhere, load back there
2. **Line-of-Sight System** - Core raycast utility used by multiple features

### Phase 2: Core Anti-Cheese Mechanics
3. **LOS Block Interactions** - Can't place/mine through walls
4. **Depth-Scaled Enemy Damage** - Deeper = enemies hit harder
5. **Dig-Activity Spawn Rate** - Mining creates noise, attracts enemies

### Phase 3: Enemy Behavior
6. **Smart Hopping** - Zombies calculate jumps to land on walls
7. **Aggro Burrowing** - Blocked enemies dig toward you

### Phase 4: Travel & Economy
8. **Travel Rework** - Remove recall items, add Portal Stones
9. **Minion Tethering** - Summons must stay near player

### Phase 5: Balance & Polish
10. **Enemy Rebalancing** - Adjust surface damage for fairness
11. **Combat Zone Block Lock** - Can't build mid-fight
12. **Configuration System** - Toggleable features

---

## Documentation Structure

```
DOCS/
├── Terraria-Survival-Mod-SPEC.md    # Main specification (AUTHORITATIVE)
│                                     # Contains detailed implementation plans
│                                     # for all committed features
│
├── WIP-DEFINITELY-IDEAS/             # Features we WILL implement
│   │                                 # Not yet ready for main spec
│   │                                 # May change significantly
│   │                                 # Move to main spec when solidified
│   └── [feature-name].md
│
├── WIP-MAYBE-IDEAS/                  # Brainstorms and possibilities
│   │                                 # Probably will NOT implement
│   │                                 # Exploratory thinking only
│   │                                 # Do not invest time here
│   └── [crazy-idea].md
│
└── [supporting-docs].md              # Analysis, research, references
    └── (like ore-mining-stats-analysis.html)
```

**Document Lifecycle:**
1. Idea starts in `WIP-MAYBE-IDEAS/` (if uncertain)
2. Promising ideas move to `WIP-DEFINITELY-IDEAS/`
3. When design is solid, content moves to main `Terraria-Survival-Mod-SPEC.md`
4. Main spec is the source of truth for implementation

---

## Planned Source Structure (DO NOT CREATE YET)

When we start coding, the structure will grow like this:

```
Source/                              # Created with Phase 0
├── TerrariaSurvivalMod.cs           # Main mod class (Phase 0)
│
├── Players/                         # Created when needed
│   └── PersistentPositionPlayer.cs  # Phase 0-1
│
├── Systems/                         # Created when needed
│   └── LineOfSightSystem.cs         # Phase 1
│
├── NPCs/                            # Created when needed
│   ├── DepthScaledDamage.cs         # Phase 2
│   ├── SmartHopper.cs               # Phase 3
│   └── AggroBurrower.cs             # Phase 3
│
├── Projectiles/                     # Created when needed
│   └── TetheredMinion.cs            # Phase 4
│
├── Items/                           # Created when needed
│   └── PortalStone.cs               # Phase 4
│
└── Utilities/                       # Created when needed
    ├── RaycastUtils.cs              # Phase 1
    └── BiomeUtils.cs                # Phase 4
```

**Remember:** This is a PLAN. These folders/files don't exist until we're coding them.

---

## Required Project Files (Phase 0)

These are the minimum files tModLoader needs:

```
TerrariaSurvivalMod/
├── build.txt                        # Mod metadata
├── description.txt                  # Mod description for browser
├── Source/
│   └── TerrariaSurvivalMod.cs       # Main mod class
└── (icon.png)                       # Optional, add later
```

---

## Technology Stack

- **Language:** C#
- **Framework:** tModLoader (Terraria modding API)
- **Target:** .NET (version depends on current tModLoader)
- **IDE:** Visual Studio or VS Code with C# extension
- **Build:** tModLoader's built-in build system

---

## Development Environment Setup

**Path configuration is machine-specific.** tModLoader and Terraria can be installed in different locations.

**Setup:**
1. Copy `.env.example` to `.env`
2. Fill in your local paths
3. Never commit `.env` (it's gitignored)

See [`.env.example`](.env.example) for detailed instructions, common paths, and symlink setup.

---

## Current Status

**Phase:** Not started
**Next Step:** Set up tModLoader development environment and create minimal working mod

---

## References

- [tModLoader Wiki](https://github.com/tModLoader/tModLoader/wiki)
- [tModLoader Documentation](https://docs.tmodloader.net/)
- Main Spec: `DOCS/Terraria-Survival-Mod-SPEC.md`