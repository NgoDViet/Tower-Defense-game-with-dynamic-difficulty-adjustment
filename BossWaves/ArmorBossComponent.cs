using UnityEngine;
using TowerDefense.Enemy;

namespace TowerDefense.Core.BossWaves
{
    /// <summary>
    /// Handles Armor Boss behavior: Every 5 sec gets +1 armor and +10% damage.
    /// Attached to enemies during ArmorBoss wave.
    /// </summary>
    public class ArmorBossComponent : MonoBehaviour
    {
        private EnemyHealth _enemyHealth;
        private float _upgradeCooldown = 5f;
        private float _upgradeTimer = 0f;

        public void Initialize(EnemyHealth enemy)
        {
            _enemyHealth = enemy;
            _upgradeTimer = _upgradeCooldown;
        }

        private void Update()
        {
            if (_enemyHealth == null || _enemyHealth.IsDead) return;

            _upgradeTimer -= Time.deltaTime;
            if (_upgradeTimer <= 0f)
            {
                ApplyUpgrade();
                _upgradeTimer = _upgradeCooldown;
            }
        }

        private void ApplyUpgrade()
        {
            // +1 armor
            _enemyHealth.ModifyArmor(1);

            // +10% damage
            _enemyHealth.ModifyAttack(1.1f);

            Debug.Log($"[ArmorBossComponent] Enemy upgraded - Armor: {_enemyHealth.Armor}, Attack: {_enemyHealth.Attack}");
        }

        private void OnDestroy()
        {
            // Clean up if needed
        }
    }
}
