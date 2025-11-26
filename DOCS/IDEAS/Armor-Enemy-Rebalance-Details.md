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

**Tin/Copper Tier:**

| Piece | Vanilla | Proposed |
|-------|---------|----------|
| Helmet | 2 | 3 |
| Chainmail | 2 | 4 |
| Greaves | 2 | 3 |
| **Total (no bonus)** | **6** | **10** |
| Set Bonus | +2 def | **Emergency Shield** (25% max HP, 5s duration, 60s CD) |

**Iron/Lead Tier:**

| Piece | Vanilla | Proposed |
|-------|---------|----------|
| Helmet | 3 | 4 |
| Chainmail | 4 | 6 |
| Greaves | 2 | 4 |
| **Total (no bonus)** | **9** | **14** |
| Set Bonus | +2 def | **+10% crit chance** |

**Silver/Tungsten Tier:**

| Piece | Vanilla | Proposed |
|-------|---------|----------|
| Helmet | 4 | 5 |
| Chainmail | 5 | 7 |
| Greaves | 3 | 5 |
| **Total (no bonus)** | **12** | **17** |
| Set Bonus | +3 def | **+15% move speed** |

**Gold/Platinum Tier:**

| Piece | Vanilla | Proposed |
|-------|---------|----------|
| Helmet | 5 | 6 |
| Chainmail | 6 | 8 |
| Greaves | 4 | 6 |
| **Total (no bonus)** | **15** | **20** |
| Set Bonus | +3 def | **Emergency Shield** (25% max HP, 10s duration, 120s CD) + debuff purge |

### Set Bonus Design Philosophy

**The progression creates meaningful choices:**

| Tier | Identity | Trade-off |
|------|----------|-----------|
| Tin/Copper | Training wheels | Safety net for new players |
| Iron/Lead | Glass cannon | Give up shield, gain kill speed |
| Silver/Tungsten | Evasion | No shield, no damage boost, just don't get hit |
| Gold/Platinum | Tank | Shield returns (slower), plus debuff cleanse |

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
        
        if (HasFullTinOrCopperSet()) {
            ActivateShield(0.25f, 5 * 60, 60 * 60, false);
        } else if (HasFullGoldOrPlatinumSet()) {
            ActivateShield(0.25f, 10 * 60, 120 * 60, true);
        }
    }
    
    private void ActivateShield(float hpPercent, int duration, int cooldown, bool purgeDebuffs) {
        shieldHP = (int)(Player.statLifeMax2 * hpPercent);
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

| Weapon | Damage | Source | Notes |
|--------|--------|--------|-------|
| Mace | 19 | Gold Chests (underground) | Chest find only |
| Flaming Mace | 19 | Mace + 99 Torches | Requires finding Mace first |
| Ball O' Hurt | 30 | Shadow Orbs (Corruption) | Evil biome |
| The Meatball | 34 | Crafted (Crimtane + Tissue) | Post-Brain of Cthulhu |
| Blue Moon | 54 | Dungeon Locked Chests | Post-Skeletron |
| Sunfury | 64 | Shadow Chests (Underworld) | Post-Skeletron |

### Solution: Craftable Early Flails

Add Lead Mace and Iron Mace as early craftable options. Same damage, different speed - Lead is slow and heavy, Iron is quick and responsive.

| Weapon | Damage | Use Time | Speed | Recipe |
|--------|--------|----------|-------|--------|
| Lead Mace | 10 | 40 | Slow | 8 Lead Bar + 3 Chain |
| Iron Mace | 10 | 28 | Fast | 8 Iron Bar + 3 Chain |

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