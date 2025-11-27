> **Status: 🔶 WORK IN PROGRESS**
>
> Implementation started: [`Source/EnemySmartHopping/SmartHopperNPC.cs`](../../Source/EnemySmartHopping/SmartHopperNPC.cs)
>
> Needs testing and tuning.

---

## Smart Hopping

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

| Obstacle     | Before                     | After                    |
| ------------ | -------------------------- | ------------------------ |
| 1-tile step  | Big jump, overshoot        | Tiny hop, lands on step  |
| 2-tile wall  | Big jump, often overshoots | Precise hop to top       |
| 3-tile wall  | Big jump, sometimes works  | Precise hop to top       |
| 4-tile wall  | Big jump, usually works    | Precise hop to top       |
| 5+ tile wall | Big jump, fails            | Normal AI (or burrowing) |

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
