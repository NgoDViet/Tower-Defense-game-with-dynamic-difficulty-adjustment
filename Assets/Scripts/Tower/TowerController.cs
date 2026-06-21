using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Enemy;
using TowerDefense.Pooling;
using TowerDefense.Projectile;

namespace TowerDefense.Tower
{
    /// <summary>
    /// Component managing a defensive tower's targeting logic, aiming, and firing mechanism.
    /// Finds enemies within range and shoots projectiles at them on a cooldown.
    /// </summary>
    public class TowerController : MonoBehaviour
    {
        public enum TargetingMode
        {
            First,
            Closest,
            Strongest
        }

        [Header("References")]
        [Tooltip("The configuration data for this tower.")]
        [SerializeField] private TowerData towerData;

        [Tooltip("Optional transform where projectiles are spawned.")]
        [SerializeField] private Transform shootPoint;

        [Header("Targeting Settings")]
        [SerializeField] private TargetingMode targetingMode = TargetingMode.First;
        [SerializeField] private LayerMask enemyLayerMask;
        [Tooltip("How frequently in seconds the target is re-evaluated.")]
        [SerializeField] private float targetReevaluateInterval = 0.1f;

        [Header("Rotation (Optional)")]
        [Tooltip("Should the tower rotate towards the target?")]
        [SerializeField] private bool rotateTowardsTarget = true;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float spriteAngleOffset = 0f;

        private EnemyHealth _targetEnemy;
        private float _fireCooldownTimer = 0f;
        private float _targetReevaluateTimer = 0f;

        public EnemyHealth TargetEnemy => _targetEnemy;

        private void Start()
        {
            if (shootPoint == null)
            {
                shootPoint = transform;
            }
        }

        private void Update()
        {
            // Only execute logic if game is actively running
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
            if (towerData == null) return;

            // Re-evaluate target on interval to save performance
            _targetReevaluateTimer -= Time.deltaTime;
            if (_targetReevaluateTimer <= 0f)
            {
                UpdateTarget();
                _targetReevaluateTimer = targetReevaluateInterval;
            }

            // Verify target validity (in range, active, alive)
            if (!IsTargetValid(_targetEnemy))
            {
                _targetEnemy = null;
            }

            // Aim and Fire
            if (_targetEnemy != null)
            {
                if (rotateTowardsTarget)
                {
                    AimAtTarget(_targetEnemy.transform.position);
                }

                _fireCooldownTimer -= Time.deltaTime;
                if (_fireCooldownTimer <= 0f)
                {
                    Shoot();
                    _fireCooldownTimer = 1f / towerData.FireRate;
                }
            }
            else
            {
                // Reset cooldown or decrement it slowly when no target is present
                _fireCooldownTimer = Mathf.Max(0f, _fireCooldownTimer - Time.deltaTime);
            }
        }

        private void UpdateTarget()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, towerData.Range, enemyLayerMask);
            if (colliders.Length == 0)
            {
                _targetEnemy = null;
                return;
            }

            EnemyHealth bestTarget = null;
            float bestMetric = float.MinValue;

            foreach (Collider2D col in colliders)
            {
                EnemyHealth enemy = col.GetComponent<EnemyHealth>();
                if (enemy == null || enemy.IsDead) continue;

                float metric = 0f;
                EnemyMovement movement = col.GetComponent<EnemyMovement>();

                switch (targetingMode)
                {
                    case TargetingMode.First:
                        if (movement != null && movement.ActivePath != null)
                        {
                            // Metric: current waypoint index combined with progress to the next waypoint.
                            // Higher value means further along the path.
                            int wpIndex = movement.CurrentWaypointIndex;
                            Transform targetWp = movement.ActivePath.GetWaypoint(wpIndex);
                            float distToWp = targetWp != null ? Vector3.Distance(movement.transform.position, targetWp.position) : 0f;
                            metric = (wpIndex * 1000f) - distToWp;
                        }
                        else
                        {
                            // Fallback if movement info isn't available: closest to tower
                            metric = -Vector3.Distance(transform.position, col.transform.position);
                        }
                        break;

                    case TargetingMode.Closest:
                        metric = -Vector3.Distance(transform.position, col.transform.position);
                        break;

                    case TargetingMode.Strongest:
                        metric = enemy.CurrentHealth;
                        break;
                }

                if (metric > bestMetric)
                {
                    bestMetric = metric;
                    bestTarget = enemy;
                }
            }

            _targetEnemy = bestTarget;
        }

        private bool IsTargetValid(EnemyHealth enemy)
        {
            if (enemy == null) return false;
            if (enemy.IsDead || !enemy.gameObject.activeSelf) return false;
            
            // Check range
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            return dist <= towerData.Range;
        }

        private void AimAtTarget(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle + spriteAngleOffset, Vector3.forward);
            }
        }

        private void Shoot()
        {
            if (towerData.ProjectilePrefab == null)
            {
                Debug.LogError($"[TowerController] {gameObject.name} is missing a Projectile Prefab configuration!");
                return;
            }

            // Spawn projectile from pool
            GameObject projectileObj = ObjectPooler.Instance.GetPooledObject(
                towerData.ProjectilePrefab, 
                shootPoint.position, 
                shootPoint.rotation
            );

            // Initialize projectile
            ProjectileController projectile = projectileObj.GetComponent<ProjectileController>();
            if (projectile != null)
            {
                projectile.Initialize(_targetEnemy, towerData.Damage, towerData.ProjectileSpeed);
            }
            else
            {
                Debug.LogWarning($"[TowerController] Spawned projectile {projectileObj.name} does not have a ProjectileController attached.");
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw targeting range circle in Scene View
            Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
            Gizmos.DrawSphere(transform.position, towerData != null ? towerData.Range : 5f);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, towerData != null ? towerData.Range : 5f);
        }
    }
}
