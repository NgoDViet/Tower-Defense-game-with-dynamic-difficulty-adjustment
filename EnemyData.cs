using UnityEngine;

namespace TowerDefense.Data
{
    public enum EnemyType
    {
        Basic,
        Fast,
        Tank,
        Armor
    }

    [CreateAssetMenu(fileName = "EnemyData", menuName = "Tower Defense/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Enemy Type")]
        public EnemyType enemyType;

        [Header("Reward")]
        public int goldReward = 10;

        public int GetHealth(int difficulty)
        {
            return Mathf.RoundToInt(GetBaseHealth() * difficulty);
        }

        public int GetAttack(int difficulty)
        {
            return Mathf.RoundToInt(GetBaseAttack() * difficulty);
        }

        public int GetArmor(int difficulty)
        {
            float armor = GetBaseArmor() * Mathf.Pow(1.25f, difficulty - 1);
            return Mathf.FloorToInt(armor);
        }

        public float GetSpeed(int difficulty)
        {
            float speed = GetBaseSpeed() * Mathf.Pow(1.15f, difficulty - 1);

            speed = Mathf.Floor(speed);

            return Mathf.Clamp(speed,1,7);
        }

        float GetBaseSpeed()
        {
            switch(enemyType)
            {
                case EnemyType.Fast: return 6f;
                case EnemyType.Tank: return 1.5f;
                case EnemyType.Armor: return 2.25f;
                default: return 3f;
            }
        }

        int GetBaseHealth()
        {
            // Base stats: Basic = 40, multiplied by type factors
            switch(enemyType)
            {
                case EnemyType.Fast: return 20;      // 40 * 0.5
                case EnemyType.Tank: return 80;      // 40 * 2
                case EnemyType.Armor: return 20;     // 40 * 0.5
                default: return 40;                  // Basic
            }
        }

        int GetBaseAttack()
        {
            // Base stats: Basic = 2, multiplied by type factors
            switch(enemyType)
            {
                case EnemyType.Fast: return 1;       // 2 * 0.5
                case EnemyType.Tank: return 6;       // 2 * 3
                case EnemyType.Armor: return 4;      // 2 * 2
                default: return 2;                   // Basic
            }
        }

        int GetBaseArmor()
        {
            // Only Armor type has base armor
            return enemyType == EnemyType.Armor ? 1 : 0;
        }
    }
}