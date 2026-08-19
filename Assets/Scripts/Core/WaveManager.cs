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

    /// <summary>
    /// Spawns waves of enemies configured directly inside this component.
    /// Spawns enemies through the ObjectPooler at the starting waypoint of the path.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The waypoint path enemies will follow.")]
        [SerializeField] private WaypointPath waypointPath;

        [Header("Enemy Prefabs")]
        [SerializeField] private GameObject basicEnemyPrefab;
        [SerializeField] private GameObject fastEnemyPrefab;
        [SerializeField] private GameObject tankEnemyPrefab;
        [SerializeField] private GameObject armorEnemyPrefab;

        [Header("Enemy Specifications")]
        [Tooltip("EnemyData configurations for each of the 4 types.")]
        [SerializeField] private EnemyData basicEnemyData;
        [SerializeField] private EnemyData fastEnemyData;
        [SerializeField] private EnemyData tankEnemyData;
        [SerializeField] private EnemyData armorEnemyData;

        [Header("Wave Controls")]
        [Tooltip("If true, the next wave starts automatically after a delay.")]
        [SerializeField] private bool autoStartNextWave = true;
        
        [Tooltip("Delay in seconds between waves when auto-start is active.")]
        [SerializeField] private float waveInterval = 5f;

        [Header("Waves Setup")]
        [SerializeField] private List<WaveSetup> waves = new List<WaveSetup>();

        private LevelData _levelData;
        private int _currentWaveIndex = -1;
        private bool _isSpawning = false;
        private int _activeSpawnGroupsCount = 0;
        private Coroutine _waveSpawnCoroutine;
        private List<WaveSetup> _initialInspectorWaves = new List<WaveSetup>();

        private void Awake()
        {
            // Store a copy of waves configured in the inspector at start
            _initialInspectorWaves = new List<WaveSetup>(waves);
        }

        // Public properties to allow editor configurations
        public GameObject BasicEnemyPrefab { get => basicEnemyPrefab; set => basicEnemyPrefab = value; }
        public GameObject FastEnemyPrefab { get => fastEnemyPrefab; set => fastEnemyPrefab = value; }
        public GameObject TankEnemyPrefab { get => tankEnemyPrefab; set => tankEnemyPrefab = value; }
        public GameObject ArmorEnemyPrefab { get => armorEnemyPrefab; set => armorEnemyPrefab = value; }
        public EnemyData BasicEnemyData { get => basicEnemyData; set => basicEnemyData = value; }
        public EnemyData FastEnemyData { get => fastEnemyData; set => fastEnemyData = value; }
        public EnemyData TankEnemyData { get => tankEnemyData; set => tankEnemyData = value; }
        public EnemyData ArmorEnemyData { get => armorEnemyData; set => armorEnemyData = value; }
        public List<WaveSetup> Waves => waves;
        public bool isEndlessMode { get; private set; }

        private void OnEnable()
        {
            EventBus<LevelStartedEvent>.Subscribe(OnLevelStarted);
            EventBus<WaveClearedEvent>.Subscribe(OnWaveCleared);
        }

        private void OnDisable()
        {
            EventBus<LevelStartedEvent>.Unsubscribe(OnLevelStarted);
            EventBus<WaveClearedEvent>.Unsubscribe(OnWaveCleared);
            
            StopAllCoroutines();
        }

        /// <summary>
        /// Requests to start the next wave immediately (useful for manual wave start buttons).
        /// </summary>
        public void StartNextWave()
        {
            Debug.Log($"[WaveManager] StartNextWave called. _isSpawning={_isSpawning}, ActiveEnemies={GameManager.Instance?.ActiveEnemiesCount}, CurrentWave={_currentWaveIndex}, TotalWaves={waves.Count}");

            if (_isSpawning)
            {
                Debug.LogWarning("[WaveManager] Cannot start next wave: A wave is currently spawning.");
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.ActiveEnemiesCount > 0)
            {
                Debug.LogWarning($"[WaveManager] Cannot start next wave: There are still active {GameManager.Instance.ActiveEnemiesCount} enemies on the field.");
                return;
            }

            int nextWaveIndex = _currentWaveIndex + 1;
            if (nextWaveIndex < waves.Count)
            {
                _currentWaveIndex = nextWaveIndex;
                Debug.Log($"[WaveManager] Transitioning to Wave {nextWaveIndex}. Starting SpawnWaveCoroutine.");
                _waveSpawnCoroutine = StartCoroutine(SpawnWaveCoroutine(_currentWaveIndex, waves[_currentWaveIndex]));
            }
            else
            {
                if (isEndlessMode)
                {
                    GenerateNextWave();
                    _currentWaveIndex++;
                    _waveSpawnCoroutine = StartCoroutine(SpawnWaveCoroutine(_currentWaveIndex, waves[_currentWaveIndex]));
                }
                else
                {
                    Debug.Log($"[WaveManager] All waves completed for this level. nextWaveIndex={nextWaveIndex}, waves.Count={waves.Count}");
                }
            }
        }

        private void GenerateNextWave()
        {
            WaveSetup newWave = new WaveSetup();
            int waveNum = _currentWaveIndex + 1; // This is the wave number (0-indexed)

            // Example scaling formulas - can be tweaked for balance
            // Basic enemies: always present, count grows linearly
            newWave.basicCount = 10 + waveNum * 2;
            newWave.basicSpawnInterval = Mathf.Max(0.1f, 0.5f - waveNum * 0.01f);

            // Fast enemies: appear from wave 3, count grows slower
            if (waveNum >= 2)
            {
                newWave.fastCount = 5 + (waveNum - 2) * 2;
                newWave.fastSpawnInterval = Mathf.Max(0.2f, 0.8f - waveNum * 0.02f);
            }

            // Tank enemies: appear from wave 5, count grows slowly
            if (waveNum >= 4)
            {
                newWave.tankCount = 2 + (waveNum - 4);
                newWave.tankSpawnInterval = Mathf.Max(0.5f, 1.5f - waveNum * 0.05f);
            }

            // Armor enemies: appear from wave 7, also grow slowly
            if (waveNum >= 6)
            {
                newWave.armorCount = 2 + (waveNum - 6);
                newWave.armorSpawnInterval = Mathf.Max(0.5f, 1.2f - waveNum * 0.04f);
            }
            
            waves.Add(newWave);
            Debug.Log($"[WaveManager] Generated Endless Wave {waveNum + 1}: Basic({newWave.basicCount}), Fast({newWave.fastCount}), Tank({newWave.tankCount}), Armor({newWave.armorCount})");
        }

        public struct DynamicSpawnGroup
        {
            public EnemyType enemyType;
            public EnemyData enemyData;
            public int count;
            public float spawnInterval;

            public DynamicSpawnGroup(EnemyType type, EnemyData data, int count, float interval)
            {
                this.enemyType = type;
                this.enemyData = data;
                this.count = count;
                this.spawnInterval = interval;
            }
        }

        private IEnumerator SpawnWaveCoroutine(int waveIndex, WaveSetup waveData)
        {
            _isSpawning = true;
            Debug.Log($"[WaveManager] Wave {waveIndex} started spawning.");
            
            // Raise event that wave has started
            EventBus<WaveStartedEvent>.Raise(new WaveStartedEvent(waveIndex, waves.Count));

            // Dynamically construct spawn groups from WaveSetup counts
            List<DynamicSpawnGroup> groups = new List<DynamicSpawnGroup>();
            
            if (waveData.basicCount > 0 && basicEnemyData != null)
                groups.Add(new DynamicSpawnGroup(EnemyType.Basic, basicEnemyData, waveData.basicCount, waveData.basicSpawnInterval));
            if (waveData.fastCount > 0 && fastEnemyData != null)
                groups.Add(new DynamicSpawnGroup(EnemyType.Fast, fastEnemyData, waveData.fastCount, waveData.fastSpawnInterval));
            if (waveData.tankCount > 0 && tankEnemyData != null)
                groups.Add(new DynamicSpawnGroup(EnemyType.Tank, tankEnemyData, waveData.tankCount, waveData.tankSpawnInterval));
            if (waveData.armorCount > 0 && armorEnemyData != null)
                groups.Add(new DynamicSpawnGroup(EnemyType.Armor, armorEnemyData, waveData.armorCount, waveData.armorSpawnInterval));

            _activeSpawnGroupsCount = groups.Count;

            if (_activeSpawnGroupsCount == 0)
            {
                // Edge case: Empty wave
                _isSpawning = false;
                EventBus<WaveCompletedEvent>.Raise(new WaveCompletedEvent(waveIndex));
                yield break;
            }

            // Start all spawn groups in parallel
            for (int i = 0; i < groups.Count; i++)
            {
                StartCoroutine(SpawnGroupCoroutine(waveIndex, groups[i]));
            }

            // Wait until all parallel spawn groups have finished spawning
            while (_activeSpawnGroupsCount > 0)
            {
                yield return null;
            }

            _isSpawning = false;
            Debug.Log($"[WaveManager] Wave {waveIndex} finished spawning all units.");
            
            // Raise event that wave spawning is completed
            EventBus<WaveCompletedEvent>.Raise(new WaveCompletedEvent(waveIndex));
        }

        private IEnumerator SpawnGroupCoroutine(int waveIndex, DynamicSpawnGroup group)
        {
            Transform startWaypoint = waypointPath != null ? waypointPath.GetWaypoint(0) : null;
            Vector3 spawnPosition = startWaypoint != null ? startWaypoint.position : transform.position;

            for (int i = 0; i < group.count; i++)
            {
                // Ensure the game is still playing/active
                if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
                {
                    // If paused, wait until we resume playing
                    while (GameManager.Instance.CurrentState != GameManager.GameState.Playing)
                    {
                        yield return null;
                    }
                }

                GameObject prefabToSpawn = null;
                switch (group.enemyType)
                {
                    case EnemyType.Fast: prefabToSpawn = fastEnemyPrefab; break;
                    case EnemyType.Tank: prefabToSpawn = tankEnemyPrefab; break;
                    case EnemyType.Armor: prefabToSpawn = armorEnemyPrefab; break;
                    default: prefabToSpawn = basicEnemyPrefab; break;
                }

                if (prefabToSpawn != null)
                {
                    // Retrieve from pool
                    GameObject enemy = ObjectPooler.Instance.GetPooledObject(prefabToSpawn, spawnPosition, Quaternion.identity);

                    // Initialize movement path and statistics
                    EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
                    if (movement != null)
                    {
                        movement.Initialize(group.enemyData, waypointPath);
                    }

                    EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                    if (health != null)
                    {
                        health.Initialize(group.enemyData);
                    }

                    // Fire spawned event (tells GameManager to increase active enemy count)
                    EventBus<EnemySpawnedEvent>.Raise(new EnemySpawnedEvent(enemy));
                }

                // Wait interval before spawning the next enemy in this group
                if (group.spawnInterval > 0 && i < group.count - 1)
                {
                    yield return new WaitForSeconds(group.spawnInterval);
                }
            }

            _activeSpawnGroupsCount--;
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            if (GameManager.Instance != null)
            {
                _levelData = GameManager.Instance.ActiveLevelData;
                isEndlessMode = GameManager.Instance.CurrentState == GameManager.GameState.Playing && evt.LevelName.EndsWith("(Endless)");
            }

            // Force auto start next wave to true as requested
            autoStartNextWave = true;

            Debug.Log($"[WaveManager] OnLevelStarted. _levelData={(_levelData != null ? _levelData.name : "null")}, waves count in _levelData={(_levelData != null && _levelData.Waves != null ? _levelData.Waves.Count.ToString() : "null")}, initial inspector waves count={_initialInspectorWaves.Count}");

            // Only sync waves list from LevelData if no waves are configured in the inspector
            if (_initialInspectorWaves.Count == 0 && _levelData != null && _levelData.Waves != null && _levelData.Waves.Count > 0)
            {
                waves.Clear();
                foreach (var waveData in _levelData.Waves)
                {
                    if (waveData == null) continue;
                    WaveSetup setup = new WaveSetup();
                    setup.basicCount = waveData.BasicCount;
                    setup.basicSpawnInterval = waveData.BasicSpawnInterval;
                    setup.fastCount = waveData.FastCount;
                    setup.fastSpawnInterval = waveData.FastSpawnInterval;
                    setup.tankCount = waveData.TankCount;
                    setup.tankSpawnInterval = waveData.TankSpawnInterval;
                    setup.armorCount = waveData.ArmorCount;
                    setup.armorSpawnInterval = waveData.ArmorSpawnInterval;
                    waves.Add(setup);
                }
                Debug.Log($"[WaveManager] Synchronized waves from LevelData. New waves count={waves.Count}");
            }
            else if (_initialInspectorWaves.Count > 0)
            {
                waves.Clear();
                waves.AddRange(_initialInspectorWaves);
                Debug.Log($"[WaveManager] Restored inspector waves (precedence rule). count={waves.Count}");
            }
            else
            {
                Debug.Log($"[WaveManager] LevelData waves not synced. Fallback to inspector waves. count={waves.Count}");
            }

            _currentWaveIndex = -1;
            _isSpawning = false;
            _activeSpawnGroupsCount = 0;

            if (_waveSpawnCoroutine != null)
            {
                StopCoroutine(_waveSpawnCoroutine);
            }
            StopAllCoroutines();

            // Start first wave after a short preparation delay
            StartCoroutine(StartFirstWaveDelayed(3f));
        }

        private IEnumerator StartFirstWaveDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartNextWave();
        }

        private void OnWaveCleared(WaveClearedEvent evt)
        {
            Debug.Log($"[WaveManager] OnWaveCleared event handler. evt.WaveIndex={evt.WaveIndex}, _currentWaveIndex={_currentWaveIndex}, autoStartNextWave={autoStartNextWave}, waves.Count={waves.Count}");
            // Auto start next wave if configured and there are more waves left
            if (autoStartNextWave || isEndlessMode)
            {
                if (_currentWaveIndex < waves.Count - 1 || isEndlessMode)
                {
                    Debug.Log($"[WaveManager] Auto-starting next wave in {waveInterval} seconds.");
                    StartCoroutine(AutoStartNextWaveCoroutine());
                }
            }
            else
            {
                Debug.Log($"[WaveManager] Will not auto-start next wave. autoStartNextWave={autoStartNextWave}, hasMoreWaves={_currentWaveIndex < waves.Count - 1}");
            }
        }

        private IEnumerator AutoStartNextWaveCoroutine()
        {
            yield return new WaitForSeconds(waveInterval);
            
            Debug.Log($"[WaveManager] AutoStartNextWaveCoroutine timer completed. CurrentState={GameManager.Instance?.CurrentState}");
            // Only start if still in playing state (e.g. didn't pause/quit in between)
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                StartNextWave();
            }
        }
    }
}
