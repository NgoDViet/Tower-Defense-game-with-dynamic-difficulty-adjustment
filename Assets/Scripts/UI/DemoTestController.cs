using UnityEngine;
using UnityEngine.InputSystem;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Enemy;
using TowerDefense.Pooling;
using TowerDefense.Tower;

namespace TowerDefense.UI
{
    /// <summary>
    /// Coordinates testing functionality for the LevelDemo scene:
    /// Spawning enemies on click, and dragging/placing tower previews via the new Input System.
    /// </summary>
    public class DemoTestController : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private GameObject towerPrefab;
        [SerializeField] private TowerData towerData;

        // Properties for editor setup bypass
        public GameObject EnemyPrefab { get => enemyPrefab; set => enemyPrefab = value; }
        public EnemyData EnemyData { get => enemyData; set => enemyData = value; }
        public GameObject TowerPrefab { get => towerPrefab; set => towerPrefab = value; }
        public TowerData TowerData { get => towerData; set => towerData = value; }

        [Header("References")]
        [SerializeField] private WaypointPath waypointPath;

        public WaypointPath WaypointPath { get => waypointPath; set => waypointPath = value; }

        private GameObject _activeTowerPreview;
        private bool _isPlacingTower = false;

        private void Update()
        {
            if (_isPlacingTower && _activeTowerPreview != null)
            {
                // Get mouse screen coordinates using Input System (with legacy fallback)
                Vector2 mouseScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
                
                // Project screen point into 2D world space
                if (Camera.main != null)
                {
                    Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Camera.main.nearClipPlane));
                    mouseWorldPos.z = 0f;

                    // Update position of the preview ghost
                    _activeTowerPreview.transform.position = mouseWorldPos;
                }

                // Left click places the tower
                bool leftClick = Mouse.current != null ? Mouse.current.leftButton.wasPressedThisFrame : Input.GetMouseButtonDown(0);
                if (leftClick)
                {
                    PlaceTower();
                }

                // Right click cancels the placement
                bool rightClick = Mouse.current != null ? Mouse.current.rightButton.wasPressedThisFrame : Input.GetMouseButtonDown(1);
                if (rightClick)
                {
                    CancelPlacement();
                }
            }
        }

        /// <summary>
        /// Instantiates a test enemy at the first waypoint of the path.
        /// </summary>
        public void SpawnEnemy()
        {
            if (enemyPrefab == null || enemyData == null || waypointPath == null)
            {
                Debug.LogError("[DemoTestController] References missing for enemy spawning!");
                return;
            }

            Transform startWp = waypointPath.GetWaypoint(0);
            Vector3 spawnPos = startWp != null ? startWp.position : Vector3.zero;

            GameObject enemyObj = ObjectPooler.Instance != null 
                ? ObjectPooler.Instance.GetPooledObject(enemyPrefab, spawnPos, Quaternion.identity)
                : Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            EnemyMovement movement = enemyObj.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.Initialize(enemyData, waypointPath);
            }

            EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.Initialize(enemyData);
            }

            EventBus<EnemySpawnedEvent>.Raise(new EnemySpawnedEvent(enemyObj));
            Debug.Log("[DemoTestController] Spawned a test enemy.");
        }

        /// <summary>
        /// Instantiates a preview tower following the cursor in a semi-transparent state.
        /// </summary>
        public void StartTowerPlacement()
        {
            if (towerPrefab == null)
            {
                Debug.LogError("[DemoTestController] Tower prefab is not assigned!");
                return;
            }

            if (_isPlacingTower)
            {
                CancelPlacement();
            }

            _isPlacingTower = true;

            // Spawn preview ghost
            _activeTowerPreview = Instantiate(towerPrefab);
            _activeTowerPreview.name = "Tower_Preview";

            // Disable target-seeking logic while dragging
            TowerController controller = _activeTowerPreview.GetComponent<TowerController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            // Set visual to semi-transparent cyan
            SpriteRenderer sr = _activeTowerPreview.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(0f, 1f, 1f, 0.5f);
            }

            Debug.Log("[DemoTestController] Tower placement preview started. Left-click to place, Right-click to cancel.");
        }

        /// <summary>
        /// Finalizes placement of the tower, enabling target shooting and restoring color.
        /// </summary>
        private void PlaceTower()
        {
            if (_activeTowerPreview == null) return;

            TowerController controller = _activeTowerPreview.GetComponent<TowerController>();
            if (controller != null)
            {
                controller.enabled = true;
            }

            SpriteRenderer sr = _activeTowerPreview.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.cyan; // basic tower cyan color
            }

            _activeTowerPreview.name = $"PlacedTower_{System.Guid.NewGuid().ToString().Substring(0, 4)}";
            _activeTowerPreview = null;
            _isPlacingTower = false;

            Debug.Log("[DemoTestController] Tower placed successfully.");
        }

        /// <summary>
        /// Discards the current tower placement preview.
        /// </summary>
        private void CancelPlacement()
        {
            if (_activeTowerPreview != null)
            {
                Destroy(_activeTowerPreview);
                _activeTowerPreview = null;
            }
            _isPlacingTower = false;
            Debug.Log("[DemoTestController] Tower placement cancelled.");
        }
    }
}
