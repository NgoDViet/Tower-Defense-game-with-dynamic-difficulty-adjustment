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

            // Hide the build site visual when occupied
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = false;
            }
        }

        /// <summary>
        /// Clears the occupancy status of the build site.
        /// </summary>
        public void ClearOccupied()
        {
            isOccupied = false;
            occupyingTower = null;

            // Show the build site visual when empty
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = true;
            }
        }
    }
}
