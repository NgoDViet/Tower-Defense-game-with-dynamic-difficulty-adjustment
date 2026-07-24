using UnityEngine;

namespace TowerDefense.Tower
{
    /// <summary>
    /// Represents a valid building site on the map where defensive towers can be placed.
    /// Tracks occupancy and provides helper references.
    /// </summary>
    public class BuildSite : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool isOccupied = false;
        [SerializeField] private GameObject occupyingTower;

        public bool IsOccupied => isOccupied;
        public GameObject OccupyingTower => occupyingTower;

        /// <summary>
        /// Marks the build site as occupied by the specified tower.
        /// </summary>
        public void SetOccupied(GameObject tower)
        {
            isOccupied = true;
            occupyingTower = tower;
        }

        /// <summary>
        /// Clears the occupancy status of the build site.
        /// </summary>
        public void ClearOccupied()
        {
            isOccupied = false;
            occupyingTower = null;
        }
    }
}
