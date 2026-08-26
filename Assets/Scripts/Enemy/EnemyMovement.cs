using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Pooling;

namespace TowerDefense.Enemy
{
    /// <summary>
    /// Moves an enemy along WaypointPath.
    /// When enemy reaches the final waypoint,
    /// EnemyReachedBaseEvent is raised.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private EnemyData enemyData;

        [SerializeField]
        private WaypointPath waypointPath;

        // =========================================================
        // MOVEMENT SETTINGS
        // =========================================================

        [Header("Movement Settings")]
        [SerializeField]
        private bool rotateTowardsMovement = true;

        [SerializeField]
        private float spriteAngleOffset = 0f;

        [SerializeField]
        private float waypointThreshold = 0.05f;

        // =========================================================
        // INTERNAL
        // =========================================================

        private EnemyHealth enemyHealth;

        private int _currentWaypointIndex = 0;

        private bool _isInitialized = false;

        // =========================================================
        // PUBLIC
        // =========================================================

        public int CurrentWaypointIndex =>
            _currentWaypointIndex;

        public WaypointPath ActivePath =>
            waypointPath;

        // =========================================================
        // AWAKE
        // =========================================================

        private void Awake()
        {
            enemyHealth =
                GetComponent<EnemyHealth>();

            if (enemyHealth == null)
            {
                Debug.LogError(
                    "[EnemyMovement] " +
                    "EnemyHealth is required."
                );

                enabled = false;
            }
        }

        // =========================================================
        // START
        // =========================================================

        private void Start()
        {
            if (
                waypointPath != null &&
                enemyData != null
            )
            {
                Initialize(
                    enemyData,
                    waypointPath
                );
            }
        }

        // =========================================================
        // INITIALIZE
        // =========================================================

        public void Initialize(
            EnemyData data,
            WaypointPath path)
        {
            if (data == null)
            {
                Debug.LogError(
                    "[EnemyMovement] " +
                    "EnemyData is NULL."
                );

                return;
            }

            if (path == null)
            {
                Debug.LogError(
                    "[EnemyMovement] " +
                    "WaypointPath is NULL."
                );

                return;
            }

            enemyData = data;

            waypointPath = path;

            _currentWaypointIndex = 0;

            _isInitialized = true;

            // -----------------------------------------------------
            // START POSITION
            // -----------------------------------------------------

            if (
                waypointPath.WaypointCount > 0
            )
            {
                Transform startWaypoint =
                    waypointPath.GetWaypoint(0);

                if (startWaypoint != null)
                {
                    transform.position =
                        startWaypoint.position;

                    _currentWaypointIndex = 1;
                }
            }
        }

        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (
                !_isInitialized ||
                waypointPath == null ||
                enemyData == null ||
                enemyHealth == null
            )
            {
                return;
            }

            // -----------------------------------------------------
            // REACHED END
            // -----------------------------------------------------

            if (
                _currentWaypointIndex >=
                waypointPath.WaypointCount
            )
            {
                ReachBase();
                return;
            }

            // -----------------------------------------------------
            // TARGET
            // -----------------------------------------------------

            Transform targetWaypoint =
                waypointPath.GetWaypoint(
                    _currentWaypointIndex
                );

            if (targetWaypoint == null)
            {
                _currentWaypointIndex++;
                return;
            }

            MoveTowards(
                targetWaypoint.position
            );
        }

        // =========================================================
        // MOVE
        // =========================================================

        private void MoveTowards(
            Vector3 targetPosition)
        {
            if (enemyHealth == null)
                return;

            float step =
                enemyHealth.MoveSpeed *
                Time.deltaTime;

            Vector3 currentPos =
                transform.position;

            transform.position =
                Vector3.MoveTowards(
                    currentPos,
                    targetPosition,
                    step
                );

            // -----------------------------------------------------
            // ROTATION
            // -----------------------------------------------------

            if (rotateTowardsMovement)
            {
                Vector3 direction =
                    (
                        targetPosition -
                        currentPos
                    ).normalized;

                if (
                    direction.sqrMagnitude >
                    0.001f
                )
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
            // WAYPOINT REACHED
            // -----------------------------------------------------

            if (
                Vector2.Distance(
                    transform.position,
                    targetPosition
                ) <= waypointThreshold
            )
            {
                _currentWaypointIndex++;
            }
        }

        // =========================================================
        // REACH BASE
        // =========================================================

        private void ReachBase()
        {
            if (!_isInitialized)
                return;

            _isInitialized = false;

            int damage =
                enemyHealth != null
                    ? enemyHealth.Attack
                    : 1;

            Debug.Log(
                $"[EnemyMovement] " +
                $"{gameObject.name} reached base | " +
                $"Damage={damage} | " +
                $"OneLife={DifficultyManager.OneLifeMode}"
            );

            // -----------------------------------------------------
            // SEND EVENT
            // GameManager decides:
            // normal -> subtract HP
            // one life -> instant defeat
            // -----------------------------------------------------

            EventBus<EnemyReachedBaseEvent>.Raise(
                new EnemyReachedBaseEvent(
                    gameObject,
                    damage
                )
            );

            // -----------------------------------------------------
            // RETURN TO POOL
            // -----------------------------------------------------

            if (
                ObjectPooler.Instance != null
            )
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