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
        [Header("References")]
        [SerializeField] private WaypointPath waypointPath;

        [Header("Enemy Prefabs")]
        [SerializeField] private GameObject basicEnemyPrefab;
        [SerializeField] private GameObject fastEnemyPrefab;
        [SerializeField] private GameObject tankEnemyPrefab;
        [SerializeField] private GameObject armorEnemyPrefab;

        [Header("Enemy Specifications")]
        [SerializeField] private EnemyData basicEnemyData;
        [SerializeField] private EnemyData fastEnemyData;
        [SerializeField] private EnemyData tankEnemyData;
        [SerializeField] private EnemyData armorEnemyData;

        [Header("Wave Controls")]
        [SerializeField] private bool autoStartNextWave = true;
        [SerializeField] private float waveInterval = 5f;
        [SerializeField] private float firstWaveDelay = 3f;

        [Header("Waves Setup")]
        [SerializeField] private List<WaveSetup> waves =
            new List<WaveSetup>();

        private LevelData _levelData;

        private int _currentWaveIndex = -1;

        private bool _isSpawning;
        private bool _waitingForNextWave;

        private int _activeSpawnGroupsCount;

        private Coroutine _waveSpawnCoroutine;
        private Coroutine _autoNextWaveCoroutine;

        private List<WaveSetup> _initialInspectorWaves =
            new List<WaveSetup>();

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

        public List<WaveSetup> Waves => waves;

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
            EventBus<LevelStartedEvent>.Subscribe(OnLevelStarted);

            EventBus<WaveClearedEvent>.Subscribe(OnWaveCleared);
        }

        // =========================================================
        // DISABLE
        // =========================================================

        private void OnDisable()
        {
            EventBus<LevelStartedEvent>.Unsubscribe(OnLevelStarted);

            EventBus<WaveClearedEvent>.Unsubscribe(OnWaveCleared);

            if (_waveSpawnCoroutine != null)
            {
                StopCoroutine(_waveSpawnCoroutine);
                _waveSpawnCoroutine = null;
            }

            if (_autoNextWaveCoroutine != null)
            {
                StopCoroutine(_autoNextWaveCoroutine);
                _autoNextWaveCoroutine = null;
            }
        }

        // =========================================================
        // START NEXT WAVE
        // =========================================================

        public void StartNextWave()
        {
            // Don't start another wave while one is spawning.
            if (_isSpawning)
            {
                Debug.Log(
                    "[WaveManager] Cannot start next wave: still spawning.");

                return;
            }

            // Don't start another wave while waiting.
            if (_waitingForNextWave)
            {
                Debug.Log(
                    "[WaveManager] Cannot start next wave: waiting.");

                return;
            }

            // Make sure there are no enemies remaining.
            if (GameManager.Instance != null &&
                GameManager.Instance.ActiveEnemiesCount > 0)
            {
                Debug.Log(
                    $"[WaveManager] Cannot start next wave: " +
                    $"{GameManager.Instance.ActiveEnemiesCount} enemies remain.");

                return;
            }

            int nextWaveIndex =
                _currentWaveIndex + 1;

            // No more waves.
            if (nextWaveIndex >= waves.Count)
            {
                Debug.Log(
                    "[WaveManager] No more waves.");

                return;
            }

            _currentWaveIndex = nextWaveIndex;

            Debug.Log(
                $"[WaveManager] ===== STARTING WAVE {_currentWaveIndex + 1}/{waves.Count} =====");

            _waveSpawnCoroutine =
                StartCoroutine(
                    SpawnWaveCoroutine(
                        _currentWaveIndex,
                        waves[_currentWaveIndex]));
        }

        // =========================================================
        // DYNAMIC SPAWN GROUP
        // =========================================================

        private struct DynamicSpawnGroup
        {
            public EnemyType enemyType;
            public EnemyData enemyData;
            public int count;
            public float spawnInterval;

            public DynamicSpawnGroup(
                EnemyType type,
                EnemyData data,
                int count,
                float interval)
            {
                enemyType = type;
                enemyData = data;
                this.count = count;
                spawnInterval = interval;
            }
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

            int multiplier =
                DifficultyManager.EnemyCountMultiplier;

            List<DynamicSpawnGroup> groups =
                new List<DynamicSpawnGroup>();

            // -----------------------------------------------------
            // BASIC
            // -----------------------------------------------------

            if (waveData.basicCount > 0 &&
                basicEnemyData != null)
            {
                groups.Add(
                    new DynamicSpawnGroup(
                        EnemyType.Basic,
                        basicEnemyData,
                        Mathf.Max(
                            1,
                            Mathf.RoundToInt(
                                waveData.basicCount * multiplier)),
                        waveData.basicSpawnInterval));
            }

            // -----------------------------------------------------
            // FAST
            // -----------------------------------------------------

            if (waveData.fastCount > 0 &&
                fastEnemyData != null)
            {
                groups.Add(
                    new DynamicSpawnGroup(
                        EnemyType.Fast,
                        fastEnemyData,
                        Mathf.Max(
                            1,
                            Mathf.RoundToInt(
                                waveData.fastCount * multiplier)),
                        waveData.fastSpawnInterval));
            }

            // -----------------------------------------------------
            // TANK
            // -----------------------------------------------------

            if (waveData.tankCount > 0 &&
                tankEnemyData != null)
            {
                groups.Add(
                    new DynamicSpawnGroup(
                        EnemyType.Tank,
                        tankEnemyData,
                        Mathf.Max(
                            1,
                            Mathf.RoundToInt(
                                waveData.tankCount * multiplier)),
                        waveData.tankSpawnInterval));
            }

            // -----------------------------------------------------
            // ARMOR
            // -----------------------------------------------------

            if (waveData.armorCount > 0 &&
                armorEnemyData != null)
            {
                groups.Add(
                    new DynamicSpawnGroup(
                        EnemyType.Armor,
                        armorEnemyData,
                        Mathf.Max(
                            1,
                            Mathf.RoundToInt(
                                waveData.armorCount * multiplier)),
                        waveData.armorSpawnInterval));
            }

            _activeSpawnGroupsCount =
                groups.Count;

            // -----------------------------------------------------
            // EMPTY WAVE
            // -----------------------------------------------------

            if (_activeSpawnGroupsCount == 0)
            {
                _isSpawning = false;

                Debug.Log(
                    $"[WaveManager] Wave {waveIndex + 1} has no enemies.");

                EventBus<WaveCompletedEvent>.Raise(
                    new WaveCompletedEvent(waveIndex));

                yield break;
            }

            // -----------------------------------------------------
            // START ALL GROUPS
            // -----------------------------------------------------

            for (int i = 0;
                 i < groups.Count;
                 i++)
            {
                StartCoroutine(
                    SpawnGroupCoroutine(groups[i]));
            }

            // -----------------------------------------------------
            // WAIT UNTIL ALL GROUPS FINISH SPAWNING
            // -----------------------------------------------------

            while (_activeSpawnGroupsCount > 0)
            {
                yield return null;
            }

            _isSpawning = false;

            Debug.Log(
                $"[WaveManager] Wave {waveIndex + 1} finished spawning.");

            // IMPORTANT:
            // This does NOT mean the wave is cleared yet.
            // GameManager waits until ActiveEnemiesCount == 0.
            EventBus<WaveCompletedEvent>.Raise(
                new WaveCompletedEvent(waveIndex));

            _waveSpawnCoroutine = null;
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
                 i < group.count;
                 i++)
            {
                // Pause.
                while (GameManager.Instance != null &&
                       GameManager.Instance.CurrentState ==
                       GameManager.GameState.Pause)
                {
                    yield return null;
                }

                // Stop spawning if game ended.
                if (GameManager.Instance != null &&
                    GameManager.Instance.CurrentState !=
                    GameManager.GameState.Playing)
                {
                    break;
                }

                GameObject prefabToSpawn =
                    GetPrefab(group.enemyType);

                if (prefabToSpawn != null &&
                    ObjectPooler.Instance != null)
                {
                    GameObject enemy =
                        ObjectPooler.Instance.GetPooledObject(
                            prefabToSpawn,
                            spawnPosition,
                            Quaternion.identity);

                    if (enemy != null)
                    {
                        // -------------------------------------------------
                        // MOVEMENT
                        // -------------------------------------------------

                        EnemyMovement movement =
                            enemy.GetComponent<EnemyMovement>();

                        if (movement != null)
                        {
                            movement.Initialize(
                                group.enemyData,
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
                                group.enemyData,
                                DifficultyManager.HealthMultiplier,
                                DifficultyManager.SpeedMultiplier);
                        }

                        // -------------------------------------------------
                        // REGISTER ENEMY
                        // -------------------------------------------------

                        EventBus<EnemySpawnedEvent>.Raise(
                            new EnemySpawnedEvent(enemy));
                    }
                }

                // -----------------------------------------------------
                // SPAWN INTERVAL
                // -----------------------------------------------------

                if (group.spawnInterval > 0f &&
                    i < group.count - 1)
                {
                    yield return new WaitForSeconds(
                        group.spawnInterval);
                }
            }

            _activeSpawnGroupsCount =
                Mathf.Max(
                    0,
                    _activeSpawnGroupsCount - 1);
        }

        // =========================================================
        // GET PREFAB
        // =========================================================

        private GameObject GetPrefab(
            EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Fast:
                    return fastEnemyPrefab;

                case EnemyType.Tank:
                    return tankEnemyPrefab;

                case EnemyType.Armor:
                    return armorEnemyPrefab;

                default:
                    return basicEnemyPrefab;
            }
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
                    GameManager.Instance.ActiveLevelData;
            }

            if (_waveSpawnCoroutine != null)
            {
                StopCoroutine(_waveSpawnCoroutine);
                _waveSpawnCoroutine = null;
            }

            if (_autoNextWaveCoroutine != null)
            {
                StopCoroutine(_autoNextWaveCoroutine);
                _autoNextWaveCoroutine = null;
            }

            // -----------------------------------------------------
            // LOAD WAVES FROM LEVEL DATA
            // -----------------------------------------------------

            if (_initialInspectorWaves.Count == 0 &&
                _levelData != null &&
                _levelData.Waves != null &&
                _levelData.Waves.Count > 0)
            {
                waves.Clear();

                foreach (var waveData in _levelData.Waves)
                {
                    if (waveData == null)
                        continue;

                    WaveSetup setup =
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
                        };

                    waves.Add(setup);
                }
            }
            else if (_initialInspectorWaves.Count > 0)
            {
                waves.Clear();
                waves.AddRange(
                    _initialInspectorWaves);
            }

            // -----------------------------------------------------
            // RESET
            // -----------------------------------------------------

            _currentWaveIndex = -1;

            _isSpawning = false;
            _waitingForNextWave = false;

            _activeSpawnGroupsCount = 0;

            // -----------------------------------------------------
            // STOP OLD COROUTINES
            // -----------------------------------------------------

            StopAllCoroutines();

            // -----------------------------------------------------
            // START WAVE 1
            // -----------------------------------------------------

            StartCoroutine(
                StartFirstWaveDelayed(
                    firstWaveDelay));
        }

        // =========================================================
        // FIRST WAVE DELAY
        // =========================================================

        private IEnumerator StartFirstWaveDelayed(
            float delay)
        {
            yield return new WaitForSeconds(delay);

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
                $"[WaveManager] ===== WAVE {evt.WaveIndex + 1}/{waves.Count} CLEARED =====");

            // Make sure this event belongs to current wave.
            if (evt.WaveIndex != _currentWaveIndex)
            {
                Debug.LogWarning(
                    $"[WaveManager] Ignoring old WaveCleared event. " +
                    $"Event={evt.WaveIndex}, Current={_currentWaveIndex}");

                return;
            }

            // Final wave.
            if (_currentWaveIndex >= waves.Count - 1)
            {
                Debug.Log(
                    "[WaveManager] FINAL WAVE CLEARED.");

                return;
            }

            // Prevent duplicate scheduling.
            if (_waitingForNextWave)
            {
                return;
            }

            if (!autoStartNextWave)
            {
                Debug.LogWarning(
                    "[WaveManager] autoStartNextWave is OFF. " +
                    "Forcing automatic next wave.");

                // IMPORTANT:
                // We intentionally continue anyway.
            }

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
                $"[WaveManager] Next wave in {waveInterval} seconds...");

            yield return new WaitForSeconds(
                Mathf.Max(0f, waveInterval));

            _waitingForNextWave = false;

            _autoNextWaveCoroutine = null;

            if (GameManager.Instance == null)
                yield break;

            if (GameManager.Instance.CurrentState !=
                GameManager.GameState.Playing)
            {
                yield break;
            }

            // Safety check.
            if (GameManager.Instance.ActiveEnemiesCount > 0)
            {
                Debug.LogWarning(
                    "[WaveManager] Cannot start next wave: " +
                    "enemies are still alive.");

                yield break;
            }

            // Final safety check.
            if (_currentWaveIndex >= waves.Count - 1)
            {
                yield break;
            }

            Debug.Log(
                $"[WaveManager] ===== AUTO START WAVE {_currentWaveIndex + 2}/{waves.Count} =====");

            StartNextWave();
        }
    }
}