using UnityEngine;
using TowerDefense.Enemy;
using TowerDefense.Pooling;

namespace TowerDefense.Projectile
{
    /// <summary>
    /// Component managing a projectile's movement towards a target enemy.
    /// Deals damage to the enemy on impact and optionally applies a slow effect.
    /// Recycles itself back to the ObjectPooler after impact.
    /// </summary>
    public class ProjectileController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Distance threshold from target to trigger impact.")]
        [SerializeField] private float impactDistanceThreshold = 0.1f;

        [SerializeField] private bool rotateTowardsTarget = true;

        [SerializeField] private float spriteAngleOffset = 0f;

        // =========================================================
        // TARGET
        // =========================================================

        private EnemyHealth _target;

        // =========================================================
        // DAMAGE
        // =========================================================

        private int _damage;

        // =========================================================
        // MOVEMENT
        // =========================================================

        private float _speed;

        private Vector3 _targetDestination;

        private bool _isInitialized = false;

        // =========================================================
        // SLOW SETTINGS
        // =========================================================

        private bool _applySlow = false;

        private float _slowPercent = 0f;

        private float _slowDuration = 0f;

        // =========================================================
        // INITIALIZE
        // =========================================================

        /// <summary>
        /// Initializes the projectile.
        /// </summary>
        public void Initialize(
            EnemyHealth target,
            int damage,
            float speed,
            bool applySlow = false,
            float slowPercent = 0f,
            float slowDuration = 0f)
        {
            _target = target;

            _damage = damage;

            _speed = speed;

            _applySlow = applySlow;

            _slowPercent = Mathf.Clamp01(slowPercent);

            _slowDuration = Mathf.Max(0f, slowDuration);

            _isInitialized = true;

            // =====================================================
            // CALCULATE TARGET DESTINATION
            // =====================================================

            if (_target != null)
            {
                Vector3 targetPos =
                    _target.transform.position;

                Vector3 projectilePos =
                    transform.position;

                Vector3 relativePos =
                    targetPos - projectilePos;

                // -------------------------------------------------
                // Calculate target velocity
                // -------------------------------------------------

                Vector3 targetVel =
                    Vector3.zero;

                EnemyMovement movement =
                    _target.GetComponent<EnemyMovement>();

                if (movement != null &&
                    movement.ActivePath != null)
                {
                    int wpIndex =
                        movement.CurrentWaypointIndex;

                    if (wpIndex <
                        movement.ActivePath.WaypointCount)
                    {
                        Transform targetWp =
                            movement.ActivePath
                            .GetWaypoint(wpIndex);

                        if (targetWp != null)
                        {
                            Vector3 dir =
                                (targetWp.position -
                                 targetPos).normalized;

                            targetVel =
                                dir *
                                _target.MoveSpeed;
                        }
                    }
                }

                // -------------------------------------------------
                // Solve quadratic equation
                // -------------------------------------------------

                float a =
                    targetVel.sqrMagnitude -
                    _speed * _speed;

                float b =
                    2f *
                    Vector3.Dot(
                        relativePos,
                        targetVel
                    );

                float c =
                    relativePos.sqrMagnitude;

                float t = -1f;

                // -------------------------------------------------
                // Prevent division by zero when a is very small
                // -------------------------------------------------

                if (Mathf.Abs(a) < 0.0001f)
                {
                    if (Mathf.Abs(b) > 0.0001f)
                    {
                        float linearT =
                            -c / b;

                        if (linearT > 0f)
                        {
                            t = linearT;
                        }
                    }
                }
                else
                {
                    float discriminant =
                        b * b -
                        4f * a * c;

                    if (discriminant >= 0f)
                    {
                        float sqrtDiscriminant =
                            Mathf.Sqrt(
                                discriminant
                            );

                        float t1 =
                            (-b -
                             sqrtDiscriminant) /
                            (2f * a);

                        float t2 =
                            (-b +
                             sqrtDiscriminant) /
                            (2f * a);

                        if (t1 > 0f &&
                            t2 > 0f)
                        {
                            t =
                                Mathf.Min(
                                    t1,
                                    t2
                                );
                        }
                        else if (t1 > 0f)
                        {
                            t = t1;
                        }
                        else if (t2 > 0f)
                        {
                            t = t2;
                        }
                    }
                }

                // -------------------------------------------------
                // Calculate predicted destination
                // -------------------------------------------------

                if (t > 0f && t < 5f)
                {
                    _targetDestination =
                        targetPos +
                        targetVel * t;
                }
                else
                {
                    _targetDestination =
                        targetPos;
                }
            }
            else
            {
                _targetDestination =
                    transform.position;
            }

            // =====================================================
            // DEBUG
            // =====================================================

            Debug.Log(
                $"[ProjectileController] " +
                $"{gameObject.name} initialized | " +
                $"Damage: {_damage} | " +
                $"Speed: {_speed} | " +
                $"Slow: {_applySlow} | " +
                $"SlowPercent: {_slowPercent} | " +
                $"SlowDuration: {_slowDuration}"
            );
        }

        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (!_isInitialized)
                return;

            Vector3 currentPos =
                transform.position;

            // =====================================================
            // MOVE
            // =====================================================

            transform.position =
                Vector3.MoveTowards(
                    currentPos,
                    _targetDestination,
                    _speed * Time.deltaTime
                );

            // =====================================================
            // ROTATION
            // =====================================================

            if (rotateTowardsTarget)
            {
                Vector3 direction =
                    (_targetDestination -
                     currentPos).normalized;

                if (direction.sqrMagnitude >
                    0.001f)
                {
                    float angle =
                        Mathf.Atan2(
                            direction.y,
                            direction.x
                        ) *
                        Mathf.Rad2Deg;

                    transform.rotation =
                        Quaternion.AngleAxis(
                            angle +
                            spriteAngleOffset,
                            Vector3.forward
                        );
                }
            }

            // =====================================================
            // HIT CHECK
            // =====================================================

            if (Vector2.Distance(
                    transform.position,
                    _targetDestination
                ) <= impactDistanceThreshold)
            {
                HitTarget();
            }
        }

        // =========================================================
        // HIT TARGET
        // =========================================================

        private void HitTarget()
        {
            // =====================================================
            // ORIGINAL TARGET
            // =====================================================

            if (_target != null &&
                !_target.IsDead &&
                _target.gameObject.activeSelf &&
                Vector2.Distance(
                    transform.position,
                    _target.transform.position
                ) <= 1.0f)
            {
                ApplyDamageAndEffects(_target);
            }
            else
            {
                // =================================================
                // FALLBACK SEARCH
                // =================================================

                Collider2D[] colliders =
                    Physics2D.OverlapCircleAll(
                        transform.position,
                        1.0f
                    );

                foreach (Collider2D col in colliders)
                {
                    EnemyHealth enemy =
                        col.GetComponent<EnemyHealth>();

                    if (enemy != null &&
                        !enemy.IsDead &&
                        enemy.gameObject.activeSelf)
                    {
                        ApplyDamageAndEffects(
                            enemy
                        );

                        break;
                    }
                }
            }

            // =====================================================
            // RECYCLE
            // =====================================================

            Recycle();
        }

        // =========================================================
        // DAMAGE + EFFECTS
        // =========================================================

        private void ApplyDamageAndEffects(
            EnemyHealth enemy)
        {
            if (enemy == null ||
                enemy.IsDead)
            {
                return;
            }

            // =====================================================
            // DAMAGE
            // =====================================================

            enemy.TakeDamage(_damage);

            // =====================================================
            // SLOW
            // =====================================================

            if (_applySlow &&
                _slowPercent > 0f &&
                _slowDuration > 0f)
            {
                enemy.ApplySlow(
                    _slowPercent,
                    _slowDuration
                );

                Debug.Log(
                    $"[ProjectileController] " +
                    $"{enemy.gameObject.name} " +
                    $"slowed by " +
                    $"{_slowPercent * 100f:F0}% " +
                    $"for {_slowDuration:F1}s."
                );
            }
        }

        // =========================================================
        // RECYCLE
        // =========================================================

        private void Recycle()
        {
            _isInitialized = false;

            _target = null;

            _damage = 0;

            _speed = 0f;

            _targetDestination =
                Vector3.zero;

            _applySlow = false;

            _slowPercent = 0f;

            _slowDuration = 0f;

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