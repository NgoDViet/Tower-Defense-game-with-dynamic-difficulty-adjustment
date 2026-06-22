using UnityEngine;
using System.Collections.Generic;

namespace TowerDefense.Pooling
{
    /// <summary>
    /// A generic, high-performance Object Pooling system.
    /// Manages spawning and recycling of GameObjects using prefab references as keys.
    /// </summary>
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        [System.Serializable]
        public struct PoolPrewarmConfig
        {
            [Tooltip("Prefab to pre-populate in the pool.")]
            public GameObject prefab;
            [Tooltip("Initial number of deactivated instances to create at start.")]
            public int size;
        }

        [Header("Pre-warm Settings")]
        [SerializeField] private List<PoolPrewarmConfig> prewarmConfigs = new List<PoolPrewarmConfig>();

        // Tracks available inactive objects for each prefab
        private readonly Dictionary<GameObject, Queue<GameObject>> _poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
        
        // Tracks active spawned objects back to their source prefab
        private readonly Dictionary<GameObject, GameObject> _activeObjectsMap = new Dictionary<GameObject, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            Prewarm();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            _poolDictionary.Clear();
            _activeObjectsMap.Clear();
        }

        /// <summary>
        /// Populates the pools with predefined sizes at start to avoid runtime instantiation overhead.
        /// </summary>
        private void Prewarm()
        {
            foreach (var config in prewarmConfigs)
            {
                if (config.prefab == null) continue;
                PrewarmPrefab(config.prefab, config.size);
            }
        }

        /// <summary>
        /// Pre-populates the pool for a specific prefab with a given count.
        /// </summary>
        public void PrewarmPrefab(GameObject prefab, int count)
        {
            if (prefab == null) return;

            if (!_poolDictionary.ContainsKey(prefab))
            {
                _poolDictionary[prefab] = new Queue<GameObject>();
            }

            Queue<GameObject> queue = _poolDictionary[prefab];
            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(prefab, transform);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }
            Debug.Log($"[ObjectPooler] Pre-warmed pool for {prefab.name} with {count} instances.");
        }

        /// <summary>
        /// Spawns an object from the pool. Instantiates a new one if the pool is empty.
        /// </summary>
        public GameObject GetPooledObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("[ObjectPooler] Attempted to spawn a null prefab!");
                return null;
            }

            if (!_poolDictionary.ContainsKey(prefab))
            {
                _poolDictionary[prefab] = new Queue<GameObject>();
            }

            GameObject obj = null;
            Queue<GameObject> queue = _poolDictionary[prefab];

            // Dequeue a valid object (checking for external destruction)
            while (queue.Count > 0)
            {
                GameObject candidate = queue.Dequeue();
                if (candidate != null)
                {
                    obj = candidate;
                    break;
                }
            }

            // Pool is empty, instantiate a new one
            if (obj == null)
            {
                obj = Instantiate(prefab);
            }

            // Set transform parameters
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            if (parent != null)
            {
                obj.transform.SetParent(parent);
            }
            else
            {
                // Re-parent to null if it was inside the pooler container
                if (obj.transform.parent == transform)
                {
                    obj.transform.SetParent(null);
                }
            }

            // Track instance to allow automatic recycling
            _activeObjectsMap[obj] = prefab;

            obj.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Returns an active pooled object back to its pool and deactivates it.
        /// </summary>
        public void ReturnToPool(GameObject obj)
        {
            if (obj == null) return;

            if (_activeObjectsMap.TryGetValue(obj, out GameObject prefab))
            {
                _activeObjectsMap.Remove(obj);
                
                obj.SetActive(false);
                obj.transform.SetParent(transform); // Parent to pool container to keep hierarchy tidy

                if (!_poolDictionary.ContainsKey(prefab))
                {
                    _poolDictionary[prefab] = new Queue<GameObject>();
                }
                
                _poolDictionary[prefab].Enqueue(obj);
            }
            else
            {
                // Fallback: If not registered in active map, destroy to avoid leaks
                Debug.LogWarning($"[ObjectPooler] ReturnToPool: {obj.name} was not spawned from ObjectPooler or is already returned. Deactivating and destroying.", obj);
                obj.SetActive(false);
                Destroy(obj);
            }
        }
    }
}
