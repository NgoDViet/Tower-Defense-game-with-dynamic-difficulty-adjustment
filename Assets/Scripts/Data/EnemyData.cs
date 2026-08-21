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

    [CreateAssetMenu(
        fileName = "EnemyData",
        menuName = "Tower Defense/Enemy Data"
    )]
    public class EnemyData : ScriptableObject
    {
        [Header("Enemy Type")]
        public EnemyType enemyType;

        [Header("Reward")]
        public int goldReward = 10;

        // =========================================================
        // PUBLIC PROPERTIES
        // =========================================================

        public string EnemyName
        {
            get
            {
                return enemyType.ToString() + " Enemy";
            }
        }

        public float MoveSpeed
        {
            get
            {
                return GetBaseSpeedValue();
            }
        }

        public int MaxHealth
        {
            get
            {
                return GetBaseHealthValue();
            }
        }

        public int GoldReward
        {
            get
            {
                return goldReward;
            }
        }

        public int DamageToBase
        {
            get
            {
                return GetBaseAttackValue();
            }
        }

        // =========================================================
        // BASE HEALTH
        // =========================================================

        public int GetBaseHealthValue()
        {
            switch (enemyType)
            {
                case EnemyType.Fast:
                    return 20;

                case EnemyType.Tank:
                    return 80;

                case EnemyType.Armor:
                    return 20;

                case EnemyType.Basic:
                default:
                    return 40;
            }
        }

        // =========================================================
        // BASE SPEED
        // =========================================================

        public float GetBaseSpeedValue()
        {
            switch (enemyType)
            {
                case EnemyType.Fast:
                    return 3f;

                case EnemyType.Tank:
                    return 0.75f;

                case EnemyType.Armor:
                    return 1.125f;

                case EnemyType.Basic:
                default:
                    return 1.5f;
            }
        }

        // =========================================================
        // BASE ATTACK
        // =========================================================

        public int GetBaseAttackValue()
        {
            switch (enemyType)
            {
                case EnemyType.Fast:
                    return 1;

                case EnemyType.Tank:
                    return 6;

                case EnemyType.Armor:
                    return 4;

                case EnemyType.Basic:
                default:
                    return 2;
            }
        }

        // =========================================================
        // BASE ARMOR
        // =========================================================

        public int GetBaseArmorValue()
        {
            return enemyType == EnemyType.Armor ? 1 : 0;
        }

        // =========================================================
        // DIFFICULTY
        // =========================================================

        public int GetHealthWithMultiplier(float multiplier)
        {
            return Mathf.RoundToInt(
                GetBaseHealthValue() * multiplier
            );
        }

        public float GetSpeedWithMultiplier(float multiplier)
        {
            return GetBaseSpeedValue() * multiplier;
        }

        // =========================================================
        // OLD COMPATIBILITY METHODS
        // =========================================================

        public int GetHealth(int difficulty)
        {
            return Mathf.RoundToInt(
                GetBaseHealthValue() * difficulty
            );
        }

        public int GetAttack(int difficulty)
        {
            return GetBaseAttackValue();
        }

        public int GetArmor(int difficulty)
        {
            return GetBaseArmorValue();
        }

        public float GetSpeed(int difficulty)
        {
            return GetBaseSpeedValue() * difficulty;
        }
    }
}