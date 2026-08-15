using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense.Enemy
{
    /// <summary>
    /// Basic enemy subtype.
    /// </summary>
    public class BasicEnemy : EnemyHealth
    {
        public override void Initialize(EnemyData data, int difficulty = 1)
        {
            base.Initialize(data, difficulty);
            
            // Base stats: Health = 40, Attack = 2, Armor = 0, Speed = 1.5f
            _maxHealth = Mathf.RoundToInt(40f * difficulty);
            _currentHealth = _maxHealth;
            _attack = Mathf.RoundToInt(2f * difficulty);
            _armor = 0;
            _moveSpeed = 1.5f * Mathf.Pow(1.15f, difficulty - 1);
            _moveSpeed = Mathf.Clamp(_moveSpeed, 0.5f, 3.5f);
        }
    }
}
