# Terraria Minion Leash & Detection Mechanics

**Source References:**
- ProjectileID constants: [Decompiled Terraria 1.4.0.5 - ProjectileID.cs](https://raw.githubusercontent.com/AdamSavard/Terarria1405/refs/heads/master/ID/ProjectileID.cs)
- Leash distances: [Official Terraria Wiki](https://terraria.wiki.gg) (citations reference `Desktop 1.4.4.9 source code`)
- AI methods: `Terraria.Projectile.cs` → `AI()`, `AI_026()`, `AI_062()`, `AI_066()`, `AI_067_FreakingPirates()`, `AI_121_StardustDragon()`

---

## Key Insight: No Declarative Leash Data

Leash distances are **NOT** exposed in `ProjectileID.Sets` arrays. Unlike yoyos (which have `YoyosMaximumRange`), minion return distances are **hardcoded in AI switch statements** per `aiStyle`. This means there's no simple lookup—you must read the AI code for each minion type.

---

## Complete Minion Table

| Minion | Staff | ProjectileID(s) | Detection | Return (Leash) | aiStyle | Notes |
|--------|-------|-----------------|-----------|----------------|---------|-------|
| **Baby Finch** | Finch Staff | 759 `BabyBird` | 50 tiles | 50 tiles | 158 | Rests on player's head when idle |
| **Baby Slime** | Slime Staff | 266 `BabySlime` | 50 + 2.5×pos | **62.5 + 2.5×pos** | 26 | Ground minion, same AI as Pygmy |
| **Flinx** | Flinx Staff | 895-896 | 50 tiles | ~500px (twirls back) | 124 | Bounces on contact; **cannot phase**; twirls to player when too far; 1.4.1+ |
| **Abigail** | Abigail's Flower | 979 `Abigail` | 125 tiles (187.5†) | **62.5 tiles** | 62 | 1.4.3+; phases through blocks during attack; scales with summons |
| **Hornet** | Hornet Staff | 373 `Hornet` | 125 tiles (187.5†) | **31.25 tiles** | 62 | 500px base / 1000px targeted |
| **Flying Imp** | Imp Staff | 375 `FlyingImp` | 125 tiles (187.5†) | **31.25 tiles** | 62 | 500px base; Shoots 376 `ImpFireball` |
| **Vampire Frog** | Vampire Frog Staff | 758 `VampireFrog` | 50 tiles | — (parrot) | 67 | Same AI as Pirate; can swim |
| **Spider** | Spider Staff | 390 `VenomSpider`, 391 `JumperSpider`, 392 `DangerousSpider` | 50 + 2.5×pos | **87.5 + 2.5×pos** | 26 | `minionPos` scaling; wall-climbing |
| **Enchanted Dagger** | Blade Staff | 864 `Smolstar` | 50 tiles | **56.25 tiles** | — | Only 75% whip tag damage |
| **Twins** | Optic Staff | 387 `Retanimini`, 388 `Spazmamini` | 125 tiles | **75 tiles** ⚠️ | 66 | **TIGHTEST flying leash** - interrupts attacks frequently |
| **Pirate** | Pirate Staff | 393 `OneEyedPirate`, 394 `SoulscourgePirate`, 395 `PirateCaptain` | 50 tiles | **31.25 tiles** (parrot) | 67 | Parrot carries back at 500px |
| **Pygmy** | Pygmy Staff | 191-194 `Pygmy`, `Pygmy2`, `Pygmy3`, `Pygmy4` | 50 + 2.5×pos | **62.5 + 2.5×pos** | 26 | Shoots 195 `PygmySpear`; `minionPos` scaling |
| **Raven** | Raven Staff | 317 `Raven` | 56.25 tiles | **87.5 tiles** | 54 | Bounces at 0.6x velocity on collision |
| **Sanguine Bat** | Sanguine Staff | 755 `BatOfLight` | — | — (phases) | 156 | `tileCollide = false`; always phases |
| **Desert Tiger** | Desert Tiger Staff | 831-835 `StormTiger*` | 100×50 rect | — | — | Pounce-based, not traditional leash |
| **Sharknado** | Tempest Staff | 407 `Tempest` | 125 tiles (187.5†) | **125 tiles** | 62 | `tileCollide` forced false; Shoots 408 `MiniSharkron` |
| **Deadly Sphere** | Deadly Sphere Staff | 533 `DeadlySphere` | 50 tiles | **93.75 tiles** | 66 | Bounces with velocity reversal on collision |
| **UFO** | Xeno Staff | 423 `UFOMinion` | 125 tiles (187.5†) | **75 tiles** (1200px targeted) | 62 | Teleports 18.75-50 tiles to target |
| **Stardust Cell** | Stardust Cell Staff | 613 `StardustCellMinion` | 125 tiles (187.5†) | **84.4 tiles** (1350px targeted) | 62 | Teleports 28.75-53.75 tiles; shoots 614 `StardustCellMinionShot` |
| **Stardust Dragon** | Stardust Dragon Staff | 625-628 `StardustDragon1-4` | 62.5 tiles (87.5†) | **125 tiles** | 121 | Head segment (625); `tileCollide = false` |
| **Terraprisma** | Terraprisma | 946 `EmpressBlade` | — | — (phases) | 156 | `tileCollide = false`; always phases |

**Legend:**
- `pos` = `minionPos` (slot index, starting at 0)
- † = Range when player targets enemy with whip/summon weapon
- ⚠️ = Notably problematic value

---

## ProjectileID.Sets Relevant to Minions

From `ProjectileID.cs` (1.4.0.5):

```csharp
// Minion projectile shots (NOT the minion itself)
public static bool[] MinionShot = ProjectileID.Sets.Factory.CreateBoolSet(
    374,  // HornetStinger
    376,  // ImpFireball  
    389,  // MiniRetinaLaser
    195,  // PygmySpear
    408,  // MiniSharkron
    433,  // UFOLaser
    614   // StardustCellMinionShot
);

// Stardust Dragon segments (need UUID for segment linking)
public static bool[] NeedsUUID = ProjectileID.Sets.Factory.CreateBoolSet(625, 626, 627, 628);
public static bool[] StardustDragon = ProjectileID.Sets.Factory.CreateBoolSet(625, 626, 627, 628);

// Desert Tiger variants
public static bool[] StormTiger = ProjectileID.Sets.Factory.CreateBoolSet(833, 834, 835);

// Minions that support targeting
public static bool[] MinionTargettingFeature = ProjectileID.Sets.Factory.CreateBoolSet(
    191, 192, 193, 194,  // Pygmy
    266,                  // BabySlime
    317,                  // Raven
    373, 375,            // Hornet, FlyingImp
    387, 388,            // Twins
    390,                  // Spider (only VenomSpider listed)
    393,                  // Pirate (only OneEyedPirate listed)
    407, 423,            // Tempest, UFO
    533,                  // DeadlySphere
    613,                  // StardustCell
    625,                  // StardustDragon (head)
    755, 758, 759,       // BatOfLight, VampireFrog, BabyBird
    831, 833, 834, 835,  // StormTiger variants
    864,                  // Smolstar (Blade Staff)
    946                   // EmpressBlade (Terraprisma)
    // ... plus sentries
);

// Can be sacrificed when re-summoning
public static bool[] MinionSacrificable = ProjectileID.Sets.Factory.CreateBoolSet(
    191, 192, 193, 194,  // Pygmy
    266,                  // BabySlime
    317,                  // Raven
    373, 375,            // Hornet, FlyingImp
    387, 388,            // Twins
    390,                  // Spider
    393,                  // Pirate
    407, 423,            // Tempest, UFO
    533,                  // DeadlySphere
    613,                  // StardustCell
    755, 758, 759,       // BatOfLight, VampireFrog, BabyBird
    831, 864, 946,       // StormTiger, Smolstar, EmpressBlade
    625, 626, 627, 628   // StardustDragon segments
);
```

---

## Leash Distance Tiers

**Note:** Values in pixels. 1 tile = 16 pixels. These are the distances at which minions trigger return/phasing mode.

### Tight Leash (≤500 pixels / 31.25 tiles) - Frequently Interrupts Combat
| Minion | Return Distance | Notes |
|--------|----------------|-------|
| Hornet | 500px (31.25 tiles) | 1000px with target |
| Flying Imp | 500px (31.25 tiles) | 1000px with target |
| Pirate | 500px (31.25 tiles) | Parrot carries back |
| Pygmy (base) | 1000px (62.5 tiles) | +40px per minionPos |
| Baby Slime | 1000px (62.5 tiles) | Same as Pygmy |

### Medium Leash (1000-1500 pixels / 62.5-93.75 tiles)
| Minion | Return Distance | Notes |
|--------|----------------|-------|
| **Twins** | 1200px (75 tiles) | **Tightest flying minion** |
| UFO | 1000px / 1200px | 1200px with target |
| Stardust Cell | 1000px / 1350px | 1350px with target |
| Raven | 1400px (87.5 tiles) | |
| Spider (base) | 1400px (87.5 tiles) | +40px per minionPos |
| Deadly Sphere | 1500px (93.75 tiles) | |

### Loose Leash (2000 pixels / 125 tiles) - Rarely Interrupts
| Minion | Return Distance | Notes |
|--------|----------------|-------|
| Sharknado | 2000px (125 tiles) | Always phases anyway |
| Stardust Dragon | 2000px (125 tiles) | Always phases anyway |

### Always Phase (No Leash Needed)
| Minion | Notes |
|--------|-------|
| Sharknado | `tileCollide` forced false in AI |
| Stardust Dragon | `tileCollide = false` in SetDefaults |
| Sanguine Bat | `tileCollide = false` in SetDefaults |
| Terraprisma | `tileCollide = false` in SetDefaults |
| Abigail | aiStyle 62 but phases through blocks during attack |

---

## Modding: Building a Leash Lookup

Since there's no native lookup, you'd need to build one manually:

```csharp
public static class MinionLeashData
{
    /// <summary>
    /// Gets the return/leash distance in PIXELS (not tiles).
    /// This is when minions trigger return mode and start phasing.
    /// </summary>
    public static float GetReturnDistance(int projectileType, bool hasTarget = false)
    {
        return projectileType switch
        {
            // aiStyle 62 minions - base 500px, extended with target
            ProjectileID.Hornet => hasTarget ? 1000f : 500f,         // 373
            ProjectileID.FlyingImp => hasTarget ? 1000f : 500f,      // 375
            ProjectileID.UFOMinion => hasTarget ? 1200f : 1000f,     // 423
            ProjectileID.StardustCellMinion => hasTarget ? 1350f : 1000f, // 613
            ProjectileID.Tempest => 2000f,                           // 407 (always phases anyway)
            979 => hasTarget ? 1000f : 500f,                         // Abigail (1.4.3+)
            
            // aiStyle 66 minions
            ProjectileID.Retanimini or
            ProjectileID.Spazmamini => 1200f,                        // 387-388 (Twins)
            ProjectileID.DeadlySphere => 1500f,                      // 533
            
            // aiStyle 26 minions (ground) - base + minionPos scaling
            ProjectileID.Pygmy or ProjectileID.Pygmy2 or
            ProjectileID.Pygmy3 or ProjectileID.Pygmy4 => 1000f,     // 191-194 (base)
            ProjectileID.BabySlime => 1000f,                         // 266
            ProjectileID.VenomSpider or ProjectileID.JumperSpider or
            ProjectileID.DangerousSpider => 1400f,                   // 390-392 (base)
            
            // aiStyle 67 minions (ground with parrot)
            393 or 394 or 395 => 500f,                               // Pirate variants
            ProjectileID.VampireFrog => 500f,                        // 758
            
            // aiStyle 54
            ProjectileID.Raven => 1400f,                             // 317
            
            // aiStyle 121 (always phases)
            ProjectileID.StardustDragon1 => 2000f,                   // 625 (head)
            
            // aiStyle 158
            ProjectileID.BabyBird => 800f,                           // 759 (Baby Finch)
            
            // Blade Staff
            ProjectileID.Smolstar => 900f,                           // 864
            
            _ => 1600f  // Conservative default (~100 tiles)
        };
    }
    
    /// <summary>
    /// Gets minionPos scaling offset in PIXELS.
    /// Ground minions get additional leash per slot index.
    /// </summary>
    public static float GetMinionPosScaling(int projectileType)
    {
        return projectileType switch
        {
            // aiStyle 26 minions get +40px per minionPos
            ProjectileID.Pygmy or ProjectileID.Pygmy2 or
            ProjectileID.Pygmy3 or ProjectileID.Pygmy4 or
            ProjectileID.BabySlime or
            ProjectileID.VenomSpider or ProjectileID.JumperSpider or
            ProjectileID.DangerousSpider => 40f,
            
            _ => 0f
        };
    }
    
    /// <summary>
    /// Returns true if minion always phases through blocks.
    /// These never need pathfinding assistance.
    /// </summary>
    public static bool AlwaysPhases(int projectileType)
    {
        return projectileType switch
        {
            ProjectileID.StardustDragon1 or ProjectileID.StardustDragon2 or
            ProjectileID.StardustDragon3 or ProjectileID.StardustDragon4 => true,  // 625-628
            ProjectileID.Tempest => true,           // 407 (Sharknado)
            ProjectileID.BatOfLight => true,        // 755 (Sanguine)
            ProjectileID.EmpressBlade => true,      // 946 (Terraprisma)
            979 => true,                            // Abigail (1.4.3+)
            _ => false
        };
    }
    
    public static float GetDetectionRange(int projectileType, bool playerTargeted = false)
    {
        float baseRange = projectileType switch
        {
            // aiStyle 26 ground minions
            ProjectileID.VenomSpider or ProjectileID.JumperSpider or
            ProjectileID.DangerousSpider or
            ProjectileID.Pygmy or ProjectileID.Pygmy2 or
            ProjectileID.Pygmy3 or ProjectileID.Pygmy4 or
            ProjectileID.BabySlime => 800f,              // 50 tiles
            
            // Other short range
            ProjectileID.BabyBird => 800f,               // 50 tiles
            ProjectileID.DeadlySphere => 800f,           // 50 tiles
            ProjectileID.Smolstar => 800f,               // 50 tiles
            ProjectileID.Raven => 900f,                  // 56.25 tiles
            
            // aiStyle 62/66 long range
            ProjectileID.Hornet or ProjectileID.FlyingImp or
            ProjectileID.Retanimini or ProjectileID.Spazmamini or
            ProjectileID.Tempest or ProjectileID.UFOMinion or
            ProjectileID.StardustCellMinion => 2000f,    // 125 tiles
            
            // Stardust Dragon
            ProjectileID.StardustDragon1 => 1000f,       // 62.5 tiles
            
            _ => 1600f  // 100 tiles default
        };
        
        // Many minions get 187.5 tile range when player targets with whip
        if (playerTargeted && baseRange >= 1000f)
            return 3000f;  // 187.5 tiles
            
        return baseRange;
    }
}
```

---

## Design Observations

1. **The Twins are broken by design** - 125 tile detection but only 75 tile leash means they constantly abandon attacks to return to player. This is the tightest leash of any flying minion.

2. **`minionPos` scaling is clever** - Spider and Pygmy get +2.5 tiles per slot index, so your first minion has the tightest leash while later ones spread out more.

3. **Player targeting is a big boost** - Many minions jump from 125 to 187.5 tile range when you target with a whip, which is 50% more reach.

4. **Ground minions (Pirate, Spider) don't teleport** - They use return-by-walking or special mechanics (parrot carry), so "leash" means something different.

5. **Post-Moon Lord minions are generous** - UFO, Stardust Dragon, and Sharknado all have 125-tile return distances, likely to handle larger arenas.