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
        // Lưu ý: Việc dùng biến static để truyền dữ liệu giữa các scene có thể gây khó quản lý sau này.
        // Cân nhắc chuyển sang dùng ScriptableObject (ví dụ: GameSessionData) cho các dự án lớn.
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
        [SerializeField] private LevelData defaultLevelData;

        [Header("Dependencies")]
        [Tooltip("Kéo thả WaveManager vào đây để tối ưu hiệu năng. Nếu để trống, game sẽ tự tìm lúc bắt đầu.")]
        [SerializeField] private WaveManager _waveManager;

        public LevelData DefaultLevelData { get => defaultLevelData; set => defaultLevelData = value; }

        private GameState _currentState = GameState.MainMenu;
        private LevelData _activeLevelData;
        private int _currentHealth;
        private int _currentGold;
        private int _currentWaveIndex = -1;
        private int _activeEnemiesCount = 0;
        private bool _isSpawningWave = false;
        private bool _isEndlessMode = false;

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

            // Đăng ký sự kiện ở Awake để đảm bảo luôn lắng nghe trong suốt vòng đời của object
            SubscribeEvents();
        }

        private void Start()
        {
            // Cache WaveManager một lần duy nhất lúc khởi chạy nếu chưa được gán
            if (_waveManager == null)
            {
                _waveManager = FindFirstObjectByType<WaveManager>();
            }

            if (defaultLevelData != null)
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

        private void OnDestroy()
        {
            // Hủy đăng ký sự kiện để tránh Memory Leak
            UnsubscribeEvents();

            // Xóa tham chiếu Singleton khi GameManager bị destroy (vd: load lại scene)
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void SubscribeEvents()
        {
            EventBus<EnemySpawnedEvent>.Subscribe(OnEnemySpawned);
            EventBus<EnemyDiedEvent>.Subscribe(OnEnemyDied);
            EventBus<EnemyReachedBaseEvent>.Subscribe(OnEnemyReachedBase);
            EventBus<WaveStartedEvent>.Subscribe(OnWaveStarted);
            EventBus<WaveCompletedEvent>.Subscribe(OnWaveCompleted);
        }

        private void UnsubscribeEvents()
        {
            EventBus<EnemySpawnedEvent>.Unsubscribe(OnEnemySpawned);
            EventBus<EnemyDiedEvent>.Unsubscribe(OnEnemyDied);
            EventBus<EnemyReachedBaseEvent>.Unsubscribe(OnEnemyReachedBase);
            EventBus<WaveStartedEvent>.Unsubscribe(OnWaveStarted);
            EventBus<WaveCompletedEvent>.Unsubscribe(OnWaveCompleted);
        }

        public void SetState(GameState newState)
        {
            if (_currentState == newState) return;

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

        public void StartLevel(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogError("[GameManager] Cannot start level with null LevelData!");
                return;
            }

            ResetGameVariables(levelData, false);
            EventBus<LevelStartedEvent>.Raise(new LevelStartedEvent(levelData.LevelName));
        }

        public void StartEndlessMode(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogError("[GameManager] Cannot start endless mode with null LevelData!");
                return;
            }

            ResetGameVariables(levelData, true);
            EventBus<LevelStartedEvent>.Raise(new LevelStartedEvent(levelData.LevelName + " (Endless)"));
        }

        /// <summary>
        /// Hàm hỗ trợ gộp chung logic reset trạng thái game, tránh lặp code.
        /// </summary>
        private void ResetGameVariables(LevelData levelData, bool isEndless)
        {
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
            _isEndlessMode = isEndless;

            SetState(GameState.Playing);

            EventBus<BaseHealthChangedEvent>.Raise(new BaseHealthChangedEvent(_currentHealth, levelData.BaseMaxHealth));
            EventBus<GoldChangedEvent>.Raise(new GoldChangedEvent(_currentGold));
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
                if (_isEndlessMode)
                    StartEndlessMode(_activeLevelData);
                else
                    StartLevel(_activeLevelData);
            }
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            _currentGold += amount;
            EventBus<GoldChangedEvent>.Raise(new GoldChangedEvent(_currentGold));
        }

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
            int maxHealth = _activeLevelData != null ? _activeLevelData.BaseMaxHealth : 20;

            EventBus<BaseHealthChangedEvent>.Raise(new BaseHealthChangedEvent(_currentHealth, maxHealth));

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

                // Sử dụng tham chiếu đã được cache thay vì FindFirstObjectByType
                if (_waveManager != null)
                {
                    if (_isEndlessMode)
                    {
                        _waveManager.StartNextWave();
                    }
                    else if (_currentWaveIndex >= _waveManager.Waves.Count - 1)
                    {
                        SetState(GameState.Victory);
                    }
                }
                else
                {
                    Debug.LogWarning("[GameManager] Thưa thiếu tham chiếu WaveManager! Không thể chuyển wave.");
                }
            }
        }
        #endregion
    }
}