# Tower Defense Enemy System - Boss Wave Implementation

## Overview
This document explains the complete enemy system including regular enemies and boss waves for your Tower Defense game.

## Enemy Types & Base Stats

### Basic Enemy
- **Speed**: 3 units/sec
- **Health**: 40
- **Attack**: 2 damage
- **Armor**: 0

### Fast Enemy
- **Speed**: 6 units/sec (2x Basic)
- **Health**: 20 (0.5x Basic)
- **Attack**: 1 damage (0.5x Basic)
- **Armor**: 0
- **Role**: Quickly traverses the map, low damage threat

### Tank Enemy
- **Speed**: 1.5 units/sec (0.5x Basic)
- **Health**: 80 (2x Basic)
- **Attack**: 6 damage (3x Basic)
- **Armor**: 0
- **Role**: Slow but dangerous, high health and damage

### Armor Enemy
- **Speed**: 2.25 units/sec (0.75x Basic)
- **Health**: 20 (0.5x Basic)
- **Attack**: 4 damage (2x Basic)
- **Armor**: 1
- **Role**: Resistant to damage, requires armor-penetrating towers

## Difficulty Scaling

All stats scale with difficulty levels using these formulas:

### Health & Attack Scaling (Linear)
```
Scaled Value = Base Value × Difficulty Level
```
Example: Basic enemy at Difficulty 3
- Health: 40 × 3 = 120
- Attack: 2 × 3 = 6

### Armor Scaling (Exponential)
```
Scaled Armor = Base Armor × 1.25^(Difficulty - 1)
Floor to integer
```
Example: Armor enemy at Difficulty 3
- Base = 1
- Scaled = 1 × 1.25^2 = 1.5625 = 1 (floored)

### Speed Scaling (Exponential, Clamped)
```
Scaled Speed = Base Speed × 1.15^(Difficulty - 1)
Floor to integer, clamp between 1 and 7
```
Example: Fast enemy at Difficulty 5
- Base = 6
- Scaled = 6 × 1.15^4 = 10.49 = 10 (floored) → 7 (clamped to max)

## Armor Damage Reduction System

Armor uses a **stacking multiplicative formula**:
```
Final Damage = Damage × 0.9^(Armor Count)
```

### Damage Reduction by Armor Level:
- **0 armor**: 0% reduction → 100% damage taken
- **1 armor**: 10% reduction → 90% damage taken
- **2 armor**: 19% reduction → 81% damage taken
- **3 armor**: 27.1% reduction → 72.9% damage taken
- **4 armor**: 34.39% reduction → 65.61% damage taken

## Boss Waves

### 1. Fast Boss Wave
**File**: `Assets/Scripts/Enemy/Boss/FastBossEnemy.cs`

**Mechanics**:
- Spawns **3 fast enemies every 3 seconds**
- Cannot be slowed by any tower effects
- 20% higher spawn count than normal waves
- 20% reduced health compared to standard fast enemies

**Strategy for Player**:
- Require burst damage or high volume fire
- Focus on multi-target towers (splash damage, area of effect)
- Tower placement should cover multiple lanes

### 2. Basic Boss Wave
**File**: `Assets/Scripts/Enemy/Boss/BasicBossEnemy.cs`

**Mechanics**:
- Gains **+10% speed** for every 20% of max health lost
- 30% more health than standard basic enemies
- Speed increases multiple times as damage accumulates

**Strategy for Player**:
- Requires **high burst damage** to kill before it becomes too fast
- Focus fire on single enemy rather than spreading damage
- High-damage towers should be prioritized

### 3. Armor Boss Wave
**File**: `Assets/Scripts/Enemy/Boss/ArmorBossEnemy.cs`

**Mechanics**:
- Every 5 seconds:
  - Gains **+1 armor** (multiplicative damage reduction)
  - Attack increased by **+10%**
- All initial stats **reduced by 10%** (except armor)
- Starts with **2 armor** (instead of base 1)

**Progression Over Time**:
- 0 sec: Armor 2, Current Attack
- 5 sec: Armor 3, Attack ×1.1
- 10 sec: Armor 4, Attack ×1.21
- 15 sec: Armor 5, Attack ×1.331

**Strategy for Player**:
- Requires **sustained high damage** to defeat before armor becomes unmanageable
- Towers that ignore armor are highly effective
- Critical to kill quickly within 15-20 seconds

### 4. Tank Boss Wave
**File**: `Assets/Scripts/Enemy/Boss/TankBossEnemy.cs`

**Mechanics**:
- 30% more health than standard tank enemies
- **Regenerates 5% of max health every 5 seconds**
- Very slow movement speed
- High damage output

**Strategy for Player**:
- Requires **concentrated focus fire** on single enemy
- Spreading damage allows healing to negate progress
- Area denial towers can help control position
- Long-range sustained damage towers are ideal

## File Structure

```
Assets/Scripts/Enemy/
├── Boss/
│   ├── BossEnemyHealth.cs          # Base class for all boss enemies
│   ├── FastBossEnemy.cs            # Fast spawning boss
│   ├── BasicBossEnemy.cs           # Speed-scaling boss
│   ├── ArmorBossEnemy.cs           # Armor-stacking boss
│   ├── TankBossEnemy.cs            # Regenerating tank boss
│   └── BossWaveManager.cs          # Boss spawning coordinator
├── EnemyHealth.cs                  # Regular enemy health system
├── EnemyMovement.cs                # Enemy pathfinding
└── WaypointPath.cs                 # Path definition
```

## Usage Example

```csharp
// Spawn a boss wave
BossWaveManager bossManager = GetComponent<BossWaveManager>();

BossWaveManager.BossWaveConfig config = new BossWaveManager.BossWaveConfig
{
	waveType = BossWaveManager.BossWaveType.ArmorBoss,
	enemyData = armorEnemyData,
	difficulty = 3,
	spawnPosition = new Vector3(0, 0, 0),
	waypointPath = pathTransform
};

bossManager.SpawnBossWave(0);
```

## Game Balance Notes

The boss waves create escalating difficulty:
1. **Fast Boss** - Tests map control and multi-target capability
2. **Basic Boss** - Punishes slow damage output
3. **Armor Boss** - Tests tower diversity (armor-piercing vs. standard)
4. **Tank Boss** - Requires sustained, focused strategy

Each boss wave demands different tower compositions and strategies, creating engaging gameplay variety.

## Integration with Wave Manager

These boss waves should be spawned by your WaveManager at designated difficulty thresholds. Consider:
- Introducing first boss at Wave 5-7
- Increasing boss difficulty with each appearance
- Mixing regular and boss waves for variety
- Scaling difficulty parameter with game progression
