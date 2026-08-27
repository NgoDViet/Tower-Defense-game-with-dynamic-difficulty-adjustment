using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense.Enemy
{
    public class TankEnemy : EnemyHealth
    {
        public override void Initialize(
            EnemyData data,
            int difficulty = 1)
        {
            Debug.Log(
                "[TankEnemy] Initialize using Global Difficulty"
            );

            InitializeWithCurrentDifficulty(data);

            Debug.Log(
                $"[TankEnemy] " +
                $"HP = {_maxHealth}, " +
                $"Attack = {_attack}, " +
                $"Armor = {_armor}, " +
                $"Speed = {_moveSpeed:F2}"
            );
        }
    }
}