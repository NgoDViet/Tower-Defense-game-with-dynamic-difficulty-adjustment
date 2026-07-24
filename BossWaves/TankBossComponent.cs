using UnityEngine;
using TowerDefense.Enemy;

namespace TowerDefense.Core.BossWaves
{
    /// <summary>
    /// Handles Tank Boss behavior: Heals 5% of max health every 5 seconds.
    /// Attached to enemies during TankBoss wave.
    /// </summary>
    public class TankBossComponent : MonoBehaviour
    {
        private EnemyHealth _enemyHealth;
        private float _healCooldown = 5f;
        private float _healPercentage = 0.05f;
        private float _healTimer = 0f;

        public void Initialize(EnemyHealth enemy)
        {
            _enemyHealth = enemy;
            _healTimer = _healCooldown;
        }

        private void Update()
        {
            if (_enemyHealth == null || _enemyHealth.IsDead) return;

            _healTimer -= Time.deltaTime;
            if (_healTimer <= 0f)
            {
                Heal();
                _healTimer = _healCooldown;
            }
        }

        private void Heal()
        {
            int healAmount = Mathf.RoundToInt(_enemyHealth.MaxHealth * _healPercentage);
            int newHealth = Mathf.Min(_enemyHealth.CurrentHealth + healAmount, _enemyHealth.MaxHealth);
            _enemyHealth.SetCurrentHealth(newHealth);

            Debug.Log($"[TankBossComponent] Tank boss healed for {healAmount} HP. Current: {newHealth}/{_enemyHealth.MaxHealth}");
        }

        private void OnDestroy()
        {
            // Clean up if needed
        }
    }
}
