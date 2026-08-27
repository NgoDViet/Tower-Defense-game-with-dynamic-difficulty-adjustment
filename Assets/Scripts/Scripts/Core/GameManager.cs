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

        public static GameManager Instance { get; private set; }

        // =========================================================
        // SCENE / GAME MODE DATA
        // =========================================================

        /// <summary>
        /// Used by MainMenu to tell the game scene to start Endless Mode.
        /// </summary>
        public static bool startAsEndless = false;

        /// <summary>
        /// Selected difficulty:
        /// 1 = Normal
        /// 2 = Hard
        /// 3 = Hell
        /// </summary>
        public static int SelectedDifficulty = 1;

        // =========================================================
        // LEVEL SETTINGS
        // =========================================================

        [Header("Level Settings")]
        [SerializeField] private LevelData defaultLevelData;

        // =========================================================
        // DEPENDENCIES
        // =========================================================

        [Header("Dependencies")]
        [Tooltip(
            "Assign WaveManager here for better performance. " +
            "If empty, GameManager will find it automatically."
        )]
        [SerializeField] private WaveManager _waveManager;

        // =========================================================
        // PROPERTIES
        // =========================================================

        public LevelData DefaultLevelData
        {
            get => defaultLevelData;
            set => defaultLevelData = value;
        }

        private GameState _currentState = GameState.MainMenu;
        private LevelData _activeLevelData;

        private int _currentHealth;
        private int _currentGold;

        // Wave 1 = index 0
        // Wave 2 = index 1
        private int _currentWaveIndex = -1;

        private int _activeEnemiesCount;
        private bool _isSpawningWave;

        private bool _isEndlessMode;

        private float _playTime;
        private int _totalDamageDealt;

        public GameState CurrentState => _currentState;

        public LevelData ActiveLevelData => _activeLevelData;

        public int CurrentHealth => _currentHealth;

        public int CurrentGold => _currentGold;

        public int CurrentWaveIndex => _currentWaveIndex;

        public int ActiveEnemiesCount => _activeEnemiesCount;

        public float PlayTime => _playTime;

        public int TotalDamageDealt => _totalDamageDealt;

        public bool IsEndlessMode => _isEndlessMode;

        // =========================================================
        // AWAKE
        // =========================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
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
            // Cache WaveManager once.
            if (_waveManager == null)
            {
                _waveManager = FindFirstObjectByType<WaveManager>();
            }

            string currentScene =
                UnityEngine.SceneManagement.SceneManager
                .GetActiveScene()
                .name;

            // Do not automatically start the game while in MainMenu.
            if (defaultLevelData != null &&
                currentScene != "MainMenu")
            {
                if (startAsEndless)
                {
                    startAsEndless = false;

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

        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (_currentState != GameState.Playing)
                return;

            _playTime += Time.deltaTime;

            // Time limit.
            if (DifficultyManager.TimeLimitMinutes > 0f &&
                _playTime >=
                DifficultyManager.TimeLimitMinutes * 60f)
            {
                Debug.Log(
                    "[GameManager] Time limit reached. Defeat.");

                SetState(GameState.Defeat);
            }
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
                .Subscribe(OnEnemySpawned);

            EventBus<EnemyDiedEvent>
                .Subscribe(OnEnemyDied);

            EventBus<EnemyReachedBaseEvent>
                .Subscribe(OnEnemyReachedBase);

            EventBus<WaveStartedEvent>
                .Subscribe(OnWaveStarted);

            EventBus<WaveCompletedEvent>
                .Subscribe(OnWaveCompleted);
        }

        private void UnsubscribeEvents()
        {
            EventBus<EnemySpawnedEvent>
                .Unsubscribe(OnEnemySpawned);

            EventBus<EnemyDiedEvent>
                .Unsubscribe(OnEnemyDied);

            EventBus<EnemyReachedBaseEvent>
                .Unsubscribe(OnEnemyReachedBase);

            EventBus<WaveStartedEvent>
                .Unsubscribe(OnWaveStarted);

            EventBus<WaveCompletedEvent>
                .Unsubscribe(OnWaveCompleted);
        }

        // =========================================================
        // DIFFICULTY
        // =========================================================

        public void SetDifficulty(int difficulty)
        {
            SelectedDifficulty = Mathf.Clamp(difficulty, 1, 3);

            Debug.Log(
                $"[GameManager] Difficulty selected: " +
                $"{SelectedDifficulty}");
        }

        // =========================================================
        // GAME STATE
        // =========================================================

        public void SetState(GameState newState)
        {
            if (_currentState == newState)
                return;

            GameState oldState = _currentState;

            _currentState = newState;

            switch (_currentState)
            {
                case GameState.MainMenu:
                case GameState.Playing:

                    Time.timeScale = 1f;

                    break;

                case GameState.Pause:

                    Time.timeScale = 0f;

                    break;

                case GameState.Victory:

                    Time.timeScale = 0f;

                    EventBus<LevelCompletedEvent>.Raise(
                        new LevelCompletedEvent(true));

                    break;

                case GameState.Defeat:

                    Time.timeScale = 0f;

                    EventBus<LevelCompletedEvent>.Raise(
                        new LevelCompletedEvent(false));

                    break;
            }

            Debug.Log(
                $"[GameManager] State changed: " +
                $"{oldState} -> {_currentState}");

            EventBus<GameStateChangedEvent>.Raise(
                new GameStateChangedEvent(
                    oldState,
                    _currentState));
        }

        // =========================================================
        // START NORMAL LEVEL
        // =========================================================

        public void StartLevel(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogError(
                    "[GameManager] Cannot start level with null LevelData!");

                return;
            }

            ResetGameVariables(
                levelData,
                false);

            EventBus<LevelStartedEvent>.Raise(
                new LevelStartedEvent(
                    levelData.LevelName));
        }

        // =========================================================
        // START ENDLESS
        // =========================================================

        public void StartEndlessMode(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogError(
                    "[GameManager] Cannot start endless mode " +
                    "with null LevelData!");

                return;
            }

            ResetGameVariables(
                levelData,
                true);

            EventBus<LevelStartedEvent>.Raise(
                new LevelStartedEvent(
                    levelData.LevelName + " (Endless)"));
        }

        // =========================================================
        // RESET
        // =========================================================

        private void ResetGameVariables(
            LevelData levelData,
            bool isEndless)
        {
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance
                    .ReturnAllActiveToPool();
            }

            _activeLevelData = levelData;

            _currentHealth =
                levelData.BaseMaxHealth;

            _currentGold =
                levelData.StartingGold;

            _currentWaveIndex = -1;

            _activeEnemiesCount = 0;

            _isSpawningWave = false;

            _isEndlessMode = isEndless;

            _playTime = 0f;

            _totalDamageDealt = 0;

            Time.timeScale = 1f;

            SetState(GameState.Playing);

            EventBus<BaseHealthChangedEvent>.Raise(
                new BaseHealthChangedEvent(
                    _currentHealth,
                    levelData.BaseMaxHealth));

            EventBus<GoldChangedEvent>.Raise(
                new GoldChangedEvent(
                    _currentGold));
        }

        // =========================================================
        // PAUSE
        // =========================================================

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
                    _activeLevelData);
            }
            else
            {
                StartLevel(
                    _activeLevelData);
            }
        }

        // =========================================================
        // GOLD
        // =========================================================

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            _currentGold += amount;

            EventBus<GoldChangedEvent>.Raise(
                new GoldChangedEvent(
                    _currentGold));
        }

        public bool TrySpendGold(int amount)
        {
            if (amount < 0)
                return false;

            if (_currentGold < amount)
                return false;

            _currentGold -= amount;

            EventBus<GoldChangedEvent>.Raise(
                new GoldChangedEvent(
                    _currentGold));

            return true;
        }

        // =========================================================
        // DAMAGE STATISTICS
        // =========================================================

        public void RegisterDamage(int damage)
        {
            if (damage <= 0)
                return;

            _totalDamageDealt += damage;
        }

        // =========================================================
        // ENEMY SPAWNED
        // =========================================================

        private void OnEnemySpawned(
            EnemySpawnedEvent evt)
        {
            _activeEnemiesCount++;

            Debug.Log(
                $"[GameManager] Enemy spawned. " +
                $"Active enemies = {_activeEnemiesCount}");
        }

        // =========================================================
        // ENEMY DIED
        // =========================================================

        private void OnEnemyDied(
            EnemyDiedEvent evt)
        {
            AddGold(evt.GoldReward);

            DecrementEnemyCount();

            Debug.Log(
                $"[GameManager] Enemy died. " +
                $"Active enemies = {_activeEnemiesCount}");
        }

        // =========================================================
        // ENEMY REACHED BASE
        // =========================================================

        private void OnEnemyReachedBase(
            EnemyReachedBaseEvent evt)
        {
            // One-life mode.
            if (DifficultyManager.OneLifeMode)
            {
                Debug.Log(
                    "[GameManager] One Life mode: " +
                    "enemy reached base. Defeat.");

                _activeEnemiesCount =
                    Mathf.Max(
                        0,
                        _activeEnemiesCount - 1);

                SetState(GameState.Defeat);

                return;
            }

            _currentHealth =
                Mathf.Max(
                    0,
                    _currentHealth -
                    evt.DamageToBase);

            int maxHealth =
                _activeLevelData != null
                    ? _activeLevelData.BaseMaxHealth
                    : 20;

            EventBus<BaseHealthChangedEvent>.Raise(
                new BaseHealthChangedEvent(
                    _currentHealth,
                    maxHealth));

            if (_currentHealth <= 0)
            {
                SetState(GameState.Defeat);
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
            WaveStartedEvent evt)
        {
            _currentWaveIndex =
                evt.WaveIndex;

            _isSpawningWave = true;

            Debug.Log(
                $"[GameManager] ===== WAVE " +
                $"{_currentWaveIndex + 1} STARTED =====");
        }

        // =========================================================
        // WAVE COMPLETED SPAWNING
        // =========================================================

        private void OnWaveCompleted(
            WaveCompletedEvent evt)
        {
            _isSpawningWave = false;

            Debug.Log(
                $"[GameManager] Wave " +
                $"{_currentWaveIndex + 1} " +
                "finished spawning.");

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
                    _activeEnemiesCount - 1);

            CheckWaveClearStatus();
        }

        // =========================================================
        // CHECK WAVE CLEAR
        // =========================================================

        private void CheckWaveClearStatus()
        {
            if (_currentState != GameState.Playing)
                return;

            if (_isSpawningWave)
                return;

            if (_activeEnemiesCount > 0)
                return;

            if (_currentWaveIndex < 0)
                return;

            Debug.Log(
                $"[GameManager] ===== WAVE " +
                $"{_currentWaveIndex + 1} CLEARED =====");

            EventBus<WaveClearedEvent>.Raise(
                new WaveClearedEvent(
                    _currentWaveIndex));

            // IMPORTANT:
            // WaveManager is responsible for starting
            // the next wave.
            //
            // GameManager only handles victory
            // for normal mode.
            if (!_isEndlessMode &&
                _waveManager != null)
            {
                if (_currentWaveIndex >=
                    _waveManager.Waves.Count - 1)
                {
                    Debug.Log(
                        "[GameManager] ===== " +
                        "ALL WAVES CLEARED =====");

                    SetState(GameState.Victory);
                }
            }
        }
    }
}