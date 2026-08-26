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
        [Header("Basic Enemy")] public int basicCount; public float basicSpawnInterval;
        [Header("Fast Enemy")] public int fastCount; public float fastSpawnInterval;
        [Header("Tank Enemy")] public int tankCount; public float tankSpawnInterval;
        [Header("Armor Enemy")] public int armorCount; public float armorSpawnInterval;
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

        [Header("Waves Setup")]
        [SerializeField] private List<WaveSetup> waves = new List<WaveSetup>();

        // Public properties
        public List<WaveSetup> Waves => waves;
        public bool IsEndlessMode { get; private set; }

        private int _currentWaveIndex = -1;
        private bool _isSpawning = false;
        private int _activeSpawnGroupsCount = 0;
        private Coroutine _waveSpawnCoroutine;
        private List<WaveSetup> _initialInspectorWaves;

        //Lưu trực tiếp tránh swtich-case nhiều lần, tối ưu hóa hiệu năng
        private struct DynamicSpawnGroup
        {
            public GameObject Prefab;
            public EnemyData Data;
            public int Count;
            public float SpawnInterval;

            public DynamicSpawnGroup(GameObject prefab, EnemyData data, int count, float interval)
            {
                Prefab = prefab;
                Data = data;
                Count = count;
                SpawnInterval = interval;
            }
        }

        private void Awake()
        {
            _initialInspectorWaves = new List<WaveSetup>(waves);
        }

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

        public void StartNextWave()
        {
            if (_isSpawning || (GameManager.Instance != null && GameManager.Instance.ActiveEnemiesCount > 0))
            {
                Debug.LogWarning("[WaveManager] Cannot start next wave: Active spawning or enemies still on field.");
                return;
            }

            int nextWaveIndex = _currentWaveIndex + 1;

            if (nextWaveIndex >= waves.Count)
            {
                if (IsEndlessMode)
                {
                    GenerateNextEndlessWave();
                }
                else
                {
                    Debug.Log("[WaveManager] All waves completed!");
                    return;
                }
            }

            _currentWaveIndex++;
            _waveSpawnCoroutine = StartCoroutine(SpawnWaveCoroutine(_currentWaveIndex, waves[_currentWaveIndex]));
        }

        private void GenerateNextEndlessWave()
        {
            int waveNum = _currentWaveIndex + 1;
            float difficultyMultiplier = 1.0f;


            if (GameManager.Instance != null)
            {

                if (GameManager.Instance.CurrentHealth >= GameManager.Instance.ActiveLevelData.BaseMaxHealth)
                    difficultyMultiplier += 0.3f;

                else if (GameManager.Instance.CurrentHealth <= GameManager.Instance.ActiveLevelData.BaseMaxHealth * 0.3f)
                    difficultyMultiplier -= 0.3f;
                if (GameManager.Instance.CurrentGold > 5000)
                    difficultyMultiplier += 0.2f;
            }

            // Tính toán tổng điểm Threat cho Wave này
            // Càng về sau Base Threat càng tăng
            int baseThreat = 20 + (waveNum * 5);
            int totalThreatPoints = Mathf.RoundToInt(baseThreat * difficultyMultiplier);

            // 3. Mua sắm quái dựa trên điểm threat 
            WaveSetup newWave = new WaveSetup();


            if (waveNum >= 5)
            {
                int armorBudget = Mathf.RoundToInt(totalThreatPoints * 0.3f);
                newWave.armorCount = armorBudget / 4;
                totalThreatPoints -= newWave.armorCount * 4;
            }

            if (waveNum >= 3)
            {
                int tankBudget = Mathf.RoundToInt(totalThreatPoints * 0.3f);
                newWave.tankCount = tankBudget / 3;
                totalThreatPoints -= newWave.tankCount * 3;
            }

            // Dành 20% cho Fast
            int fastBudget = Mathf.RoundToInt(totalThreatPoints * 0.2f);
            newWave.fastCount = fastBudget / 2;
            totalThreatPoints -= newWave.fastCount * 2;

            // Số điểm còn lại dồn hết cho quái Basic
            newWave.basicCount = totalThreatPoints;

            // 4. Áp dụng tốc độ ra quái (Càng khó ra càng nhanh)
            float spawnRateBase = Mathf.Max(0.2f, 1.0f - (difficultyMultiplier * 0.1f));
            newWave.basicSpawnInterval = spawnRateBase;
            newWave.fastSpawnInterval = spawnRateBase * 0.8f;
            newWave.tankSpawnInterval = spawnRateBase * 1.5f;
            newWave.armorSpawnInterval = spawnRateBase * 2.0f;

            waves.Add(newWave);
            Debug.Log($"[DDA] Wave {waveNum} | Multiplier: {difficultyMultiplier} | Total Threat: {baseThreat * difficultyMultiplier}");
        }

        private IEnumerator SpawnWaveCoroutine(int waveIndex, WaveSetup waveData)
        {
            _isSpawning = true;
            EventBus<WaveStartedEvent>.Raise(new WaveStartedEvent(waveIndex, waves.Count));

            List<DynamicSpawnGroup> groups = new List<DynamicSpawnGroup>();

            // Tối ưu: Đưa thẳng Prefab vào Group, bỏ qua logic EnemyType switch-case ở dưới
            if (waveData.basicCount > 0 && basicEnemyPrefab != null)
                groups.Add(new DynamicSpawnGroup(basicEnemyPrefab, basicEnemyData, waveData.basicCount, waveData.basicSpawnInterval));
            if (waveData.fastCount > 0 && fastEnemyPrefab != null)
                groups.Add(new DynamicSpawnGroup(fastEnemyPrefab, fastEnemyData, waveData.fastCount, waveData.fastSpawnInterval));
            if (waveData.tankCount > 0 && tankEnemyPrefab != null)
                groups.Add(new DynamicSpawnGroup(tankEnemyPrefab, tankEnemyData, waveData.tankCount, waveData.tankSpawnInterval));
            if (waveData.armorCount > 0 && armorEnemyPrefab != null)
                groups.Add(new DynamicSpawnGroup(armorEnemyPrefab, armorEnemyData, waveData.armorCount, waveData.armorSpawnInterval));

            _activeSpawnGroupsCount = groups.Count;

            if (_activeSpawnGroupsCount == 0)
            {
                _isSpawning = false;
                EventBus<WaveCompletedEvent>.Raise(new WaveCompletedEvent(waveIndex));
                yield break;
            }

            foreach (var group in groups)
            {
                StartCoroutine(SpawnGroupCoroutine(group));
            }

            while (_activeSpawnGroupsCount > 0)
            {
                yield return null;
            }

            _isSpawning = false;
            EventBus<WaveCompletedEvent>.Raise(new WaveCompletedEvent(waveIndex));
        }

        private IEnumerator SpawnGroupCoroutine(DynamicSpawnGroup group)
        {
            Vector3 spawnPosition = waypointPath != null ? waypointPath.GetWaypoint(0).position : transform.position;

            // 1. TÍNH TOÁN CÁC HỆ SỐ SCALE CHO ENDLESS MODE
            float healthMultiplier = 1f;
            float attackMultiplier = 1f;
            int bonusArmor = 0;
            float speedMultiplier = 1f;

            if (IsEndlessMode)
            {
                // Tính số wave hiện tại tính từ lúc bắt đầu endless
                int endlessWaveCount = _currentWaveIndex - _initialInspectorWaves.Count + 1;
                if (endlessWaveCount > 0)
                {
                    // Máu: Tăng 10% mỗi 3 round
                    healthMultiplier += (endlessWaveCount / 3) * 0.1f;

                    // Sát thương: Tăng 15% mỗi 5 round
                    attackMultiplier += (endlessWaveCount / 5) * 0.15f;

                    // Giáp: Cộng thêm 1 giáp phẳng mỗi 10 round
                    bonusArmor += (endlessWaveCount / 10) * 1;

                    // Tốc độ chạy: Tăng 5% mỗi 15 round
                    speedMultiplier += (endlessWaveCount / 15) * 0.05f;
                }
            }

            // 2. SINH QUÁI VÀ ÁP DỤNG THÔNG SỐ
            for (int i = 0; i < group.Count; i++)
            {
                GameObject enemy = ObjectPooler.Instance.GetPooledObject(group.Prefab, spawnPosition, Quaternion.identity);

                if (enemy.TryGetComponent<EnemyMovement>(out var movement))
                    movement.Initialize(group.Data, waypointPath);

                if (enemy.TryGetComponent<EnemyHealth>(out var health))
                {
                    // Khởi tạo các chỉ số gốc từ ScriptableObject trước
                    health.Initialize(group.Data);

                    // Lần lượt áp dụng các buff nếu đang ở chế độ Endless và đạt mốc
                    if (IsEndlessMode)
                    {
                        if (healthMultiplier > 1f) health.ModifyHealth(healthMultiplier);
                        if (attackMultiplier > 1f) health.ModifyAttack(attackMultiplier);
                        if (bonusArmor > 0) health.ModifyArmor(bonusArmor);
                        if (speedMultiplier > 1f) health.ModifySpeed(speedMultiplier);
                    }
                }

                EventBus<EnemySpawnedEvent>.Raise(new EnemySpawnedEvent(enemy));

                if (group.SpawnInterval > 0 && i < group.Count - 1)
                {
                    yield return new WaitForSeconds(group.SpawnInterval);
                }
            }

            _activeSpawnGroupsCount--;
        }
        private void OnLevelStarted(LevelStartedEvent evt)
        {
            var levelData = GameManager.Instance?.ActiveLevelData;
            IsEndlessMode = evt.LevelName.EndsWith("(Endless)");
            autoStartNextWave = true;

            SyncWavesData(levelData);

            _currentWaveIndex = -1;
            _isSpawning = false;
            _activeSpawnGroupsCount = 0;

            StopAllCoroutines();
            StartCoroutine(StartFirstWaveDelayed(3f));
        }

        private void SyncWavesData(LevelData levelData)
        {
            if (_initialInspectorWaves.Count == 0 && levelData != null && levelData.Waves?.Count > 0)
            {
                waves.Clear();
                foreach (var waveData in levelData.Waves)
                {
                    if (waveData == null) continue;
                    waves.Add(new WaveSetup
                    {
                        basicCount = waveData.BasicCount,
                        basicSpawnInterval = waveData.BasicSpawnInterval,
                        fastCount = waveData.FastCount,
                        fastSpawnInterval = waveData.FastSpawnInterval,
                        tankCount = waveData.TankCount,
                        tankSpawnInterval = waveData.TankSpawnInterval,
                        armorCount = waveData.ArmorCount,
                        armorSpawnInterval = waveData.ArmorSpawnInterval
                    });
                }
            }
            else if (_initialInspectorWaves.Count > 0)
            {
                waves.Clear();
                waves.AddRange(_initialInspectorWaves);
            }
        }

        private IEnumerator StartFirstWaveDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartNextWave();
        }

        private void OnWaveCleared(WaveClearedEvent evt)
        {
            // Tránh gọi double nếu có script khác cũng đang thử bắt đầu wave
            if (autoStartNextWave || IsEndlessMode)
            {
                if (_currentWaveIndex < waves.Count - 1 || IsEndlessMode)
                {
                    StartCoroutine(AutoStartNextWaveCoroutine());
                }
            }
        }

        private IEnumerator AutoStartNextWaveCoroutine()
        {
            yield return new WaitForSeconds(waveInterval);
            StartNextWave();
        }
    }
}