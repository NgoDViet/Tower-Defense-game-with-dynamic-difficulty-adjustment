using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense.Enemy
{
    public class BasicEnemy : EnemyHealth
    {
        public override void Initialize(
            EnemyData data,
            int difficulty = 1)
        {
            Debug.Log(
                "[BasicEnemy] Initialize using Global Difficulty"
            );

            InitializeWithCurrentDifficulty(data);

            Debug.Log(
                $"[BasicEnemy] " +
                $"HP = {_maxHealth}, " +
                $"Attack = {_attack}, " +
                $"Armor = {_armor}, " +
                $"Speed = {_moveSpeed:F2}"
            );
        }
    }
}