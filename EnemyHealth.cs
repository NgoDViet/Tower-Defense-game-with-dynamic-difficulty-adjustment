using System.Collections;
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

        [Tooltip("Animator used for walk and death animations.")]
        [SerializeField] private Animator animator;

        [Header("Animation")]
        [Tooltip("Time to wait before returning the enemy to the object pool.")]
        [SerializeField] private float deathAnimationDuration = 0.5f;

        protected int _currentHealth;
        protected int _maxHealth;
        protected int _armor;
        protected int _attack;
        protected float _moveSpeed;
        protected bool _isDead;
        protected int _difficulty = 1;

        public int Armor => _armor;
        public int Attack => _attack;
        public float MoveSpeed => _moveSpeed;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth > 0
            ? _maxHealth
            : (enemyData != null ? enemyData.GetHealth(1) : 10);

        public bool IsDead => _isDead;
        public EnemyData EnemyData => enemyData;

        protected virtual void Awake()
        {
            // Automatically find the Animator on this GameObject
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

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

            // Reset the animator so the enemy starts walking again
            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }

        /// <summary>
        /// Programmatically initializes the health component.
        /// Useful for pooled spawns.
        /// </summary>
        public virtual void Initialize(EnemyData data, int difficulty = 1)
        {
            enemyData = data;
            _difficulty = difficulty;

            _maxHealth = data.GetHealth(difficulty);
            _currentHealth = _maxHealth;

            _attack = data.GetAttack(difficulty);
            _armor = data.GetArmor(difficulty);
            _moveSpeed = data.GetSpeed(difficulty);

            _isDead = false;
        }

        /// <summary>
        /// Applies damage to the enemy.
        /// Armor enemies take 80% of incoming damage.
        /// </summary>
        public virtual void TakeDamage(int damage)
        {
            if (_isDead) return;

            float damageMultiplier = _armor == 1 ? 0.8f : 1.0f;
            float finalDamage = damage * damageMultiplier;

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

        public void ModifyAttack(float multiplier)
        {
            _attack = Mathf.RoundToInt(_attack * multiplier);
        }

        public void ModifySpeed(float multiplier)
        {
            _moveSpeed *= multiplier;
            _moveSpeed = Mathf.Clamp(_moveSpeed, 1f, 7f);
        }

        public void SetCanBeSlowed(bool value)
        {
            // Used by slowing resistance system
        }

        /// <summary>
        /// Handles enemy death, plays the death animation,
        /// then returns the enemy to the object pool.
        /// </summary>
        protected virtual void Die()
        {
            if (_isDead) return;

            _isDead = true;

            int goldReward = enemyData != null
                ? enemyData.GoldReward
                : 10;

            // Raise the death event
            EventBus<EnemyDiedEvent>.Raise(
                new EnemyDiedEvent(gameObject, goldReward)
            );

            Debug.Log(
                $"[EnemyHealth] Enemy {gameObject.name} died. Rewarded {goldReward} gold."
            );

            // Play death animation
            if (animator != null)
            {
                animator.SetTrigger("Die");

                StartCoroutine(ReturnToPoolAfterDeath());
            }
            else
            {
                ReturnEnemyToPool();
            }
        }

        /// <summary>
        /// Waits for the death animation to finish.
        /// </summary>
        private IEnumerator ReturnToPoolAfterDeath()
        {
            yield return new WaitForSeconds(deathAnimationDuration);

            ReturnEnemyToPool();
        }

        /// <summary>
        /// Returns the enemy to the object pool or destroys it
        /// if no ObjectPooler exists.
        /// </summary>
        private void ReturnEnemyToPool()
        {
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