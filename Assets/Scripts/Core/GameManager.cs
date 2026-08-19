using UnityEngine;
using TowerDefense.Data;
using TowerDefense.Pooling;

namespace TowerDefense.Core
{
    /// <summary>
    /// Core game manager that orchestrates the overall game state, player stats (health and gold),
    /// and listens to core game events via the EventBus.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static bool startAsEndless = false;

        public enum GameState
        {
            MainMenu,
            Playing,
            Pause,
            Victory,
            Defeat
        }

        public static GameManager Instance { get; private set; }

        [Header("Level Settings")]
        [SerializeField] private LevelData defaultLevelData; // Fallback level data if not started dynamically

        // Properties for editor setup bypass
        public LevelData DefaultLevelData { get => defaultLevelData; set => defaultLevelData = value; }

        private GameState _currentState = GameState.MainMenu;
        private LevelData _activeLevelData;
        private int _currentHealth;
        private int _currentGold;
        private int _currentWaveIndex = -1;
        private int _activeEnemiesCount = 0;
        private bool _isSpawningWave = false;
        private bool _isEndlessMode = false;

        // Getters
        public GameState CurrentState => _currentState;
        public LevelData ActiveLevelData => _activeLevelData;
        public int CurrentHealth => _currentHealth;
        public int CurrentGold => _currentGold;
        public int CurrentWaveIndex => _currentWaveIndex;
        public int ActiveEnemiesCount => _activeEnemiesCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            // Subscribe to game events
            EventBus<EnemySpawnedEvent>.Subscribe(OnEnemySpawned);
            EventBus<EnemyDiedEvent>.Subscribe(OnEnemyDied);
            EventBus<EnemyReachedBaseEvent>.Subscribe(OnEnemyReachedBase);
            EventBus<WaveStartedEvent>.Subscribe(OnWaveStarted);
            EventBus<WaveCompletedEvent>.Subscribe(OnWaveCompleted);
        }

        private void OnDisable()
        {
            // Unsubscribe to avoid memory leaks
            EventBus<EnemySpawnedEvent>.Unsubscribe(OnEnemySpawned);
            EventBus<EnemyDiedEvent>.Unsubscribe(OnEnemyDied);
            EventBus<EnemyReachedBaseEvent>.Unsubscribe(OnEnemyReachedBase);
            EventBus<WaveStartedEvent>.Unsubscribe(OnWaveStarted);
            EventBus<WaveCompletedEvent>.Unsubscribe(OnWaveCompleted);
        }

        private void Start()
        {
            // If default level data is specified, start playing immediately (useful for testing scene directly)
            if (defaultLevelData != null)
            {
                if (startAsEndless)
                {
                    startAsEndless = false; // Consume the flag
                    StartEndlessMode(defaultLevelData);
                }
                else
                {
                    StartLevel(defaultLevelData);
                }
            }
            else
            {
                SetState(GameState.MainMenu);
            }
        }

        /// <summary>
        /// Transitions the game to a new state and manages side effects like time scale.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (_currentState == newState) return;

            GameState oldState = _currentState;
            _currentState = newState;

            // Handle state transitions
            switch (_currentState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Pause:
                    Time.timeScale = 0f;
                    break;
                case GameState.Victory:
                    Time.timeScale = 0f;
                    EventBus<LevelCompletedEvent>.Raise(new LevelCompletedEvent(true));
                    break;
                case GameState.Defeat:
                    Time.timeScale = 0f;
                    EventBus<LevelCompletedEvent>.Raise(new LevelCompletedEvent(false));
                    break;
            }

            Debug.Log($"[GameManager] State changed from {oldState} to {_currentState}");
            EventBus<GameStateChangedEvent>.Raise(new GameStateChangedEvent(oldState, _currentState));
        }

        /// <summary>
        /// Starts a level, initializing health, gold, and triggering events.
        /// </summary>
        public void StartLevel(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogError("[GameManager] Cannot start level with null LevelData!");
                return;
            }

            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnAllActiveToPool();
            }

            _activeLevelData = levelData;
            _currentHealth = levelData.BaseMaxHealth;
            _currentGold = levelData.StartingGold;
            _currentWaveIndex = -1;
            _activeEnemiesCount = 0;
            _isSpawningWave = false;
            _isEndlessMode = false;

            SetState(GameState.Playing);

            EventBus<LevelStartedEvent>.Raise(new LevelStartedEvent(levelData.LevelName));
            EventBus<BaseHealthChangedEvent>.Raise(new BaseHealthChangedEvent(_currentHealth, levelData.BaseMaxHealth));
            EventBus<GoldChangedEvent>.Raise(new GoldChangedEvent(_currentGold));
        }

        public void StartEndlessMode(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogError("[GameManager] Cannot start endless mode with null LevelData!");
                return;
            }

            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnAllActiveToPool();
            }

            _activeLevelData = levelData;
            _currentHealth = levelData.BaseMaxHealth;
            _currentGold = levelData.StartingGold;
            _currentWaveIndex = -1;
            _activeEnemiesCount = 0;
            _isSpawningWave = false;
            _isEndlessMode = true;

            SetState(GameState.Playing);

            EventBus<LevelStartedEvent>.Raise(new LevelStartedEvent(levelData.LevelName + " (Endless)"));
            EventBus<BaseHealthChangedEvent>.Raise(new BaseHealthChangedEvent(_currentHealth, levelData.BaseMaxHealth));
            EventBus<GoldChangedEvent>.Raise(new GoldChangedEvent(_currentGold));
        }

        /// <summary>
        /// Toggles pause state during gameplay.
        /// </summary>
        public void TogglePause()
        {
            if (_currentState == GameState.Playing)
            {
                SetState(GameState.Pause);
            }
            else if (_currentState == GameState.Pause)
            {
                SetState(GameState.Playing);
            }
        }

        /// <summary>
        /// Restarts the currently active level.
        /// </summary>
        public void RestartLevel()
        {
            if (_activeLevelData != null)
            {
                if (_isEndlessMode)
                {
                    StartEndlessMode(_activeLevelData);
                }
                else
                {
                    StartLevel(_activeLevelData);
                }
            }
        }

        /// <summary>
        /// Adds gold to the player's account.
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            _currentGold += amount;
            EventBus<GoldChangedEvent>.Raise(new GoldChangedEvent(_currentGold));
        }

        /// <summary>
        /// Attempts to purchase an item. Returns true if successful.
        /// </summary>
        public bool TrySpendGold(int amount)
        {
            if (amount < 0) return false;
            if (_currentGold >= amount)
            {
                _currentGold -= amount;
                EventBus<GoldChangedEvent>.Raise(new GoldChangedEvent(_currentGold));
                return true;
            }
            return false;
        }

        #region Event Handlers

        private void OnEnemySpawned(EnemySpawnedEvent evt)
        {
            _activeEnemiesCount++;
        }

        private void OnEnemyDied(EnemyDiedEvent evt)
        {
            AddGold(evt.GoldReward);
            DecrementEnemyCount();
        }

        private void OnEnemyReachedBase(EnemyReachedBaseEvent evt)
        {
            _currentHealth = Mathf.Max(0, _currentHealth - evt.DamageToBase);
            EventBus<BaseHealthChangedEvent>.Raise(new BaseHealthChangedEvent(_currentHealth, _activeLevelData != null ? _activeLevelData.BaseMaxHealth : 20));

            if (_currentHealth <= 0)
            {
                SetState(GameState.Defeat);
            }
            else
            {
                DecrementEnemyCount();
            }
        }

        private void OnWaveStarted(WaveStartedEvent evt)
        {
            _currentWaveIndex = evt.WaveIndex;
            _isSpawningWave = true;
        }

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            _isSpawningWave = false;
            CheckWaveClearStatus();
        }

        private void DecrementEnemyCount()
        {
            _activeEnemiesCount = Mathf.Max(0, _activeEnemiesCount - 1);
            CheckWaveClearStatus();
        }

        private void CheckWaveClearStatus()
        {
            if (!_isSpawningWave && _activeEnemiesCount <= 0 && _currentState == GameState.Playing)
            {
                Debug.Log($"[GameManager] Wave {_currentWaveIndex} fully cleared.");
                EventBus<WaveClearedEvent>.Raise(new WaveClearedEvent(_currentWaveIndex));

                WaveManager waveManager = FindFirstObjectByType<WaveManager>();
                if (waveManager != null)
                {
                    if (_isEndlessMode)
                    {
                        waveManager.StartNextWave();
                    }
                    else if (_currentWaveIndex >= waveManager.Waves.Count - 1)
                    {
                        SetState(GameState.Victory);
                    }
                }
            }
        }
        #endregion
    }
}
