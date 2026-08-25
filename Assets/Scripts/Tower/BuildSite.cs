using UnityEngine;

namespace TowerDefense.Tower
{
    /// <summary>
    /// Một ô đất có thể xây Tower.
    /// Mỗi BuildSite giữ chính xác Tower đang nằm trên nó.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BuildSite : MonoBehaviour
    {
        // =========================================================
        // STATE
        // =========================================================

        [Header("State")]

        [SerializeField]
        private bool isOccupied = false;

        [SerializeField]
        private GameObject occupyingTower;

        // =========================================================
        // PROPERTIES
        // =========================================================

        public bool IsOccupied
        {
            get
            {
                return isOccupied;
            }
        }

        public GameObject OccupyingTower
        {
            get
            {
                return occupyingTower;
            }
        }

        // =========================================================
        // AWAKE
        // =========================================================

        private void Awake()
        {
            UpdateBuildSiteVisual();
        }

        // =========================================================
        // SET OCCUPIED
        // =========================================================

        public void SetOccupied(
            GameObject tower)
        {
            if (tower == null)
            {
                Debug.LogWarning(
                    "[BuildSite] " +
                    "Cannot occupy site with null tower."
                );

                return;
            }

            isOccupied = true;

            occupyingTower = tower;

            UpdateBuildSiteVisual();

            Debug.Log(
                "[BuildSite] OCCUPIED | " +
                $"Site={gameObject.name} | " +
                $"Tower={tower.name}"
            );
        }

        // =========================================================
        // CLEAR
        // =========================================================

        public void ClearOccupied()
        {
            Debug.Log(
                "[BuildSite] CLEAR | " +
                $"Site={gameObject.name} | " +
                $"Tower=" +
                (occupyingTower != null
                    ? occupyingTower.name
                    : "NULL")
            );

            isOccupied = false;

            occupyingTower = null;

            UpdateBuildSiteVisual();
        }

        // =========================================================
        // VISUAL
        // =========================================================

        private void UpdateBuildSiteVisual()
        {
            SpriteRenderer sr =
                GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                sr.enabled =
                    !isOccupied;
            }
        }

        // =========================================================
        // CLICK
        // =========================================================

        private void OnMouseDown()
        {
            Debug.Log(
                "[BuildSite] Clicked: " +
                gameObject.name
            );

            if (TowerPlacementManager.Instance == null)
            {
                Debug.LogWarning(
                    "[BuildSite] " +
                    "TowerPlacementManager not found."
                );

                return;
            }

            // -----------------------------------------------------
            // Quan trọng:
            // Truyền CHÍNH BuildSite này.
            // -----------------------------------------------------

            TowerPlacementManager.Instance
                .OpenRadialMenu(this);
        }

        // =========================================================
        // DEBUG
        // =========================================================

        private void OnDrawGizmosSelected()
        {
            Collider2D col =
                GetComponent<Collider2D>();

            if (col == null)
                return;

            Gizmos.color =
                Color.yellow;

            Gizmos.DrawWireCube(
                col.bounds.center,
                col.bounds.size
            );
        }
    }
}