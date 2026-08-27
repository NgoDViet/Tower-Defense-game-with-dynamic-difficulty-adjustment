using UnityEngine;
using TowerDefense.Data;
using TowerDefense.Pooling;

namespace TowerDefense.Core
{
    /// <summary>
    /// Core game manager.
    ///
    /// Handles:
    /// - Game state
    /// - Base HP
    /// - Gold
    /// - Passive gold over time
    /// - Enemy kill rewards
    /// - Wave state
    /// - Endless mode
    /// - Difficulty selection
    /// - Time limit
    /// - One-life mode
    /// - Total damage statistics
    /// - Enemy counting
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // =========================================================
        // GAME STATE
        // =========================================================

        public enum GameState
        {
            MainMenu,
            Playing,
            Pause,
            Victory,
            Defeat
        }


        public static GameManager Instance
        {
            get;
            private set;
        }


        // =========================================================
        // GAME MODE DATA
        // =========================================================

        public static bool startAsEndless = false;

        /// <summary>
        /// 1 = Normal
        /// 2 = Hard
        /// 3 = Hell
        /// </summary>
        public static int SelectedDifficulty = 1;


        // =========================================================
        // LEVEL SETTINGS
        // =========================================================

        [Header("Level Settings")]

        [SerializeField]
        private LevelData defaultLevelData;


        // =========================================================
        // DEPENDENCIES
        // =========================================================

        [Header("Dependencies")]

        [Tooltip(
            "Assign WaveManager here for better performance. " +
            "If empty, GameManager will find it automatically."
        )]

        [SerializeField]
        private WaveManager _waveManager;


        // =========================================================
// PASSIVE GOLD
// =========================================================

private const int PASSIVE_GOLD_AMOUNT = 10;

private const float PASSIVE_GOLD_INTERVAL = 10f;

private float _passiveGoldTimer;


        // =========================================================
        // ENEMY KILL GOLD BALANCE
        // =========================================================

        [Header("Enemy Kill Gold")]

        [Tooltip(
            "Multiplier applied to enemy gold rewards. " +
            "0.5 = 50% of original reward."
        )]

        [Range(0f, 1f)]
        [SerializeField]
        private float enemyGoldRewardMultiplier = 0.5f;


        // =========================================================
        // PROPERTIES
        // =========================================================

        public LevelData DefaultLevelData
        {
            get
            {
                return defaultLevelData;
            }

            set
            {
                defaultLevelData = value;
            }
        }


        private GameState _currentState =
            GameState.MainMenu;

        private LevelData _activeLevelData;

        private int _currentHealth;

        private int _currentGold;

        private int _currentWaveIndex = -1;

        private int _activeEnemiesCount;

        private bool _isSpawningWave;

        private bool _isEndlessMode;

        private float _playTime;

        private int _totalDamageDealt;


        public GameState CurrentState =>
            _currentState;

        public LevelData ActiveLevelData =>
            _activeLevelData;

        public int CurrentHealth =>
            _currentHealth;

        public int CurrentGold =>
            _currentGold;

        public int CurrentWaveIndex =>
            _currentWaveIndex;

        public int ActiveEnemiesCount =>
            _activeEnemiesCount;

        public float PlayTime =>
            _playTime;

        public int TotalDamageDealt =>
            _totalDamageDealt;

        public bool IsEndlessMode =>
            _isEndlessMode;


        // =========================================================
        // PASSIVE GOLD PROPERTIES
        // =========================================================

      public int PassiveGoldAmount
{
    get
    {
        return PASSIVE_GOLD_AMOUNT;
    }
}

public float PassiveGoldInterval
{
    get
    {
        return PASSIVE_GOLD_INTERVAL;
    }
}


        public float EnemyGoldRewardMultiplier =>
            enemyGoldRewardMultiplier;


        // =========================================================
        // AWAKE
        // =========================================================

        private void Awake()
        {
            if (
                Instance != null &&
                Instance != this
            )
            {
                Destroy(gameObject);
                return;
            }


            Instance = this;


            SubscribeEvents();
        }


        // =========================================================
        // START
        // =========================================================

        private void Start()
        {
            // -----------------------------------------------------
            // CACHE WAVEMANAGER
            // -----------------------------------------------------

            if (_waveManager == null)
            {
                _waveManager =
                    FindFirstObjectByType<WaveManager>();
            }


            // -----------------------------------------------------
            // CURRENT SCENE
            // -----------------------------------------------------

            string currentScene =
                UnityEngine.SceneManagement.SceneManager
                    .GetActiveScene()
                    .name;


            // -----------------------------------------------------
            // START GAME
            // -----------------------------------------------------

            if (
                defaultLevelData != null &&
                currentScene != "MainMenu"
            )
            {
                if (startAsEndless)
                {
                    startAsEndless = false;

                    StartEndlessMode(
                        defaultLevelData
                    );
                }
                else
                {
                    StartLevel(
                        defaultLevelData
                    );
                }
            }
            else
            {
                SetState(
                    GameState.MainMenu
                );
            }
        }


        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (
                _currentState !=
                GameState.Playing
            )
            {
                return;
            }


            // =====================================================
            // PLAY TIME
            // =====================================================

            _playTime +=
                Time.deltaTime;


            // =====================================================
            // PASSIVE GOLD TIMER
            // =====================================================

            UpdatePassiveGold();


            // =====================================================
            // TIME LIMIT
            // =====================================================

            if (
                DifficultyManager.TimeLimitMinutes > 0f &&
                _playTime >=
                DifficultyManager.TimeLimitMinutes * 60f
            )
            {
                Debug.Log(
                    "[GameManager] " +
                    "Time limit reached. Defeat."
                );


                SetState(
                    GameState.Defeat
                );
            }
        }


        // =========================================================
// PASSIVE GOLD
// =========================================================

private void UpdatePassiveGold()
{
    // =====================================================
    // PASSIVE GOLD DISABLED
    // =====================================================

    if (!DifficultyManager.PassiveGoldEnabled)
    {
        return;
    }


    _passiveGoldTimer -=
        Time.deltaTime;


    if (_passiveGoldTimer > 0f)
    {
        return;
    }


    int amount =
        PassiveGoldAmount;


    if (amount > 0)
    {
        AddGold(
            amount
        );


        Debug.Log(
            "[GameManager] " +
            $"Passive gold +{amount} G | " +
            $"Total Gold={_currentGold}"
        );
    }


    _passiveGoldTimer =
        PassiveGoldInterval;
}


        // =========================================================
        // DESTROY
        // =========================================================

        private void OnDestroy()
        {
            UnsubscribeEvents();


            if (Instance == this)
            {
                Instance = null;
            }
        }


        // =========================================================
        // EVENT SUBSCRIPTION
        // =========================================================

        private void SubscribeEvents()
        {
            EventBus<EnemySpawnedEvent>
                .Subscribe(
                    OnEnemySpawned
                );


            EventBus<EnemyDiedEvent>
                .Subscribe(
                    OnEnemyDied
                );


            EventBus<EnemyReachedBaseEvent>
                .Subscribe(
                    OnEnemyReachedBase
                );


            EventBus<WaveStartedEvent>
                .Subscribe(
                    OnWaveStarted
                );


            EventBus<WaveCompletedEvent>
                .Subscribe(
                    OnWaveCompleted
                );
        }


        private void UnsubscribeEvents()
        {
            EventBus<EnemySpawnedEvent>
                .Unsubscribe(
                    OnEnemySpawned
                );


            EventBus<EnemyDiedEvent>
                .Unsubscribe(
                    OnEnemyDied
                );


            EventBus<EnemyReachedBaseEvent>
                .Unsubscribe(
                    OnEnemyReachedBase
                );


            EventBus<WaveStartedEvent>
                .Unsubscribe(
                    OnWaveStarted
                );


            EventBus<WaveCompletedEvent>
                .Unsubscribe(
                    OnWaveCompleted
                );
        }


        // =========================================================
        // DIFFICULTY
        // =========================================================

        public void SetDifficulty(
            int difficulty
        )
        {
            SelectedDifficulty =
                Mathf.Clamp(
                    difficulty,
                    1,
                    3
                );


            Debug.Log(
                "[GameManager] Difficulty selected: " +
                SelectedDifficulty
            );
        }


        // =========================================================
        // GAME STATE
        // =========================================================

        public void SetState(
            GameState newState
        )
        {
            if (
                _currentState ==
                newState
            )
            {
                return;
            }


            GameState oldState =
                _currentState;


            _currentState =
                newState;


            switch (_currentState)
            {
                case GameState.MainMenu:

                case GameState.Playing:

                    Time.timeScale =
                        1f;

                    break;


                case GameState.Pause:

                    Time.timeScale =
                        0f;

                    break;


                case GameState.Victory:

                    Time.timeScale =
                        0f;


                    EventBus<LevelCompletedEvent>.Raise(
                        new LevelCompletedEvent(
                            true
                        )
                    );

                    break;


                case GameState.Defeat:

                    Time.timeScale =
                        0f;


                    EventBus<LevelCompletedEvent>.Raise(
                        new LevelCompletedEvent(
                            false
                        )
                    );

                    break;
            }


            Debug.Log(
                "[GameManager] State changed: " +
                $"{oldState} -> {_currentState}"
            );


            EventBus<GameStateChangedEvent>.Raise(
                new GameStateChangedEvent(
                    oldState,
                    _currentState
                )
            );
        }


        // =========================================================
        // START NORMAL LEVEL
        // =========================================================

        public void StartLevel(
            LevelData levelData
        )
        {
            if (levelData == null)
            {
                Debug.LogError(
                    "[GameManager] " +
                    "Cannot start level with null LevelData!"
                );

                return;
            }


            ResetGameVariables(
                levelData,
                false
            );


            EventBus<LevelStartedEvent>.Raise(
                new LevelStartedEvent(
                    levelData.LevelName
                )
            );
        }


        // =========================================================
        // START ENDLESS
        // =========================================================

        public void StartEndlessMode(
            LevelData levelData
        )
        {
            if (levelData == null)
            {
                Debug.LogError(
                    "[GameManager] " +
                    "Cannot start endless mode " +
                    "with null LevelData!"
                );

                return;
            }


            ResetGameVariables(
                levelData,
                true
            );


            EventBus<LevelStartedEvent>.Raise(
                new LevelStartedEvent(
                    levelData.LevelName +
                    " (Endless)"
                )
            );
        }


        // =========================================================
        // RESET VARIABLES
        // =========================================================

        private void ResetGameVariables(
            LevelData levelData,
            bool isEndless
        )
        {
            if (
                ObjectPooler.Instance != null
            )
            {
                ObjectPooler.Instance
                    .ReturnAllActiveToPool();
            }


            _activeLevelData =
                levelData;


            _currentHealth =
                levelData.BaseMaxHealth;


            _currentGold =
                levelData.StartingGold;


            _currentWaveIndex =
                -1;


            _activeEnemiesCount =
                0;


            _isSpawningWave =
                false;


            _isEndlessMode =
                isEndless;


            _playTime =
                0f;


            _totalDamageDealt =
                0;


            // -----------------------------------------------------
            // RESET PASSIVE GOLD TIMER
            // -----------------------------------------------------

            _passiveGoldTimer =
                PassiveGoldInterval;


            Time.timeScale =
                1f;


            SetState(
                GameState.Playing
            );


            EventBus<BaseHealthChangedEvent>.Raise(
                new BaseHealthChangedEvent(
                    _currentHealth,
                    levelData.BaseMaxHealth
                )
            );


            EventBus<GoldChangedEvent>.Raise(
                new GoldChangedEvent(
                    _currentGold
                )
            );


            Debug.Log(
                "[GameManager] Game reset | " +
                $"Starting Gold={_currentGold} | " +
                $"Passive Gold=+{PassiveGoldAmount} every " +
                $"{PassiveGoldInterval}s | " +
                $"Kill Reward Multiplier=" +
                $"{EnemyGoldRewardMultiplier * 100f:0}%"
            );
        }


        // =========================================================
        // PAUSE
        // =========================================================

        public void TogglePause()
        {
            if (
                _currentState ==
                GameState.Playing
            )
            {
                SetState(
                    GameState.Pause
                );
            }
            else if (
                _currentState ==
                GameState.Pause
            )
            {
                SetState(
                    GameState.Playing
                );
            }
        }


        // =========================================================
        // RESTART
        // =========================================================

        public void RestartLevel()
        {
            if (_activeLevelData == null)
                return;


            if (_isEndlessMode)
            {
                StartEndlessMode(
                    _activeLevelData
                );
            }
            else
            {
                StartLevel(
                    _activeLevelData
                );
            }
        }


        // =========================================================
        // GOLD
        // =========================================================

        public void AddGold(
            int amount
        )
        {
            if (amount <= 0)
                return;


            _currentGold +=
                amount;


            EventBus<GoldChangedEvent>.Raise(
                new GoldChangedEvent(
                    _currentGold
                )
            );
        }


        public bool TrySpendGold(
            int amount
        )
        {
            if (amount < 0)
                return false;


            if (_currentGold < amount)
                return false;


            _currentGold -=
                amount;


            EventBus<GoldChangedEvent>.Raise(
                new GoldChangedEvent(
                    _currentGold
                )
            );


            return true;
        }


        // =========================================================
        // DAMAGE STATISTICS
        // =========================================================

        public void RegisterDamage(
            int damage
        )
        {
            if (damage <= 0)
                return;


            _totalDamageDealt +=
                damage;
        }


        // =========================================================
        // ENEMY SPAWNED
        // =========================================================

        private void OnEnemySpawned(
            EnemySpawnedEvent evt
        )
        {
            _activeEnemiesCount++;


            Debug.Log(
                "[GameManager] Enemy spawned. " +
                $"Active enemies = {_activeEnemiesCount}"
            );
        }


        // =========================================================
        // ENEMY DIED
        // =========================================================

        private void OnEnemyDied(
            EnemyDiedEvent evt
        )
        {
            // -----------------------------------------------------
            // ORIGINAL REWARD
            // -----------------------------------------------------

            int originalReward =
                Mathf.Max(
                    0,
                    evt.GoldReward
                );


            // -----------------------------------------------------
            // BALANCED REWARD
            // -----------------------------------------------------

            int actualReward =
                Mathf.RoundToInt(
                    originalReward *
                    enemyGoldRewardMultiplier
                );


            // -----------------------------------------------------
            // GIVE GOLD
            // -----------------------------------------------------

            if (actualReward > 0)
            {
                AddGold(
                    actualReward
                );
            }


            // -----------------------------------------------------
            // SHOW REWARD
            // -----------------------------------------------------

            Debug.Log(
                "[GameManager] " +
                "Enemy killed | " +
                $"Original Reward={originalReward} G | " +
                $"Applied Reward={actualReward} G | " +
                $"Total Gold={_currentGold}"
            );


            // -----------------------------------------------------
            // ENEMY COUNT
            // -----------------------------------------------------

            DecrementEnemyCount();
        }


        // =========================================================
        // ENEMY REACHED BASE
        // =========================================================

        private void OnEnemyReachedBase(
            EnemyReachedBaseEvent evt
        )
        {
            // -----------------------------------------------------
            // ONE LIFE
            // -----------------------------------------------------

            if (DifficultyManager.OneLifeMode)
            {
                Debug.Log(
                    "[GameManager] " +
                    "One Life mode: enemy reached base. " +
                    "Defeat."
                );


                _activeEnemiesCount =
                    Mathf.Max(
                        0,
                        _activeEnemiesCount - 1
                    );


                SetState(
                    GameState.Defeat
                );


                return;
            }


            // -----------------------------------------------------
            // DAMAGE BASE
            // -----------------------------------------------------

            _currentHealth =
                Mathf.Max(
                    0,
                    _currentHealth -
                    evt.DamageToBase
                );


            int maxHealth =
                _activeLevelData != null
                    ? _activeLevelData.BaseMaxHealth
                    : 20;


            EventBus<BaseHealthChangedEvent>.Raise(
                new BaseHealthChangedEvent(
                    _currentHealth,
                    maxHealth
                )
            );


            if (_currentHealth <= 0)
            {
                SetState(
                    GameState.Defeat
                );
            }
            else
            {
                DecrementEnemyCount();
            }
        }


        // =========================================================
        // WAVE STARTED
        // =========================================================

        private void OnWaveStarted(
            WaveStartedEvent evt
        )
        {
            _currentWaveIndex =
                evt.WaveIndex;


            _isSpawningWave =
                true;


            Debug.Log(
                "[GameManager] ===== WAVE " +
                $"{_currentWaveIndex + 1} STARTED ====="
            );
        }


        // =========================================================
        // WAVE COMPLETED
        // =========================================================

        private void OnWaveCompleted(
            WaveCompletedEvent evt
        )
        {
            _isSpawningWave =
                false;


            Debug.Log(
                "[GameManager] Wave " +
                $"{_currentWaveIndex + 1} " +
                "finished spawning."
            );


            CheckWaveClearStatus();
        }


        // =========================================================
        // ENEMY COUNT
        // =========================================================

        private void DecrementEnemyCount()
        {
            _activeEnemiesCount =
                Mathf.Max(
                    0,
                    _activeEnemiesCount - 1
                );


            CheckWaveClearStatus();
        }


        // =========================================================
        // CHECK WAVE CLEAR
        // =========================================================

        private void CheckWaveClearStatus()
        {
            if (
                _currentState !=
                GameState.Playing
            )
            {
                return;
            }


            if (_isSpawningWave)
                return;


            if (_activeEnemiesCount > 0)
                return;


            if (_currentWaveIndex < 0)
                return;


            Debug.Log(
                "[GameManager] ===== WAVE " +
                $"{_currentWaveIndex + 1} CLEARED ====="
            );


            EventBus<WaveClearedEvent>.Raise(
                new WaveClearedEvent(
                    _currentWaveIndex
                )
            );


            if (
                !_isEndlessMode &&
                _waveManager != null
            )
            {
                if (
                    _currentWaveIndex >=
                    _waveManager.Waves.Count - 1
                )
                {
                    Debug.Log(
                        "[GameManager] ===== " +
                        "ALL WAVES CLEARED ====="
                    );


                    SetState(
                        GameState.Victory
                    );
                }
            }
        }
    }
}