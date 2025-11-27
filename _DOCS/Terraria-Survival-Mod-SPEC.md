# Terraria Survival Overhaul Mod

A tModLoader mod to slow the game pace and expand the depth by removing exploits that make interacting with enemies optional.

---

## Core Philosophy

Terraria presents platformer-combat aesthetics but gives players tools that make engagement optional. This mod surgically removes the worst offenders while preserving legitimate building and crafting QoL.

Currently a "Moon Lord" speedrun can be done in 50-70 minutes by an expert. When we are done, the minimum viable time for this should be closer to 10-20 hours, with a typical run taking 30-40 hours.

NOTE: Reddit thread at: https://www.reddit.com/r/Terraria/comments/1p6n7jd/interest_in_survival_overhaul_mod/

---

## Implementation Order

**Foundational (do first):**

- [x] Persistent Player Position - without this nothing else matters
- [x] Minion Tethering - no AFK murder rooms, A* pathfinding for player following
- [x] Armor Redesign (partial):
  - [x] Defense redistribution (set bonus defense baked into pieces)
  - [x] Emergency Shield (Copper/Tin low-tier, Gold/Platinum high-tier)
  - [x] Shiny set bonus (ore/gem sparkle effect)
  - [ ] Iron/Lead +10% crit chance
  - [ ] Silver/Tungsten +15% move speed
- [ ] Enemy Smart Jump Height (WIP - code exists, needs testing)

**Core Anti-Cheese:**

- [ ] LOS/Path Only Interactions - remove cheese looting, etc
- [ ] Aggro Burrowing - enemies dig to reach you
- [ ] Depth-Scaled Difficulty - makes the world dangerous

**Biome Survival Lean-in:**

- [ ] Biome Crafting Rework - require biome specific buildings/machines, bootstrap in every biome
- [ ] Travel Rework - delete recall, delete mirror, consider craftable pylons

**Cleanup:**

- [ ] Enemy Rebalancing - make surface survivable without cheese

**Maybe:**

- [ ] Combat Building Lockout? - can't wall off mid-fight (lowest priority, may not need)

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
