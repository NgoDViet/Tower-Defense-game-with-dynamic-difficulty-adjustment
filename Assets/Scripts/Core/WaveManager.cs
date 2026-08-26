using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TowerDefense.Data;
using TowerDefense.Enemy;
using TowerDefense.Pooling;

namespace TowerDefense.Core
{
    [System.Serializable]
    public struct WaveSetup
    {
        [Header("Basic Enemy")]
        public int basicCount;
        public float basicSpawnInterval;

        [Header("Fast Enemy")]
        public int fastCount;
        public float fastSpawnInterval;

        [Header("Tank Enemy")]
        public int tankCount;
        public float tankSpawnInterval;

        [Header("Armor Enemy")]
        public int armorCount;
        public float armorSpawnInterval;
    }

    public class WaveManager : MonoBehaviour
    {
        // =========================================================
        // REFERENCES
        // =========================================================

        [Header("References")]
        [SerializeField] private WaypointPath waypointPath;

        // =========================================================
        // ENEMY PREFABS
        // =========================================================

        [Header("Enemy Prefabs")]
        [SerializeField] private GameObject basicEnemyPrefab;
        [SerializeField] private GameObject fastEnemyPrefab;
        [SerializeField] private GameObject tankEnemyPrefab;
        [SerializeField] private GameObject armorEnemyPrefab;

        // =========================================================
        // ENEMY DATA
        // =========================================================

        [Header("Enemy Specifications")]
        [SerializeField] private EnemyData basicEnemyData;
        [SerializeField] private EnemyData fastEnemyData;
        [SerializeField] private EnemyData tankEnemyData;
        [SerializeField] private EnemyData armorEnemyData;

        // =========================================================
        // WAVE SETTINGS
        // =========================================================

        [Header("Wave Controls")]
        [SerializeField] private bool autoStartNextWave = true;

        [SerializeField] private float waveInterval = 5f;

        [SerializeField] private float firstWaveDelay = 3f;

        // =========================================================
        // WAVE DATA
        // =========================================================

        [Header("Waves Setup")]
        [SerializeField]
        private List<WaveSetup> waves =
            new List<WaveSetup>();

        // =========================================================
        // INTERNAL DATA
        // =========================================================

        private LevelData _levelData;

        private int _currentWaveIndex = -1;

        private bool _isSpawning;

        private bool _waitingForNextWave;

        private int _activeSpawnGroupsCount;

        private Coroutine _waveSpawnCoroutine;

        private Coroutine _autoNextWaveCoroutine;

        private List<WaveSetup> _initialInspectorWaves =
            new List<WaveSetup>();

        // =========================================================
        // PROPERTIES
        // =========================================================

        public List<WaveSetup> Waves => waves;

        public bool IsEndlessMode { get; private set; }

        public GameObject BasicEnemyPrefab
        {
            get => basicEnemyPrefab;
            set => basicEnemyPrefab = value;
        }

        public GameObject FastEnemyPrefab
        {
            get => fastEnemyPrefab;
            set => fastEnemyPrefab = value;
        }

        public GameObject TankEnemyPrefab
        {
            get => tankEnemyPrefab;
            set => tankEnemyPrefab = value;
        }

        public GameObject ArmorEnemyPrefab
        {
            get => armorEnemyPrefab;
            set => armorEnemyPrefab = value;
        }

        public EnemyData BasicEnemyData
        {
            get => basicEnemyData;
            set => basicEnemyData = value;
        }

        public EnemyData FastEnemyData
        {
            get => fastEnemyData;
            set => fastEnemyData = value;
        }

        public EnemyData TankEnemyData
        {
            get => tankEnemyData;
            set => tankEnemyData = value;
        }

        public EnemyData ArmorEnemyData
        {
            get => armorEnemyData;
            set => armorEnemyData = value;
        }

        // =========================================================
        // DYNAMIC SPAWN GROUP
        // =========================================================

        private struct DynamicSpawnGroup
        {
            public GameObject Prefab;

            public EnemyData Data;

            public int Count;

            public float SpawnInterval;

            public DynamicSpawnGroup(
                GameObject prefab,
                EnemyData data,
                int count,
                float spawnInterval)
            {
                Prefab = prefab;
                Data = data;
                Count = count;
                SpawnInterval = spawnInterval;
            }
        }

        // =========================================================
        // AWAKE
        // =========================================================

        private void Awake()
        {
            _initialInspectorWaves =
                new List<WaveSetup>(waves);
        }

        // =========================================================
        // ENABLE
        // =========================================================

        private void OnEnable()
        {
            EventBus<LevelStartedEvent>
                .Subscribe(OnLevelStarted);

            EventBus<WaveClearedEvent>
                .Subscribe(OnWaveCleared);
        }

        // =========================================================
        // DISABLE
        // =========================================================

        private void OnDisable()
        {
            EventBus<LevelStartedEvent>
                .Unsubscribe(OnLevelStarted);

            EventBus<WaveClearedEvent>
                .Unsubscribe(OnWaveCleared);

            if (_waveSpawnCoroutine != null)
            {
                StopCoroutine(
                    _waveSpawnCoroutine);

                _waveSpawnCoroutine = null;
            }

            if (_autoNextWaveCoroutine != null)
            {
                StopCoroutine(
                    _autoNextWaveCoroutine);

                _autoNextWaveCoroutine = null;
            }
        }

        // =========================================================
        // START NEXT WAVE
        // =========================================================
        public void StartNextWave()
        {
            if (_isSpawning)
            {
                Debug.Log(
                    "[WaveManager] Cannot start next wave: " +
                    "still spawning.");

                return;
            }

            if (_waitingForNextWave)
            {
                Debug.Log(
                    "[WaveManager] Cannot start next wave: " +
                    "waiting.");

                return;
            }

            if (GameManager.Instance != null &&
                GameManager.Instance.ActiveEnemiesCount > 0)
            {
                Debug.Log(
                    $"[WaveManager] Cannot start next wave: " +
                    $"{GameManager.Instance.ActiveEnemiesCount} " +
                    "enemies remain.");

                return;
            }

            int nextWaveIndex = _currentWaveIndex + 1;

            // =====================================================
            // FIXED WAVES
            // =====================================================
            // Endless OFF = only use waves configured for the level.
            if (!IsEndlessMode &&
                nextWaveIndex >= waves.Count)
            {
                Debug.Log(
                    "[WaveManager] Fixed Waves completed. " +
                    "No more waves.");

                return;
            }

            // =====================================================
            // ENDLESS WAVES
            // =====================================================
            // Endless ON = generate a new wave before accessing
            // waves[nextWaveIndex] when the configured waves end.
            if (IsEndlessMode &&
                nextWaveIndex >= waves.Count)
            {
                GenerateNextEndlessWave();
            }

            // Safety check.
            if (nextWaveIndex >= waves.Count)
            {
                Debug.LogError(
                    "[WaveManager] Cannot start wave. " +
                    $"Index={nextWaveIndex}, Count={waves.Count}");

                return;
            }

            _currentWaveIndex = nextWaveIndex;

            Debug.Log(
                $"[WaveManager] ===== STARTING WAVE " +
                $"{_currentWaveIndex + 1} ===== " +
                $"Mode={(IsEndlessMode ? "ENDLESS" : "FIXED")}");

            _waveSpawnCoroutine =
                StartCoroutine(
                    SpawnWaveCoroutine(
                        _currentWaveIndex,
                        waves[_currentWaveIndex]));
        }

        // =========================================================
        // GENERATE ENDLESS WAVE
        // =========================================================

        private void GenerateNextEndlessWave()
        {
            int waveNum =
                _currentWaveIndex + 1;

            float difficultyMultiplier = 1f;

            // -----------------------------------------------------
            // PLAYER PERFORMANCE DDA
            // -----------------------------------------------------

            if (GameManager.Instance != null &&
                GameManager.Instance.ActiveLevelData != null)
            {
                int maxHealth =
                    GameManager.Instance
                    .ActiveLevelData
                    .BaseMaxHealth;

                int currentHealth =
                    GameManager.Instance
                    .CurrentHealth;

                int currentGold =
                    GameManager.Instance
                    .CurrentGold;

                // Player has full HP.
                if (currentHealth >= maxHealth)
                {
                    difficultyMultiplier += 0.3f;
                }
                // Player is close to death.
                else if (currentHealth <= maxHealth * 0.3f)
                {
                    difficultyMultiplier -= 0.3f;
                }

                // Player has lots of gold.
                if (currentGold > 500)
                {
                    difficultyMultiplier += 0.2f;
                }
            }

            difficultyMultiplier =
                Mathf.Max(
                    0.5f,
                    difficultyMultiplier);

            // -----------------------------------------------------
            // BASE THREAT
            // -----------------------------------------------------

            int baseThreat =
                20 + (waveNum * 5);

            int totalThreatPoints =
                Mathf.RoundToInt(
                    baseThreat *
                    difficultyMultiplier);

            // -----------------------------------------------------
            // ARMOR
            // -----------------------------------------------------

            WaveSetup newWave =
                new WaveSetup();

            if (waveNum >= 5)
            {
                int armorBudget =
                    Mathf.RoundToInt(
                        totalThreatPoints * 0.3f);

                newWave.armorCount =
                    armorBudget / 4;

                totalThreatPoints -=
                    newWave.armorCount * 4;
            }

            // -----------------------------------------------------
            // TANK
            // -----------------------------------------------------

            if (waveNum >= 3)
            {
                int tankBudget =
                    Mathf.RoundToInt(
                        totalThreatPoints * 0.3f);

                newWave.tankCount =
                    tankBudget / 3;

                totalThreatPoints -=
                    newWave.tankCount * 3;
            }

            // -----------------------------------------------------
            // FAST
            // -----------------------------------------------------

            int fastBudget =
                Mathf.RoundToInt(
                    totalThreatPoints * 0.2f);

            newWave.fastCount =
                fastBudget / 2;

            totalThreatPoints -=
                newWave.fastCount * 2;

            // -----------------------------------------------------
            // BASIC
            // -----------------------------------------------------

            newWave.basicCount =
                Mathf.Max(
                    0,
                    totalThreatPoints);

            // -----------------------------------------------------
            // SPAWN INTERVAL
            // -----------------------------------------------------

            float spawnRateBase =
                Mathf.Max(
                    0.2f,
                    1f -
                    difficultyMultiplier * 0.1f);

            newWave.basicSpawnInterval =
                spawnRateBase;

            newWave.fastSpawnInterval =
                spawnRateBase * 0.8f;

            newWave.tankSpawnInterval =
                spawnRateBase * 1.5f;

            newWave.armorSpawnInterval =
                spawnRateBase * 2f;

            waves.Add(newWave);

            Debug.Log(
                $"[DDA] Wave {waveNum} | " +
                $"Multiplier: {difficultyMultiplier} | " +
                $"Threat: {baseThreat * difficultyMultiplier}");
        }

        // =========================================================
        // SPAWN WAVE
        // =========================================================

        private IEnumerator SpawnWaveCoroutine(
            int waveIndex,
            WaveSetup waveData)
        {
            _isSpawning = true;

            _waitingForNextWave = false;

            EventBus<WaveStartedEvent>.Raise(
                new WaveStartedEvent(
                    waveIndex,
                    waves.Count));

            List<DynamicSpawnGroup> groups =
                new List<DynamicSpawnGroup>();

            // =====================================================
            // BASIC
            // =====================================================

            if (waveData.basicCount > 0 &&
                basicEnemyPrefab != null &&
                basicEnemyData != null)
            {
                groups.Add(
                    new DynamicSpawnGroup(
                        basicEnemyPrefab,
                        basicEnemyData,
                        CalculateEnemyCount(
                            waveData.basicCount),
                        waveData.basicSpawnInterval));
            }

            // =====================================================
            // FAST
            // =====================================================

            if (waveData.fastCount > 0 &&
                fastEnemyPrefab != null &&
                fastEnemyData != null)
            {
                groups.Add(
                    new DynamicSpawnGroup(
                        fastEnemyPrefab,
                        fastEnemyData,
                        CalculateEnemyCount(
                            waveData.fastCount),
                        waveData.fastSpawnInterval));
            }

            // =====================================================
            // TANK
            // =====================================================

            if (waveData.tankCount > 0 &&
                tankEnemyPrefab != null &&
                tankEnemyData != null)
            {
                groups.Add(
                    new DynamicSpawnGroup(
                        tankEnemyPrefab,
                        tankEnemyData,
                        CalculateEnemyCount(
                            waveData.tankCount),
                        waveData.tankSpawnInterval));
            }

            // =====================================================
            // ARMOR
            // =====================================================

            if (waveData.armorCount > 0 &&
                armorEnemyPrefab != null &&
                armorEnemyData != null)
            {
                groups.Add(
                    new DynamicSpawnGroup(
                        armorEnemyPrefab,
                        armorEnemyData,
                        CalculateEnemyCount(
                            waveData.armorCount),
                        waveData.armorSpawnInterval));
            }

            _activeSpawnGroupsCount =
                groups.Count;

            // =====================================================
            // EMPTY WAVE
            // =====================================================

            if (_activeSpawnGroupsCount == 0)
            {
                _isSpawning = false;

                EventBus<WaveCompletedEvent>.Raise(
                    new WaveCompletedEvent(
                        waveIndex));

                yield break;
            }

            // =====================================================
            // START GROUPS
            // =====================================================

            for (int i = 0;
                 i < groups.Count;
                 i++)
            {
                StartCoroutine(
                    SpawnGroupCoroutine(
                        groups[i]));
            }

            // =====================================================
            // WAIT FOR ALL GROUPS
            // =====================================================

            while (_activeSpawnGroupsCount > 0)
            {
                yield return null;
            }

            _isSpawning = false;

            Debug.Log(
                $"[WaveManager] Wave " +
                $"{waveIndex + 1} finished spawning.");

            EventBus<WaveCompletedEvent>.Raise(
                new WaveCompletedEvent(
                    waveIndex));

            _waveSpawnCoroutine = null;
        }

        // =========================================================
        // ENEMY COUNT MULTIPLIER
        // =========================================================

        private int CalculateEnemyCount(
            int originalCount)
        {
            int multiplier =
                Mathf.Max(
                    1,
                    DifficultyManager.EnemyCountMultiplier);

            return Mathf.Max(
                1,
                Mathf.RoundToInt(
                    originalCount * multiplier));
        }

        // =========================================================
        // SPAWN GROUP
        // =========================================================

        private IEnumerator SpawnGroupCoroutine(
            DynamicSpawnGroup group)
        {
            Transform startWaypoint =
                waypointPath != null
                    ? waypointPath.GetWaypoint(0)
                    : null;

            Vector3 spawnPosition =
                startWaypoint != null
                    ? startWaypoint.position
                    : transform.position;

            for (int i = 0;
                 i < group.Count;
                 i++)
            {
                // -------------------------------------------------
                // PAUSE
                // -------------------------------------------------

                while (GameManager.Instance != null &&
                       GameManager.Instance.CurrentState ==
                       GameManager.GameState.Pause)
                {
                    yield return null;
                }

                // -------------------------------------------------
                // GAME ENDED
                // -------------------------------------------------

                if (GameManager.Instance != null &&
                    GameManager.Instance.CurrentState !=
                    GameManager.GameState.Playing)
                {
                    break;
                }

                // -------------------------------------------------
                // GET ENEMY
                // -------------------------------------------------

                if (ObjectPooler.Instance == null)
                {
                    Debug.LogError(
                        "[WaveManager] ObjectPooler.Instance is null.");

                    break;
                }

                GameObject enemy =
                    ObjectPooler.Instance.GetPooledObject(
                        group.Prefab,
                        spawnPosition,
                        Quaternion.identity);

                if (enemy == null)
                {
                    Debug.LogWarning(
                        "[WaveManager] Failed to get " +
                        "enemy from pool.");

                    continue;
                }

                // -------------------------------------------------
                // MOVEMENT
                // -------------------------------------------------

                EnemyMovement movement =
                    enemy.GetComponent<EnemyMovement>();

                if (movement != null)
                {
                    movement.Initialize(
                        group.Data,
                        waypointPath);
                }

                // -------------------------------------------------
                // HEALTH
                // -------------------------------------------------

                EnemyHealth health =
                    enemy.GetComponent<EnemyHealth>();

                if (health != null)
                {
                    health.Initialize(
                        group.Data,
                        DifficultyManager.HealthMultiplier,
                        DifficultyManager.SpeedMultiplier);
                }

                // -------------------------------------------------
                // REGISTER ENEMY
                // -------------------------------------------------

                EventBus<EnemySpawnedEvent>.Raise(
                    new EnemySpawnedEvent(
                        enemy));

                // -------------------------------------------------
                // SPAWN INTERVAL
                // -------------------------------------------------

                if (group.SpawnInterval > 0f &&
                    i < group.Count - 1)
                {
                    yield return new WaitForSeconds(
                        group.SpawnInterval);
                }
            }

            _activeSpawnGroupsCount =
                Mathf.Max(
                    0,
                    _activeSpawnGroupsCount - 1);
        }

        // =========================================================
        // LEVEL STARTED
        // =========================================================

        private void OnLevelStarted(
            LevelStartedEvent evt)
        {
            if (GameManager.Instance != null)
            {
                _levelData =
                    GameManager.Instance
                    .ActiveLevelData;
            }

            // Determine Endless Mode from the actual Challenge Setting.
            // Do NOT use the level name: the player can enable/disable
            // Endless Mode independently of the selected level.
            IsEndlessMode =
                PlayerPrefs.GetInt(
                    "TowerDefense_EndlessMode",
                    0) == 1;

            Debug.Log(
                $"[WaveManager] Endless Mode = {IsEndlessMode}");

            // Always enable automatic wave progression.
            autoStartNextWave = true;

            // -----------------------------------------------------
            // STOP OLD COROUTINES
            // -----------------------------------------------------

            if (_waveSpawnCoroutine != null)
            {
                StopCoroutine(
                    _waveSpawnCoroutine);

                _waveSpawnCoroutine = null;
            }

            if (_autoNextWaveCoroutine != null)
            {
                StopCoroutine(
                    _autoNextWaveCoroutine);

                _autoNextWaveCoroutine = null;
            }

            // -----------------------------------------------------
            // LOAD LEVEL WAVES
            // -----------------------------------------------------

            SyncWavesData(_levelData);

            // -----------------------------------------------------
            // RESET
            // -----------------------------------------------------

            _currentWaveIndex = -1;

            _isSpawning = false;

            _waitingForNextWave = false;

            _activeSpawnGroupsCount = 0;

            // -----------------------------------------------------
            // START FIRST WAVE
            // -----------------------------------------------------

            StartCoroutine(
                StartFirstWaveDelayed(
                    firstWaveDelay));
        }

        // =========================================================
        // SYNC WAVES
        // =========================================================

        private void SyncWavesData(
            LevelData levelData)
        {
            // If Inspector contains waves,
            // restore Inspector configuration.
            if (_initialInspectorWaves.Count > 0)
            {
                waves.Clear();

                waves.AddRange(
                    _initialInspectorWaves);

                return;
            }

            // Otherwise load from LevelData.
            if (levelData == null ||
                levelData.Waves == null ||
                levelData.Waves.Count == 0)
            {
                return;
            }

            waves.Clear();

            foreach (var waveData in levelData.Waves)
            {
                if (waveData == null)
                    continue;

                waves.Add(
                    new WaveSetup
                    {
                        basicCount =
                            waveData.BasicCount,

                        basicSpawnInterval =
                            waveData.BasicSpawnInterval,

                        fastCount =
                            waveData.FastCount,

                        fastSpawnInterval =
                            waveData.FastSpawnInterval,

                        tankCount =
                            waveData.TankCount,

                        tankSpawnInterval =
                            waveData.TankSpawnInterval,

                        armorCount =
                            waveData.ArmorCount,

                        armorSpawnInterval =
                            waveData.ArmorSpawnInterval
                    });
            }
        }

        // =========================================================
        // FIRST WAVE DELAY
        // =========================================================

        private IEnumerator StartFirstWaveDelayed(
            float delay)
        {
            yield return new WaitForSeconds(
                Mathf.Max(
                    0f,
                    delay));

            if (GameManager.Instance == null)
                yield break;

            if (GameManager.Instance.CurrentState !=
                GameManager.GameState.Playing)
            {
                yield break;
            }

            StartNextWave();
        }

        // =========================================================
        // WAVE CLEARED
        // =========================================================
        private void OnWaveCleared(
            WaveClearedEvent evt)
        {
            Debug.Log(
                $"[WaveManager] ===== WAVE " +
                $"{evt.WaveIndex + 1} CLEARED =====");

            // Ignore an old/stale event.
            if (evt.WaveIndex != _currentWaveIndex)
            {
                Debug.LogWarning(
                    $"[WaveManager] Ignoring old " +
                    $"WaveCleared event. " +
                    $"Event={evt.WaveIndex}, " +
                    $"Current={_currentWaveIndex}");

                return;
            }

            // =====================================================
            // FIXED WAVES
            // =====================================================
            if (!IsEndlessMode &&
                _currentWaveIndex >= waves.Count - 1)
            {
                Debug.Log(
                    "[WaveManager] FINAL FIXED WAVE CLEARED.");

                // Stop after the final configured wave.
                return;
            }

            // =====================================================
            // ENDLESS WAVES
            // =====================================================
            if (IsEndlessMode)
            {
                Debug.Log(
                    "[WaveManager] Endless mode active. " +
                    "Scheduling next wave.");

                ScheduleNextWave();
                return;
            }

            // =====================================================
            // NORMAL / HARD / HELL
            // =====================================================
            if (!autoStartNextWave)
            {
                Debug.LogWarning(
                    "[WaveManager] autoStartNextWave " +
                    "is OFF. Next wave will still be started.");
            }

            ScheduleNextWave();
        }

        // =========================================================
        // SCHEDULE NEXT WAVE
        // =========================================================

        private void ScheduleNextWave()
        {
            if (_waitingForNextWave)
                return;

            if (_autoNextWaveCoroutine != null)
            {
                StopCoroutine(
                    _autoNextWaveCoroutine);
            }

            _autoNextWaveCoroutine =
                StartCoroutine(
                    AutoStartNextWaveCoroutine());
        }

        // =========================================================
        // AUTO START NEXT WAVE
        // =========================================================
        private IEnumerator AutoStartNextWaveCoroutine()
        {
            _waitingForNextWave = true;

            Debug.Log(
                $"[WaveManager] Next wave in " +
                $"{waveInterval} seconds...");

            yield return new WaitForSeconds(
                Mathf.Max(
                    0f,
                    waveInterval));

            _waitingForNextWave = false;
            _autoNextWaveCoroutine = null;

            if (GameManager.Instance == null)
                yield break;

            if (GameManager.Instance.CurrentState !=
                GameManager.GameState.Playing)
            {
                yield break;
            }

            if (GameManager.Instance.ActiveEnemiesCount > 0)
            {
                Debug.LogWarning(
                    "[WaveManager] Cannot start next wave: " +
                    "enemies are still alive.");

                yield break;
            }

            // Fixed mode stops after the final configured wave.
            if (!IsEndlessMode &&
                _currentWaveIndex >= waves.Count - 1)
            {
                Debug.Log(
                    "[WaveManager] Fixed Waves finished.");

                yield break;
            }

            // Endless mode generates a new wave automatically
            // inside StartNextWave() when the configured waves end.
            StartNextWave();
        }
    }
}