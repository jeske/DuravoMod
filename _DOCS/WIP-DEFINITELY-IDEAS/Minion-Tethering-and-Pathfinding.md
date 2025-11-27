> **Status: ✅ IMPLEMENTED**
>
> Implementation: [`Source/TetheredMinions/`](../../Source/TetheredMinions/)
> - `TetheredMinionProjectile.cs` - Main tethering logic
> - `TilePathfinder.cs` - A* pathfinding for player following
> - `MinionStateExtractor.cs` - Minion state utilities
> - `MinionLeashData.cs` - Leash data structures

---

## Minion Tethering and Pathfinding

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
