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
            
            // Base stats: Health = 100, Attack = 3, Armor = 0, Speed = 1.8f
            _maxHealth = Mathf.RoundToInt(100f * difficulty);
            _currentHealth = _maxHealth;
            _attack = Mathf.RoundToInt(3f * difficulty);
            _armor = 0;
            _moveSpeed = 1.8f * Mathf.Pow(1.15f, difficulty - 1);
            _moveSpeed = Mathf.Clamp(Mathf.Floor(_moveSpeed), 1f, 7f);
        }
    }
}
