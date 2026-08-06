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
            // Call base class which handles all stat initialization with new discrete difficulty system
            base.Initialize(data, difficulty);
        }
    }
}
