using UnityEngine;
using TowerDefense.Enemy;
using System.Collections.Generic;

namespace TowerDefense.Core.BossWaves
{
    /// <summary>
    /// Defines the types of boss waves available.
    /// Each type modifies normal enemy stats rather than creating new enemy types.
    /// </summary>
    public enum BossWaveType
    {
        None,           // Normal wave, no modifications
        FastBoss,       // 20% more enemies, 20% less health, cannot be slowed
        BasicBoss,      // +10% speed per 20% health lost, +30% health
        ArmorBoss,      // Every 5 sec: +1 armor & +10% damage, all stats -10%
        TankBoss        // +30% health, heals 5% every 5 sec
    }

    /// <summary>
    /// Applies stat modifications to normal enemies based on boss wave type.
    /// Applied at enemy spawn time during a boss wave.
    /// </summary>
    public class BossWaveModifier : MonoBehaviour
    {
        private BossWaveType _currentBossWaveType = BossWaveType.None;

        [SerializeField] private float _updateInterval = 5f; // For armor/tank regeneration
        private float _updateTimer = 0f;

        // Track which enemies are affected by this boss wave
        private List<EnemyHealth> _waveEnemies = new List<EnemyHealth>();

        public BossWaveType CurrentBossWaveType => _currentBossWaveType;

        private void Update()
        {
            if (_currentBossWaveType == BossWaveType.None) return;

            _updateTimer -= Time.deltaTime;
            if (_updateTimer <= 0f)
            {
                ApplyPeriodicModifications();
                _updateTimer = _updateInterval;
            }
        }

        /// <summary>
        /// Sets the boss wave type and applies initial modifications to all spawned enemies.
        /// Call this before spawning the wave.
        /// </summary>
        public void SetBossWaveType(BossWaveType bossWaveType)
        {
            _currentBossWaveType = bossWaveType;
            _waveEnemies.Clear();
            Debug.Log($"[BossWaveModifier] Set boss wave type to: {bossWaveType}");
        }

        /// <summary>
        /// Applies boss wave modifiers to an enemy when it's spawned.
        /// </summary>
        public void ApplyModificationsToEnemy(EnemyHealth enemy)
        {
            if (enemy == null || _currentBossWaveType == BossWaveType.None) return;

            _waveEnemies.Add(enemy);

            switch (_currentBossWaveType)
            {
                case BossWaveType.FastBoss:
                    ApplyFastBossModifiers(enemy);
                    break;

                case BossWaveType.BasicBoss:
                    ApplyBasicBossModifiers(enemy);
                    break;

                case BossWaveType.ArmorBoss:
                    ApplyArmorBossModifiers(enemy);
                    break;

                case BossWaveType.TankBoss:
                    ApplyTankBossModifiers(enemy);
                    break;
            }
        }

        /// <summary>
        /// Fast Boss: 20% less health, cannot be slowed
        /// </summary>
        private void ApplyFastBossModifiers(EnemyHealth enemy)
        {
            enemy.ModifyHealth(0.8f); // 20% less health
            enemy.SetCanBeSlowed(false); // Cannot be slowed
        }

        /// <summary>
        /// Basic Boss: +30% health
        /// Speed increases by 10% for every 20% max health lost (handled in BasicBossComponent)
        /// </summary>
        private void ApplyBasicBossModifiers(EnemyHealth enemy)
        {
            enemy.ModifyHealth(1.3f); // 30% more health

            // Add speed scaling component
            BasicBossComponent speedScaler = enemy.gameObject.AddComponent<BasicBossComponent>();
            speedScaler.Initialize(enemy);
        }

        /// <summary>
        /// Armor Boss: All stats -10%, armor starts at 2, gains +1 armor & +10% damage every 5 sec
        /// </summary>
        private void ApplyArmorBossModifiers(EnemyHealth enemy)
        {
            enemy.ModifyHealth(0.9f); // -10% health
            enemy.ModifyAttack(0.9f); // -10% attack
            enemy.ModifySpeed(0.9f); // -10% speed
            enemy.ModifyArmor(1); // Starts with +1 armor (so total armor = 2)

            // Add periodic upgrade component
            ArmorBossComponent armorUpgrader = enemy.gameObject.AddComponent<ArmorBossComponent>();
            armorUpgrader.Initialize(enemy);
        }

        /// <summary>
        /// Tank Boss: +30% health, heals 5% every 5 sec
        /// </summary>
        private void ApplyTankBossModifiers(EnemyHealth enemy)
        {
            enemy.ModifyHealth(1.3f); // 30% more health

            // Add healing component
            TankBossComponent healer = enemy.gameObject.AddComponent<TankBossComponent>();
            healer.Initialize(enemy);
        }

        /// <summary>
        /// Apply modifications that happen every 5 seconds during the wave.
        /// </summary>
        private void ApplyPeriodicModifications()
        {
            // Components handle their own periodic updates
            // This is kept for future global wave effects if needed
        }

        public void RemoveEnemy(EnemyHealth enemy)
        {
            _waveEnemies.Remove(enemy);
        }
    }
}
