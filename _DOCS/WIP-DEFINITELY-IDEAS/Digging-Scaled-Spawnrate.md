> **Status: 📋 INCOMPLETE SPEC - BRAINSTORMING**
>
> Design phase. No implementation yet.

---

## Digging-Scaled Spawn Rate (aka Difficulty)

### Problem

Hellevator (even with dynamite) is optimal strategy because enemies in the default game can be ignored.  

A zombie in hell hits about as hard as a zombie on the surface. There's no reason NOT to rush straight down. (is this true? i thought there were harder hitting mobs deeper - jeske)

### Solution

Two synergistic systems that make depth = danger:

### 5A: Depth-Scaled Enemy Damage

```csharp
public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers) {
    float depthRatio = (float)npc.position.Y / (Main.maxTilesY * 16f);
    float multiplier = 1f + (depthRatio * 1.5f); // 1x surface, 2.5x at hell
  
    modifiers.FinalDamage *= multiplier;
}
```

| Depth       | Damage Multiplier |
| ----------- | ----------------- |
| Surface     | 1.0x              |
| Underground | 1.3x              |
| Cavern      | 1.7x              |
| Hell        | 2.5x              |

Numbers are tunable. Goal: trash mobs in cavern layer should chunk you if you have no armor.

### 5B: Dig-Activity Spawn Rate

Digging creates noise. Noise attracts enemies. More noise = more enemies. And this scales with depth.

```csharp
public class DigActivityTracker : ModPlayer {
    private int recentDigCount = 0;
    private int digDecayTimer = 0;
  
    public override void PostUpdate() {
        // Decay dig count over time (moving quietly)
        if (digDecayTimer++ > 60) { // every second
            recentDigCount = Math.Max(0, recentDigCount - 1);
            digDecayTimer = 0;
        }
    }
  
    public void OnBlockMined() {
        recentDigCount++;
    }
  
    public float GetSpawnRateMultiplier() {
        float depthRatio = Player.position.Y / (Main.maxTilesY * 16f);
        float digNoise = Math.Min(recentDigCount / 10f, 3f); // caps at 3x from digging
        float depthBonus = 1f + (depthRatio * 2f); // 1x surface, 3x at hell
  
        return 1f + (digNoise * depthBonus);
    }
}

// Hook into spawn rate
public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns) {
    float multiplier = player.GetModPlayer<DigActivityTracker>().GetSpawnRateMultiplier();
    spawnRate = (int)(spawnRate / multiplier); // lower = more spawns
    maxSpawns = (int)(maxSpawns * multiplier);
}
```

### The Math

**Surface, digging constantly:** ~1.5x spawn rate, 1x damage. Manageable.

**Cavern, digging constantly:** ~6x spawn rate, 1.7x damage. Overwhelming.

**Cavern, moving through caves quietly:** ~1.5x spawn rate, 1.7x damage. Challenging but fair.

**Hell, digging hellevator:** ~9x spawn rate, 2.5x damage. Suicide.

### Gameplay Impact

* **Hellevator becomes suicide** - Constant digging at depth = swarmed by powered-up enemies
* **Cave navigation rewarded** - Moving quietly through existing caves = manageable spawns
* **Early game protected** - Surface digging is still fine for newbies
* **Armor actually matters** - You NEED defense to survive deep enemies
* **Expedition pacing** - Can't just speedrun to hell on day 1

### Synergy with Other Features

| Feature             | Synergy                                                |
| ------------------- | ------------------------------------------------------ |
| Burrowing enemies   | They dig TO you, buffed by depth, there's MORE of them |
| LOS block placement | Can't just wall them off while digging                 |
| Persistent position | Can't logout to escape the swarm you summoned          |
| Mortal minions      | Minions die to the buffed damage too                   |

This is the anti-hellevator system. Each feature alone is avoidable. Together, they make "dig straight down" a death sentence.

---

