# Duravo QOL Mod - Feature Specification

A tModLoader mod focused on quality-of-life improvements that make Terraria more intuitive and fun, without fundamentally changing the game's balance or difficulty.

---

## Implementation Status

### Core QOL Features
- [x] **Persistent Player Position** - Log back in where you left off
- [x] **Smarter Minion Pathfinding** - Minions navigate around obstacles via A* pathfinding
- [x] **Armor Visual Feedback** - Emergency Shield with damage-blocked feedback
- [x] **Crafted Armor Rework** - Defense baked into pieces, 2pc mix-and-match set bonuses
- [x] **Shiny Ore Detection** - 2pc crafted armor bonus makes nearby ores sparkle

### In Progress
- [ ] **Enemy Smart Hopping** - NPCs calculate jump trajectories better (code exists, needs testing)

### Planned
- [ ] **Mini-Player Healthbar** - Auto-Hiding mini healthbar below player to visualize damage
- [ ] **Crafting UI Improvements** - Better pattern discoverability in crafting menu
- [ ] **Craftable Rope from Fiber** - Craft rope from plant fibers (vine, cobweb, etc.)
- [ ] **Native Biome Bootstrap** - Establish bases anywhere with local materials (beds, workbenches)
- [ ] **Shared Minimap State** - shares reveal state among co-op players

### Maybe

- [ ] **Enemy Mini Healthbars** - Small health bars above enemies after disengaging to show if you are close to killing them
- [ ] **Sort Armor by Tier** - inventory sort should sort armors by strength/tier not alphabetical

---

## Core Philosophy

Quality of life is about reducing friction, not changing the game. Features should:

1. **Feel like they should have been in vanilla** - No one looks at these and thinks "that's OP"
2. **Respect player time** - Eliminate tedious repetition, not challenge
3. **Add visual clarity** - Help players understand game mechanics
4. **Be individually toggleable** - Player choice matters

---

## Feature Details

### Persistent Player Position

**What it does:** Saves your exact position when you log out. When you return, you're exactly where you left off instead of at world spawn.

**Why it matters:** 
- No more losing exploration progress to real life
- No more logout cheese to escape danger (medium/hardcore)
- Spawn immunity on load prevents unfair deaths

**Config options:**
- Enable/disable per world
- Spawn immunity duration

---

### Smarter Minion Pathfinding

**What it does:** Minions use A* pathfinding to navigate around terrain instead of getting stuck. When truly isolated from the player, they teleport back.

**Why it matters:**
- Minions actually function in caves and buildings
- No more minions stuck on the other side of a wall
- Prevents AFK farming exploit (minions won't fight in another room)

**Config options:**
- Leash distance
- Enable/disable teleport behavior

---

### Crafted Armor Rework

**What it does:** 
- Defense bonuses baked into individual pieces (no penalty for breaking a set)
- New 2-piece set bonuses that work across armor tiers
- Visual feedback on shield absorption

**Set Bonuses (2pc any crafted armor):**
| Set | 2pc Bonus |
|-----|-----------|
| Copper/Tin | Emergency Shield (absorbs one hit, 60s cooldown) |
| Iron/Lead | +10% crit chance |
| Silver/Tungsten | +15% move speed |
| Gold/Platinum | Enhanced Emergency Shield (absorbs one hit, 30s cooldown) |

**Why it matters:**
- Finding one better piece is ALWAYS an upgrade (no set penalty)
- Mix-and-match armor becomes viable
- Early game feels more rewarding

---

### Shiny Ore Detection

**What it does:** With any 2pc crafted armor set bonus, ores just outside torch light range sparkle, making them easier to spot while mining.

**Why it matters:**
- Reduces tedious missed-ore anxiety
- Makes early mining more engaging
- Visual reward for wearing crafted armor

**Config options:**
- Sparkle intensity
- Detection range

---

### Crafting UI Improvements (Planned)

**What it does:** Improves recipe discoverability in the crafting menu.

**Possible features:**
- Show recipes you're close to being able to craft
- Highlight new recipes when you pick up new materials
- Better categorization/filtering

---

### Craftable Rope from Fiber (Planned)

**What it does:** Allows crafting rope from various plant fibers found throughout the world.

**Recipes:**
| Material | Rope Output |
|----------|-------------|
| 3 Vine | 10 Rope |
| 10 Cobweb | 10 Rope |
| 5 Jungle Grass Seeds | 10 Rope |

**Why it matters:**
- Rope is essential but inconsistently available
- Uses materials that are often abundant but useless
- Encourages exploration of different biomes

---

### Native Biome Bootstrap (Planned)

**What it does:** Allows players to establish functional bases in any biome using only local materials.

**Core additions:**
- Workbenches craftable from biome-native materials (bone, ice, etc.)
- Beds craftable from local padding materials (cobweb, hay, vertebrae, etc.)
- All basic crafting stations accessible in every biome

**Why it matters:**
- Supports diverse playstyles (underground-only runs, etc.)
- Reduces tedious surface trips
- Makes every biome feel like a viable home

See: [biome-crafting-bootstrap.md](WIP-DEFINITELY-IDEAS/biome-crafting-bootstrap.md) for detailed recipes.

---

## Configuration

All features are individually toggleable via mod config menu. Default: all enabled.

```
DuravoQOLModConfig
├── PersistentPosition (bool)
├── SmartMinionPathfinding (bool)  
├── CraftedArmorRework (bool)
├── ShinyOreDetection (bool)
├── ShinySparkleIntensity (float 0-1)
├── CraftingUIImprovements (bool)
├── CraftableRope (bool)
└── NativeBiomeBootstrap (bool)
```

---

## Testing Checklist

### Persistent Position
- [ ] Logout in Hell, reload - should be in Hell
- [ ] Logout in cave, cave gets filled in by world edit - should fall back to spawn
- [ ] Die, then logout - should NOT restore death position
- [ ] Spawn immunity prevents unfair deaths on load

### Smarter Minions
- [ ] Minion navigates around L-shaped obstacle to reach target
- [ ] Minion teleports back when completely separated by solid wall
- [ ] Minion does NOT attack enemies in rooms player can't reach

### Armor Rework
- [ ] Single piece of Iron armor provides proportional defense
- [ ] 2pc Copper gives Emergency Shield buff
- [ ] Shield absorbs hit and shows "(blocked)" combat text
- [ ] Shield cooldown displays correctly

### Shiny Ore Detection
- [ ] With 2pc armor, ores outside torch range sparkle
- [ ] Sparkles don't appear on already-visible ores
- [ ] Different ore types all sparkle correctly

---

## Future Ideas (Maybe)

- Super Shiny buff for higher tier armor (longer range, ignores low-tier ores)
- Crafting history/favorites
- Quick-stack improvements
- Better boss summon item tooltips

---

## Resources

- [tModLoader Wiki](https://github.com/tModLoader/tModLoader/wiki)
- [tModLoader Documentation](https://docs.tmodloader.net/)
- Project Structure: [_PROJECT_STRUCTURE.md](../_PROJECT_STRUCTURE.md)