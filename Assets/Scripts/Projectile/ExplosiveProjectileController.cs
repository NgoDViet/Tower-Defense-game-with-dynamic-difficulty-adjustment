using UnityEngine;
using TowerDefense.Enemy;
using TowerDefense.Pooling;
using TowerDefense.Effects;

namespace TowerDefense.Projectile
{
    /// <summary>
    /// Component managing an explosive projectile's movement towards a target enemy destination.
    /// Deals Area of Effect (AoE) damage to all enemies in a radius upon impact.
    /// </summary>
    public class ExplosiveProjectileController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Distance threshold from target to trigger impact.")]
        [SerializeField] private float impactDistanceThreshold = 0.1f;
        [SerializeField] private bool rotateTowardsTarget = true;
        [SerializeField] private float spriteAngleOffset = 0f;

        [Header("Explosion Settings")]
        [SerializeField] private float explosionRadius = 2.0f;
        [SerializeField] private Sprite explosionCircleSprite;
        [SerializeField] private Color explosionColor = new Color(1f, 0.4f, 0f, 0.8f);

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

                // Solve quadratic equation for intersection time t
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

                // Extrapolate destination based on valid intersection time
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

            // Move towards destination
            transform.position = Vector3.MoveTowards(currentPos, _targetDestination, _speed * Time.deltaTime);

            // Rotate towards target destination
            if (rotateTowardsTarget)
            {
                Vector3 direction = (_targetDestination - currentPos).normalized;
                if (direction.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.AngleAxis(angle + spriteAngleOffset, Vector3.forward);
                }
            }

            // Check impact distance
            if (Vector2.Distance(transform.position, _targetDestination) <= impactDistanceThreshold)
            {
                Explode();
            }
        }

        private void Explode()
        {
            // Spawn explosion visual
            if (explosionCircleSprite != null)
            {
                GameObject visualGO = new GameObject("ExplosionEffect");
                visualGO.transform.position = transform.position;
                ExplosionVisual visual = visualGO.AddComponent<ExplosionVisual>();
                visual.Initialize(explosionRadius, explosionColor, explosionCircleSprite);
            }

            // Find all enemies within the blast radius and damage them
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (Collider2D col in colliders)
            {
                EnemyHealth enemy = col.GetComponent<EnemyHealth>();
                if (enemy != null && !enemy.IsDead && enemy.gameObject.activeSelf)
                {
                    enemy.TakeDamage(_damage);
                }
            }

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
