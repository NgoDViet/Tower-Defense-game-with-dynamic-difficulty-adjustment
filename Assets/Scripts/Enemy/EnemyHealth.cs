using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Pooling;

namespace TowerDefense.Enemy
{
    /// <summary>
    /// Base class managing enemy health, attack, armor,
    /// movement speed, slow effect and death.
    /// Uses the global DifficultyManager.
    /// </summary>
    public class EnemyHealth : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        protected EnemyData enemyData;

        // =========================================================
        // INTERNAL DATA
        // =========================================================

        protected bool _isDead;

        protected int _currentHealth;
        protected int _maxHealth;

        protected int _armor;
        protected int _attack;

        protected float _moveSpeed;

        // Slow system
        protected float _slowMultiplier = 1f;
        protected float _slowTimer = 0f;

        // =========================================================
        // PROPERTIES
        // =========================================================

        public int Armor => _armor;

        public int Attack => _attack;

        public float MoveSpeed =>
            _moveSpeed * _slowMultiplier;

        public int CurrentHealth => _currentHealth;

        public int MaxHealth =>
            _maxHealth > 0
                ? _maxHealth
                : (enemyData != null
                    ? enemyData.GetBaseHealthValue()
                    : 10);

        public bool IsDead => _isDead;

        public EnemyData EnemyData => enemyData;

        // =========================================================
        // START
        // =========================================================

        protected virtual void Start()
        {
            if (enemyData != null && _maxHealth <= 0)
            {
                InitializeWithCurrentDifficulty(enemyData);
            }
        }

        // =========================================================
        // UPDATE
        // =========================================================

        protected virtual void Update()
        {
            if (_slowTimer <= 0f)
                return;

            _slowTimer -= Time.deltaTime;

            if (_slowTimer <= 0f)
            {
                _slowTimer = 0f;
                _slowMultiplier = 1f;
            }
        }

        // =========================================================
        // ENABLE
        // =========================================================

        protected virtual void OnEnable()
        {
            _isDead = false;

            // Reset slow effect when reused from Object Pool
            _slowMultiplier = 1f;
            _slowTimer = 0f;

            if (_maxHealth > 0)
            {
                _currentHealth = _maxHealth;
            }
        }

        // =========================================================
        // INITIALIZE WITH GLOBAL DIFFICULTY
        // =========================================================

        public virtual void InitializeWithCurrentDifficulty(
            EnemyData data)
        {
            if (data == null)
            {
                Debug.LogError(
                    "[EnemyHealth] EnemyData is NULL!"
                );

                return;
            }

            Initialize(
                data,
                DifficultyManager.HealthMultiplier,
                DifficultyManager.SpeedMultiplier
            );
        }

        // =========================================================
        // MAIN INITIALIZE
        // =========================================================

        public virtual void Initialize(
            EnemyData data,
            float healthMultiplier,
            float speedMultiplier)
        {
            if (data == null)
            {
                Debug.LogError(
                    "[EnemyHealth] EnemyData is NULL!"
                );

                return;
            }

            enemyData = data;

            // =====================================================
            // RESET STATUS
            // =====================================================

            _isDead = false;

            _slowMultiplier = 1f;
            _slowTimer = 0f;

            // =====================================================
            // HP
            // =====================================================

            _maxHealth =
                data.GetHealthWithMultiplier(
                    healthMultiplier
                );

            _currentHealth = _maxHealth;

            // =====================================================
            // ATTACK
            // =====================================================

            _attack =
                data.GetBaseAttackValue();

            // =====================================================
            // ARMOR
            // =====================================================

            _armor =
                data.GetBaseArmorValue();

            // =====================================================
            // SPEED
            // =====================================================

            _moveSpeed =
                data.GetSpeedWithMultiplier(
                    speedMultiplier
                );

            // =====================================================
            // DEBUG
            // =====================================================

            Debug.Log(
                $"[EnemyHealth] {gameObject.name} initialized | " +
                $"Difficulty: {DifficultyManager.DifficultyName} | " +
                $"HP: {_maxHealth} | " +
                $"Speed: {_moveSpeed:F2} | " +
                $"Attack: {_attack} | " +
                $"Armor: {_armor}"
            );
        }

        // =========================================================
        // COMPATIBILITY INITIALIZE
        // =========================================================

        public virtual void Initialize(
            EnemyData data,
            int difficulty = 1)
        {
            float multiplier =
                Mathf.Max(1f, difficulty);

            Initialize(
                data,
                multiplier,
                multiplier
            );
        }

        // =========================================================
        // HEALTH
        // =========================================================

        public void SetCurrentHealth(int health)
        {
            _currentHealth =
                Mathf.Clamp(
                    health,
                    0,
                    _maxHealth
                );
        }

        // =========================================================
        // DAMAGE
        // =========================================================

        /// <summary>
        /// Normal damage. Armor reduces the damage.
        /// </summary>
        public virtual void TakeDamage(int damage)
        {
            if (_isDead)
                return;

            if (damage <= 0)
                return;

            float finalDamage =
                damage *
                Mathf.Pow(
                    0.9f,
                    _armor
                );

            int actualDamage =
                Mathf.CeilToInt(
                    finalDamage
                );

            _currentHealth -= actualDamage;

            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
            }
        }

        // =========================================================
        // DAMAGE IGNORING ARMOR
        // =========================================================

        /// <summary>
        /// Damage that completely ignores enemy armor.
        /// Used by Mage Tower.
        /// </summary>
        public virtual void TakeDamageIgnoringArmor(
            int damage)
        {
            if (_isDead)
                return;

            if (damage <= 0)
                return;

            _currentHealth -= damage;

            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
            }
        }

        // =========================================================
        // SLOW
        // =========================================================

        /// <summary>
        /// Applies a temporary slow effect.
        ///
        /// Example:
        /// slowPercent = 0.3f
        /// means enemy speed becomes 70%.
        /// </summary>
        public virtual void ApplySlow(
            float slowPercent,
            float duration)
        {
            if (_isDead)
                return;

            slowPercent =
                Mathf.Clamp01(slowPercent);

            duration =
                Mathf.Max(
                    0f,
                    duration
                );

            _slowMultiplier =
                Mathf.Clamp01(
                    1f - slowPercent
                );

            _slowTimer =
                Mathf.Max(
                    _slowTimer,
                    duration
                );
        }

        // =========================================================
        // SLOW RESISTANCE
        // =========================================================

        public void SetCanBeSlowed(bool value)
        {
            // Reserved for slow resistance system
        }

        // =========================================================
        // MODIFIERS
        // =========================================================

        public void ModifyHealth(float multiplier)
        {
            _maxHealth =
                Mathf.RoundToInt(
                    _maxHealth * multiplier
                );

            _currentHealth = _maxHealth;
        }

        public void ModifyArmor(int addedArmor)
        {
            _armor += addedArmor;

            _armor =
                Mathf.Max(
                    _armor,
                    0
                );
        }

        public void ModifyAttack(float multiplier)
        {
            _attack =
                Mathf.RoundToInt(
                    _attack * multiplier
                );
        }

        public void ModifySpeed(float multiplier)
        {
            _moveSpeed *= multiplier;

            _moveSpeed =
                Mathf.Clamp(
                    _moveSpeed,
                    0.5f,
                    3.5f
                );
        }

        // =========================================================
        // DEATH
        // =========================================================

        protected virtual void Die()
        {
            if (_isDead)
                return;

            _isDead = true;

            int goldReward =
                enemyData != null
                    ? enemyData.GoldReward
                    : 10;

            EventBus<EnemyDiedEvent>.Raise(
                new EnemyDiedEvent(
                    gameObject,
                    goldReward
                )
            );

            Debug.Log(
                $"[EnemyHealth] {gameObject.name} died. " +
                $"Rewarded {goldReward} gold."
            );

            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnToPool(
                    gameObject
                );
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}