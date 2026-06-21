using UnityEngine;

namespace TowerDefense.Data
{
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "Tower Defense/Enemy Data", order = 1)]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string enemyName = "Basic Enemy";
        
        [Header("Movement Settings")]
        [Tooltip("Speed of the enemy along the path in units per second.")]
        [SerializeField] private float moveSpeed = 2f;

        [Header("Gameplay Stats")]
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private int goldReward = 10;
        [SerializeField] private int damageToBase = 1;

        // Public getters to expose data while keeping fields read-only from the outside
        public string EnemyName => enemyName;
        public float MoveSpeed => moveSpeed;
        public int MaxHealth => maxHealth;
        public int GoldReward => goldReward;
        public int DamageToBase => damageToBase;
    }
}
