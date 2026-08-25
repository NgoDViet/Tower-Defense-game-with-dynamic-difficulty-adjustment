using UnityEngine;

namespace TowerDefense.Tower
{
    /// <summary>
    /// Compatibility component cho Radial Menu.
    ///
    /// Radial Menu thực tế được tạo và quản lý bởi
    /// TowerPlacementManager.
    /// </summary>
    public class TowerRadialMenu : MonoBehaviour
    {
        // =========================================================
        // SINGLETON
        // =========================================================

        public static TowerRadialMenu Instance
        {
            get;
            private set;
        }

        // =========================================================
        // AWAKE
        // =========================================================

        private void Awake()
        {
            if (Instance != null &&
                Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        // =========================================================
        // OPEN
        // =========================================================

        public void Open(BuildSite site)
        {
            if (site == null)
                return;

            if (TowerPlacementManager.Instance == null)
            {
                Debug.LogWarning(
                    "[TowerRadialMenu] " +
                    "TowerPlacementManager not found."
                );

                return;
            }

            TowerPlacementManager.Instance
                .OpenRadialMenu(site);
        }

        // =========================================================
        // CLOSE
        // =========================================================

        public void Close()
        {
            if (TowerPlacementManager.Instance != null)
            {
                TowerPlacementManager.Instance
                    .CloseRadialMenu();
            }
        }

        // =========================================================
        // DESTROY
        // =========================================================

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}