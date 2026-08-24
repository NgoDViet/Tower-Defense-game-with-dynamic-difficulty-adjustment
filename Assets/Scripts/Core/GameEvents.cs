using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// Event fired when an enemy successfully navigates the path and reaches the base.
    /// </summary>
    public readonly struct EnemyReachedBaseEvent
    {
        public readonly GameObject EnemyGameObject;
        public readonly int DamageToBase;
        public EnemyReachedBaseEvent(GameObject enemy, int damage) => (EnemyGameObject, DamageToBase) = (enemy, damage);
    }

    /// <summary>
    /// Event fired when an enemy is spawned into the scene.
    /// </summary>
    public readonly struct EnemySpawnedEvent
    {
        public readonly GameObject EnemyGameObject;
        public EnemySpawnedEvent(GameObject enemy) => EnemyGameObject = enemy;
    }

    /// <summary>
    /// Event fired when an enemy is killed or destroyed.
    /// </summary>
    public readonly struct EnemyDiedEvent
    {
        public readonly GameObject EnemyGameObject;
        public readonly int GoldReward;
        public EnemyDiedEvent(GameObject enemy, int gold) => (EnemyGameObject, GoldReward) = (enemy, gold);
    }

    /// <summary>
    /// Event fired when the global game state changes.
    /// </summary>
    public readonly struct GameStateChangedEvent
    {
        public readonly GameManager.GameState PreviousState;
        public readonly GameManager.GameState NewState;
        public GameStateChangedEvent(GameManager.GameState prev, GameManager.GameState next) => (PreviousState, NewState) = (prev, next);
    }

    /// <summary>
    /// Event fired when the base health changes.
    /// </summary>
    public readonly struct BaseHealthChangedEvent
    {
        public readonly int CurrentHealth;
        public readonly int MaxHealth;
        public BaseHealthChangedEvent(int current, int max) => (CurrentHealth, MaxHealth) = (current, max);
    }

    /// <summary>
    /// Event fired when the player's gold amount changes.
    /// </summary>
    public readonly struct GoldChangedEvent
    {
        public readonly int CurrentGold;
        public GoldChangedEvent(int gold) => CurrentGold = gold;
    }

    /// <summary>
    /// Event fired when a wave begins spawning.
    /// </summary>
    public readonly struct WaveStartedEvent
    {
        public readonly int WaveIndex;
        public readonly int TotalWaves;
        public WaveStartedEvent(int index, int total) => (WaveIndex, TotalWaves) = (index, total);
    }

    /// <summary>
    /// Event fired when a wave has finished spawning all its enemies.
    /// </summary>
    public readonly struct WaveCompletedEvent
    {
        public readonly int WaveIndex;
        public WaveCompletedEvent(int index) => WaveIndex = index;
    }

    /// <summary>
    /// Event fired when all enemies of a wave have been cleared.
    /// </summary>
    public readonly struct WaveClearedEvent
    {
        public readonly int WaveIndex;
        public WaveClearedEvent(int index) => WaveIndex = index;
    }

    /// <summary>
    /// Event fired when a level starts.
    /// </summary>
    public readonly struct LevelStartedEvent
    {
        public readonly string LevelName;
        public LevelStartedEvent(string name) => LevelName = name;
    }

    /// <summary>
    /// Event fired when the level is completed (won or lost).
    /// </summary>
    public readonly struct LevelCompletedEvent
    {
        public readonly bool Won;
        public LevelCompletedEvent(bool won) => Won = won;
    }
}