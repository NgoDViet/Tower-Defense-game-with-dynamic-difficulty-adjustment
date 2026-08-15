using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense.Enemy
{
    /// <summary>
    /// Tank enemy subtype (largest size, high health, slow speed).
    /// </summary>
    public class TankEnemy : EnemyHealth
    {
        public override void Initialize(EnemyData data, int difficulty = 1)
        {
            base.Initialize(data, difficulty);
            
            // Base stats: Health = 80, Attack = 6, Armor = 0, Speed = 0.75f
            _maxHealth = Mathf.RoundToInt(80f * difficulty);
            _currentHealth = _maxHealth;
            _attack = Mathf.RoundToInt(6f * difficulty);
            _armor = 0;
            _moveSpeed = 0.75f * Mathf.Pow(1.15f, difficulty - 1);
            _moveSpeed = Mathf.Clamp(_moveSpeed, 0.5f, 3.5f);
        }
    }
}
