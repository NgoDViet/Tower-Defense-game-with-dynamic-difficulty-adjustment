using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TowerDefense.Data;
using TowerDefense.Enemy;
using TowerDefense.Pooling;

namespace TowerDefense.Core
{
    /// <summary>
    /// Spawns waves of enemies according to the LevelData configuration.
    /// Spawns enemies through the ObjectPooler at the starting waypoint of the path.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The waypoint path enemies will follow.")]
        [SerializeField] private WaypointPath waypointPath;

        [Header("Wave Controls")]
        [Tooltip("If true, the next wave starts automatically after a delay.")]
        [SerializeField] private bool autoStartNextWave = true;
        
        [Tooltip("Delay in seconds between waves when auto-start is active.")]
        [SerializeField] private float waveInterval = 5f;

        private LevelData _levelData;
        private int _currentWaveIndex = -1;
        private bool _isSpawning = false;
        private int _activeSpawnGroupsCount = 0;
        private Coroutine _waveSpawnCoroutine;

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
            if (_isSpawning)
            {
                Debug.LogWarning("[WaveManager] Cannot start next wave: A wave is currently spawning.");
                return;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("[WaveManager] Cannot start next wave: GameManager instance is missing.");
                return;
            }

            if (GameManager.Instance.ActiveEnemiesCount > 0)
            {
                Debug.LogWarning("[WaveManager] Cannot start next wave: There are still active enemies on the field.");
                return;
            }

            if (_levelData == null)
            {
                Debug.LogError("[WaveManager] LevelData is not loaded yet.");
                return;
            }

            int nextWaveIndex = _currentWaveIndex + 1;
            if (nextWaveIndex < _levelData.Waves.Count)
            {
                _currentWaveIndex = nextWaveIndex;
                int difficulty = (_currentWaveIndex / 5) + 1;
                GameManager.Instance.SetDifficulty(difficulty);
                
                _waveSpawnCoroutine = StartCoroutine(SpawnWaveCoroutine(_currentWaveIndex, _levelData.Waves[_currentWaveIndex]));
            }
            else
            {
                Debug.Log("[WaveManager] All waves completed for this level.");
            }
        }

        private IEnumerator SpawnWaveCoroutine(int waveIndex, WaveData waveData)
        {
            _isSpawning = true;
            Debug.Log($"[WaveManager] Wave {waveIndex} started spawning.");
            
            // Raise event that wave has started
            EventBus<WaveStartedEvent>.Raise(new WaveStartedEvent(waveIndex, _levelData.Waves.Count));

            List<EnemySpawnGroup> groups = waveData.SpawnGroups;
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

        private IEnumerator SpawnGroupCoroutine(int waveIndex, EnemySpawnGroup group)
        {
            // Delay before this enemy type starts spawning in the current wave
            if (group.delayBeforeGroup > 0)
            {
                yield return new WaitForSeconds(group.delayBeforeGroup);
            }

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

                if (group.enemyPrefab == null)
                {
                    Debug.LogWarning($"[WaveManager] Spawn group has no enemy prefab for wave {waveIndex}.");
                }
                else if (ObjectPooler.Instance == null)
                {
                    Debug.LogError("[WaveManager] ObjectPooler instance is missing.");
                }
                else
                {
                    GameObject enemy = ObjectPooler.Instance.GetPooledObject(group.enemyPrefab, spawnPosition, Quaternion.identity);

                    if (enemy == null)
                    {
                        Debug.LogError($"[WaveManager] Failed to spawn enemy prefab {group.enemyPrefab.name}.");
                    }
                    else
                    {
                        EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
                        if (movement != null)
                        {
                            movement.Initialize(group.enemyData, waypointPath);
                        }
                        else
                        {
                            Debug.LogWarning($"[WaveManager] Spawned enemy {enemy.name} is missing EnemyMovement.");
                        }

                        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                        if (health != null)
                        {
                            health.Initialize(group.enemyData, GameManager.Instance.CurrentDifficulty);
                        }
                        else
                        {
                            Debug.LogWarning($"[WaveManager] Spawned enemy {enemy.name} is missing EnemyHealth.");
                        }

                        EventBus<EnemySpawnedEvent>.Raise(new EnemySpawnedEvent(enemy));
                    }
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
            // Auto start next wave if configured and there are more waves left
            if (autoStartNextWave && _levelData != null && _currentWaveIndex < _levelData.Waves.Count - 1)
            {
                StartCoroutine(AutoStartNextWaveCoroutine());
            }
        }

        private IEnumerator AutoStartNextWaveCoroutine()
        {
            yield return new WaitForSeconds(waveInterval);
            
            // Only start if still in playing state (e.g. didn't pause/quit in between)
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                StartNextWave();
            }
        }
    }
}
