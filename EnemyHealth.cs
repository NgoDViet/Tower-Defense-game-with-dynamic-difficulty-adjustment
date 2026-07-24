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
        private int _maxHealth;
        private int _armor;
        private int _attack;
        public int Armor => _armor;
        public int Attack => _attack;
        private float _moveSpeed;
        public float MoveSpeed => _moveSpeed;
        private bool _isDead;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth > 0 ? _maxHealth : (enemyData != null ? enemyData.GetHealth(1) : 10);
        public bool IsDead => _isDead;

        public void SetCurrentHealth(int health)
        {
            _currentHealth = Mathf.Clamp(health, 0, _maxHealth);
        }

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
            if (_maxHealth > 0)
            {
                _currentHealth = _maxHealth;
            }
            else if (enemyData != null)
            {
                _maxHealth = enemyData.GetHealth(1);
                _currentHealth = _maxHealth;
            }
            else
            {
                _currentHealth = 10;
            }

            _isDead = false;
        }

        /// <summary>
        /// Programmatically initializes the health component (useful for pooled spawns).
        /// </summary>
        public void Initialize(EnemyData data, int difficulty = 1)
        {
            enemyData = data;

            _maxHealth = data.GetHealth(difficulty);
            _currentHealth = _maxHealth;

            _attack = data.GetAttack(difficulty);
            _armor = data.GetArmor(difficulty);
            _moveSpeed = data.GetSpeed(difficulty);

            _isDead = false;
        }
        /// <summary>
        /// Applies damage to the enemy. Triggers death if health falls to 0 or below.
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (_isDead) return;

            float finalDamage = damage * Mathf.Pow(0.9f, _armor);
            _currentHealth -= Mathf.CeilToInt(finalDamage);

            // Optional: Draw hit effects or floating text here in the future

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Modifier methods for boss wave adjustments.
        /// </summary>
        public void ModifyHealth(float multiplier)
        {
            _maxHealth = Mathf.RoundToInt(_maxHealth * multiplier);
            _currentHealth = _maxHealth;
        }

        public void ModifyArmor(int addedArmor)
        {
            _armor += addedArmor;
            _armor = Mathf.Max(_armor, 0); // Clamp to 0 minimum
        }

        public void ModifyAttack(float multiplier)
        {
            _attack = Mathf.RoundToInt(_attack * multiplier);
        }

        public void ModifySpeed(float multiplier)
        {
            _moveSpeed *= multiplier;
            _moveSpeed = Mathf.Clamp(_moveSpeed, 1f, 7f); // Maintain speed constraints
        }

        public void SetCanBeSlowed(bool value)
        {
            // TODO: Implement slowing resistance system
            // This will be used by fast boss waves
        }

        /// <summary>
        /// Handles death logic, rewarding gold and returning the enemy to the object pool.
        /// </summary>
        private void Die()
        {
            _isDead = true;

            int goldReward = enemyData != null ? enemyData.goldReward : 10;
            
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
