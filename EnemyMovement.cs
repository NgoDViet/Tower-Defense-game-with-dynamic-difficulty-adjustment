using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Pooling;

namespace TowerDefense.Enemy
{
    /// <summary>
    /// Handles moving an enemy along a WaypointPath using speed from EnemyHealth.
    /// Raises a reached base event and destroys itself when finishing the path.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
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

        private EnemyHealth enemyHealth;
        private int _currentWaypointIndex = 0;
        private bool _isInitialized = false;

        public int CurrentWaypointIndex => _currentWaypointIndex;
        public WaypointPath ActivePath => waypointPath;

        private void Awake()
        {
            enemyHealth = GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                Debug.LogError("[EnemyMovement] EnemyHealth component is required but missing.");
                enabled = false;
            }
        }

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
            if (data == null)
            {
                Debug.LogError("[EnemyMovement] Cannot initialize movement without EnemyData.");
                return;
            }

            if (path == null)
            {
                Debug.LogError("[EnemyMovement] Cannot initialize movement without a WaypointPath.");
                return;
            }

            enemyData = data;
            waypointPath = path;
            _currentWaypointIndex = 0;
            _isInitialized = true;

            if (waypointPath.WaypointCount > 0)
            {
                Transform startWaypoint = waypointPath.GetWaypoint(0);
                if (startWaypoint != null)
                {
                    transform.position = startWaypoint.position;
                    _currentWaypointIndex = 1;
                }
            }
        }

        private void Update()
        {
            if (!_isInitialized || waypointPath == null || enemyData == null || enemyHealth == null) return;

            if (_currentWaypointIndex >= waypointPath.WaypointCount)
            {
                ReachBase();
                return;
            }

            Transform targetWaypoint = waypointPath.GetWaypoint(_currentWaypointIndex);
            if (targetWaypoint == null)
            {
                _currentWaypointIndex++;
                return;
            }

            MoveTowards(targetWaypoint.position);
        }

        private void MoveTowards(Vector3 targetPosition)
        {
            if (enemyHealth == null) return;

            // Fetch current speed from EnemyHealth instead of EnemyData
            float step = enemyHealth.MoveSpeed * Time.deltaTime;
            Vector3 currentPos = transform.position;

            transform.position = Vector3.MoveTowards(currentPos, targetPosition, step);

            if (rotateTowardsMovement)
            {
                Vector3 direction = (targetPosition - currentPos).normalized;
                if (direction.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.AngleAxis(angle + spriteAngleOffset, Vector3.forward);
                }
            }

            if (Vector3.Distance(transform.position, targetPosition) <= waypointThreshold)
            {
                _currentWaypointIndex++;
            }
        }

        private void ReachBase()
        {
            _isInitialized = false;

            int damage = enemyHealth != null ? enemyHealth.Attack : 1;
            EventBus<EnemyReachedBaseEvent>.Raise(new EnemyReachedBaseEvent(gameObject, damage));

            Debug.Log($"[EnemyMovement] Enemy {gameObject.name} reached the base and dealt {damage} damage.");

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
