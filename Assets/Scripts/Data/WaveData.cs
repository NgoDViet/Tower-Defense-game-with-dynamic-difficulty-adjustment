using UnityEngine;

namespace TowerDefense.Data
{
    [CreateAssetMenu(fileName = "NewWaveData", menuName = "Tower Defense/Wave Data", order = 3)]
    public class WaveData : ScriptableObject
    {
        [Header("Enemy Counts")]
        [Tooltip("Number of Basic enemies to spawn in this wave.")]
        [SerializeField] private int basicCount = 5;

        [Tooltip("Number of Fast enemies to spawn in this wave.")]
        [SerializeField] private int fastCount = 0;

        [Tooltip("Number of Tank enemies to spawn in this wave.")]
        [SerializeField] private int tankCount = 0;

        [Tooltip("Number of Armor enemies to spawn in this wave.")]
        [SerializeField] private int armorCount = 0;

        [Header("Spawn Settings")]
        [Tooltip("Time between spawns in seconds for this wave.")]
        [SerializeField] private float spawnInterval = 1.0f;

        // Public Getters
        public int BasicCount => basicCount;
        public int FastCount => fastCount;
        public int TankCount => tankCount;
        public int ArmorCount => armorCount;
        public float SpawnInterval => spawnInterval;
    }
}
