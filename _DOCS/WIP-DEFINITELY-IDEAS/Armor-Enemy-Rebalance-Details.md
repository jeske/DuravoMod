# Item & Enemy Rebalance

## Philosophy

Vanilla Terraria balance assumes cheese. Remove cheese, balance breaks. This document tracks specific rebalancing needed to make the mod playable.

**Guiding principle:** Buff player tools rather than nerf enemies. Upgrades should FEEL meaningful, not just slightly-less-terrible.

---

## Armor Progression Rework

### Problem

Early armor upgrades are anti-incremental:

1. Set bonuses are flat defense
2. Upgrading one piece = lose set bonus = net zero
3. Must farm entire set before seeing improvement
4. Material cliffs (need 50+ bars for full set) make progression feel stuck

Example: Player has Mining Helmet + Tin Chest + Tin Greaves (7 defense). Upgrading chest to Iron gains +1 defense but is wasted unless you commit to full Iron.

### Solution

1. **Bake defense into pieces** - each piece gives full value standalone
2. **Set bonuses become utility** - crit, speed, knockback resist (not more defense)
3. **Higher base values** - compensate for cheese removal

### Proposed Early Armor Values

**Goal:** Redistribute vanilla set bonus defense into pieces. Same total defense, but each piece stands alone.

**Copper Tier:**

| Piece           | Vanilla     | Proposed                                    |
| --------------- | ----------- | ------------------------------------------- |
| Helmet          | 1           | 1                                           |
| Chainmail       | 2           | 3 and **Shield** (30 HP, 5s, 60s cd) |
| Greaves         | 1           | 2                                           |
| Set Bonus       | +2 def      | **Shiny** (nearby ores/gems sparkle)  |
| **TOTAL** | **6** | **6**                                 |

**Tin Tier:**

| Piece           | Vanilla     | Proposed                                    |
| --------------- | ----------- | ------------------------------------------- |
| Helmet          | 2           | 2                                           |
| Chainmail       | 2           | 3 and **Shield** (30 HP, 5s, 60s cd) |
| Greaves         | 2           | 2                                           |
| Set Bonus       | +1 def      | **Shiny** (nearby ores/gems sparkle)  |
| **TOTAL** | **7** | **7**                                 |

**Iron Tier:**

| Piece           | Vanilla     | Proposed                                   |
| --------------- | ----------- | ------------------------------------------ |
| Helmet          | 2           | 2                                          |
| Chainmail       | 3           | 4 and +**10% crit chance**           |
| Greaves         | 2           | 3                                          |
| Set Bonus       | +2 def      | **Shiny** (nearby ores/gems sparkle) |
| **TOTAL** | **9** | **9**                                |

**Lead Tier:**

| Piece           | Vanilla      | Proposed                                   |
| --------------- | ------------ | ------------------------------------------ |
| Helmet          | 3            | 3                                          |
| Chainmail       | 3            | 4 and +**10% crit chance**          |
| Greaves         | 3            | 3                                          |
| Set Bonus       | +1 def       | **Shiny** (nearby ores/gems sparkle) |
| **TOTAL** | **10** | **10**                               |

**Silver Tier:**

| Piece           | Vanilla      | Proposed                                   |
| --------------- | ------------ | ------------------------------------------ |
| Helmet          | 3            | 3                                          |
| Chainmail       | 4            | 5 and **+15% move speed**            |
| Greaves         | 3            | 4                                          |
| Set Bonus       | +2 def       | **Shiny** (nearby ores/gems sparkle) |
| **TOTAL** | **12** | **12**                               |

**Tungsten Tier:**

| Piece           | Vanilla      | Proposed                                   |
| --------------- | ------------ | ------------------------------------------ |
| Helmet          | 4            | 4                                          |
| Chainmail       | 4            | 5 and **+15% move speed**            |
| Greaves         | 3            | 4                                          |
| Set Bonus       | +2 def       | **Shiny** (nearby ores/gems sparkle) |
| **TOTAL** | **13** | **13**                               |

**Gold Tier:**

| Piece           | Vanilla      | Proposed                                              |
| --------------- | ------------ | ----------------------------------------------------- |
| Helmet          | 4            | 4                                                     |
| Chainmail       | 5            | 6 and **Shield** (15% HP, 10s, 120s cd, purges) |
| Greaves         | 4            | 6                                                     |
| Set Bonus       | +3 def       | **Shiny** (nearby ores/gems sparkle)            |
| **TOTAL** | **16** | **16**                                          |

**Platinum Tier:**

| Piece           | Vanilla      | Proposed                                               |
| --------------- | ------------ | ------------------------------------------------------ |
| Helmet          | 5            | 5                                                      |
| Chainmail       | 5            | 7 and **+Shield** (15% HP, 10s, 120s cd, purges) |
| Greaves         | 4            | 6                                                      |
| Set Bonus       | +4 def       | **Shiny** (nearby ores/gems sparkle)             |
| **TOTAL** | **18** | **18**                                           |

### Set Bonus Design Philosophy

**The progression creates meaningful choices:**

| Tier            | Identity        | Trade-off                                      |
| --------------- | --------------- | ---------------------------------------------- |
| Tin/Copper      | Training wheels | Safety net for new players                     |
| Iron/Lead       | Glass cannon    | Give up shield, gain kill speed                |
| Silver/Tungsten | Evasion         | No shield, no damage boost, just don't get hit |
| Gold/Platinum   | Tank            | Shield returns (slower), plus debuff cleanse   |

**Key tensions:**

- Tin → Iron: "Do I give up my panic button for +4 def and +10% crit?"
- Iron → Silver: "Trade damage for speed? Only +3 def gain..."
- Silver → Gold: "Give up speed, become unkillable?"

Some players may *stay* in Silver for mobility even when Gold is affordable. That's good design - horizontal choice, not just vertical power.

### Shield Implementation

```csharp
public class ArmorShieldEffect : ModPlayer {
    private int shieldCooldown = 0;
    private int shieldHP = 0;
    private int shieldDuration = 0;
  
    public override void PostUpdate() {
        if (shieldCooldown > 0) shieldCooldown--;
        if (shieldDuration > 0) {
            shieldDuration--;
            if (shieldDuration == 0) shieldHP = 0; // Shield expires
        }
    }
  
    public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
        if (shieldHP > 0) {
            int absorbed = Math.Min(shieldHP, (int)modifiers.FinalDamage.Base);
            shieldHP -= absorbed;
            modifiers.FinalDamage.Base -= absorbed;
            CombatText.NewText(Player.Hitbox, Color.Cyan, $"Absorbed {absorbed}");
        }
    }
  
    public override void OnHurt(Player.HurtInfo info) {
        if (shieldCooldown > 0) return;
    
        if (HasTinOrCopperChestplate()) {
            ActivateShield(30, 5 * 60, 60 * 60, false); // Flat 30 HP shield
        } else if (HasGoldOrPlatinumChestplate()) {
            int shieldAmount = (int)(Player.statLifeMax2 * 0.15f); // 15% HP shield
            ActivateShield(shieldAmount, 10 * 60, 120 * 60, true);
        }
    }
  
    private void ActivateShield(int shieldAmount, int duration, int cooldown, bool purgeDebuffs) {
        shieldHP = shieldAmount;
        shieldDuration = duration;
        shieldCooldown = cooldown;
    
        if (purgeDebuffs) {
            // Clear common debuffs
            Player.ClearBuff(BuffID.OnFire);
            Player.ClearBuff(BuffID.Poisoned);
            Player.ClearBuff(BuffID.Venom);
            Player.ClearBuff(BuffID.Chilled);
            Player.ClearBuff(BuffID.Frozen);
            Player.ClearBuff(BuffID.Burning);
        }
    
        CombatText.NewText(Player.Hitbox, Color.Gold, $"+{shieldHP} Shield");
    }
}

### Why Buff Rather Than Nerf Enemies

- Upgrades feel rewarding instead of "less bad"
- Each piece matters - no wasted partial upgrades
- Expert/Master damage stays punishing but survivable with gear
- Incentivizes engagement with progression system

### Implementation Notes

```csharp
public class ArmorRebalance : GlobalItem {
    public override void SetDefaults(Item item) {
        switch (item.type) {
            case ItemID.TinHelmet:
                item.defense = 3;
                break;
            case ItemID.TinChainmail:
                item.defense = 4;
                break;
            // etc.
        }
    }
  
    public override void UpdateEquip(Item item, Player player) {
        // Set bonus logic handled in ModPlayer
    }
}
```

---

## Enemy Rebalance (Placeholder)

*To be filled in - need to identify specific pain points*

- Surface night enemy damage vs. available armor?
- Underground enemy scaling?
- Expert/Master multipliers with cheese removed?

---

## Weapon Progression: Early Flails

### Problem

Pre-Hardmode flails have a gap: the only early options are chest-only (Mace) or require evil biome access (Ball O' Hurt / Meatball). Players who want cursor-aimed melee have no craftable path.

### Vanilla Pre-Hardmode Flails

| Weapon       | Damage | Source                      | Notes                       |
| ------------ | ------ | --------------------------- | --------------------------- |
| Mace         | 19     | Gold Chests (underground)   | Chest find only             |
| Flaming Mace | 19     | Mace + 99 Torches           | Requires finding Mace first |
| Ball O' Hurt | 30     | Shadow Orbs (Corruption)    | Evil biome                  |
| The Meatball | 34     | Crafted (Crimtane + Tissue) | Post-Brain of Cthulhu       |
| Blue Moon    | 54     | Dungeon Locked Chests       | Post-Skeletron              |
| Sunfury      | 64     | Shadow Chests (Underworld)  | Post-Skeletron              |

### Solution: Craftable Early Flails

Add Lead Mace and Iron Mace as early craftable options. Same damage, different speed - Lead is slow and heavy, Iron is quick and responsive.

| Weapon    | Damage | Use Time | Speed | Recipe               |
| --------- | ------ | -------- | ----- | -------------------- |
| Lead Mace | 10     | 40       | Slow  | 8 Lead Bar + 3 Chain |
| Iron Mace | 10     | 28       | Fast  | 8 Iron Bar + 3 Chain |

### Why This Matters

- **Cursor-aim is valuable** - worth trading damage for control
- **Same damage = real choice** - pick based on playstyle or available materials
- **~30% faster attacks on Iron** - meaningfully better DPS for same damage number
- **Lead isn't "worse Iron"** - it's a different tool (slower but same hit damage)
- **Low barrier** - 8 bars + 3 chain is affordable early game

### Progression Flow

```
Wood/Cactus Sword (12 dmg, directional)
    ↓
Lead Mace (10 dmg, cursor-aim, slow) ←→ Iron Mace (10 dmg, cursor-aim, fast)
    ↓
Mace (19 dmg, if found in chest)
    ↓
Ball O' Hurt / Meatball (30-34 dmg, evil biome)
    ↓
Blue Moon (54 dmg, post-Skeletron)
    ↓
Sunfury (64 dmg, Underworld)
```

The Lead/Iron Maces give players cursor-aim early at the cost of raw damage. They're sidegrades to ore swords, not straight upgrades.

---

## Weapon Progression (Placeholder)

*To be filled in*

- Bow damage scaling?
- Melee viability without cheese?
- Thrown weapon availability?

---

## Brainstorm Queue

- [ ] Mining helmet: should it have more defense to remain competitive?
- [ ] Shield accessories: earlier availability?
- [ ] Healing item balance with longer fights?
- [ ] Defense vs damage reduction - which scales better?

---

## Testing Scenarios

- [ ] First night survival in Tin armor (no cheese)
- [ ] First cave exploration in Iron armor
- [ ] Eye of Cthulhu with Silver gear (no arena cheese)
- [ ] Blood Moon at each armor tier
