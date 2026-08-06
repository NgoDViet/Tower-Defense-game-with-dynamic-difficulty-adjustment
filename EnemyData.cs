using UnityEngine;

namespace TowerDefense.Data
{
    public enum EnemyType
    {
        Basic,
        Fast,
        Tank,
        Armor,
        BossTank
    }

    [CreateAssetMenu(fileName = "EnemyData", menuName = "Tower Defense/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Enemy Type")]
        public EnemyType enemyType;

        [Header("Reward")]
        public int goldReward = 10;

        // Legacy compatibility getters (force compile/runtime safety)
        public string EnemyName => enemyType.ToString() + " Enemy";
        public float MoveSpeed => GetBaseSpeed();
        public int MaxHealth => GetBaseHealth();
        public int GoldReward => goldReward;
        public int DamageToBase => GetBaseAttack();

        public int GetHealth(int difficulty)
        {
            // Discrete difficulty levels: Easy (0) = 0.80x, Normal (1) = 1.0x, Hard (2) = 1.15x
            float multiplier = GetHealthMultiplier(difficulty);
            return Mathf.RoundToInt(GetBaseHealth() * multiplier);
        }

        public int GetAttack(int difficulty)
        {
            // Attack scales with health multiplier
            float multiplier = GetHealthMultiplier(difficulty);
            return Mathf.RoundToInt(GetBaseAttack() * multiplier);
        }

        public int GetArmor(int difficulty)
        {
            return GetBaseArmor();
        }

        public float GetSpeed(int difficulty)
        {
            // Speed remains normal for all difficulties
            return GetBaseSpeed();
        }

        public int GetGoldReward(int difficulty)
        {
            // Discrete difficulty levels: Easy (0) = 1.1x, Normal (1) = 1.0x, Hard (2) = 1.0x
            float multiplier = GetGoldMultiplier(difficulty);
            return Mathf.RoundToInt(goldReward * multiplier);
        }

        private float GetHealthMultiplier(int difficulty)
        {
            // difficulty: 0 = Easy, 1 = Normal, 2 = Hard
            switch (difficulty)
            {
                case 0: return 0.80f;  // Easy
                case 1: return 1.0f;   // Normal
                case 2: return 1.15f;  // Hard
                default: return 1.0f;  // Fallback to Normal
            }
        }

        private float GetGoldMultiplier(int difficulty)
        {
            // difficulty: 0 = Easy, 1 = Normal, 2 = Hard
            switch (difficulty)
            {
                case 0: return 1.1f;   // Easy: 1.1x gold reward
                case 1: return 1.0f;   // Normal: 1.0x gold reward
                case 2: return 1.0f;   // Hard: 1.0x gold reward
                default: return 1.0f;  // Fallback to Normal
            }
        }

        public float GetQuantityMultiplier(int difficulty)
        {
            // difficulty: 0 = Easy, 1 = Normal, 2 = Hard
            switch (difficulty)
            {
                case 0: return 0.9f;   // Easy: 0.9x enemy quantities
                case 1: return 1.0f;   // Normal: 1.0x enemy quantities
                case 2: return 1.1f;   // Hard: 1.1x enemy quantities
                default: return 1.0f;  // Fallback to Normal
            }
        }

        private float GetBaseSpeed()
        {
            switch (enemyType)
            {
                case EnemyType.Fast: return 5f;
                case EnemyType.Tank: return 1.8f;
                case EnemyType.Armor: return 2.5f;
                case EnemyType.BossTank: return 1.4f;
                default: return 3f;
            }
        }

        private int GetBaseHealth()
        {
            // Base stats: Basic = 40, multiplied by type factors
            switch (enemyType)
            {
                case EnemyType.Fast: return 20;      // 40 * 0.5
                case EnemyType.Tank: return 100;      // 40 * 2
                case EnemyType.Armor: return 60;     // 40 * 0.5
                case EnemyType.BossTank: return 450; // 40 * 3.75
                default: return 40;                  // Basic
            }
        }

        private int GetBaseAttack()
        {
            // Base stats: Basic = 2, multiplied by type factors
            switch (enemyType)
            {
                case EnemyType.Fast: return 1;       // 2 * 0.5
                case EnemyType.Tank: return 3;       // 2 * 3
                case EnemyType.Armor: return 2;      // 2 * 2
                case EnemyType.BossTank: return 5;   // 2 * 4
                default: return 1;                   // Basic
            }
        }

        private int GetBaseArmor()
        {
            // Only Armor and BossTank types have base armor
            return (enemyType == EnemyType.Armor || enemyType == EnemyType.BossTank) ? 1 : 0;
        }
    }
}
