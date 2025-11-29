> **Status: 🔶 PARTIAL IMPLEMENTATION**
>
> Implementation: [`Source/ArmorRebalance/`](../../Source/ArmorRebalance/)
>
> **Completed:**
>
> - [X] Defense redistribution into pieces (no more set bonus defense)
> - [X] Emergency Shield (Copper/Tin, Gold/Platinum chestplates)
> - [X] Shiny set bonus (ore/gem sparkle effect)
> - [X] Shield visual layer
> - [X] Fragile debuff system
>
> **Not Yet Implemented:**
>
> - [ ] Iron/Lead +10% crit chance on chestplate
> - [ ] Silver/Tungsten +15% move speed on chestplate
> - [ ] Early flails (Lead Mace, Iron Mace)
> - [ ] Enemy rebalancing tuning

---

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
2. **Multi-piece buffs replace set bonuses** - mix-and-match across armor types
3. **Higher base values** - compensate for cheese removal

---

## Multi-Piece Buff System

Instead of requiring a full matching set, armor pieces contribute **tags** toward multi-piece buffs. Wearing 2+ pieces with the same tag activates the buff. This enables mix-and-match builds.

### Buff Definitions

| Buff | Pieces Required | Effect | Contributing Armors |
|------|-----------------|--------|---------------------|
| **Shiny** | 2+ | Nearby ores/gems sparkle (base range) | Copper, Tin, Silver |
| **Super Shiny** | 2+ | Nearby ores/gems sparkle (farther range) | Gold, Platinum |

**Note:** Super Shiny counts as Shiny (but not vice versa). A player with 1 Gold + 1 Copper piece has 2 Shiny-contributors, activating Shiny 2pc.

---

## Chestplate-Only Effects

Some effects are granted directly by the chestplate, not as multi-piece buffs:

| Effect | Chestplates | Description |
|--------|-------------|-------------|
| **Shield (Basic)** | Copper, Tin | 30HP shield on damage, 5s duration, 60s cooldown |
| **Heavy** | Iron, Lead, Tungsten | +15% knockback dealt |
| **Speed Boost** | Silver | +15% move speed |
| **Shield (Advanced)** | Gold, Platinum | 15% max HP shield, 10s duration, 120s cooldown, purges debuffs |

**Note:** Iron/Lead/Tungsten do NOT contribute to the Shiny system - they only provide the Heavy knockback effect.

---

### Tooltip Display System

Tooltips change based on context (inventory vs equipped):

**Inventory View** (unequipped armor in inventory/chest):
- Shows what the armor provides as a static description
- Color: Standard greyish tooltip text
- Example: `Heavy (2pc) +15% knockback and knockback reflection`

**Equipped View** (hovering over equipped armor piece):
- Shows the current state of the buff (current/required pieces)
- **Inactive** (not enough pieces):
  - Buff name + description: Darker grey (dimmed)
  - Current count: RED
  - Example: `Heavy (`<span style="color:red">`1`</span>`/2pc) +15% knockback and knockback reflection`
- **Active** (2+ matching pieces equipped):
  - Buff name: GREEN
  - Description: Standard light grey
  - Example: <span style="color:green">`Heavy`</span>` (2/2pc) +15% knockback and knockback reflection`

**Implementation Notes:**
```csharp
// Pseudo-code for tooltip generation
string GetMultiPieceTooltip(Item item, bool isEquipped) {
    var tag = GetArmorTag(item); // "Shiny", "Heavy", "Super Shiny"
    var description = GetTagDescription(tag);
    
    if (!isEquipped) {
        // Inventory view - static description
        return $"{tag} (2pc) {description}";
    }
    
    // Equipped view - show current state
    int currentCount = CountEquippedPiecesWithTag(tag);
    bool isActive = currentCount >= 2;
    
    if (isActive) {
        // Green buff name, light grey description
        return $"[c/00FF00:{tag}] ({currentCount}/2pc) {description}";
    } else {
        // Dimmed text, red count
        return $"[c/666666:{tag}] ([c/FF0000:{currentCount}]/2pc) {description}]";
    }
}
```

### Armor Tag Assignments

| Armor | Shiny Tag | Chest Bonus |
|-------|-----------|-------------|
| **Copper** | Shiny | Shield (30HP, 5s, 60s cd) |
| **Tin** | Shiny | Shield (30HP, 5s, 60s cd) |
| **Iron** | *(none)* | Heavy (+15% knockback) |
| **Lead** | *(none)* | Heavy (+15% knockback) |
| **Silver** | Shiny | +15% move speed |
| **Tungsten** | *(none)* | Heavy (+15% knockback) |
| **Gold** | Super Shiny | Shield (15%HP, 10s, 120s, purge) |
| **Platinum** | Super Shiny | Shield (15%HP, 10s, 120s, purge) |

---

### Proposed Early Armor Values

**Goal:** Redistribute vanilla set bonus defense into pieces. Same total defense, but each piece stands alone.

| Tier | Vanilla H/C/B | V.Set | V.Def | Proposed H/C/B | P.Chest Bonus | P.Tag | P.Def |
|------|---------------|-------|-------|----------------|---------------|-------|-------|
| **Copper** | 1/2/1 | +2 def | **6** | 1/3/2 | Shield (30HP, 5s, 60s cd) | Shiny | **6** |
| **Tin** | 2/2/1 | +2 def | **7** | 2/3/2 | Shield (30HP, 5s, 60s cd) | Shiny | **7** |
| **Iron** | 2/3/2 | +2 def | **9** | 2/4/3 | Heavy (+15% knockback) | *(none)* | **9** |
| **Lead** | 3/3/3 | +1 def | **10** | 3/4/3 | Heavy (+15% knockback) | *(none)* | **10** |
| **Silver** | 3/4/3 | +2 def | **12** | 3/5/4 | +15% move speed | Shiny | **12** |
| **Tungsten** | 4/4/3 | +2 def | **13** | 4/5/4 | Heavy (+15% knockback) | *(none)* | **13** |
| **Gold** | 4/5/4 | +3 def | **16** | 4/6/6 | Shield (15%HP, 10s, 120s, purge) | Super Shiny | **16** |
| **Platinum** | 5/5/4 | +4 def | **18** | 5/7/6 | Shield (15%HP, 10s, 120s, purge) | Super Shiny | **18** |

**Legend:** H/C/B = Helmet/Chainmail/Boots defense values.

---

### Multi-Piece Buff Design Philosophy

**Mix-and-match creates meaningful choices:**

| Build Example | Pieces | Active Buffs | Trade-off |
|---------------|--------|--------------|-----------|
| Tin Helm + Copper Chest + Silver Boots | 3 Shiny | Shiny 2pc | Pure mining/exploration focus |
| Iron Helm + Lead Chest + Tungsten Boots | 0 Shiny | Heavy (chestplate) | Combat focus, no sparkle |
| Gold Helm + Tin Chest + Lead Boots | 2 Shiny (Gold counts) | Shiny 2pc | Mixed - ore detection + Heavy chestplate |
| Gold Helm + Platinum Chest | 2 Super Shiny | Super Shiny 2pc | Best mining, double shield chest bonus |

**Key tensions:**

- Shiny vs Heavy: "Do I want ore detection or knockback power?"
- Mixing tiers: "Gold helm + Tin chest = I get Shiny AND keep my early shield"
- Super Shiny investment: "Is the extended range worth 2 high-tier pieces?"

**Note on Heavy:** Unlike Shiny, Heavy is a chestplate-only effect. There's no 2pc bonus; you get the knockback bonus just by wearing an Iron, Lead, or Tungsten chestplate. This means you can mix Heavy chestplate + Shiny helmet/boots for both ore detection AND knockback power.

Players can now upgrade individual pieces without losing their build identity. A player who values Shiny can upgrade Copper → Tin → Silver while maintaining their ore detection.

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
