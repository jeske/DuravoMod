> **Status: 📋 INCOMPLETE SPEC - BRAINSTORMING**
>
> Design phase. No implementation yet.

---

## Aggro Burrowing

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

* Any enemies that phase" (worms, ghosts) shouldn't modify blocks, they already ignore them
* Some modded enemies may have custom AI - need to check for conflicts
* Boss fights might need exclusion zones (why?)
