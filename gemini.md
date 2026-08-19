# Gemini Code Assist Context: Dynamic Difficulty Tower Defense

## 🎯 Project Overview

- **Project Type**: 3D Tower Defense with Dynamic Difficulty Adjustment.
- **Engine Version**: Unity (Built-in or URP - context uses standard material setups and TMP).
- **Programming Language**: C# (.NET Standard 2.1).
- **Core Loop**: Defend against waves of enemies -> Manage Economy -> Place/Upgrade Towers -> Handle Dynamic Difficulty.

## 🏗️ Architecture & Patterns

- **Event-Driven Architecture**: The project strictly uses `EventBus.cs` and `GameEvents.cs` for cross-component communication. Avoid tight coupling and excessive Singletons.
- **Data-Driven Design (Scriptable Objects)**:
  - All stats and configurations MUST be driven by `ScriptableObjects` located in `Assets/Scripts/Data/` (e.g., `EnemyData.cs`, `TowerData.cs`, `LevelData.cs`, `WaveData.cs`).
  - Do not hardcode stats (health, damage, speed) inside MonoBehaviours.
- **Object Pooling**: Mandatory for performance. All frequently spawned entities (Enemies, Projectiles, Particle Effects) must use the existing `ObjectPooler.cs` in `Assets/Scripts/Pooling/`.

## 📂 Project Structure & Namespaces

- `Assets/Scripts/Core/`: Global managers (`GameManager.cs`, `WaveManager.cs`, `GameSpeedController.cs`) and Event system.
- `Assets/Scripts/Data/`: ScriptableObject classes.
- `Assets/Scripts/Enemy/`: Enemy behaviors (`EnemyMovement.cs`, `EnemyHealth.cs`) and specific types (`BasicEnemy`, `FastEnemy`, `TankEnemy`, `ArmorEnemy`). Uses `WaypointPath.cs` for navigation.
- `Assets/Scripts/Tower/`: Tower logic (`TowerController.cs`, `TowerPlacementManager.cs`) and interactive nodes (`BuildSite.cs`).
- `Assets/Scripts/Projectile/`: Bullet and hit logic (`ProjectileController.cs`).
- `Assets/Scripts/UI/`: User Interface (`UIManager.cs`, `TowerSlot.cs`).
- `Assets/Scripts/Pooling/`: Reusable object pools (`ObjectPooler.cs`).

## 📜 Coding Conventions

- **Naming Rules**:
  - `PascalCase` for Classes, Structs, Methods, and public properties.
  - `camelCase` for local variables and parameters.
  - `_camelCase` for private and protected fields (e.g., `_currentHealth`, `_enemyData`).
- **Unity Best Practices**:
  - Always use `[SerializeField]` to expose private fields to the Inspector instead of making them public.
  - Require components where necessary using `[RequireComponent(typeof(ComponentName))]`.
  - Cache all component references in `Awake()` or `Start()`. NEVER use `GetComponent` or `FindObjectOfType` in `Update()`.

## 🤖 Instructions for AI Agent (Gemini/Copilot)

When generating or modifying code for this project, you MUST strictly adhere to these rules:

1. **Use the EventBus**: If a tower destroys an enemy, or player health drops, DO NOT directly call `GameManager.Instance.ReduceHealth()`. Instead, trigger an event via `EventBus` (e.g., `EventBus.Trigger(GameEvents.EnemyKilled)`).
2. **Use the ObjectPooler**: When shooting projectiles or spawning enemies, DO NOT use `Instantiate()` or `Destroy()`. Use the methods provided by `ObjectPooler.cs` to fetch and return objects.
3. **Data Dependency**: Any new Enemy or Tower class you create must take its base stats from an injected `EnemyData` or `TowerData` ScriptableObject.
4. **Waypoints over NavMesh**: Enemies in this project move using `WaypointPath.cs`. Do not write NavMeshAgent logic unless explicitly requested.
5. **Code Output**: Only provide the specific C# code block requested. Add brief XML summaries (`/// <summary>`) for new public methods. Include safety null checks.

## 🧪 Important Modules Context

- `BuildSite.cs`: Represents a node where a tower can be placed.
- `TowerPlacementManager.cs`: Handles the logic of selecting a `TowerData` and instantiating it on a `BuildSite`.
- `WaveManager.cs`: Controls the flow of enemies based on `WaveData`.
