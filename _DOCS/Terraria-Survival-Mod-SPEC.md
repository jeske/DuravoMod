# Terraria Survival Overhaul Mod

A tModLoader mod to slow the game pace and expand the depth by removing exploits that make interacting with enemies optional.

---

## Core Philosophy

Terraria presents platformer-combat aesthetics but gives players tools that make engagement optional. This mod surgically removes the worst offenders while preserving legitimate building and crafting QoL.

Currently a "Moon Lord" speedrun can be done in 50-70 minutes by an expert. WHen we are done, the minimum viable time for this should be closer to 10-20 hours, with a typical ru

NOTE: Reddit thread at: https://www.reddit.com/r/Terraria/comments/1p6n7jd/interest_in_survival_overhaul_mod/

---

## Line-of-Sight Interactions (Priority: HIGH)

### Problem

Players can place blocks, destroy blocks, loot chests, and grab items through walls. This enables:

* Sealed tunnels built from inside
* Looting surface chests from underground
* Grabbing drops without exposure
* Mining ore through walls

### Solution

ALL world interactions require line-of-sight from player to target.

### Affected Actions

| Action                  | Current       | With Mod     |
| ----------------------- | ------------- | ------------ |
| Place block             | Through walls | LOS required |
| Destroy block           | Through walls | LOS required |
| Open chest              | Through walls | LOS required |
| Grab dropped item       | Through walls | LOS required |
| Interact with furniture | Through walls | LOS required |

### Implementation

**Dual-ray approach:**

* Cast two rays: one from player "shoulder" (upper hitbox), one from "waist" (lower hitbox)
* If BOTH rays are blocked by solid tiles, deny the action
* If either ray has clear LOS, allow the action

This handles edge cases like small gaps - you can still interact through a 1-tile window, but not through solid walls.

```csharp
bool HasLineOfSight(Player player, Vector2 targetPos) {
    Vector2 shoulder = player.Center + new Vector2(0, -12);
    Vector2 waist = player.Center + new Vector2(0, 8);
  
    bool shoulderClear = RaycastClear(shoulder, targetPos);
    bool waistClear = RaycastClear(waist, targetPos);
  
    return shoulderClear || waistClear;
}

bool RaycastClear(Vector2 start, Vector2 target) {
    foreach (Point tile in TilesAlongRay(start, target)) {
        if (IsSolidTile(tile)) return false;
    }
    return true;
}

bool IsSolidTile(Point tile) {
    Tile t = Main.tile[tile.X, tile.Y];
    return t.HasTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType];
}
```

**Hook points:**

* `Player.PlaceThing_Tiles()` - block placement
* `Player.PickTile()` - block destruction
* `Chest.Unlock()` / chest interaction - looting
* `Item.GetPickedUpBy()` or similar - item pickup
* `Player.TileInteractionsUse()` - furniture interaction

**Edge cases:**

| Scenario          | Behavior                         |
| ----------------- | -------------------------------- |
| Platforms         | Do NOT block LOS                 |
| Furniture/torches | Do NOT block LOS                 |
| Actuated blocks   | Do NOT block LOS (they're "off") |
| Trees/foliage     | Do NOT block LOS                 |
| Doors (closed)    | DO block LOS                     |

### Gameplay Impact

* Cannot extend sealed tunnels from inside
* Cannot loot surface from underground tunnels
* Cannot grab drops through walls
* Must actually BE in the environment to interact with it
* Exploration requires exposure

---

## Persistent Player Position (Priority: HIGH)

### Problem

The game always respawns you at your bed/world spawn when loading a world, regardless of where you saved. This eliminates all commitment to exploration - you can logout from anywhere and wake up safe at home.

### Why This Matters

Without this fix, it's not even a game. There's no such thing as "being deep in enemy territory" because you can always escape by quitting. Combined with Softcore's meaningless death, every expedition has zero stakes.

**The escape hierarchy without this fix:**

1. Recall mirror (slow, has cast time)
2. Suicide (free in Softcore)
3. Logout (instant, always works)

**With this fix:**

* Softcore players can still suicide to get home (acceptable - they chose easy mode)
* Mediumcore/Hardcore players must actually prepare for expeditions
* Logging out is no longer an escape - it's just a pause

### Implementation

```csharp
public class PersistentPosition : ModPlayer {
    private Vector2 savedPosition;
    private bool hasValidPosition;

    public override void SaveData(TagCompound tag) {
        // Don't save position if player is dead
        if (Player.dead) return;
      
        tag["posX"] = Player.position.X;
        tag["posY"] = Player.position.Y;
        tag["hasPos"] = true;
    }

    public override void LoadData(TagCompound tag) {
        if (tag.ContainsKey("hasPos")) {
            savedPosition = new Vector2(tag.GetFloat("posX"), tag.GetFloat("posY"));
            hasValidPosition = true;
        }
    }

    public override void OnEnterWorld() {
        if (hasValidPosition) {
            // Validate position before restoring
            if (IsPositionSafe(savedPosition)) {
                Player.position = savedPosition;
            }
            hasValidPosition = false;
        }
    }
  
    private bool IsPositionSafe(Vector2 pos) {
        // Check if position is inside solid blocks
        Point tilePos = pos.ToTileCoordinates();
      
        // Check a few tiles around the position
        for (int x = 0; x < 2; x++) {
            for (int y = 0; y < 3; y++) {
                Tile tile = Main.tile[tilePos.X + x, tilePos.Y + y];
                if (tile.HasTile && Main.tileSolid[tile.TileType]) {
                    return false; // Would spawn inside blocks
                }
            }
        }
        return true;
    }
}
```

### Edge Cases

| Scenario                     | Behavior                                       |
| ---------------------------- | ---------------------------------------------- |
| Position now inside blocks   | Fall back to spawn point                       |
| Player died at save location | Don't restore (let normal death handling work) |
| Position in destroyed area   | Fall back to spawn point                       |
| Multiplayer                  | Sync position to server on join                |
| World was edited externally  | Validate position, fall back if invalid        |

### Gameplay Impact

* **Expeditions have commitment** - Going to Hell means you're IN Hell until you get back
* **Preparation matters** - Bring recall items, build pylon networks, plan your exit
* **Logout isn't escape** - Just pauses the danger, doesn't remove it
* **Depth = risk** - The further from spawn, the more committed you are

### Interaction with Death Modes

| Character Mode | Can Escape Via...               |
| -------------- | ------------------------------- |
| Softcore       | Suicide (acceptable)            |
| Mediumcore     | Must survive or lose items      |
| Hardcore       | Must survive or lose everything |

This feature makes Mediumcore/Hardcore actually mean something.

---

## Minion Tethering (Priority: HIGH)

### Problem

Minions can fight in other rooms, through walls, while you're completely safe. You can wall off a minion and AFK while it farms for you.

### Solution

Minions must stay near you and maintain line-of-sight. No remote-control murder.

### Rules

1. **Proximity tether** - Minions stay within ~5 tiles of player
2. **LOS requirement** - Must maintain line-of-sight to player
3. **Despawn on weapon switch** - Unselect the summon weapon = minion disappears

### Implementation

```csharp
public class TetheredMinion : GlobalProjectile {
    private const float MAX_DISTANCE = 80f; // ~5 tiles
    private const int LOS_CHECK_INTERVAL = 30; // every 0.5 sec
    private int losTimer = 0;
  
    public override void AI(Projectile proj) {
        if (!proj.minion) return;
      
        Player owner = Main.player[proj.owner];
        float distance = Vector2.Distance(proj.Center, owner.Center);
      
        // Teleport back if too far
        if (distance > MAX_DISTANCE) {
            proj.Center = owner.Center + new Vector2(Main.rand.Next(-40, 40), -20);
        }
      
        // Check LOS periodically
        if (losTimer++ > LOS_CHECK_INTERVAL) {
            losTimer = 0;
            if (!HasLineOfSight(proj.Center, owner.Center)) {
                // Teleport to player if no LOS
                proj.Center = owner.Center + new Vector2(Main.rand.Next(-40, 40), -20);
            }
        }
    }
}

// Despawn on weapon switch
public class MinionWeaponTracker : ModPlayer {
    private int lastSelectedItem = -1;
  
    public override void PostUpdate() {
        if (Player.selectedItem != lastSelectedItem) {
            Item prev = lastSelectedItem >= 0 ? Player.inventory[lastSelectedItem] : null;
            if (prev != null && IsSummonWeapon(prev)) {
                // Player switched away from summon weapon - kill minions of that type
                DespawnMinionsOfType(prev);
            }
            lastSelectedItem = Player.selectedItem;
        }
    }
}
```

### Why Simpler Than HP

Original idea: Give minions HP, let them die, require resummoning.

Problems:

* Tracking HP per minion instance
* Visual feedback for minion health
* Balancing HP values across all minion types
* Revive/resummon UX

Tethering achieves the same goal (can't exploit minions) with simpler implementation:

* No HP tracking
* No death/respawn logic
* Just position and LOS checks

### Gameplay Impact

* **No AFK farming** - Minion stays with you, you must be present
* **No room-clearing** - Can't send minion through a hole to clear enemies
* **No wall exploits** - LOS check prevents fighting through walls
* **Summoner is companion class** - Minion fights WITH you, not FOR you

### Edge Cases

| Scenario                         | Behavior                                              |
| -------------------------------- | ----------------------------------------------------- |
| Minion chases enemy through wall | Teleports back to player                              |
| Player grapples away quickly     | Minion teleports to catch up                          |
| Weapon switch during boss fight  | Minions despawn (intentional - commitment to loadout) |
| Multiple minion types            | Each tracks its own weapon separately?                |

---

## Aggro Burrowing (Priority: HIGH)

### Problem

Underground enemies are completely neutralized by a single layer of blocks. Player can tunnel through any biome in perfect safety.

### Solution

When underground enemies are aggroed but path-blocked by terrain, they begin digging toward the player.

### Design Philosophy

* **Underground only** - This is about cave exploration tension, not base defense
* **Chasing, not raiding** - Enemies dig to reach you, not to destroy your structures
* **Audible warning** - Player hears digging sounds, creating dread
* **Material hierarchy** - Different burrowers can dig through different materials

### Implementation

**Aggro + Blocked Detection:**

```csharp
bool ShouldBurrow(NPC npc, Player target) {
    // Only underground
    if (target.position.Y < Main.worldSurface * 16) return false;
  
    // Must be aggroed (within aggro range, has LOS or recently had LOS)
    if (!IsAggroed(npc, target)) return false;
  
    // Must be path-blocked
    if (HasClearPath(npc, target)) return false;
  
    // Must be a burrowing enemy type
    if (!CanBurrow(npc.type)) return false;
  
    return true;
}
```

**Burrowing Behavior:**

```csharp
void UpdateBurrowing(NPC npc, Player target) {
    if (burrowCooldown > 0) {
        burrowCooldown--;
        return;
    }
  
    // Find next block in path toward player
    Point blockToDig = GetNextBlockInPath(npc.Center, target.Center);
  
    // Check if this enemy can dig this block type
    if (!CanDigBlock(npc.type, blockToDig)) {
        // Stuck - maybe give up after X seconds?
        return;
    }
  
    // Dig the block
    WorldGen.KillTile(blockToDig.X, blockToDig.Y);
  
    // Play digging sound (directional, so player knows where)
    SoundEngine.PlaySound(SoundID.Dig, npc.Center);
  
    // Cooldown before next dig (slower = more tension, faster = more threat)
    burrowCooldown = GetDigSpeed(npc.type);
}
```

### Burrower Counter System (Native Blocks)

Simple rule: **Build with the local materials.** Enemies can't destroy their own biome.

| Biome          | Enemies                       | Counter Block      | Acquisition                       |
| -------------- | ----------------------------- | ------------------ | --------------------------------- |
| Underground    | Skeletons, Worms              | Stone              | Mine it (trivial)                 |
| Ice            | Ice Bats, Ice Slimes          | Ice blocks         | Mine it                           |
| Jungle         | Hornets, Man Eaters           | Mud, Jungle grass  | Mine it                           |
| Corruption     | Eaters, Devourers             | Ebonstone          | Mine it                           |
| Crimson        | Face Monsters, Blood Crawlers | Crimstone          | Mine it                           |
| **Hell** | Demons, Fire Imps             | **Obsidian** | **CREATE it**(water + lava) |

### Why This Works

**Early biomes:** You arrive, you mine local materials, you build with them. Intuitive.

**Hell is different:** Obsidian requires active creation:

1. Bring water to Hell (buckets or dig a channel)
2. Water + lava = obsidian
3. Digging to set this up = spawn rate spike
4. You're getting swarmed by depth-scaled Hell enemies while engineering your defense

Obsidian is the only counter you can't just mine. It's the skill check for late pre-Hardmode.

### What Blocks DON'T Work

* **Surface wood** - useless underground
* **Imported stone** - works in Underground only, not in biomes
* **Any foreign biome block** - no protection

This forces biome engagement. You can't haul 600 wood and build a safehouse anywhere.

### Hardmode Escalation (TBD)

Hardmode may require:

* Cross-biome counters
* Double-thick layered walls (two materials)
* New counter mechanics entirely

Design these after core system is proven.

### Dig Speed by Category

All burrowers dig at similar speeds - the question is *whether* they can dig, not  *how fast* .

| Block Type               | Base Dig Time                                       |
| ------------------------ | --------------------------------------------------- |
| Dirt, Mud, Clay          | 0.5 sec                                             |
| Sand, Silt, Slush        | 0.3 sec                                             |
| Stone, Ice               | 1.0 sec                                             |
| Hardened Sand, Sandstone | 1.2 sec                                             |
| Ore blocks               | 1.5 sec                                             |
| Brick (player-crafted)   | 2.0 sec                                             |
| Dungeon Brick, Lihzahrd  | Cannot dig (worldgen protection, not player-usable) |

### Player Strategy Implications

* **Learn the biome** - First priority is mining local counter material
* **Surface materials are worthless** - Wood, dirt won't save you
* **Hell requires planning** - Bring water, prepare for chaos
* **Native materials ARE the loot** - Biome blocks become survival resources

### Audio Design

Critical for tension. Player should hear:

1. **Distant scratching** when burrower starts digging (directional audio)
2. **Getting louder** as they get closer
3. **Block break sounds** as each block is destroyed
4. **Silence** is now ominous - either they broke through or gave up

```csharp
void PlayBurrowingAudio(NPC npc, Player target) {
    float distance = Vector2.Distance(npc.Center, target.Center);
    float volume = MathHelper.Clamp(1f - (distance / 800f), 0.1f, 1f);
  
    // Scratching/digging ambient sound
    SoundEngine.PlaySound(SoundID.Dig, npc.Center, volume);
}
```

### Aggro Persistence

How long does an enemy "remember" you after losing LOS?

| Difficulty | Memory Duration |
| ---------- | --------------- |
| Normal     | 5 seconds       |
| Expert     | 15 seconds      |
| Master     | 30 seconds      |

After memory expires, enemy stops digging and resumes patrol.

### Edge Cases

| Scenario                   | Behavior                                                         |
| -------------------------- | ---------------------------------------------------------------- |
| Player teleports away      | Enemy continues for memory duration, then stops                  |
| Player dies                | Enemy stops immediately                                          |
| Enemy reaches player       | Normal combat resumes                                            |
| Path requires 50+ blocks   | Give up after X blocks? Or persist? (Config)                     |
| Multiple enemies           | All dig independently (could create interesting breach patterns) |
| Enemy digs into lava/water | Takes environmental damage, may die                              |

### Gameplay Impact

* **Sealed tunnels no longer safe** - Must keep moving or fight
* **Material choice matters** - Carrying obsidian/dungeon brick for emergency walls
* **Audio awareness** - Sound design becomes gameplay information
* **Depth = danger** - Deeper enemies are stronger burrowers

### Compatibility Notes

* Worm enemies already burrow - don't double-modify them
* Some modded enemies may have custom AI - need to check for conflicts
* Boss fights might need exclusion zones

---

## Depth-Scaled Difficulty (Priority: HIGH)

### Problem

Hellevator is optimal strategy because enemy threat doesn't scale with depth. A zombie in hell hits about as hard as a zombie on the surface. There's no reason NOT to rush straight down.

### Solution

Two synergistic systems that make depth = danger:

### 5A: Depth-Scaled Enemy Damage

```csharp
public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers) {
    float depthRatio = (float)npc.position.Y / (Main.maxTilesY * 16f);
    float multiplier = 1f + (depthRatio * 1.5f); // 1x surface, 2.5x at hell
  
    modifiers.FinalDamage *= multiplier;
}
```

| Depth       | Damage Multiplier |
| ----------- | ----------------- |
| Surface     | 1.0x              |
| Underground | 1.3x              |
| Cavern      | 1.7x              |
| Hell        | 2.5x              |

Numbers are tunable. Goal: trash mobs in cavern layer should chunk you if you have no armor.

### 5B: Dig-Activity Spawn Rate

Digging creates noise. Noise attracts enemies. More noise = more enemies. And this scales with depth.

```csharp
public class DigActivityTracker : ModPlayer {
    private int recentDigCount = 0;
    private int digDecayTimer = 0;
  
    public override void PostUpdate() {
        // Decay dig count over time (moving quietly)
        if (digDecayTimer++ > 60) { // every second
            recentDigCount = Math.Max(0, recentDigCount - 1);
            digDecayTimer = 0;
        }
    }
  
    public void OnBlockMined() {
        recentDigCount++;
    }
  
    public float GetSpawnRateMultiplier() {
        float depthRatio = Player.position.Y / (Main.maxTilesY * 16f);
        float digNoise = Math.Min(recentDigCount / 10f, 3f); // caps at 3x from digging
        float depthBonus = 1f + (depthRatio * 2f); // 1x surface, 3x at hell
      
        return 1f + (digNoise * depthBonus);
    }
}

// Hook into spawn rate
public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns) {
    float multiplier = player.GetModPlayer<DigActivityTracker>().GetSpawnRateMultiplier();
    spawnRate = (int)(spawnRate / multiplier); // lower = more spawns
    maxSpawns = (int)(maxSpawns * multiplier);
}
```

### The Math

**Surface, digging constantly:** ~1.5x spawn rate, 1x damage. Manageable.

**Cavern, digging constantly:** ~6x spawn rate, 1.7x damage. Overwhelming.

**Cavern, moving through caves quietly:** ~1.5x spawn rate, 1.7x damage. Challenging but fair.

**Hell, digging hellevator:** ~9x spawn rate, 2.5x damage. Suicide.

### Gameplay Impact

* **Hellevator becomes suicide** - Constant digging at depth = swarmed by powered-up enemies
* **Cave navigation rewarded** - Moving quietly through existing caves = manageable spawns
* **Early game protected** - Surface digging is still fine for newbies
* **Armor actually matters** - You NEED defense to survive deep enemies
* **Expedition pacing** - Can't just speedrun to hell on day 1

### Synergy with Other Features

| Feature             | Synergy                                                |
| ------------------- | ------------------------------------------------------ |
| Burrowing enemies   | They dig TO you, buffed by depth, there's MORE of them |
| LOS block placement | Can't just wall them off while digging                 |
| Persistent position | Can't logout to escape the swarm you summoned          |
| Mortal minions      | Minions die to the buffed damage too                   |

This is the anti-hellevator system. Each feature alone is avoidable. Together, they make "dig straight down" a death sentence.

---

## Smart Hopping (Priority: HIGH)

### Problem
Players can exploit zombie AI by building short walls, pits, or "murder holes" that cause zombies to overshoot with their fixed-height jump. The standard zombie jump is always the same arc regardless of obstacle height, so a 2-tile wall causes the same jump as a 4-tile wall - often landing them past the player or onto platforms where they get stuck.

### Solution
When a zombie is blocked by a short wall (1-4 tiles) in the direction of the player, calculate the exact jump trajectory to land ON TOP of that wall, not overshoot it.

### Trigger Condition
```
zombie.onGround AND
zombie.blocked by solid wall toward player AND
wall height is 1-4 tiles AND
clear space above wall to land
```

That's it. No pit detection, no complex state tracking.

### Physics

Terraria uses approximately:
- Gravity: `g ≈ 0.3` per tick (pixels/tick²)
- 1 tile = 16 pixels

For a zombie to land exactly on top of a wall H tiles high:

**Vertical velocity:**
```
// Minimum to reach height H, plus small margin
vy = -sqrt(2 * g * H * 16) * 1.15
```

**Flight time:**
```
t_peak = -vy / g
// We want to land AT height H, not return to ground
peak_height = vy² / (2g)
fall_distance = peak_height - (H * 16)
t_fall = sqrt(2 * fall_distance / g)
t_total = t_peak + t_fall
```

**Horizontal velocity:**
```
// Distance to land on top of wall (wall position + half zombie width)
distance = (tiles_to_wall + 1) * 16
vx = distance / t_total * direction
```

### Implementation

```csharp
public class SmartHopper : GlobalNPC {
    private const float GRAVITY = 0.3f;
    private const float TILE = 16f;
    private const int MAX_WALL_HEIGHT = 4;
    
    public override bool PreAI(NPC npc) {
        if (!IsGroundEnemy(npc)) return true;
        if (npc.velocity.Y != 0) return true; // Must be grounded
        if (npc.target < 0) return true;
        
        Player target = Main.player[npc.target];
        if (!target.active || target.dead) return true;
        
        int dir = (target.Center.X > npc.Center.X) ? 1 : -1;
        Point pos = npc.Center.ToTileCoordinates();
        
        // Check if blocked by wall in direction of player
        if (!IsSolid(pos.X + dir, pos.Y)) return true; // Not blocked
        
        // Find wall height (how many tiles until clear?)
        int wallHeight = 0;
        for (int h = 0; h <= MAX_WALL_HEIGHT; h++) {
            if (!IsSolid(pos.X + dir, pos.Y - h - 1)) {
                wallHeight = h + 1;
                break;
            }
        }
        
        if (wallHeight == 0 || wallHeight > MAX_WALL_HEIGHT) {
            return true; // Wall too high or no clearance - normal AI
        }
        
        // Check landing zone is clear (need 3 tiles vertical for zombie)
        for (int h = 0; h < 3; h++) {
            if (IsSolid(pos.X + dir, pos.Y - wallHeight - h)) {
                return true; // Can't fit on top of wall
            }
        }
        
        // Calculate smart jump
        float heightPixels = wallHeight * TILE;
        
        // Vertical: just enough to clear, with 15% margin
        float vy = -(float)Math.Sqrt(2 * GRAVITY * heightPixels) * 1.15f;
        
        // Time calculation
        float t_peak = -vy / GRAVITY;
        float peakHeight = (vy * vy) / (2 * GRAVITY);
        float fallDist = peakHeight - heightPixels;
        float t_fall = (float)Math.Sqrt(2 * Math.Max(0, fallDist) / GRAVITY);
        float t_total = t_peak + t_fall;
        
        // Horizontal: land on top of wall
        float distPixels = TILE * 1.5f; // Land solidly on the wall top
        float vx = (distPixels / t_total) * dir;
        
        // Clamp for sanity
        vy = Math.Clamp(vy, -12f, -2f);
        vx = Math.Clamp(vx, -5f, 5f);
        
        npc.velocity.Y = vy;
        npc.velocity.X = vx;
        
        return true;
    }
    
    private bool IsGroundEnemy(NPC npc) {
        return npc.aiStyle == 3 ||  // Fighter AI (zombies, skeletons)
               npc.aiStyle == 26 || // Unicorn AI  
               npc.aiStyle == 38;   // Tortoise AI
    }
    
    private bool IsSolid(int x, int y) {
        if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) return true;
        Tile tile = Main.tile[x, y];
        return tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
    }
}
```

### Examples

**Murder hole (2-tile pit):**
- Zombie falls in pit
- Walks toward player, hits 2-tile wall
- Smart hop: `vy ≈ -5.3` instead of default `vy ≈ -8`
- Lands on player's level, not on platform above

**Natural ledge:**
- Zombie chasing player uphill
- Hits 1-tile step
- Small hop lands exactly on step
- Continues chase smoothly

**Player barricade (3-tile wall):**
- Zombie approaches 3-high dirt wall
- Calculated jump lands on top
- Player can't hide behind short walls anymore

### What This Changes

| Obstacle | Before | After |
|----------|--------|-------|
| 1-tile step | Big jump, overshoot | Tiny hop, lands on step |
| 2-tile wall | Big jump, often overshoots | Precise hop to top |
| 3-tile wall | Big jump, sometimes works | Precise hop to top |
| 4-tile wall | Big jump, usually works | Precise hop to top |
| 5+ tile wall | Big jump, fails | Normal AI (or burrowing) |

### Performance

- Check only runs when zombie is grounded
- Simple tile lookups, no pathfinding
- Adds maybe 4-8 tile checks per grounded zombie per tick
- Negligible impact

### Gameplay Impact

- Murder holes don't work anymore
- Short walls don't save you
- Natural terrain doesn't break zombie pathing
- Zombies feel smarter and more threatening
- Closed doors still block (this is about OPEN door cheese)

## Travel Rework (Priority: HIGH)

### Problem

Mirror and Recall Potions provide instant escape from anywhere. Combined with spawn-on-logout, there's zero commitment to exploration. Every expedition is risk-free.

### Solution

Delete recall items. Replace with earned portal infrastructure.

### Changes

**Removed from game:**

* Magic Mirror
* Ice Mirror
* Cell Phone (recall function)
* Recall Potions
* Remove from loot tables, crafting, NPC shops

**Softcore exception:**
Softcore players get a UI "Return Home" button. They can suicide for free anyway, so recall is just convenience. Let them have it - they chose easy mode.

**New system: Portal Stones**

Build a valid home in a biome → can place a Portal Stone → teleport between your Portal Stones.

### Portal Stone Mechanics

**Placement requirements:**

* Valid NPC housing (walls, light, furniture)
* Built from **in-biome materials** (no importing wood everywhere)
* Minimum size threshold?

**Usage:**

* Interact with Portal Stone → shows list of other Portal Stones
* Select destination → teleport
* **Mana cost based on distance** - longer jumps cost more mana
* If insufficient mana: partial teleport? Failed teleport? Delayed teleport?

```csharp
int CalculateManaCost(Vector2 from, Vector2 to) {
    float distance = Vector2.Distance(from, to);
    float worldWidth = Main.maxTilesX * 16f;
    float distanceRatio = distance / worldWidth;
  
    // 10 mana for short hop, 200 mana for cross-world
    return (int)(10 + (distanceRatio * 190));
}
```

**Crafting (rough idea):**

* Early game: 20 Stone + 5 Gems + crafting station
* Should be achievable once you've established a real base
* Not so cheap you spam them everywhere

### Gameplay Loop Created

1. **Spawn** - You start here, it's safe
2. **Explore** - Travel to new biome (dangerous, one-way commitment)
3. **Survive** - Fight to gather local materials
4. **Establish** - Build valid home from in-biome blocks
5. **Anchor** - Place Portal Stone
6. **Connect** - Now you can travel between home and new base
7. **Repeat** - Push further, build more bases

Each Portal Stone is EARNED. The network grows with your accomplishment.

### Mana Cost Implications

* **Mages can travel easier** - Class identity perk
* **Warriors need mana potions** - Resource cost for travel
* **Long-distance travel is expensive** - Encourages intermediate bases
* **Emergency escape costs resources** - Not free like Mirror was

### Edge Cases

| Scenario                             | Behavior                                       |
| ------------------------------------ | ---------------------------------------------- |
| Not enough mana                      | Teleport fails, mana not consumed              |
| Portal Stone destroyed               | Removed from network                           |
| Die at destination                   | Normal death rules apply                       |
| Multiplayer                          | Each player can use any placed Portal Stone?   |
| Building destroyed but Stone remains | Stone stops working until housing valid again? |

### Synergy with Other Features

| Feature                       | Synergy                                           |
| ----------------------------- | ------------------------------------------------- |
| Persistent position           | Can't logout-escape, must reach a Portal Stone    |
| In-biome building requirement | Must engage with biome to build valid portal base |
| Burrowing enemies             | Base defense matters, can't just plop a portal    |
| Depth scaling                 | Deep bases are hard to establish = valuable       |

### Why This Works

* **Exploration has commitment** - No free escape
* **Bases have purpose** - Portal network is the reward
* **Biome engagement required** - Can't import materials
* **Mana becomes travel resource** - New use for mana on non-mages
* **Progression is visible** - Your portal network shows your accomplishment

---

## Enemy Rebalancing (Priority: HIGH)

### Problem

Expert mode enemies are balanced around the assumption that players will cheese. When cheese is removed, Expert damage may be too punishing for legitimate play - especially on the surface.

Current Expert mode + no cheese =  **impossible** , not  **challenging** .

### Observation

When enemies hit hard and cheese exists → players cheese more
When enemies hit hard and cheese is removed → players die immediately

The mod removes cheese. Enemy damage must be rebalanced to match.

### Approach

**Surface should be accessible.** New players need to learn the game. First few nights shouldn't be instant death.

**Depth scaling handles difficulty.** We already have depth-scaled damage. Surface can be easier because depth makes it harder naturally.

### Proposed Changes

| Layer           | Vanilla Expert | With Mod             |
| --------------- | -------------- | -------------------- |
| Surface (day)   | ~1.0x          | 0.6x                 |
| Surface (night) | ~1.0x          | 0.8x                 |
| Underground     | ~1.0x          | 1.0x (unchanged)     |
| Cavern          | ~1.0x          | 1.0x + depth scaling |
| Hell            | ~1.0x          | 1.0x + depth scaling |

The depth scaling system (Feature: Depth-Scaled Difficulty) adds multipliers as you go deeper. Surface gets a REDUCTION to compensate for cheese removal.

### Why This Works

**Vanilla Expert balance assumption:**

* Player can wall off
* Player can tunnel safely
* Player can recall instantly
* Player can AFK with minions
* Therefore: enemies must hit HARD to matter at all

**Mod balance assumption:**

* Player must fight
* Player is exposed
* Player can't escape easily
* Therefore: enemies can hit MODERATELY and still matter

### Implementation

```csharp
public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers) {
    float depth = target.position.Y / (Main.maxTilesY * 16f);
    float surfaceThreshold = (float)Main.worldSurface / Main.maxTilesY;
  
    if (depth < surfaceThreshold) {
        // Surface - reduce damage
        bool isDay = Main.dayTime;
        float surfaceMultiplier = isDay ? 0.6f : 0.8f;
        modifiers.FinalDamage *= surfaceMultiplier;
    } else {
        // Underground and below - apply depth scaling
        float depthBelowSurface = (depth - surfaceThreshold) / (1f - surfaceThreshold);
        float depthMultiplier = 1f + (depthBelowSurface * 1.5f);
        modifiers.FinalDamage *= depthMultiplier;
    }
}
```

### Tuning Notes

These numbers are starting points. Playtesting required.

Key questions:

* Can a new player survive first night with copper armor?
* Is Underground the right difficulty for "you should have some gear now"?
* Does Hell feel appropriately deadly?

### Interaction with Difficulty Modes

| Mode    | Surface Mult | Depth Scaling |
| ------- | ------------ | ------------- |
| Classic | 0.7x         | 1.0x - 2.0x   |
| Expert  | 0.6x         | 1.0x - 2.5x   |
| Master  | 0.5x         | 1.0x - 3.0x   |

Master gets the BIGGEST surface reduction because Master enemies hit absurdly hard. But also the steepest depth scaling.

---

## Combat Zone Block Lock (Priority: MEDIUM)

### Problem

Players can place blocks mid-combat to create instant barriers.

### Solution

Block placement disabled while in combat (enemies within X tiles).

### Considerations

* What's the radius? Too small = easy to cheese, too large = frustrating
* Should destruction also be blocked? Probably not - mining to escape is fair
* Boss fights: larger radius or always-on during boss?
* Exception for platforms? (mobility aid vs. full barrier)

```csharp
bool CanPlaceBlock(Player player) {
    // Check for nearby hostile NPCs
    float combatRadius = 400f; // ~25 tiles
  
    foreach (NPC npc in Main.npc) {
        if (npc.active && !npc.friendly && npc.damage > 0) {
            if (Vector2.Distance(player.Center, npc.Center) < combatRadius) {
                return false;
            }
        }
    }
    return true;
}
```

---

## Open Questions

1. **Mod compatibility** : How will this interact with Calamity, Thorium, etc.? New enemy types need categorization.
2. **Configuration** : Should features be toggleable per-world or global?
3. **Multiplayer** : Sync issues with position persistence, portal networks, burrowing state?
4. **Performance** : Raycast per placement attempt, pathfinding for burrowers, dig-noise tracking?
5. **Portal Stone crafting recipe** : What's the right cost? Too cheap = spam, too expensive = tedious.
6. **Biome detection** : How to determine "in-biome" for housing validation? Block percentage threshold?
7. **Mana cost tuning** : What's the right curve for portal distance? Linear? Exponential?
8. **Counter balance** : Is obsidian too hard to create in Hell? Too easy?
9. **Audio mixing** : How many simultaneous digging sounds before it's cacophony?
10. **Modded enemies** : Default behavior for unrecognized enemy types? (Probably: can dig everything except native biome blocks)
11. **Softcore recall button** : Where in UI? Always visible or only when safe?

---

## Implementation Order

**Foundational (do first):**

* Persistent Player Position - without this nothing else matters
* LOS Interactions - affects all other features

**Core anti-cheese (do together):**

* Depth-Scaled Difficulty - makes the world dangerous
* Aggro Burrowing - enemies dig to reach you
* Enemy Rebalancing - make surface survivable without cheese

**Travel overhaul:**

* Travel Rework - delete recall, add portal stones

**Cleanup:**

* Minion Tethering - no AFK murder rooms
* Combat Block Lock - can't wall off mid-fight (lowest priority, may not need)

---

## Testing Scenarios

**Persistent Position:**

* [ ] Logout in Hell, reload - should be in Hell
* [ ] Logout in cave, cave gets filled in by world edit - should fall back to spawn
* [ ] Die, then logout - should NOT restore death position
* [ ] Softcore suicide after loading in dangerous area - should work (intended escape)

**Depth-Scaled Difficulty:**

* [ ] Surface zombie damage vs cavern zombie damage - cavern should hurt more
* [ ] Dig 50 blocks on surface - spawn rate should increase slightly
* [ ] Dig 50 blocks in cavern - spawn rate should increase dramatically
* [ ] Walk through existing cave system - spawn rate should stay low
* [ ] Attempt hellevator with no armor - should be overwhelmed and killed
* [ ] Hell enemy damage should be ~2.5x surface equivalent

**Travel Rework:**

* [ ] Magic Mirror removed from loot tables
* [ ] Recall Potions removed from loot/shop
* [ ] Softcore player has UI recall button
* [ ] Mediumcore/Hardcore player has NO recall option
* [ ] Portal Stone placeable in valid in-biome housing
* [ ] Portal Stone NOT placeable if house built from foreign materials
* [ ] Teleport between two Portal Stones works
* [ ] Mana cost scales with distance
* [ ] Insufficient mana prevents teleport

**LOS Block Interaction:**

* [ ] Mining ore veins (LOS should allow normal mining)
* [ ] Mining ore through a 1-block wall (should fail - no LOS)
* [ ] Extending tunnel from inside sealed tube (should fail)
* [ ] Building through a small gap (should work - one ray has LOS)

**Aggro Burrowing:**

* [ ] Box yourself in during Eye of Cthulhu fight (surface - no burrowing)
* [ ] Box yourself in underground with zombies using wood (should hear digging, breach)
* [ ] Box yourself in underground using stone (should hold - native counter)
* [ ] Box yourself in Corruption using ebonstone (should hold - native counter)
* [ ] Box yourself in Hell using obsidian (should hold - obsidian counter)
* [ ] Box yourself in Hell using hellstone (should fail - hellstone is NOT the counter)
* [ ] Enemy burrows into lava (should take damage/die)

**Minion Tethering:**

* [ ] Minion stays within ~5 tiles of player
* [ ] Minion teleports back if it goes through a wall
* [ ] Switching away from summon weapon despawns minions
* [ ] Cannot AFK farm - minion won't fight in another room

**Integration Tests:**

* [ ] Hellevator attempt with all features enabled - should be suicide
* [ ] Proper cave exploration with native counter blocks and armor - should be challenging but viable
* [ ] Early game surface gameplay - should be unchanged/accessible
* [ ] Establish base in Corruption: mine ebonstone, build house, place portal - should work
* [ ] Try to build portal in Corruption using surface wood - should fail

---

## Resources

* tModLoader docs: https://github.com/tModLoader/tModLoader/wiki
* Terraria source (decompiled): `Player.PlaceThing_Tiles()`, `Player.PickTile()`
* Example mods with similar hooks: TBD
