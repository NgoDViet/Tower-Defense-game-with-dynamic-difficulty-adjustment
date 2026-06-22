using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Pooling;

namespace TowerDefense.Enemy
{
    /// <summary>
    /// Manages the health and death of an enemy unit.
    /// Integrates with the ObjectPooler for reuse and raises events on death.
    /// </summary>
    public class EnemyHealth : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The EnemyData ScriptableObject containing max health and gold reward stats.")]
        [SerializeField] private EnemyData enemyData;

        private int _currentHealth;
        private bool _isDead;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => enemyData != null ? enemyData.MaxHealth : 10;
        public bool IsDead => _isDead;

        private void Start()
        {
            if (enemyData != null)
            {
                Initialize(enemyData);
            }
        }

        private void OnEnable()
        {
            // Reset state when retrieved from pool
            if (enemyData != null)
            {
                _currentHealth = enemyData.MaxHealth;
            }
            _isDead = false;
        }

        /// <summary>
        /// Programmatically initializes the health component (useful for pooled spawns).
        /// </summary>
        public void Initialize(EnemyData data)
        {
            enemyData = data;
            _currentHealth = enemyData != null ? enemyData.MaxHealth : 10;
            _isDead = false;
        }

        /// <summary>
        /// Applies damage to the enemy. Triggers death if health falls to 0 or below.
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (_isDead) return;

            _currentHealth -= damage;
            
            // Optional: Draw hit effects or floating text here in the future
            
            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Handles death logic, rewarding gold and returning the enemy to the object pool.
        /// </summary>
        private void Die()
        {
            _isDead = true;

            int goldReward = enemyData != null ? enemyData.GoldReward : 10;
            
            // Raise the death event via the EventBus
            EventBus<EnemyDiedEvent>.Raise(new EnemyDiedEvent(gameObject, goldReward));
            
            Debug.Log($"[EnemyHealth] Enemy {gameObject.name} died. Rewarded {goldReward} gold.");

            // Recycle GameObject
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnToPool(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
