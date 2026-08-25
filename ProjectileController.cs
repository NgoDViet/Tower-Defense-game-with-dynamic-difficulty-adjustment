using UnityEngine;
using TowerDefense.Enemy;
using TowerDefense.Pooling;
using TowerDefense.Core;

namespace TowerDefense.Projectile
{
    /// <summary>
    /// Component managing a projectile's movement towards a target enemy.
    /// Deals damage to the enemy on impact and recycles itself back to the ObjectPooler.
    /// </summary>
    public class ProjectileController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Distance threshold from target to trigger impact.")]
        [SerializeField] private float impactDistanceThreshold = 0.1f;
        [SerializeField] private bool rotateTowardsTarget = true;
        [SerializeField] private float spriteAngleOffset = 0f;

        private EnemyHealth _target;
        private int _damage;
        private float _speed;
        private Vector3 _targetDestination;
        private bool _isInitialized = false;

        /// <summary>
        /// Initializer called immediately after retrieving from the pool.
        /// </summary>
        public void Initialize(EnemyHealth target, int damage, float speed)
        {
            _target = target;
            _damage = damage;
            _speed = speed;
            _isInitialized = true;

            if (_target != null)
            {
                Vector3 targetPos = _target.transform.position;
                Vector3 projectilePos = transform.position;
                Vector3 relativePos = targetPos - projectilePos;

                // Calculate target velocity
                Vector3 targetVel = Vector3.zero;
                EnemyMovement movement = _target.GetComponent<EnemyMovement>();
                if (movement != null && movement.ActivePath != null)
                {
                    int wpIndex = movement.CurrentWaypointIndex;
                    if (wpIndex < movement.ActivePath.WaypointCount)
                    {
                        Transform targetWp = movement.ActivePath.GetWaypoint(wpIndex);
                        if (targetWp != null)
                        {
                            Vector3 dir = (targetWp.position - targetPos).normalized;
                            targetVel = dir * _target.MoveSpeed;
                        }
                    }
                }

                // Solve quadratic equation for intersection time t: a*t^2 + b*t + c = 0
                float a = targetVel.sqrMagnitude - _speed * _speed;
                float b = 2f * Vector3.Dot(relativePos, targetVel);
                float c = relativePos.sqrMagnitude;

                float discriminant = b * b - 4f * a * c;
                float t = -1f;

                if (discriminant >= 0f)
                {
                    float t1 = (-b - Mathf.Sqrt(discriminant)) / (2f * a);
                    float t2 = (-b + Mathf.Sqrt(discriminant)) / (2f * a);

                    if (t1 > 0f && t2 > 0f)
                    {
                        t = Mathf.Min(t1, t2);
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

                // Extrapolate destination based on valid time t (with a safety threshold of 5 seconds to prevent extreme paths)
                if (t > 0f && t < 5f)
                {
                    _targetDestination = targetPos + targetVel * t;
                }
                else
                {
                    _targetDestination = targetPos;
                }
            }
            else
            {
                _targetDestination = transform.position;
            }
        }

        private void Update()
        {
            if (!_isInitialized) return;

            Vector3 currentPos = transform.position;

            // Move towards the target destination
            transform.position = Vector3.MoveTowards(currentPos, _targetDestination, _speed * Time.deltaTime);

            // Rotate towards target destination vector
            if (rotateTowardsTarget)
            {
                Vector3 direction = (_targetDestination - currentPos).normalized;
                if (direction.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.AngleAxis(angle + spriteAngleOffset, Vector3.forward);
                }
            }

            // Check if we hit the target destination
            if (Vector2.Distance(transform.position, _targetDestination) <= impactDistanceThreshold)
            {
                HitTarget();
            }
        }

        private void HitTarget()
        {
            // Apply damage to original target if it is close enough to impact point
            if (_target != null && !_target.IsDead && _target.gameObject.activeSelf &&
                Vector2.Distance(transform.position, _target.transform.position) <= 1.0f)
            {
                _target.TakeDamage(_damage);
            }
            else
            {
                // Fallback: search for any active enemy within a 1.0f radius of the impact point
                Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 1.0f);
                foreach (Collider2D col in colliders)
                {
                    EnemyHealth enemy = col.GetComponent<EnemyHealth>();
                    if (enemy != null && !enemy.IsDead && enemy.gameObject.activeSelf)
                    {
                        enemy.TakeDamage(_damage);
                        break; // single target projectile
                    }
                }
            }

            // Spawn optional impact explosion/particles here in the future
            
            Recycle();
        }

        private void Recycle()
        {
            _isInitialized = false;
            _target = null;

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
