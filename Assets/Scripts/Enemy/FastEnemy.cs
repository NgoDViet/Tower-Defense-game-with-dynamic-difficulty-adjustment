using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense.Enemy
{
    /// <summary>
    /// Fast enemy.
    /// </summary>
    public class FastEnemy : EnemyHealth
    {
        public override void Initialize(
            EnemyData data,
            int difficulty = 1)
        {
            Debug.Log(
                "[FastEnemy] Initialize using Global Difficulty"
            );

            base.InitializeWithCurrentDifficulty(data);

            Debug.Log(
                $"[FastEnemy] " +
                $"HP = {_maxHealth}, " +
                $"Attack = {_attack}, " +
                $"Armor = {_armor}, " +
                $"Speed = {_moveSpeed:F2}"
            );
        }
    }
}