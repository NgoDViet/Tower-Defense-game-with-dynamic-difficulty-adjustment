using UnityEngine;
using TowerDefense.Data;
using TowerDefense.Pooling;

namespace TowerDefense.Core
{
    /// <summary>
    /// Core game manager:
    /// Game state, HP, gold, wave state, timer,
    /// damage statistics, one-life rule and time-limit rule.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
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
        [SerializeField] private LevelData defaultLevelData;

        public LevelData DefaultLevelData
        {
            get => defaultLevelData;
            set => defaultLevelData = value;
        }

        private GameState _currentState = GameState.MainMenu;
        private LevelData _activeLevelData;

        private int _currentHealth;
        private int _currentGold;

        // Wave index starts from 0 internally.
        // Wave 1 = index 0
        // Wave 2 = index 1
        private int _currentWaveIndex = -1;

        private int _activeEnemiesCount;
        private bool _isSpawningWave;

        private float _playTime;
        private int _totalDamageDealt;

        public static int SelectedDifficulty = 1;

        public GameState CurrentState => _currentState;
        public LevelData ActiveLevelData => _activeLevelData;
        public int CurrentHealth => _currentHealth;
        public int CurrentGold => _currentGold;
        public int CurrentWaveIndex => _currentWaveIndex;
        public int ActiveEnemiesCount => _activeEnemiesCount;
        public float PlayTime => _playTime;
        public int TotalDamageDealt => _totalDamageDealt;

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
            EventBus<EnemySpawnedEvent>.Subscribe(OnEnemySpawned);
            EventBus<EnemyDiedEvent>.Subscribe(OnEnemyDied);
            EventBus<EnemyReachedBaseEvent>.Subscribe(OnEnemyReachedBase);

            EventBus<WaveStartedEvent>.Subscribe(OnWaveStarted);
            EventBus<WaveCompletedEvent>.Subscribe(OnWaveCompleted);
        }

        private void OnDisable()
        {
            EventBus<EnemySpawnedEvent>.Unsubscribe(OnEnemySpawned);
            EventBus<EnemyDiedEvent>.Unsubscribe(OnEnemyDied);
            EventBus<EnemyReachedBaseEvent>.Unsubscribe(OnEnemyReachedBase);

            EventBus<WaveStartedEvent>.Unsubscribe(OnWaveStarted);
            EventBus<WaveCompletedEvent>.Unsubscribe(OnWaveCompleted);
        }

        private void Start()
        {
            if (defaultLevelData != null &&
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
            {
                StartLevel(defaultLevelData);
            }
            else
            {
                SetState(GameState.MainMenu);
            }
        }

        private void Update()
        {
            if (_currentState != GameState.Playing)
                return;

            _playTime += Time.deltaTime;

            if (DifficultyManager.TimeLimitMinutes > 0f &&
                _playTime >= DifficultyManager.TimeLimitMinutes * 60f)
            {
                Debug.Log("[GameManager] Time limit reached. Defeat.");

                SetState(GameState.Defeat);
            }
        }

        public void SetDifficulty(int difficulty)
        {
            SelectedDifficulty = difficulty;
        }

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

            EventBus<GameStateChangedEvent>.Raise(
                new GameStateChangedEvent(oldState, _currentState));
        }

        public void StartLevel(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogError(
                    "[GameManager] Cannot start level with null LevelData!");

                return;
            }

            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnAllActiveToPool();
            }

            _activeLevelData = levelData;

            _currentHealth = levelData.BaseMaxHealth;
            _currentGold = levelData.StartingGold;

            // IMPORTANT:
            // -1 means no wave has started yet.
            _currentWaveIndex = -1;

            _activeEnemiesCount = 0;
            _isSpawningWave = false;

            _playTime = 0f;
            _totalDamageDealt = 0;

            Time.timeScale = 1f;

            SetState(GameState.Playing);

            EventBus<LevelStartedEvent>.Raise(
                new LevelStartedEvent(levelData.LevelName));

            EventBus<BaseHealthChangedEvent>.Raise(
                new BaseHealthChangedEvent(
                    _currentHealth,
                    levelData.BaseMaxHealth));

            EventBus<GoldChangedEvent>.Raise(
                new GoldChangedEvent(_currentGold));
        }

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

        public void RestartLevel()
        {
            if (_activeLevelData != null)
            {
                StartLevel(_activeLevelData);
            }
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            _currentGold += amount;

            EventBus<GoldChangedEvent>.Raise(
                new GoldChangedEvent(_currentGold));
        }

        public bool TrySpendGold(int amount)
        {
            if (amount < 0 || _currentGold < amount)
                return false;

            _currentGold -= amount;

            EventBus<GoldChangedEvent>.Raise(
                new GoldChangedEvent(_currentGold));

            return true;
        }

        public void RegisterDamage(int damage)
        {
            if (damage > 0)
            {
                _totalDamageDealt += damage;
            }
        }

        // =========================================================
        // ENEMY SPAWNED
        // =========================================================

        private void OnEnemySpawned(EnemySpawnedEvent evt)
        {
            _activeEnemiesCount++;

            Debug.Log(
                $"[GameManager] Enemy spawned. Active enemies = {_activeEnemiesCount}");
        }

        // =========================================================
        // ENEMY DIED
        // =========================================================

        private void OnEnemyDied(EnemyDiedEvent evt)
        {
            AddGold(evt.GoldReward);

            DecrementEnemyCount();

            Debug.Log(
                $"[GameManager] Enemy died. Active enemies = {_activeEnemiesCount}");
        }

        // =========================================================
        // ENEMY REACHED BASE
        // =========================================================

        private void OnEnemyReachedBase(EnemyReachedBaseEvent evt)
        {
            if (DifficultyManager.OneLifeMode)
            {
                Debug.Log(
                    "[GameManager] One Life mode: enemy reached base. Defeat.");

                _activeEnemiesCount =
                    Mathf.Max(0, _activeEnemiesCount - 1);

                SetState(GameState.Defeat);

                return;
            }

            _currentHealth =
                Mathf.Max(
                    0,
                    _currentHealth - evt.DamageToBase);

            EventBus<BaseHealthChangedEvent>.Raise(
                new BaseHealthChangedEvent(
                    _currentHealth,
                    _activeLevelData != null
                        ? _activeLevelData.BaseMaxHealth
                        : 20));

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

        private void OnWaveStarted(WaveStartedEvent evt)
        {
            _currentWaveIndex = evt.WaveIndex;

            _isSpawningWave = true;

            Debug.Log(
                $"[GameManager] ===== WAVE STARTED: {_currentWaveIndex + 1} =====");
        }

        // =========================================================
        // WAVE COMPLETED SPAWNING
        // =========================================================

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            _isSpawningWave = false;

            Debug.Log(
                $"[GameManager] Wave {_currentWaveIndex + 1} finished spawning.");

            CheckWaveClearStatus();
        }

        // =========================================================
        // ENEMY COUNT
        // =========================================================

        private void DecrementEnemyCount()
        {
            _activeEnemiesCount =
                Mathf.Max(0, _activeEnemiesCount - 1);

            CheckWaveClearStatus();
        }

        // =========================================================
        // CHECK WAVE CLEAR
        // =========================================================

        private void CheckWaveClearStatus()
        {
            if (_currentState != GameState.Playing)
                return;

            // Still spawning enemies.
            if (_isSpawningWave)
                return;

            // Still have enemies alive.
            if (_activeEnemiesCount > 0)
                return;

            // No wave has started yet.
            if (_currentWaveIndex < 0)
                return;

            Debug.Log(
                $"[GameManager] ===== WAVE {_currentWaveIndex + 1} CLEARED =====");

            // Tell WaveManager that the wave is completely cleared.
            EventBus<WaveClearedEvent>.Raise(
                new WaveClearedEvent(_currentWaveIndex));

            // Check if this was the FINAL wave.
            WaveManager waveManager =
                FindFirstObjectByType<WaveManager>();

            if (waveManager == null)
            {
                Debug.LogWarning(
                    "[GameManager] WaveManager not found.");

                return;
            }

            int totalWaves = waveManager.Waves.Count;

            if (totalWaves <= 0)
                return;

            if (_currentWaveIndex >= totalWaves - 1)
            {
                Debug.Log(
                    "[GameManager] ===== ALL WAVES CLEARED =====");

                SetState(GameState.Victory);
            }
        }
    }
}