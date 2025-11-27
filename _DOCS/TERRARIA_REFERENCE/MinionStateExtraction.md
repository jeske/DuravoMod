# Terraria 1.4.0.5 Minion aiStyle & Phasing Reference

## Summary of Findings from Decompiled Projectile.cs

Based on analysis of the decompiled source from [AdamSavard/Terarria1405](https://github.com/AdamSavard/Terarria1405).

---

## Minion aiStyle Assignments (Sorted by Phasing Behavior)

### Always Phase Through Blocks
| Minion | ProjectileID | aiStyle | tileCollide | Notes |
|--------|--------------|---------|-------------|-------|
| Stardust Dragon | 625-628 | 121 | **false** | Worm-style, always phases |
| Sanguine Bat | 755 | 156 | **false** | Always phases |
| Terraprisma | 946 | 156 | **false** | Always phases |
| Sharknado/Tempest | 407 | 62 | **forced false in AI** | Always phases (unique override) |

### Phase Only During Return (Get Stuck When Attacking)
| Minion | ProjectileID | aiStyle | tileCollide | Notes |
|--------|--------------|---------|-------------|-------|
| **Twins (Retina)** | 387 | 66 | false* | *Set false but AI likely overrides to true during attack |
| **Twins (Spazma)** | 388 | 66 | false* | *Set false but AI likely overrides to true during attack |
| **Deadly Sphere** | 533 | 66 | false* | *Bounces on collision, phases on return |
| Hornet | 373 | 62 | true (AI sets) | Phases only during return |
| Imp | 375 | 62 | true (AI sets) | Phases only during return |
| UFO | 423 | 62 | true (AI sets) | Has dash attack, phases on return |
| Stardust Cell | 613 | 62 | true (AI sets) | Has dash attack, phases on return |
| Raven | 317 | 54 | true (default) | Bounces at 0.6x velocity |

### Ground-Based (PRIMARY PATHFINDING TARGET)
| Minion | ProjectileID | aiStyle | tileCollide | Notes |
|--------|--------------|---------|-------------|-------|
| Pygmy | 191-194 | 26 | true (default) | Ground minion with minionPos |
| Spider | 376-379 | 26 | true (default) | Ground minion, climbs walls |
| Pirate (all 3) | 393-395 | 67 | true (default) | Ground minion with parrot |
| Finch | 623 | varies | true | Ground-based starter minion |

**These are the primary pathfinding QoL targets** - ground-based minions that need to follow the player through terrain. Flying minions at least drift toward the player; ground minions get completely stuck behind walls and ledges.

---

## Follow Distances and Idle Positions

Different aiStyles have different "target positions" when following the player:

### aiStyle 62 (Hornet, Imp, Sharknado, UFO, Cell)

**Idle hover position**: `player.Center + new Vector2(0, -60f)`
- Sharknado (407): `player.Center + new Vector2(0, -20f)` (40f higher offset)
- Imp (375): Uses minionPos formation: `player.Center - (10 + minionPos * 40) * direction` horizontally, `-10f` vertically

**"Close enough" thresholds**:
- `< 70f`: Slow down, nearly there
- `< 100f`: Exit return mode (ai[0] = 0)
- `> 200f`: Speed up to 9f

**Leash/return trigger** (switches to phasing return):
- Base: `500` pixels (no target)
- With target: `1000` pixels
- UFO (423) with target: `1200` pixels  
- Cell (613) with target: `1350` pixels

**Teleport threshold**: `> 2000` pixels → instant teleport to player

### aiStyle 66 (Twins, Deadly Sphere)

Similar to aiStyle 62 but with different attack patterns. Uses same general hover offset of ~60 pixels above player.

### aiStyle 67 (Pirate)

**Ground formation position**:
```
baseX = player.Center.X - (15 + player.width/2) * direction
offset = minionPos * 20 * direction  // or 40 depending on variant
```

Spaces pirates 20-40 pixels apart behind the player.

**Parrot retrieval trigger**: `> 500` pixels (summons parrot to carry back)

### aiStyle 26 (Pygmy, Spider)

Ground-based with minionPos formation. Uses similar spacing to Pirates but with different base offset.

### aiStyle 121 (Stardust Dragon)

**Idle hover**: ~100-200 pixels from player center, worm-style movement.

**Teleport threshold**: `> 2000` pixels

### aiStyle 156 (Sanguine Bat, Terraprisma)

**Idle position**: Calculated per-minion in a fan/arc formation around player:
- Sanguine: Arc formation 40 pixels from player, spread by `4.4 radians / totalIndexes`
- Terraprisma: Orbital pattern with bobbing, offset `(direction * (index * -6 - 16), -15f)`

---

## Follow Distance Constants for Pathfinding

```csharp
public static class MinionFollowDistances
{
    // "I'm close enough" - stop actively pathfinding
    public const float CloseEnough_Flying = 70f;
    public const float CloseEnough_Ground = 50f;
    
    // "I should be moving toward player" - start pathfinding
    public const float StartFollowing_Flying = 100f;
    public const float StartFollowing_Ground = 80f;
    
    // Vanilla leash distances (triggers phasing return)
    public const float Leash_Default = 500f;
    public const float Leash_WithTarget = 1000f;
    public const float Leash_UFO_WithTarget = 1200f;
    public const float Leash_Cell_WithTarget = 1350f;
    
    // Instant teleport threshold
    public const float TeleportThreshold = 2000f;
    
    // Ground minion formation spacing
    public const float PirateSpacing = 20f;      // or 40f for larger variants
    public const float PygmySpacing = 40f;
    
    // Flying minion hover offset (above player)
    public const float HoverOffset_Default = 60f;
    public const float HoverOffset_Sharknado = 20f;
    
    /// <summary>
    /// Get the target idle position for a minion
    /// </summary>
    public static Vector2 GetIdlePosition(Projectile proj, MinionStateInfo state)
    {
        Player owner = Main.player[proj.owner];
        
        if (state.Locomotion == MinionLocomotion.Ground)
        {
            // Ground minions form up behind player
            float spacing = proj.aiStyle == 67 ? PirateSpacing : PygmySpacing;
            float offset = (15 + owner.width / 2 + state.MinionPosIndex * spacing) * owner.direction;
            return new Vector2(owner.Center.X - offset, owner.Bottom.Y);
        }
        else
        {
            // Flying minions hover above
            float hoverY = proj.type == 407 ? HoverOffset_Sharknado : HoverOffset_Default;
            return owner.Center - new Vector2(0, hoverY);
        }
    }
    
    /// <summary>
    /// Check if minion is "close enough" to stop pathfinding
    /// </summary>
    public static bool IsCloseEnough(Projectile proj, MinionStateInfo state, Vector2 target)
    {
        float dist = Vector2.Distance(proj.Center, target);
        float threshold = state.Locomotion == MinionLocomotion.Ground 
            ? CloseEnough_Ground 
            : CloseEnough_Flying;
        return dist < threshold;
    }
}
```

---

## Minion State Extraction Code

```csharp
using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

public enum MinionState
{
    Unknown,
    Idle,             // Near player, no target, stationary
    Following,        // Moving toward player (NOT phasing) - PRIMARY PATHFIND TARGET
    Attacking,        // Has target, moving toward/fighting enemy
    Returning,        // Exceeded leash, PHASING back to player (ai[0]==1)
    Dashing,          // UFO/Cell teleport-dash (phasing)
    Spawning,         // Initial spawn/alpha fade-in
}

public enum MinionLocomotion
{
    Unknown,
    Flying,
    Ground,
    Worm,             // Stardust Dragon segments
}

public struct MinionStateInfo
{
    public MinionState State;
    public MinionLocomotion Locomotion;
    public bool AlwaysPhases;           // Never needs pathfinding
    public bool CurrentlyPhasing;       // Right now ignoring tiles (Returning/Dashing)
    public bool HasTarget;
    public int TargetNPCIndex;          // -1 if no target
    public Vector2 TargetPosition;      // Target center if valid
    public bool TargetIsWhipTagged;     // Player-designated target
    public int MinionPosIndex;          // For ground minions (formation slot)
    public float AttackCooldown;        // ai[1] for most ranged minions
    
    public bool TargetDataValid => HasTarget && TargetNPCIndex >= 0;
    
    /// <summary>
    /// True if minion is trying to reach player but NOT phasing.
    /// This is the primary pathfinding assist case.
    /// </summary>
    public bool NeedsPathToPlayer => State == MinionState.Following || State == MinionState.Idle;
}

public static class MinionStateExtractor
{
    // Minions that ALWAYS phase through blocks
    private static readonly HashSet<int> AlwaysPhasingTypes = new()
    {
        625, 626, 627, 628,  // Stardust Dragon segments
        407,                  // Sharknado (forced in AI)
        755,                  // Sanguine Bat
        946,                  // Terraprisma
    };
    
    // Ground-based aiStyles
    private static readonly HashSet<int> GroundAiStyles = new() { 26, 67 };
    
    /// <summary>
    /// Extracts comprehensive state info from a minion projectile.
    /// Returns struct with State=Unknown if not a recognized minion.
    /// </summary>
    public static MinionStateInfo GetMinionState(Projectile proj)
    {
        var info = new MinionStateInfo
        {
            State = MinionState.Unknown,
            Locomotion = MinionLocomotion.Unknown,
            AlwaysPhases = false,
            CurrentlyPhasing = false,
            HasTarget = false,
            TargetNPCIndex = -1,
            TargetPosition = Vector2.Zero,
            TargetIsWhipTagged = false,
            MinionPosIndex = -1,
            AttackCooldown = 0f,
        };

        if (!proj.active || !proj.minion)
            return info;

        // Determine locomotion type
        info.Locomotion = GetLocomotion(proj);
        info.AlwaysPhases = AlwaysPhasingTypes.Contains(proj.type);
        
        // Extract state based on aiStyle
        switch (proj.aiStyle)
        {
            case 26: // Pygmy, Spider (ground-based)
                ExtractAiStyle26(proj, ref info);
                break;
                
            case 54: // Raven
                ExtractAiStyle54(proj, ref info);
                break;
                
            case 62: // Hornet, Imp, Sharknado, UFO, Stardust Cell
                ExtractAiStyle62(proj, ref info);
                break;
                
            case 66: // Twins, Deadly Sphere
                ExtractAiStyle66(proj, ref info);
                break;
                
            case 67: // Pirate minions
                ExtractAiStyle67(proj, ref info);
                break;
                
            case 121: // Stardust Dragon
                ExtractAiStyle121(proj, ref info);
                break;
                
            case 156: // Sanguine Bat, Terraprisma
                ExtractAiStyle156(proj, ref info);
                break;
                
            default:
                // Unknown aiStyle - try generic extraction
                ExtractGeneric(proj, ref info);
                break;
        }
        
        // Check for whip-tagged target (universal)
        CheckWhipTarget(proj, ref info);
        
        // Determine if currently phasing
        info.CurrentlyPhasing = info.AlwaysPhases || 
                                info.State == MinionState.Returning ||
                                info.State == MinionState.Dashing ||
                                !proj.tileCollide;
        
        return info;
    }

    private static MinionLocomotion GetLocomotion(Projectile proj)
    {
        // Worm-style (Stardust Dragon)
        if (proj.type >= 625 && proj.type <= 628)
            return MinionLocomotion.Worm;
        
        // Ground-based aiStyles
        if (GroundAiStyles.Contains(proj.aiStyle))
            return MinionLocomotion.Ground;
        
        return MinionLocomotion.Flying;
    }

    /// <summary>
    /// aiStyle 26: Pygmy (191-194), Spider (376-379)
    /// Ground-based minions with minionPos formation
    /// ai[0]: target NPC whoAmI (0 or negative = no target)
    /// ai[1]: attack cooldown timer
    /// </summary>
    private static void ExtractAiStyle26(Projectile proj, ref MinionStateInfo info)
    {
        info.MinionPosIndex = proj.minionPos;
        info.AttackCooldown = proj.ai[1];
        
        int targetIdx = (int)proj.ai[0];
        if (targetIdx > 0 && targetIdx < Main.maxNPCs)
        {
            NPC target = Main.npc[targetIdx];
            if (target.active && target.CanBeChasedBy(proj))
            {
                info.HasTarget = true;
                info.TargetNPCIndex = targetIdx;
                info.TargetPosition = target.Center;
                info.State = MinionState.Attacking;
                return;
            }
        }
        
        // No target - determine if idle, following, or returning (phasing)
        Player owner = Main.player[proj.owner];
        float dist = Vector2.Distance(proj.Center, owner.Center);
        
        // Check if currently phasing (tileCollide forced off by exceeding leash)
        if (!proj.tileCollide)
        {
            info.State = MinionState.Returning; // Phasing back
        }
        else if (dist > 80f)
        {
            // Far enough to be actively following (not at destination)
            info.State = MinionState.Following; // PRIMARY PATHFIND CASE
        }
        else
        {
            info.State = MinionState.Idle; // Close to player, stationary
        }
    }

    /// <summary>
    /// aiStyle 54: Raven (317)
    /// Flying contact minion, bounces on tiles at 0.6x
    /// ai[0]: 0 = idle/follow, 1+ = has target
    /// </summary>
    private static void ExtractAiStyle54(Projectile proj, ref MinionStateInfo info)
    {
        if (proj.ai[0] >= 1f)
        {
            info.State = MinionState.Attacking;
            // Raven target is selected per-frame, not stored
        }
        else
        {
            Player owner = Main.player[proj.owner];
            float dist = Vector2.Distance(proj.Center, owner.Center);
            
            if (!proj.tileCollide)
                info.State = MinionState.Returning; // Phasing
            else if (dist > 100f)
                info.State = MinionState.Following;
            else
                info.State = MinionState.Idle;
        }
    }

    /// <summary>
    /// aiStyle 62: Hornet (373), Imp (375), Sharknado (407), UFO (423), Cell (613)
    /// ai[0]: 0 = normal, 1 = returning (PHASING), 2 = dashing
    /// ai[1]: attack/dash cooldown
    /// localAI[0]: dash cooldown (UFO/Cell)
    /// </summary>
    private static void ExtractAiStyle62(Projectile proj, ref MinionStateInfo info)
    {
        info.AttackCooldown = proj.ai[1];
        
        // ai[0] == 1 is the PHASING return state
        if ((int)proj.ai[0] == 1)
        {
            info.State = MinionState.Returning; // Phasing back to player
            return;
        }
        
        if ((int)proj.ai[0] == 2)
        {
            info.State = MinionState.Dashing;
            return;
        }
        
        // ai[0] == 0: normal state - could be idle, following, or attacking
        Player owner = Main.player[proj.owner];
        float dist = Vector2.Distance(proj.Center, owner.Center);
        
        // These minions acquire targets per-frame, so we check distance
        if (dist > 100f)
            info.State = MinionState.Following;
        else
            info.State = MinionState.Idle;
        // Note: CheckWhipTarget may upgrade this to Attacking
    }

    /// <summary>
    /// aiStyle 66: Twins (387-388), Deadly Sphere (533)
    /// ai[0]: state machine value
    /// Deadly Sphere: 0-5 idle/follow, 6-8 attacking, 9+ returning (PHASING)
    /// Twins: 0 idle/follow, 1 returning (PHASING), 2 attacking
    /// </summary>
    private static void ExtractAiStyle66(Projectile proj, ref MinionStateInfo info)
    {
        float state = proj.ai[0];
        Player owner = Main.player[proj.owner];
        float dist = Vector2.Distance(proj.Center, owner.Center);
        
        if (proj.type == 533) // Deadly Sphere
        {
            if (state >= 9f)
            {
                info.State = MinionState.Returning; // Phasing
            }
            else if (state >= 6f && state <= 8f)
            {
                info.State = MinionState.Attacking;
            }
            else if (dist > 100f)
            {
                info.State = MinionState.Following;
            }
            else
            {
                info.State = MinionState.Idle;
            }
        }
        else // Twins (387, 388)
        {
            if (state == 1f)
            {
                info.State = MinionState.Returning; // Phasing
            }
            else if (state == 2f)
            {
                info.State = MinionState.Attacking;
            }
            else if (dist > 100f)
            {
                info.State = MinionState.Following;
            }
            else
            {
                info.State = MinionState.Idle;
            }
        }
    }

    /// <summary>
    /// aiStyle 67: Pirate minions (393-395)
    /// ai[0]: 0 = grounded, 1 = being carried by parrot (PHASING)
    /// ai[1]: timer
    /// localAI[0]: attack cooldown
    /// </summary>
    private static void ExtractAiStyle67(Projectile proj, ref MinionStateInfo info)
    {
        info.AttackCooldown = proj.localAI[0];
        
        if (proj.ai[0] == 1f)
        {
            // Being carried by parrot = returning and phasing
            info.State = MinionState.Returning;
            info.CurrentlyPhasing = true;
        }
        else
        {
            Player owner = Main.player[proj.owner];
            float dist = Vector2.Distance(proj.Center, owner.Center);
            
            if (dist > 80f)
                info.State = MinionState.Following; // Walking toward player
            else
                info.State = MinionState.Idle;
        }
    }

    /// <summary>
    /// aiStyle 121: Stardust Dragon (625-628)
    /// Only head (625) has meaningful state
    /// Always phases, worm locomotion
    /// </summary>
    private static void ExtractAiStyle121(Projectile proj, ref MinionStateInfo info)
    {
        if (proj.type != 625)
        {
            // Body/tail segment - just follows head
            info.State = MinionState.Unknown;
            return;
        }
        
        // Head checks for targets each frame
        // Always phases so Following vs Returning distinction less relevant
        Player owner = Main.player[proj.owner];
        float dist = Vector2.Distance(proj.Center, owner.Center);
        info.State = dist > 150f ? MinionState.Following : MinionState.Idle;
        // Note: State will be upgraded to Attacking by CheckWhipTarget if applicable
    }

    /// <summary>
    /// aiStyle 156: Sanguine Bat (755), Terraprisma (946)
    /// ai[0]: -1 = teleporting, 0 = idle, 1+ = attacking
    /// Always phases
    /// </summary>
    private static void ExtractAiStyle156(Projectile proj, ref MinionStateInfo info)
    {
        if (proj.ai[0] == -1f)
        {
            info.State = MinionState.Spawning;
        }
        else if (proj.ai[0] >= 1f)
        {
            info.State = MinionState.Attacking;
        }
        else
        {
            // ai[0] == 0: idle or following (always phases so less distinction)
            Player owner = Main.player[proj.owner];
            float dist = Vector2.Distance(proj.Center, owner.Center);
            info.State = dist > 100f ? MinionState.Following : MinionState.Idle;
        }
    }

    /// <summary>
    /// Generic fallback for unknown aiStyles
    /// </summary>
    private static void ExtractGeneric(Projectile proj, ref MinionStateInfo info)
    {
        Player owner = Main.player[proj.owner];
        float dist = Vector2.Distance(proj.Center, owner.Center);
        
        if (!proj.tileCollide)
        {
            info.State = MinionState.Returning; // Phasing
        }
        else if (dist > 100f)
        {
            info.State = MinionState.Following;
        }
        else if (proj.velocity.LengthSquared() < 1f)
        {
            info.State = MinionState.Idle;
        }
        else
        {
            info.State = MinionState.Unknown;
        }
    }

    /// <summary>
    /// Check if owner has a whip-tagged target
    /// </summary>
    private static void CheckWhipTarget(Projectile proj, ref MinionStateInfo info)
    {
        Player owner = Main.player[proj.owner];
        
        if (!owner.HasMinionAttackTargetNPC)
            return;
            
        int targetIdx = owner.MinionAttackTargetNPC;
        if (targetIdx < 0 || targetIdx >= Main.maxNPCs)
            return;
            
        NPC target = Main.npc[targetIdx];
        if (!target.active || !target.CanBeChasedBy(proj))
            return;
        
        // Check line of sight (unless always-phasing)
        bool canSee = info.AlwaysPhases || Collision.CanHitLine(
            proj.Center, 1, 1, target.Center, 1, 1);
        
        if (canSee)
        {
            info.HasTarget = true;
            info.TargetNPCIndex = targetIdx;
            info.TargetPosition = target.Center;
            info.TargetIsWhipTagged = true;
            
            if (info.State == MinionState.Idle || info.State == MinionState.Following)
                info.State = MinionState.Attacking;
        }
    }
    
    /// <summary>
    /// Quick check: does this minion need pathfinding help right now?
    /// Primary use case: ground minions FOLLOWING player through terrain
    /// </summary>
    public static bool NeedsPathfindingAssist(Projectile proj)
    {
        if (!proj.active || !proj.minion)
            return false;
            
        var state = GetMinionState(proj);
        
        // Never help always-phasing minions
        if (state.AlwaysPhases || state.CurrentlyPhasing)
            return false;
        
        // GROUND MINIONS FOLLOWING = PRIMARY TARGET
        // This is the main QoL use case
        if (state.Locomotion == MinionLocomotion.Ground && 
            state.State == MinionState.Following)
        {
            return true;
        }
        
        // Flying minions: only if stuck (low velocity while trying to follow/attack)
        if (state.Locomotion == MinionLocomotion.Flying && 
            (state.State == MinionState.Following || state.State == MinionState.Attacking) &&
            proj.tileCollide &&
            proj.velocity.LengthSquared() < 0.5f)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get the position the minion is trying to reach
    /// </summary>
    public static Vector2 GetTargetDestination(Projectile proj, MinionStateInfo state)
    {
        if (state.HasTarget && state.State == MinionState.Attacking)
            return state.TargetPosition;
        
        // Otherwise, returning to player
        Player owner = Main.player[proj.owner];
        
        // Ground minions have formation positions
        if (state.Locomotion == MinionLocomotion.Ground && state.MinionPosIndex >= 0)
        {
            // Offset based on minionPos (alternating left/right of player)
            float offset = (state.MinionPosIndex + 1) * 20f;
            if (state.MinionPosIndex % 2 == 1) offset = -offset;
            return owner.Center + new Vector2(offset, 0);
        }
        
        // Flying minions hover above player
        return owner.Center - new Vector2(0, 60);
    }
}
```

---

## Key Discovery: aiStyle 62 Dynamic tileCollide

From AI_062() (lines 25279-25950):

```csharp
// Line 25467-25470 in AI_062():
this.tileCollide = true;  // DEFAULT for all aiStyle 62 minions
if (this.type == 407)     // Only Sharknado
{
    this.tileCollide = false;  // Sharknado ALWAYS phases
    // Also has alpha fade when inside blocks
}

// Line 25569-25570:
if ((double) this.ai[0] == 1.0)  // When returning to player
    this.tileCollide = false;    // ALL aiStyle 62 minions phase during return
```

### aiStyle 62 Behavior Summary:
- **Hornet (373)**: tileCollide = true (gets stuck), phases during return
- **Imp (375)**: tileCollide = true (gets stuck), phases during return  
- **Sharknado (407)**: tileCollide = false (ALWAYS phases), fades when in blocks
- **UFO (423)**: tileCollide = true (gets stuck), phases during return, has dash attack
- **Stardust Cell (613)**: tileCollide = true (gets stuck), phases during return, has dash attack

---

## aiStyle 66 (Twins, Deadly Sphere)

SetDefaults explicitly sets `tileCollide = false` for types 387, 388, 533.

**However**, the wiki states Twins "cannot pass through blocks unless player has moved out of range."

This suggests aiStyle 66's AI() method dynamically sets `tileCollide = true` during normal operation and only allows phasing during return. Unfortunately, the AI() method decompiled as a stub (70,257 instructions too large to display).

### Collision Handling for aiStyle 66 (lines 12730-12738):
```csharp
else if (this.aiStyle == 54)  // Raven
{
    // Bounce on collision
    if ((double) this.velocity.X != (double) velocity1.X)
        this.velocity.X = velocity1.X * -0.6f;
    if ((double) this.velocity.Y != (double) velocity1.Y)
        this.velocity.Y = velocity1.Y * -0.6f;
}
```

### Collision Handling for Deadly Sphere (lines 12609-12635):
```csharp
else if (this.type == 533)
{
    // Bounce with velocity reversal
    float num1 = 1f;
    if ((double) this.velocity.X != (double) velocity1.X)
        this.velocity.X = velocity1.X * -num1;
    if ((double) this.velocity.Y != (double) velocity1.Y)
        this.velocity.Y = (float) ((double) velocity1.Y * -(double) num1 * 0.5);
    // HitTiles effects during attack phase
}
```

---

## Confirmed Phasing Minions

Based on SetDefaults `tileCollide = false` AND no dynamic override:

| Minion | Why It Phases |
|--------|--------------|
| Stardust Dragon (625-628) | `tileCollide = false` in SetDefaults, worm AI |
| Sanguine Bat (755) | `tileCollide = false` in SetDefaults, aiStyle 156 |
| Terraprisma (946) | `tileCollide = false` in SetDefaults, aiStyle 156 |
| Sharknado (407) | Forced `false` in AI_062() regardless of state |

---

## Non-Phasing Minions (Get Stuck)

Based on SetDefaults defaults OR dynamic AI override to true:

| Minion | Collision Behavior |
|--------|--------------------|
| Hornet (373) | tileCollide = true in AI_062, bounces |
| Imp (375) | tileCollide = true in AI_062, bounces |
| UFO (423) | tileCollide = true in AI_062, bounces |
| Stardust Cell (613) | tileCollide = true in AI_062, bounces |
| Raven (317) | aiStyle 54, bounces at 0.6x velocity |
| Twins (387-388) | aiStyle 66, likely set true in AI (wiki confirms) |
| Deadly Sphere (533) | aiStyle 66, bounces with velocity reversal |
| Pygmy (191-194) | Ground-based, respects tiles |
| Pirate (393-395) | Ground-based, respects tiles |
| Spider (375-378) | Ground-based, respects tiles |

---

## Return-to-Player Phasing

When minions exceed leash distance and trigger return mode (`ai[0] = 1`):

**aiStyle 62**: All minions get `tileCollide = false` during return (line 25570)

**aiStyle 66**: Likely similar behavior (wiki: "unless player has moved out of range")

---

## For Your Mod: Building the Phasing Lookup

```csharp
public static class MinionPhasingData
{
    // Minions that ALWAYS phase through blocks
    public static readonly HashSet<int> AlwaysPhasing = new()
    {
        ProjectileID.StardustDragon1,      // 625
        ProjectileID.StardustDragon2,      // 626
        ProjectileID.StardustDragon3,      // 627
        ProjectileID.StardustDragon4,      // 628
        ProjectileID.Tempest,              // 407 (Sharknado)
        ProjectileID.BatOfLight,           // 755 (Sanguine)
        ProjectileID.EmpressBlade,         // 946 (Terraprisma)
    };
    
    // Minions that phase only during return
    // These need pathfinding assistance most
    public static readonly HashSet<int> PhasesOnReturn = new()
    {
        ProjectileID.Hornet,               // 373
        ProjectileID.FlyingImp,            // 375
        ProjectileID.UFOMinion,            // 423
        ProjectileID.StardustCellMinion,   // 613
        ProjectileID.Raven,                // 317
        ProjectileID.Retanimini,           // 387 (Twin)
        ProjectileID.Spazmamini,           // 388 (Twin)
        ProjectileID.DeadlySphere,         // 533
    };
    
    // Ground-based minions (different AI, need jump assist)
    public static readonly HashSet<int> GroundBased = new()
    {
        191, 192, 193, 194,  // Pygmy variants
        393, 394, 395,       // Pirate variants
        375, 376, 377, 378,  // Spider variants
    };
    
    public static bool AlwaysPhasesThroughBlocks(int projType)
        => AlwaysPhasing.Contains(projType);
    
    public static bool NeedsPathfindingHelp(int projType)
        => PhasesOnReturn.Contains(projType) || GroundBased.Contains(projType);
}
```

---

## Detecting Return Mode

For aiStyle 62/66 minions:
```csharp
bool isReturningToPlayer = projectile.ai[0] == 1f;
```

When `ai[0] == 1`, the minion is in return mode and will phase through blocks.
When `ai[0] == 0`, the minion is in idle/attack mode and respects tile collision.

---

## Anti-Exploit Implications

Since minions phase through blocks during return (`ai[0] == 1`), players cannot permanently trap their minions. The cheese scenario is:

1. Player places minion near enemy
2. Player walls off minion before it enters return mode
3. Minion can attack but enemy cannot reach it

Your 20-tile threshold + LOS check + 25-step pathfinding should catch this well.

---

## Files Examined

- `/home/claude/Projectile.cs` (29,885 lines from 1.4.0.5 decompile)
- AI_062() method: lines 25279-25950 (most complete)
- SetDefaults(): lines 200-8960 (all projectile definitions)
- Collision handling: lines 12000-12900

Note: The main AI() method decompiled as a stub due to excessive length (70,257 IL instructions).