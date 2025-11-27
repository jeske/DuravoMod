

> **Status: 📋 INCOMPLETE SPEC - BRAINSTORMING**
>
> Design phase. No implementation yet.

---

## Enemy Rebalancing

### Problem

Expert mode enemies are balanced around the assumption that players will cheese. When cheese is removed, Expert damage may be too punishing for legitimate play - especially on the surface.

Current Expert mode + no cheese =  **impossible** , not  **challenging** .

### Observation

When enemies hit hard and cheese exists → players cheese more
When enemies hit hard and cheese is removed → players die immediately

The mod removes cheese. Enemy damage must be rebalanced to match.

### Approach

**Surface should be accessible.** New players need to learn the game. First few nights shouldn't be instant death. But then again, we added shields to chest pieces, so maybe that's enough?

**Digging scaled spawn.** We already have digging scaled spawn. THe faster you go the harder things are. 

We might still need to tone some dmg down.

### Proposed Changes (MAYBE)

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

