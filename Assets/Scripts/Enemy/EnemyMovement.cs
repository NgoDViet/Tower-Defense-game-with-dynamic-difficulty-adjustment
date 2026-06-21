using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Pooling;

namespace TowerDefense.Enemy
{
    /// <summary>
    /// Handles moving an enemy along a WaypointPath using speed from EnemyData.
    /// Raises a reached base event and destroys itself when finishing the path.
    /// </summary>
    public class EnemyMovement : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The EnemyData ScriptableObject containing stats.")]
        [SerializeField] private EnemyData enemyData;
        
        [Tooltip("The path for the enemy to follow.")]
        [SerializeField] private WaypointPath waypointPath;

        [Header("Movement Settings")]
        [Tooltip("Should the sprite rotate to face the direction of movement?")]
        [SerializeField] private bool rotateTowardsMovement = true;
        
        [Tooltip("Offset angle if the sprite's default 'front' faces a direction other than right (X+).")]
        [SerializeField] private float spriteAngleOffset = 0f;

        [Tooltip("Distance threshold to consider a waypoint reached.")]
        [SerializeField] private float waypointThreshold = 0.05f;

        private int _currentWaypointIndex = 0;
        private bool _isInitialized = false;

        public int CurrentWaypointIndex => _currentWaypointIndex;
        public WaypointPath ActivePath => waypointPath;

        private void Start()
        {
            // If waypoints and data are set in Inspector (useful for testing single instances in scene)
            if (waypointPath != null && enemyData != null)
            {
                Initialize(enemyData, waypointPath);
            }
        }

        /// <summary>
        /// Programmatically initializes the movement parameters (e.g. from a Wave Manager).
        /// </summary>
        public void Initialize(EnemyData data, WaypointPath path)
        {
            enemyData = data;
            waypointPath = path;
            _currentWaypointIndex = 0;
            _isInitialized = true;

            if (waypointPath.WaypointCount > 0)
            {
                // Snap to the starting waypoint immediately on spawn
                Transform startWaypoint = waypointPath.GetWaypoint(0);
                if (startWaypoint != null)
                {
                    transform.position = startWaypoint.position;
                    // Move target to the next waypoint
                    _currentWaypointIndex = 1;
                }
            }
        }

        private void Update()
        {
            if (!_isInitialized || waypointPath == null || enemyData == null) return;

            if (_currentWaypointIndex >= waypointPath.WaypointCount)
            {
                ReachBase();
                return;
            }

            Transform targetWaypoint = waypointPath.GetWaypoint(_currentWaypointIndex);
            if (targetWaypoint == null)
            {
                // Move to next if waypoint is missing
                _currentWaypointIndex++;
                return;
            }

            MoveTowards(targetWaypoint.position);
        }

        private void MoveTowards(Vector3 targetPosition)
        {
            float step = enemyData.MoveSpeed * Time.deltaTime;
            Vector3 currentPos = transform.position;

            // Perform movement
            transform.position = Vector3.MoveTowards(currentPos, targetPosition, step);

            // Handle 2D rotation towards movement target
            if (rotateTowardsMovement)
            {
                Vector3 direction = (targetPosition - currentPos).normalized;
                if (direction.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.AngleAxis(angle + spriteAngleOffset, Vector3.forward);
                }
            }

            // Check if reached waypoint
            if (Vector3.Distance(transform.position, targetPosition) <= waypointThreshold)
            {
                _currentWaypointIndex++;
            }
        }

        private void ReachBase()
        {
            _isInitialized = false;

            // Raise reached base event via the EventBus
            int damage = enemyData != null ? enemyData.DamageToBase : 1;
            EventBus<EnemyReachedBaseEvent>.Raise(new EnemyReachedBaseEvent(gameObject, damage));

            // Log event to console for verification
            Debug.Log($"[EnemyMovement] Enemy {gameObject.name} reached the base and dealt {damage} damage.");

            // Return to object pool or destroy if no pool exists
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
