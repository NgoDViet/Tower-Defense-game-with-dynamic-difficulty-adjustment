# Boss Wave Modifier System - Modifier Approach

## Overview
This system modifies NORMAL enemies during boss waves rather than creating separate boss enemy types.

When a boss wave starts, the same enemy composition is spawned but with stat modifications applied based on the boss wave type.

## How It Works

### Wave Composition Example
**Normal Wave:**
- 5 Basic enemies
- 5 Fast enemies  
- 5 Tank enemies

**Fast Boss Wave:**
- 6 Basic enemies (5 * 1.2 = 20% more)
- 6 Fast enemies (20% more quantity)
- 6 Tank enemies (20% more quantity)
- ALL enemies have 20% less health
- ALL enemies cannot be slowed

---

## Boss Wave Types

### 1. Fast Boss Wave
**Modifications Applied:**
- 20% more enemies spawned
- 20% less health on ALL enemies
- Cannot be slowed (immune to slow effects)

**Formula:**
```
New Quantity = Base Quantity * 1.2
New Health = Base Health * 0.8
```

**Example:**
```
Base: 5 Fast enemies with 20 HP each
Boss: 6 Fast enemies with 16 HP each
```

---

### 2. Basic Boss Wave
**Modifications Applied:**
- 30% more health on ALL enemies
- +10% speed for every 20% max health lost

**Formula:**
```
New Health = Base Health * 1.3
Speed Increase = +10% per 20% health threshold crossed
```

**Example:**
```
Base: Basic enemy with 40 HP, 3 speed
Boss: Basic enemy with 52 HP, 3 speed initially
- At 41.6 HP lost (20% threshold): Speed becomes 3.3
- At 83.2 HP lost (40% threshold): Speed becomes 3.63
```

---

### 3. Armor Boss Wave
**Modifications Applied:**
- All initial stats reduced by 10%
- Starts with +1 armor (total armor = 2)
- Every 5 seconds: +1 armor and +10% attack

**Formula:**
```
New Health = Base Health * 0.9
New Attack = Base Attack * 0.9
New Speed = Base Speed * 0.9
Starting Armor = 2 (from 1)
Periodic: Armor += 1, Attack *= 1.1 every 5 sec
```

**Armor Damage Reduction:**
```
Damage Taken = Damage * 0.9^(armor)
0 armor: 100% damage
1 armor: 90% damage
2 armor: 81% damage
3 armor: 72.9% damage
4 armor: 65.61% damage
```

**Example:**
```
Base: Armor enemy with 20 HP, 4 attack, 1 armor
Boss at spawn: 18 HP, 3.6 attack, 2 armor
Boss at 5 sec: 18 HP, 3.96 attack, 3 armor
Boss at 10 sec: 18 HP, 4.36 attack, 4 armor
```

---

### 4. Tank Boss Wave
**Modifications Applied:**
- 30% more health on ALL enemies
- Heals 5% of max health every 5 seconds

**Formula:**
```
New Health = Base Health * 1.3
Heal Amount = Max Health * 0.05 every 5 seconds
```

**Example:**
```
Base: Tank enemy with 80 HP
Boss: Tank enemy with 104 HP
- Every 5 sec: Heals 5.2 HP (5% of 104)
- With sustained damage of 3 HP/sec: Enemy heals 26 HP but takes 15 HP = net +11 HP
- Requires high damage output to overcome healing
```

---

## Integration Code

### In Your Wave Manager / Enemy Spawn System

```csharp
// Get reference to BossWaveIntegration
BossWaveIntegration bossIntegration = GetComponent<BossWaveIntegration>();

// Before spawning wave
bossIntegration.StartBossWave(BossWaveType.FastBoss);

// When spawning each enemy
int baseQuantity = 5; // Your normal enemy count
int actualQuantity = bossIntegration.CalculateWaveQuantity(baseQuantity);

for (int i = 0; i < actualQuantity; i++)
{
	// Spawn enemy normally
	GameObject enemyObj = ObjectPooler.Instance.GetPooledObject(
		prefab, 
		spawnPos, 
		Quaternion.identity
	);

	EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
	health.Initialize(enemyData, difficulty);

	// Apply boss wave modifications
	bossIntegration.ApplyBossWaveModifiers(health);
}

// When wave ends
bossIntegration.EndBossWave();
```

---

## File Structure

```
Assets/Scripts/Core/BossWaves/
├── BossWaveModifier.cs           # Main modifier system & BossWaveType enum
├── BasicBossComponent.cs          # Speed scaling behavior
├── ArmorBossComponent.cs          # Armor & damage upgrade behavior
├── TankBossComponent.cs           # Healing behavior
└── BossWaveIntegration.cs        # Integration helper for wave managers
```

---

## EnemyHealth Public API

Methods added to `EnemyHealth.cs` for boss wave modifications:

```csharp
// Modify stats
public void ModifyHealth(float multiplier)    // Multiply max health
public void ModifyArmor(int addedArmor)       // Add armor stacks
public void ModifyAttack(float multiplier)    // Multiply attack damage
public void ModifySpeed(float multiplier)     // Multiply movement speed
public void SetCanBeSlowed(bool value)        // Immune to slowing effects

// Health management
public void SetCurrentHealth(int health)      // Set current health (clamped to max)
```

---

## Key Differences from Boss Enemy Classes

### Old Approach (Removed)
- Created 6 separate boss enemy classes
- Complex inheritance hierarchy
- Duplicated logic
- **60 compilation errors**

### New Approach (Current)
- Normal enemies with modifier components
- Lightweight component-based additions
- Cleaner, reusable code
- No compilation errors
- Easy to integrate with existing wave system

---

## Setup Steps

1. **Add BossWaveIntegration to your Game Manager or Wave Spawner:**
   ```csharp
   BossWaveIntegration bossIntegration = gameObject.AddComponent<BossWaveIntegration>();
   bossIntegration.bossWaveModifier = GetComponentInChildren<BossWaveModifier>();
   ```

2. **Create BossWaveModifier GameObject:**
   - Create empty GameObject
   - Attach `BossWaveModifier.cs` script
   - Assign in inspector or via code

3. **Call methods when spawning waves:**
   - `StartBossWave(type)` before spawning
   - `ApplyBossWaveModifiers(enemy)` for each enemy
   - `CalculateWaveQuantity(base)` for quantity adjustments
   - `EndBossWave()` after wave completes

---

## Testing

### Fast Boss Wave Test
```
Normal: 5 FastEnemy (6 spd, 20 hp)
Boss: 6 FastEnemy (6 spd, 16 hp each) - cannot be slowed

Expected: More enemies, lower health, harder to control
```

### Armor Boss Wave Test
```
Normal: 2 ArmorEnemy (1 armor)
Boss: 2 ArmorEnemy (2 armor), -10% stats
- At 5 sec: 3 armor, +10% attack
- At 10 sec: 4 armor, +21% attack from base

Expected: Progressively harder, damage reduction increases
```

---

## Game Balance Notes

Each boss wave type creates different strategic challenges:
- **Fast Boss**: Requires multi-target capable towers
- **Basic Boss**: Punishes spreading damage, demands burst damage
- **Armor Boss**: Tests tower diversity, armor-piercing towers required
- **Tank Boss**: Requires sustained, focused damage output
