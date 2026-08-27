using UnityEngine;
using TowerDefense.Enemy;
using TowerDefense.Pooling;

namespace TowerDefense.Projectile
{
    /// <summary>
    /// Controls projectile movement, target tracking,
    /// damage, slow effect and object pooling.
    /// </summary>
    public class ProjectileController : MonoBehaviour
    {
        // =========================================================
        // MOVEMENT SETTINGS
        // =========================================================

        [Header("Movement Settings")]

        [Tooltip("Distance from destination required to trigger impact.")]
        [SerializeField]
        private float impactDistanceThreshold = 0.1f;

        [Tooltip("Should projectile rotate towards its destination?")]
        [SerializeField]
        private bool rotateTowardsTarget = true;

        [Tooltip("Rotation offset for projectile sprite.")]
        [SerializeField]
        private float spriteAngleOffset = 0f;


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


        // =========================================================
        // STATE
        // =========================================================

        private bool _isInitialized;


        // =========================================================
        // SLOW
        // =========================================================

        private bool _applySlow;

        private float _slowPercent;

        private float _slowDuration;


        // =========================================================
        // INITIALIZE
        // =========================================================

        /// <summary>
        /// Initializes projectile with target, damage,
        /// movement speed and optional slow effect.
        /// </summary>
        public void Initialize(
            EnemyHealth target,
            int damage,
            float speed,
            bool applySlow = false,
            float slowPercent = 0f,
            float slowDuration = 0f)
        {
            // -----------------------------------------------------
            // RESET OLD STATE
            // -----------------------------------------------------

            _target = target;

            _damage = Mathf.Max(0, damage);

            _speed = Mathf.Max(0f, speed);

            _applySlow = applySlow;

            _slowPercent =
                Mathf.Clamp01(slowPercent);

            _slowDuration =
                Mathf.Max(0f, slowDuration);

            _isInitialized = true;


            // -----------------------------------------------------
            // CALCULATE TARGET DESTINATION
            // -----------------------------------------------------

            if (_target == null)
            {
                _targetDestination =
                    transform.position;

                return;
            }


            Vector3 targetPosition =
                _target.transform.position;


            // -----------------------------------------------------
            // PREDICT ENEMY MOVEMENT
            // -----------------------------------------------------

            Vector3 targetVelocity =
                Vector3.zero;

            EnemyMovement movement =
                _target.GetComponent<EnemyMovement>();


            if (movement != null &&
                movement.ActivePath != null)
            {
                int waypointIndex =
                    movement.CurrentWaypointIndex;


                if (waypointIndex <
                    movement.ActivePath.WaypointCount)
                {
                    Transform waypoint =
                        movement.ActivePath.GetWaypoint(
                            waypointIndex
                        );


                    if (waypoint != null)
                    {
                        Vector3 direction =
                            (
                                waypoint.position -
                                targetPosition
                            ).normalized;


                        targetVelocity =
                            direction *
                            _target.MoveSpeed;
                    }
                }
            }


            // -----------------------------------------------------
            // INTERCEPT CALCULATION
            // -----------------------------------------------------

            Vector3 relativePosition =
                targetPosition -
                transform.position;


            float projectileSpeed =
                _speed;


            float a =
                targetVelocity.sqrMagnitude -
                projectileSpeed * projectileSpeed;


            float b =
                2f *
                Vector3.Dot(
                    relativePosition,
                    targetVelocity
                );


            float c =
                relativePosition.sqrMagnitude;


            float interceptTime = -1f;


            // -----------------------------------------------------
            // CASE: A IS ALMOST ZERO
            // -----------------------------------------------------

            if (Mathf.Abs(a) < 0.0001f)
            {
                if (Mathf.Abs(b) > 0.0001f)
                {
                    float t =
                        -c / b;


                    if (t > 0f)
                    {
                        interceptTime = t;
                    }
                }
            }


            // -----------------------------------------------------
            // NORMAL QUADRATIC SOLUTION
            // -----------------------------------------------------

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
                        (-b - sqrtDiscriminant) /
                        (2f * a);


                    float t2 =
                        (-b + sqrtDiscriminant) /
                        (2f * a);


                    if (t1 > 0f &&
                        t2 > 0f)
                    {
                        interceptTime =
                            Mathf.Min(
                                t1,
                                t2
                            );
                    }
                    else if (t1 > 0f)
                    {
                        interceptTime = t1;
                    }
                    else if (t2 > 0f)
                    {
                        interceptTime = t2;
                    }
                }
            }


            // -----------------------------------------------------
            // LIMIT PREDICTION
            // -----------------------------------------------------

            if (interceptTime > 0f &&
                interceptTime < 5f)
            {
                _targetDestination =
                    targetPosition +
                    targetVelocity *
                    interceptTime;
            }
            else
            {
                _targetDestination =
                    targetPosition;
            }


            // -----------------------------------------------------
            // DEBUG
            // -----------------------------------------------------

            Debug.Log(
                $"[ProjectileController] " +
                $"{gameObject.name} initialized | " +
                $"Target={_target.gameObject.name} | " +
                $"Damage={_damage} | " +
                $"Speed={_speed:F2} | " +
                $"Slow={_applySlow} | " +
                $"SlowPercent={_slowPercent * 100f:F0}% | " +
                $"SlowDuration={_slowDuration:F2}s"
            );
        }


        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (!_isInitialized)
                return;


            // -----------------------------------------------------
            // TARGET CHECK
            // -----------------------------------------------------

            if (_target == null)
            {
                Recycle();
                return;
            }


            if (_target.IsDead ||
                !_target.gameObject.activeSelf)
            {
                Recycle();
                return;
            }


            // -----------------------------------------------------
            // MOVE
            // -----------------------------------------------------

            Vector3 currentPosition =
                transform.position;


            float movementStep =
                _speed *
                Time.deltaTime;


            transform.position =
                Vector3.MoveTowards(
                    currentPosition,
                    _targetDestination,
                    movementStep
                );


            // -----------------------------------------------------
            // ROTATION
            // -----------------------------------------------------

            if (rotateTowardsTarget)
            {
                Vector3 direction =
                    _targetDestination -
                    currentPosition;


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


            // -----------------------------------------------------
            // IMPACT CHECK
            // -----------------------------------------------------

            float distance =
                Vector2.Distance(
                    transform.position,
                    _targetDestination
                );


            if (distance <=
                impactDistanceThreshold)
            {
                HitTarget();
            }
        }


        // =========================================================
        // HIT TARGET
        // =========================================================

        private void HitTarget()
        {
            if (!_isInitialized)
                return;


            // -----------------------------------------------------
            // TRY ORIGINAL TARGET
            // -----------------------------------------------------

            if (_target != null &&
                !_target.IsDead &&
                _target.gameObject.activeSelf)
            {
                float distanceToTarget =
                    Vector2.Distance(
                        transform.position,
                        _target.transform.position
                    );


                if (distanceToTarget <= 1f)
                {
                    ApplyDamageAndEffects(
                        _target
                    );

                    Recycle();

                    return;
                }
            }


            // -----------------------------------------------------
            // FALLBACK SEARCH
            // -----------------------------------------------------

            Collider2D[] colliders =
                Physics2D.OverlapCircleAll(
                    transform.position,
                    1f
                );


            foreach (Collider2D collider in colliders)
            {
                if (collider == null)
                    continue;


                EnemyHealth enemy =
                    collider.GetComponent<EnemyHealth>();


                if (enemy == null)
                {
                    enemy =
                        collider.GetComponentInParent<EnemyHealth>();
                }


                if (enemy == null)
                    continue;


                if (enemy.IsDead)
                    continue;


                if (!enemy.gameObject.activeSelf)
                    continue;


                ApplyDamageAndEffects(
                    enemy
                );


                break;
            }


            // -----------------------------------------------------
            // RECYCLE
            // -----------------------------------------------------

            Recycle();
        }


        // =========================================================
        // DAMAGE + EFFECTS
        // =========================================================

        private void ApplyDamageAndEffects(
            EnemyHealth enemy)
        {
            if (enemy == null)
                return;


            if (enemy.IsDead)
                return;


            // -----------------------------------------------------
            // DAMAGE
            // -----------------------------------------------------

            if (_damage > 0)
            {
                enemy.TakeDamage(
                    _damage
                );
            }


            // -----------------------------------------------------
            // SLOW
            // -----------------------------------------------------

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
            // -----------------------------------------------------
            // PREVENT DOUBLE RECYCLE
            // -----------------------------------------------------

            if (!_isInitialized)
                return;


            _isInitialized = false;


            // -----------------------------------------------------
            // RESET STATE
            // -----------------------------------------------------

            _target = null;

            _damage = 0;

            _speed = 0f;

            _targetDestination =
                Vector3.zero;

            _applySlow = false;

            _slowPercent = 0f;

            _slowDuration = 0f;


            // -----------------------------------------------------
            // RETURN TO OBJECT POOL
            // -----------------------------------------------------

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


        // =========================================================
        // ENABLE
        // =========================================================

        private void OnEnable()
        {
            _isInitialized = false;

            _target = null;

            _damage = 0;

            _speed = 0f;

            _targetDestination =
                transform.position;

            _applySlow = false;

            _slowPercent = 0f;

            _slowDuration = 0f;
        }


        // =========================================================
        // GIZMOS
        // =========================================================

        private void OnDrawGizmosSelected()
        {
            Gizmos.color =
                Color.yellow;


            Gizmos.DrawWireSphere(
                transform.position,
                impactDistanceThreshold
            );
        }
    }
}