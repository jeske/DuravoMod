> **Status: ✅ IMPLEMENTED**
>
> Implementation: [`Source/PersistentPosition/PersistentPositionPlayer.cs`](../../Source/PersistentPosition/PersistentPositionPlayer.cs)

---

## Persistent Player Position

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