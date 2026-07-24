using UnityEngine;
using TowerDefense.Enemy;

namespace TowerDefense.Core.BossWaves
{
    /// <summary>
    /// Handles Basic Boss behavior: +10% speed per 20% max health lost.
    /// Attached to enemies during BasicBoss wave.
    /// </summary>
    public class BasicBossComponent : MonoBehaviour
    {
        private EnemyHealth _enemyHealth;
        private EnemyMovement _enemyMovement;
        private float _baseSpeed;
        private int _20PercentHealthThreshold;
        private int _speedIncreaseCount = 0;

        public void Initialize(EnemyHealth enemy)
        {
            _enemyHealth = enemy;
            _enemyMovement = GetComponent<EnemyMovement>();

            if (_enemyHealth != null)
            {
                _baseSpeed = _enemyHealth.MoveSpeed;
                _20PercentHealthThreshold = Mathf.RoundToInt(_enemyHealth.MaxHealth * 0.2f);
            }
        }

        private void Update()
        {
            if (_enemyHealth == null) return;

            // Check if enemy has crossed a 20% health threshold
            int thresholdCrossed = (int)((_enemyHealth.MaxHealth - _enemyHealth.CurrentHealth) / (float)_20PercentHealthThreshold);

            if (thresholdCrossed > _speedIncreaseCount)
            {
                int increments = thresholdCrossed - _speedIncreaseCount;
                for (int i = 0; i < increments; i++)
                {
                    IncreaseSpeed();
                }
                _speedIncreaseCount = thresholdCrossed;
            }
        }

        private void IncreaseSpeed()
        {
            _enemyHealth.ModifySpeed(1.1f); // +10% speed
        }

        private void OnDestroy()
        {
            // Clean up if needed
        }
    }
}
