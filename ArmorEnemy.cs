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
            
            // Base stats: Health = 60, Attack = 2.5, Armor = 1, Speed = 2f
            _maxHealth = Mathf.RoundToInt(60f * difficulty);
            _currentHealth = _maxHealth;
            _attack = Mathf.RoundToInt(2.5f * difficulty);
            _armor = 1;
            _moveSpeed = 2f * Mathf.Pow(1.15f, difficulty - 1);
            _moveSpeed = Mathf.Clamp(Mathf.Floor(_moveSpeed), 1f, 7f);
        }
    }
}
