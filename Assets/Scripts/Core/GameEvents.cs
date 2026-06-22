using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// Event fired when an enemy successfully navigates the path and reaches the base.
    /// </summary>
    public struct EnemyReachedBaseEvent
    {
        public GameObject EnemyGameObject;
        public int DamageToBase;

        public EnemyReachedBaseEvent(GameObject enemyGameObject, int damageToBase)
        {
            EnemyGameObject = enemyGameObject;
            DamageToBase = damageToBase;
        }
    }

    /// <summary>
    /// Event fired when an enemy is spawned into the scene.
    /// </summary>
    public struct EnemySpawnedEvent
    {
        public GameObject EnemyGameObject;

        public EnemySpawnedEvent(GameObject enemyGameObject)
        {
            EnemyGameObject = enemyGameObject;
        }
    }

    /// <summary>
    /// Event fired when an enemy is killed or destroyed.
    /// </summary>
    public struct EnemyDiedEvent
    {
        public GameObject EnemyGameObject;
        public int GoldReward;

        public EnemyDiedEvent(GameObject enemyGameObject, int goldReward)
        {
            EnemyGameObject = enemyGameObject;
            GoldReward = goldReward;
        }
    }

    /// <summary>
    /// Event fired when the global game state changes.
    /// </summary>
    public struct GameStateChangedEvent
    {
        public GameManager.GameState PreviousState;
        public GameManager.GameState NewState;

        public GameStateChangedEvent(GameManager.GameState previousState, GameManager.GameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }

    /// <summary>
    /// Event fired when the base health changes.
    /// </summary>
    public struct BaseHealthChangedEvent
    {
        public int CurrentHealth;
        public int MaxHealth;

        public BaseHealthChangedEvent(int currentHealth, int maxHealth)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
        }
    }

    /// <summary>
    /// Event fired when the player's gold amount changes.
    /// </summary>
    public struct GoldChangedEvent
    {
        public int CurrentGold;

        public GoldChangedEvent(int currentGold)
        {
            CurrentGold = currentGold;
        }
    }

    /// <summary>
    /// Event fired when a wave begins spawning.
    /// </summary>
    public struct WaveStartedEvent
    {
        public int WaveIndex;
        public int TotalWaves;

        public WaveStartedEvent(int waveIndex, int totalWaves)
        {
            WaveIndex = waveIndex;
            TotalWaves = totalWaves;
        }
    }

    /// <summary>
    /// Event fired when a wave has finished spawning all its enemies.
    /// </summary>
    public struct WaveCompletedEvent
    {
        public int WaveIndex;

        public WaveCompletedEvent(int waveIndex)
        {
            WaveIndex = waveIndex;
        }
    }

    /// <summary>
    /// Event fired when all enemies of a wave have been cleared.
    /// </summary>
    public struct WaveClearedEvent
    {
        public int WaveIndex;

        public WaveClearedEvent(int waveIndex)
        {
            WaveIndex = waveIndex;
        }
    }

    /// <summary>
    /// Event fired when a level starts.
    /// </summary>
    public struct LevelStartedEvent
    {
        public string LevelName;

        public LevelStartedEvent(string levelName)
        {
            LevelName = levelName;
        }
    }

    /// <summary>
    /// Event fired when the level is completed (won or lost).
    /// </summary>
    public struct LevelCompletedEvent
    {
        public bool Won;

        public LevelCompletedEvent(bool won)
        {
            Won = won;
        }
    }
}
