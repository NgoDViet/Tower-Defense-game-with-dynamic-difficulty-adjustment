using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Pooling;

namespace TowerDefense.Enemy
{
    /// <summary>
    /// Base class managing the health and death of an enemy unit.
    /// Integrates with the ObjectPooler for reuse and raises events on death.
    /// </summary>
    public class EnemyHealth : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The EnemyData ScriptableObject containing max health and gold reward stats.")]
        [SerializeField] protected EnemyData enemyData;

        protected int _currentHealth;
        protected int _maxHealth;
        protected int _armor;
        protected int _attack;
        protected float _moveSpeed;
        protected bool _isDead;

        public int Armor => _armor;
        public int Attack => _attack;
        public float MoveSpeed => _moveSpeed;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth > 0 ? _maxHealth : (enemyData != null ? enemyData.GetHealth(1) : 10);
        public bool IsDead => _isDead;

        public void SetCurrentHealth(int health)
        {
            _currentHealth = Mathf.Clamp(health, 0, _maxHealth);
        }

        protected virtual void Start()
        {
            if (enemyData != null)
            {
                Initialize(enemyData);
            }
        }

        protected virtual void OnEnable()
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
        public virtual void Initialize(EnemyData data, int difficulty = 1)
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
        public virtual void TakeDamage(int damage)
        {
            if (_isDead) return;

            float finalDamage = damage * Mathf.Pow(0.9f, _armor);
            _currentHealth -= Mathf.CeilToInt(finalDamage);

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
            // Used by slowing resistance system
        }

        /// <summary>
        /// Handles death logic, rewarding gold and returning the enemy to the object pool.
        /// </summary>
        protected virtual void Die()
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
