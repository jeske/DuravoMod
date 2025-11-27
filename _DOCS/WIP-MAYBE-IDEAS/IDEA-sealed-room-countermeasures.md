# Sealed Room Countermeasures - Speculative Design

## The Problem

Players can wall themselves into complete safety at any time. Dig a hole, place 4 blocks, stand inside - you're now invincible. No enemy can reach you, no mechanic punishes this, and you can:

- AFK overnight
- Wait out events (blood moon, goblin army)
- Safely log out anywhere
- Heal to full with zero risk
- Plan your next move indefinitely

This breaks all tension. Exploration has no commitment because safety is always 4 blocks away.

---

## Design Goals

1. **Sealed rooms should have consequences** - not necessarily deadly, but meaningful
2. **Consequences should be escapable** - player can always choose to unseal
3. **Natural caves should be fine** - only PLAYER-CREATED seals are problematic
4. **Base building should still work** - homes with doors aren't "sealed"
5. **Underground expeditions need preparation** - you can't just wall-and-wait forever

---

## Proposed Solution: Oxygen / Air Quality System

### Core Concept

Air tiles can either "reach sky" or be "sealed." Sealed areas slowly deplete oxygen. Players in sealed areas take increasing debuffs, eventually damage.

### Why This Works

- **Walling in = clock starts ticking** - you bought time, not safety
- **Natural caves are pre-computed as sealed** - entering them already starts the clock
- **Breaking through to surface air resets everything** - one hole to sky = breathable
- **Doors count as air-permeable** - bases with exits are fine
- **Creates preparation gameplay** - bring oxygen items for deep expeditions

---

## Technical Implementation

### Data Structure

Each air tile stores a small value:

```
0 = solid (not air)
1 = reaches sky (breathable)
2+ = sealed cavern ID (lookup in table)
```

Separate lookup table:
```csharp
Dictionary<ushort, bool> cavernHasAir; // true = connected to sky
```

### World Generation

**Phase 1: Sky flood fill**
```csharp
// Start from all tiles at y=0 (sky level)
// Flood fill downward through air tiles
// Mark all reached tiles as "1" (reaches sky)
Queue<Point> openSet = new Queue<Point>();

// Seed with sky row
for (int x = 0; x < worldWidth; x++) {
    if (!IsSolid(x, 0)) {
        openSet.Enqueue(new Point(x, 0));
        airStatus[x, 0] = 1;
    }
}

// Flood fill
while (openSet.Count > 0) {
    Point p = openSet.Dequeue();
    foreach (Point neighbor in GetNeighbors(p)) {
        if (!IsSolid(neighbor) && airStatus[neighbor.X, neighbor.Y] == 0) {
            airStatus[neighbor.X, neighbor.Y] = 1;
            openSet.Enqueue(neighbor);
        }
    }
}
```

**Phase 2: Identify sealed caverns**
```csharp
ushort nextCavernId = 2;

for (int y = 0; y < worldHeight; y++) {
    for (int x = 0; x < worldWidth; x++) {
        if (!IsSolid(x, y) && airStatus[x, y] == 0) {
            // Found unsealed air tile - it's a sealed cavern
            FloodFillCavern(x, y, nextCavernId);
            cavernHasAir[nextCavernId] = false; // Sealed
            nextCavernId++;
        }
    }
}
```

### Runtime Updates

**Player breaks a block:**
```csharp
void OnBlockDestroyed(int x, int y) {
    // Get air status of all neighbors
    List<byte> neighborStatuses = GetNeighborAirStatuses(x, y);
    
    if (neighborStatuses.Contains(1)) {
        // Adjacent to sky-connected air - this tile is now breathable
        airStatus[x, y] = 1;
        
        // Check if we just opened a sealed cavern
        foreach (byte status in neighborStatuses) {
            if (status >= 2 && !cavernHasAir[status]) {
                // Cavern is now connected to sky!
                cavernHasAir[status] = true;
                // Optionally: flood fill to convert all cavern tiles to "1"
                // Or just leave them - lookup will show breathable
            }
        }
    } else if (neighborStatuses.Any(s => s >= 2)) {
        // Adjacent to a cavern - join that cavern
        byte cavernId = neighborStatuses.First(s => s >= 2);
        airStatus[x, y] = cavernId;
    } else {
        // Isolated air pocket - create new tiny cavern
        airStatus[x, y] = nextCavernId;
        cavernHasAir[nextCavernId] = false;
        nextCavernId++;
    }
}
```

**Player places a block:**
```csharp
void OnBlockPlaced(int x, int y) {
    byte previousStatus = airStatus[x, y];
    airStatus[x, y] = 0; // Now solid
    
    if (previousStatus == 1) {
        // Might have sealed something off
        // Check each air neighbor - are they still connected to sky?
        foreach (Point neighbor in GetAirNeighbors(x, y)) {
            if (!CanReachSky(neighbor)) {
                // This area is now sealed
                AssignNewCavernId(neighbor);
            }
        }
    }
}

bool CanReachSky(Point start) {
    // Limited flood fill to find sky connection
    // Budget-capped to prevent lag
    // If budget exhausted without finding sky, assume sealed
}
```

### Oxygen Depletion

```csharp
public class OxygenSystem : ModPlayer {
    private float oxygenLevel = 100f;
    private const float MAX_OXYGEN = 100f;
    private const float DEPLETION_RATE = 0.5f; // per second in sealed area
    private const float RECOVERY_RATE = 5f; // per second in breathable area
    
    public override void PostUpdate() {
        bool inBreathableAir = IsBreathable(Player.Center);
        
        if (inBreathableAir) {
            oxygenLevel = Math.Min(MAX_OXYGEN, oxygenLevel + RECOVERY_RATE * deltaTime);
        } else {
            oxygenLevel = Math.Max(0, oxygenLevel - DEPLETION_RATE * deltaTime);
        }
        
        ApplyOxygenEffects();
    }
    
    private bool IsBreathable(Vector2 position) {
        Point tile = position.ToTileCoordinates();
        byte status = airStatus[tile.X, tile.Y];
        
        if (status == 0) return false; // Inside solid block??
        if (status == 1) return true;  // Sky-connected
        return cavernHasAir[status];   // Check cavern status
    }
    
    private void ApplyOxygenEffects() {
        if (oxygenLevel > 75) {
            // Fine
        } else if (oxygenLevel > 50) {
            // Minor debuff - slight slowness
            Player.AddBuff(BuffID.Slow, 60);
        } else if (oxygenLevel > 25) {
            // Moderate - darkness effect, slower
            Player.AddBuff(BuffID.Darkness, 60);
            Player.AddBuff(BuffID.Slow, 60);
        } else if (oxygenLevel > 0) {
            // Severe - confusion, weakness
            Player.AddBuff(BuffID.Confused, 60);
            Player.AddBuff(BuffID.Weak, 60);
        } else {
            // Suffocating - taking damage
            Player.Hurt(PlayerDeathReason.ByCustomReason("suffocated"), 5, 0);
        }
    }
}
```

---

## Edge Cases

| Scenario | Behavior |
|----------|----------|
| Door in wall | Doors are air-permeable - room stays breathable |
| Platform ceiling | Platforms are air-permeable - not sealed |
| 1-tile hole to surface | Entire connected area becomes breathable |
| Player seals themselves in | Oxygen starts depleting immediately |
| Two players, one seals | Oxygen depletes for both equally |
| Breaking into sealed cavern | Was already sealed - you inherit low oxygen |
| Hellevator with door at top | Air flows down entire shaft - all breathable |
| Tiny sealed pocket | Gets unique cavern ID, depletes fast |

---

## Oxygen Items (New Content)

### Breathing Reed
- Craftable: 10 wood + 5 vines
- When held: +50% oxygen capacity
- Flavor: A hollow reed to breathe through

### Oxygen Bubble
- Craftable: 10 glass + 5 gel + 1 water bottle
- Single use: Restores 50 oxygen instantly
- Can be hotkeyed for emergencies

### Air Pump
- Craftable: 20 iron + 10 wire + 5 cogs
- Placeable: Creates "breathable zone" in 10-tile radius
- Requires no power, but must be connected to surface via tube/shaft
- Basically a "fake sky connection" for underground bases

### Rebreather
- Found: Underground chests, rare
- Accessory: -50% oxygen depletion rate
- Essential for deep exploration

### Diving Helmet (repurposed)
- Currently only works in water
- Now also works in sealed air: -30% depletion
- Stacks with rebreather

---

## Tuning Knobs

| Parameter | Suggested Value | Notes |
|-----------|-----------------|-------|
| Max oxygen | 100 | Simple percentage |
| Depletion rate (sealed) | 0.5/sec | ~3 minutes to danger |
| Recovery rate (breathable) | 5/sec | 20 seconds to full |
| Debuff thresholds | 75/50/25/0 | Escalating consequences |
| Damage at 0 | 5/sec | Slow death, time to react |
| Door air permeability | Yes | Essential for bases |
| Platform permeability | Yes | Platforms aren't seals |

---

## Performance Considerations

### World Gen
- Flood fill entire world once: O(world size), but only at gen time
- Cavern ID assignment: same pass, no extra cost

### Runtime
- Block break: O(1) neighbor check + possible O(cavern size) promotion
- Block place: O(1) usually, O(limited flood) for seal detection
- Player oxygen check: O(1) table lookup

### Memory
- 1 byte per tile for air status: ~7MB for large world
- Cavern table: negligible (few thousand entries max)

### Worst Case
- Player seals off half the world: massive flood fill
- Mitigation: cap flood fill budget, spread across frames, or just accept rare lag spike

---

## Alternative Approaches Considered

### Depth-based oxygen (simpler)
- Below Y depth, oxygen depletes regardless of connectivity
- Pro: No seal detection needed
- Con: Doesn't feel logical, arbitrary depth cutoffs

### Time-based seal penalty
- Track how long player has been in one spot
- Debuffs if stationary too long while "threatened"
- Pro: Simpler, no world analysis
- Con: Punishes legitimate cautious play, doesn't address the core issue

### Enemy burrowing only
- Enemies dig through seal
- Pro: Already in main mod doc
- Con: Doesn't address non-combat scenarios (waiting out events)

### Seal = spawn point
- Enemies spawn inside sealed areas
- Pro: Direct punishment
- Con: Feels unfair, magical enemy appearance

---

## Integration with Main Mod

This system complements:

- **Aggro Burrowing**: Enemies dig through, oxygen depletes - double pressure
- **Persistent Position**: Can't logout to escape suffocation
- **LOS Interactions**: Can't break your seal from inside (already covered)
- **Smart Hopping**: Enemies reach you even with small walls

Together these create the experience: **You cannot hide. You can only fight, flee, or prepare.**

---

## Open Questions

1. **Should NPCs need oxygen?** (Town NPCs in sealed bases?)
2. **Blood moon / events while sealed** - does oxygen deplete faster? Spawn enemies inside?
3. **Boss arenas** - should building a sealed arena be allowed with enough prep?
4. **Multiplayer** - shared oxygen pool? Per-player?
5. **Liquid interactions** - water already has breath meter, how do they combine?
6. **Hell** - should hell be inherently low-oxygen? (volcanic fumes)

---

## Summary

The oxygen system transforms "wall yourself in" from a perfect defense into a temporary measure. You can still do it - but you're buying time, not safety. The clock is always ticking, and eventually you have to face what's outside.

This creates meaningful decisions:
- Do I seal up and use my oxygen reserve, or fight now?
- Is this natural cave breathable, or do I need to find/make a sky connection?
- How deep can I go with my current oxygen gear?
- Should I invest in an air pump for my underground base?

The cheese becomes a resource management challenge instead of an exploit.