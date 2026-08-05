using UnityEngine;

namespace TowerDefense.Enemy
{
    /// <summary>
    /// Component representing a waypoint path for enemies to follow.
    /// Draws gizmos in the Scene View to visualize the path.
    /// </summary>
    public class WaypointPath : MonoBehaviour
    {
        [Header("Path Settings")]
        [Tooltip("The ordered list of transforms defining the path.")]
        [SerializeField] private Transform[] waypoints;

        /// <summary>
        /// Gets the total number of waypoints.
        /// </summary>
        public int WaypointCount => waypoints != null ? waypoints.Length : 0;

        /// <summary>
        /// Gets the waypoint transform at the specified index.
        /// </summary>
        public Transform GetWaypoint(int index)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Length)
            {
                return null;
            }
            return waypoints[index];
        }

        private void Start()
        {
            SetupLineRenderer();
        }

        private void SetupLineRenderer()
        {
            LineRenderer lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            if (lineRenderer != null && waypoints != null && waypoints.Length > 0)
            {
                lineRenderer.positionCount = waypoints.Length;
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] != null)
                    {
                        lineRenderer.SetPosition(i, waypoints[i].position);
                    }
                }

                lineRenderer.startWidth = 0.15f;
                lineRenderer.endWidth = 0.15f;
                lineRenderer.useWorldSpace = true;
                lineRenderer.loop = false;

                Shader lineShader = Shader.Find("Sprites/Default");
                if (lineShader != null)
                {
                    lineRenderer.material = new Material(lineShader);
                }
                
                Color pathColor = new Color(0f, 1f, 0.5f, 0.4f);
                lineRenderer.startColor = pathColor;
                lineRenderer.endColor = pathColor;
                lineRenderer.sortingOrder = -1; // Render behind characters
            }
        }

        /// <summary>
        /// Context menu utility to automatically assign children Transforms as waypoints.
        /// Right-click component in inspector to run this.
        /// </summary>
        [ContextMenu("Populate From Children")]
        public void PopulateFromChildren()
        {
            int childCount = transform.childCount;
            waypoints = new Transform[childCount];
            for (int i = 0; i < childCount; i++)
            {
                waypoints[i] = transform.GetChild(i);
            }
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[WaypointPath] Populated {childCount} waypoints from children.", this);
#endif
        }

        private void OnDrawGizmos()
        {
            // If the waypoints array is empty but we have children, draw children path in editor as preview
            if (waypoints == null || waypoints.Length == 0)
            {
                if (transform.childCount > 0)
                {
                    Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f); // Cyan helper color
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        Transform child = transform.GetChild(i);
                        if (child == null) continue;

                        Gizmos.DrawSphere(child.position, 0.2f);
                        if (i < transform.childCount - 1)
                        {
                            Transform nextChild = transform.GetChild(i + 1);
                            if (nextChild != null)
                            {
                                Gizmos.DrawLine(child.position, nextChild.position);
                            }
                        }
                    }
                }
                return;
            }

            // Draw configured waypoints path
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;

                // Draw waypoint sphere
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(waypoints[i].position, 0.25f);

                // Draw line and direction to next waypoint
                if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                {
                    Vector3 currentPos = waypoints[i].position;
                    Vector3 nextPos = waypoints[i + 1].position;

                    // Draw connecting line
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(currentPos, nextPos);

                    // Draw direction indicator at midpoint
                    Vector3 direction = (nextPos - currentPos).normalized;
                    Vector3 midPoint = currentPos + (nextPos - currentPos) * 0.5f;

                    Gizmos.color = Color.yellow;
                    Vector3 arrowLeft = Quaternion.Euler(0, 0, 135) * direction * 0.2f;
                    Vector3 arrowRight = Quaternion.Euler(0, 0, -135) * direction * 0.2f;

                    Gizmos.DrawRay(midPoint, arrowLeft);
                    Gizmos.DrawRay(midPoint, arrowRight);
                }
            }
        }
    }
}
