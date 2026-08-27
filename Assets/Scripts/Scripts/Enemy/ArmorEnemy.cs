using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense.Enemy
{
    public class ArmorEnemy : EnemyHealth
    {
        public override void Initialize(
            EnemyData data,
            int difficulty = 1)
        {
            Debug.Log(
                "[ArmorEnemy] Initialize using Global Difficulty"
            );

            InitializeWithCurrentDifficulty(data);

            Debug.Log(
                $"[ArmorEnemy] " +
                $"HP = {_maxHealth}, " +
                $"Attack = {_attack}, " +
                $"Armor = {_armor}, " +
                $"Speed = {_moveSpeed:F2}"
            );
        }
    }
}