using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense.Enemy
{
    /// <summary>
    /// Fast enemy subtype (smaller size, fast speed, low health).
    /// </summary>
    public class FastEnemy : EnemyHealth
    {
        public override void Initialize(EnemyData data, int difficulty = 1)
        {
            base.Initialize(data, difficulty);
            
            // Base stats: Health = 20, Attack = 1, Armor = 0, Speed = 6f
            _maxHealth = Mathf.RoundToInt(20f * difficulty);
            _currentHealth = _maxHealth;
            _attack = Mathf.RoundToInt(1f * difficulty);
            _armor = 0;
            _moveSpeed = 6f * Mathf.Pow(1.15f, difficulty - 1);
            _moveSpeed = Mathf.Clamp(Mathf.Floor(_moveSpeed), 1f, 7f);
        }
    }
}
