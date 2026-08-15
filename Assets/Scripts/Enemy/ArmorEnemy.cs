using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense.Enemy
{
    /// <summary>
    /// Armored enemy subtype (medium-large size, starting armor).
    /// </summary>
    public class ArmorEnemy : EnemyHealth
    {
        public override void Initialize(EnemyData data, int difficulty = 1)
        {
            base.Initialize(data, difficulty);
            
            // Base stats: Health = 20, Attack = 4, Armor = 1, Speed = 1.125f
            _maxHealth = Mathf.RoundToInt(20f * difficulty);
            _currentHealth = _maxHealth;
            _attack = Mathf.RoundToInt(4f * difficulty);
            _armor = Mathf.FloorToInt(1f * Mathf.Pow(1.25f, difficulty - 1));
            _moveSpeed = 1.125f * Mathf.Pow(1.15f, difficulty - 1);
            _moveSpeed = Mathf.Clamp(_moveSpeed, 0.5f, 3.5f);
        }
    }
}
