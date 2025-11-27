> **Status: 📋 INCOMPLETE SPEC - BRAINSTORMING**
>
> Design phase. No implementation yet.

---

## Only "Reachable" Interactions

### Problem

Players can place blocks, destroy blocks, loot chests, and grab items through walls. This enables:

* Sealed tunnels built from inside
* Looting surface chests from underground
* Grabbing drops without exposure
* Mining ore through walls

### Our Motivation

* Players have to interact to get things done. The game should not allow "non enemy interactive" looting, etc.

### Solution

ALL world interactions require a maximum length "reachable" and "open" gap, computed via local A*. Some interactions might require a 2-space gap, because most enemies can only go through a 2-space gap. We might also require the 2-space gap be open for a minimum time, because otherwise the player could just quickly destroy/create blocks to loot safetly.

### Affected Actions

| Action                  | Current       | With Mod      |
| ----------------------- | ------------- | ------------  |
| Place block             | Through walls | Path required |
| Destroy block           | Through walls | Path required |
| Open chest              | Through walls | Path required |
| Interact with furniture | Through walls | Path required |
| Destroy item/vase       | Through walls | LOS required  |
| Grab dropped item       | Through walls | LOS required  |

### Implementation

** Local A* Pathfinding **

For quality of life, we will use a local A* pathfinding algorithm with a maximum path length that is fairly short (5? squares from head to target) and computed on a small grid to determine if the player can reach the target. This will allow us to determine if the player can reach the target without LOS unreasonably interfering with quality of life.

**Multi-ray LOS calculation **

To avoid situations where it looks like the player has Line of Sight, but doesn't have LOS from a specific point we decide to calculate from, we can test multiple rays in an optimistic fashion. THe first one that succeeds fast-succeeds. Only if all fail do we have to test all paths.

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