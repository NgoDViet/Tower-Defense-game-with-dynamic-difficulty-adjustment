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
        private Vector3 _lastKnownPosition;
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
                _lastKnownPosition = _target.transform.position;
            }
            else
            {
                _lastKnownPosition = transform.position;
            }
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // Handle target loss (e.g. enemy died or got pooled already)
            if (_target == null || !_target.gameObject.activeSelf || _target.IsDead)
            {
                // Recycle immediately if target is gone to prevent orphan projectiles
                Recycle();
                return;
            }

            _lastKnownPosition = _target.transform.position;
            Vector3 currentPos = transform.position;

            // Move towards the target
            transform.position = Vector3.MoveTowards(currentPos, _lastKnownPosition, _speed * Time.deltaTime);

            // Rotate towards target movement vector
            if (rotateTowardsTarget)
            {
                Vector3 direction = (_lastKnownPosition - currentPos).normalized;
                if (direction.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.RadDeg;
                    transform.rotation = Quaternion.AngleAxis(angle + spriteAngleOffset, Vector3.forward);
                }
            }

            // Check if we hit the target
            if (Vector3.Distance(transform.position, _lastKnownPosition) <= impactDistanceThreshold)
            {
                HitTarget();
            }
        }

        private void HitTarget()
        {
            // Apply damage
            if (_target != null && !_target.IsDead)
            {
                _target.TakeDamage(_damage);
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
