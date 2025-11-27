# Terraria Survival Overhaul Mod

A tModLoader mod to slow the game pace and expand the depth by removing exploits that make interacting with enemies optional.

---

## Core Philosophy

Terraria presents platformer-combat aesthetics but gives players tools that make engagement optional. This mod surgically removes the worst offenders while preserving legitimate building and crafting QoL.

Currently a "Moon Lord" speedrun can be done in 50-70 minutes by an expert. WHen we are done, the minimum viable time for this should be closer to 10-20 hours, with a typical ru

NOTE: Reddit thread at: https://www.reddit.com/r/Terraria/comments/1p6n7jd/interest_in_survival_overhaul_mod/

---




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
