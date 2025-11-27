> **Status: 📋 INCOMPLETE SPEC - BRAINSTORMING**
>
> Design phase. No implementation yet.

---

# Biome Bootstrap & Emergency Beds

## Core Concept

Players can establish a respawn point in ANY biome using locally-sourced materials. No surface wood required. Quality varies by material - some beds are better than others.

---

## Universal Materials

**Planks** - structural material, converted at any workbench from its native material:

| Workbench | Source → Planks |
|-----------|-----------------|
| Wood | Wood |
| Boreal | Boreal Wood |
| Palm | Palm Wood |
| Cactus | Cactus |
| Bone | Bone |
| Mushroom | Glowing Mushroom |
| Frozen | Ice Block |
| Ebonwood | Ebonwood |
| Shadewood | Shadewood |
| Pearlwood | Pearlwood |

**Hay** - now drops 20% chance from cutting grass/plants with any weapon. Sickle remains 100% + bonus quantity.

---

## Bed Recipes

### Comfort Beds (no debuff)

| Bed | Recipe | Source Biome |
|-----|--------|--------------|
| Hay Bed | 5 Planks + 30 Hay | Surface Forest |
| Vine Bed | 5 Planks + 30 Vine Fiber | Jungle |
| Cobweb Bed | 5 Planks + 30 Cobweb | Cavern / Spider Nest |
| Kelp Bed | 5 Planks + 30 Kelp | Ocean |
| Mushroom Bed | 5 Planks + 30 Glowing Mushroom | Mushroom Biome |
| Spore Bed | 5 Planks + 30 Vile Mushroom | Corruption |
| Flesh Bed | 5 Planks + 30 Vertebrae | Crimson |
| Fairy Bed | 5 Planks + 30 Pixie Dust | Hallow |
| Ash Bed | 5 Planks + 30 Ash Block | Hell |

### Cold Beds (Chilled 30s on respawn)

| Bed | Recipe | Source Biome |
|-----|--------|--------------|
| Snow Bed | 5 Planks + 30 Snow | Surface Snow |
| Ice Bed | 10 Ice Block + 10 Snow Block | Underground Snow |

### Slab Beds (Slow 30s on respawn)

For biomes with no soft materials. You're sleeping on carved rock.

| Bed | Recipe | Source Biome |
|-----|--------|--------------|
| Stone Slab | 30 Stone | Anywhere |
| Sandstone Slab | 30 Sandstone | Desert |
| Granite Slab | 30 Granite | Granite Cave |
| Marble Slab | 30 Marble | Marble Cave |
| Obsidian Slab | 30 Obsidian | Hell |

---

## The Bootstrap

Minimum path to underground spawn point:

```
Find cobwebs → Find bones →
Craft Bone Workbench (10 bone) →
Convert bone → planks →
Craft Cobweb Bed (5 planks + 30 cobweb) →
Place bed, set spawn →
Die with dignity
```

No surface trip required.

---

## Integration Notes

- Silk beds remain unchanged (cosmetic upgrade)
- All beds function identically for spawning
- Debuffs are brief inconvenience, not punishment
- Slab beds exist for completionism / comedy / desperation