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
        }
    }
}
