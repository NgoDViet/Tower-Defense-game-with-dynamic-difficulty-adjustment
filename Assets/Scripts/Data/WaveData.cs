using UnityEngine;
using System.Collections.Generic;

namespace TowerDefense.Data
{
    [System.Serializable]
    public struct EnemySpawnGroup
    {
        [Tooltip("ScriptableObject containing enemy stats.")]
        public EnemyData enemyData;
        
        [Tooltip("Prefab of the enemy corresponding to this data.")]
        public GameObject enemyPrefab;
        
        [Tooltip("How many enemies of this type to spawn in this wave group.")]
        public int count;
        
        [Tooltip("Time between spawns in seconds.")]
        public float spawnInterval;
        
        [Tooltip("Delay in seconds before starting to spawn this group in the wave.")]
        public float delayBeforeGroup;
    }

    [CreateAssetMenu(fileName = "NewWaveData", menuName = "Tower Defense/Wave Data", order = 3)]
    public class WaveData : ScriptableObject
    {
        [SerializeField] private List<EnemySpawnGroup> spawnGroups = new List<EnemySpawnGroup>();

        public List<EnemySpawnGroup> SpawnGroups => spawnGroups;
    }
}
