using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Pooling;

namespace TowerDefense.Enemy
{
    /// <summary>
    /// Base class managing enemy health, attack, armor and speed.
    /// Uses the global DifficultyManager.
    /// </summary>
    public class EnemyHealth : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        protected EnemyData enemyData;

        protected int _currentHealth;
        protected int _maxHealth;
        protected int _armor;
        protected int _attack;
        protected float _moveSpeed;
        protected bool _isDead;

        // =========================================================
        // PUBLIC PROPERTIES
        // =========================================================

        public int Armor => _armor;

        public int Attack => _attack;

        public float MoveSpeed => _moveSpeed;

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
        // HEALTH
        // =========================================================

        public void SetCurrentHealth(int health)
        {
            _currentHealth = Mathf.Clamp(
                health,
                0,
                _maxHealth
            );
        }

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
        // ENABLE
        // =========================================================

        protected virtual void OnEnable()
        {
            _isDead = false;

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

            enemyData = data;

            float healthMultiplier =
                DifficultyManager.HealthMultiplier;

            float speedMultiplier =
                DifficultyManager.SpeedMultiplier;

            // HP
            _maxHealth =
                data.GetHealthWithMultiplier(
                    healthMultiplier
                );

            _currentHealth = _maxHealth;

            // Attack
            _attack =
                data.GetBaseAttackValue();

            // Armor
            _armor =
                data.GetBaseArmorValue();

            // Speed
            _moveSpeed =
                data.GetSpeedWithMultiplier(
                    speedMultiplier
                );

            _isDead = false;

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
        // INITIALIZE WITH MULTIPLIERS
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

            _maxHealth =
                data.GetHealthWithMultiplier(
                    healthMultiplier
                );

            _currentHealth = _maxHealth;

            _attack =
                data.GetBaseAttackValue();

            _armor =
                data.GetBaseArmorValue();

            _moveSpeed =
                data.GetSpeedWithMultiplier(
                    speedMultiplier
                );

            _isDead = false;

            Debug.Log(
                $"[EnemyHealth] {gameObject.name} initialized | " +
                $"HP: {_maxHealth} | " +
                $"Speed: {_moveSpeed:F2}"
            );
        }

        // =========================================================
        // COMPATIBILITY INITIALIZE
        // =========================================================

        public virtual void Initialize(
            EnemyData data,
            int difficulty = 1)
        {
            // Ignore old difficulty number.
            // Global DifficultyManager is now the source of truth.

            InitializeWithCurrentDifficulty(data);
        }

        // =========================================================
        // DAMAGE
        // =========================================================

        public virtual void TakeDamage(int damage)
        {
            if (_isDead)
                return;

            float finalDamage =
                damage *
                Mathf.Pow(
                    0.9f,
                    _armor
                );

            _currentHealth -=
                Mathf.CeilToInt(
                    finalDamage
                );

            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
            }
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

            _armor = Mathf.Max(
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

            // Không giới hạn 3.5 để Hell vẫn có thể
            // tăng tốc đúng theo multiplier.
            _moveSpeed = Mathf.Max(
                _moveSpeed,
                0.1f
            );
        }

        public void SetCanBeSlowed(bool value)
        {
            // Reserved for slow resistance system
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