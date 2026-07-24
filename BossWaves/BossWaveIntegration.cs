using UnityEngine;
using TowerDefense.Core.BossWaves;
using TowerDefense.Enemy;

namespace TowerDefense.Core
{
    /// <summary>
    /// Example integration showing how to use BossWaveModifier when spawning waves.
    /// Add this to your existing WaveManager or spawning system.
    /// </summary>
    public class BossWaveIntegration : MonoBehaviour
    {
        [SerializeField] private BossWaveModifier bossWaveModifier;
        private BossWaveType _currentWaveType = BossWaveType.None;

        /// <summary>
        /// Call this before spawning a wave to set the boss wave type.
        /// </summary>
        public void StartBossWave(BossWaveType bossWaveType)
        {
            _currentWaveType = bossWaveType;
            if (bossWaveModifier != null)
            {
                bossWaveModifier.SetBossWaveType(bossWaveType);
            }
            Debug.Log($"[BossWaveIntegration] Starting {bossWaveType} wave");
        }

        /// <summary>
        /// Call this when spawning each enemy during a boss wave.
        /// Example usage in your enemy spawn code:
        /// 
        /// GameObject enemyObj = ObjectPooler.Instance.GetPooledObject(prefab, pos, rot);
        /// EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
        /// health.Initialize(enemyData, difficulty);
        /// 
        /// // Apply boss wave modifiers
        /// bossWaveIntegration.ApplyBossWaveModifiers(health);
        /// 
        /// // Calculate quantity for boss waves (20% more enemies)
        /// int quantity = CalculateWaveQuantity(basQuantity);
        /// </summary>
        public void ApplyBossWaveModifiers(EnemyHealth enemy)
        {
            if (bossWaveModifier != null)
            {
                bossWaveModifier.ApplyModificationsToEnemy(enemy);
            }
        }

        /// <summary>
        /// Calculate quantity for boss waves.
        /// For example: 5 enemies becomes 6 (5 * 1.2 = 6)
        /// </summary>
        public int CalculateWaveQuantity(int baseQuantity)
        {
            if (_currentWaveType == BossWaveType.FastBoss)
            {
                return Mathf.RoundToInt(baseQuantity * 1.2f); // 20% more enemies
            }
            return baseQuantity;
        }

        /// <summary>
        /// Call this when the wave ends to reset for next normal wave.
        /// </summary>
        public void EndBossWave()
        {
            _currentWaveType = BossWaveType.None;
            if (bossWaveModifier != null)
            {
                bossWaveModifier.SetBossWaveType(BossWaveType.None);
            }
            Debug.Log("[BossWaveIntegration] Boss wave ended");
        }
    }
}
